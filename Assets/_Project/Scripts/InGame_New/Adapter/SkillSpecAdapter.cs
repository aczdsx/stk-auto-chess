using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 -> SkillParams 변환 어댑터.
    /// 스킬 ID별 아키타입 매핑과 파라미터 추출을 담당.
    ///
    /// 플로우:
    /// 1. ClassifySkill() — 스킬 ID → 아키타입 분류
    /// 2. BuildParams()   — 아키타입 기본값 세팅 + 스펙 데이터에서 파라미터 추출
    /// 3. ApplySkillSpecificParams() — 개별 스킬의 특수 파라미터 오버라이드
    /// 4. ExtractSkillHitTimes() — SKL 애니메이션 키프레임 추출
    /// </summary>
    public static class SkillSpecAdapter
    {
        // ──────────────────────────────────────────────
        // 1. BuildParams — 스펙 → SkillParams 변환
        // ──────────────────────────────────────────────

        /// <summary>SkillActive 스펙에서 SkillParams 생성 (단일 스펙)</summary>
        public static SkillParams BuildParams(SkillActive spec, int tickRate)
        {
            return BuildParams(spec, null, tickRate);
        }

        /// <summary>SkillActive 스펙에서 SkillParams 생성 (전체 스펙 리스트 — 다중 base_rate 지원)</summary>
        public static SkillParams BuildParams(SkillActive spec, List<SkillActive> specList, int tickRate)
        {
            var archetype = ClassifySkill(spec);
            var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;

            // specList[0]은 쿨타임 — PERCENT row를 찾아 데미지 배율로 사용
            int powerPercent = 0;
            if (specList != null)
            {
                for (int i = 1; i < specList.Count; i++)
                {
                    if (specList[i].skill_value_type == SkillValueType.PERCENT)
                    {
                        powerPercent = Mathf.RoundToInt(specList[i].base_rate);
                        break;
                    }
                }
            }

            var p = new SkillParams
            {
                SkillId = spec.skill_group_id,
                PowerPercent = powerPercent > 0 ? powerPercent : 200,
                DamageType = dmgType,
                CastFrames = 0,
                TargetCount = 1,
                HitCount = 1,
                TargetType = SkillTargetType.NearestEnemy,
                WorldTickRate = tickRate,
                CooldownSeconds = specList != null && specList.Count > 0
                    ? specList[0].base_rate : 0f,
            };

            // 아키타입별 기본값
            switch (archetype)
            {
                case SimSkillArchetype.DamageCC:
                    p.CCType = CrowdControlType.Stun;
                    p.CCDurationFrames = 60;
                    break;
                case SimSkillArchetype.ConeDamage:
                    p.Param0 = 2;
                    break;
                case SimSkillArchetype.PatternDamage:
                    p.Param1 = 1;
                    break;
                case SimSkillArchetype.MultiHit:
                    p.HitCount = 3;
                    break;
                case SimSkillArchetype.MultiTargetHeal:
                    p.TargetCount = 3;
                    break;
                case SimSkillArchetype.TeleportStrike:
                    p.Param0 = 1;
                    p.CCType = CrowdControlType.Stun;
                    p.CCDurationFrames = 60;
                    p.CastFrames = 30;
                    break;
                case SimSkillArchetype.AoEDamage:
                    p.Param0 = 1;
                    break;
            }

            // 개별 스킬 특수 파라미터 오버라이드
            ApplySkillSpecificParams(ref p, spec.skill_group_id, specList, tickRate);

            // SKL 클립에서 Execute 이벤트 타이밍 자동 추출
            ExtractSkillHitTimes(ref p, spec.prefab_id, tickRate);

            return p;
        }

        // ──────────────────────────────────────────────
        // 2. ClassifySkill — 스킬 ID → 아키타입 분류
        // ──────────────────────────────────────────────

        /// <summary>스킬 ID로 아키타입 분류</summary>
        public static SimSkillArchetype ClassifySkill(SkillActive spec)
        {
            int id = spec.skill_group_id;

            // ── 커스텀 플레이어 스킬 ──
            switch (id)
            {
                case 215252102: // 유니
                case 217433302: // 미노
                case 217363204: // 베인
                case 217413301: // 테토라
                case 215422301: // 멘샤
                case 217553404: // 클레이
                case 217563405: // 마리에
                case 217653505: // 엔키
                case 217333202: // 에이프릴
                case 217613501: // 오데트
                case 217523403: // 아드리아
                case 217663506: // 시라유키
                case 215532401: // 필리아
                case 217433303: // 하티
                case 217323201: // 미사
                case 217263103: // 루키다
                case 215642501: // 엘리스
                case 215322201: // 메이
                case 217353203: // 라키유
                    return SimSkillArchetype.Custom;
            }

            // ── 아키타입 기반 플레이어 스킬 ──
            switch (id)
            {
                case 215362202: return SimSkillArchetype.DamageCC;    // 시이나 (침묵)
                case 217243102: return SimSkillArchetype.DiamondAoE;  // 블린
                case 1406031:   return SimSkillArchetype.Heal;        // 아란
            }

            // ── 몬스터 스킬 ──
            if (IsMonsterSkill(id))
                return ClassifyMonsterSkill(id);

            return SimSkillArchetype.SingleDamage;
        }

        /// <summary>
        /// 아키타입 -> SimSkillBase 인스턴스.
        /// 모든 아키타입이 Recipe로 전환되었으므로 현재 항상 null 반환.
        /// SkillFactory에서 Recipe → SimSkillGeneric 경로를 우선 사용.
        /// </summary>
        public static SimSkillBase CreateFromArchetype(SimSkillArchetype archetype)
        {
            return null;
        }

        // ──────────────────────────────────────────────
        // 3. 몬스터 스킬 분류
        // ──────────────────────────────────────────────

        private static bool IsMonsterSkill(int id)
        {
            return id >= 1100000 || (id >= 20000 && id <= 40000);
        }

        private static SimSkillArchetype ClassifyMonsterSkill(int id)
        {
            switch (id)
            {
                // ── 커스텀 (SkillFactory에서 개별 등록) ──
                case 250108001:                     // 0챕터 보스 탱커 — 전방 직선 순차 타격 + 넉백
                case 230101005: case 230202004:     // 0/1챕터 저격수 — 단일 투사체
                    return SimSkillArchetype.Custom;

                // ── DamageCC (데미지 + 스턴) ──
                case 1102061:                       // 5챕터 탱커
                case 230404002: case 230505002: case 230606002:  // 4/5/2챕터 탱커
                case 240107001:                     // 베놈
                case 240407301:                     // 사막 전갈
                case 250208101:                     // 1챕터 보스
                    return SimSkillArchetype.DamageCC;

                // ── ConeDamage (전방 부채꼴) ──
                case 230101002:                     // 0챕터 가디언
                case 230404001: case 230505001: case 230606001:  // 4/5/3챕터 가디언
                case 280109001:                     // 공허의 토마
                    return SimSkillArchetype.ConeDamage;

                // ── DiamondAoE (맨허튼 거리 범위) ──
                case 230202003: case 230606003:     // 1/2챕터 마법사 — 십자가 범위 (Param0=1)
                    return SimSkillArchetype.DiamondAoE;

                // ── PatternDamage (다중 패턴 AoE) ──
                case 1103041:                       // 6챕터 마법사
                case 1203021:                       // 5챕터 독 두꺼비
                case 230505003:                     // 5챕터 마법사
                case 250608501:                     // 2챕터 보스
                case 280109002:                     // 라플라스마녀
                    return SimSkillArchetype.PatternDamage;

                // ── LineDamage (직선 관통) ──
                case 1104081:                       // 6챕터 저격수
                case 230404004: case 230505004: case 230606004:  // 4/5/3챕터 저격수
                case 240107002:                     // 빅마우스
                    return SimSkillArchetype.LineDamage;

                // ── TeleportStrike (이동 후 공격) ──
                case 1202091:                       // 6챕터 버팔로
                case 240407302:                     // 샌드웜
                case 250108002: case 250108003:     // 정글 버팔로 / 샌드웜
                    return SimSkillArchetype.TeleportStrike;

                // ── MultiHit (다단히트) ──
                case 1105031:                       // 6챕터 암살자
                case 230404005: case 230505005:     // 4/5챕터 암살자
                    return SimSkillArchetype.MultiHit;

                // ── MultiTargetHeal (다대상 힐) ──
                case 1106041:                       // 6챕터 서포터
                case 230404006: case 230505006: case 230606006:  // 4/5/3챕터 서포터
                    return SimSkillArchetype.MultiTargetHeal;
            }

            // 매핑 없는 몬스터 → 기본 단일 데미지
            return SimSkillArchetype.SingleDamage;
        }

        // ──────────────────────────────────────────────
        // 4. 개별 스킬 특수 파라미터 오버라이드
        // ──────────────────────────────────────────────

        /// <summary>
        /// 아키타입 기반 스킬의 스펙 특수 파라미터 적용.
        /// 커스텀 스킬은 각 클래스의 InitializeFromSpec에서 직접 처리.
        /// </summary>
        private static void ApplySkillSpecificParams(ref SkillParams p, int id,
            List<SkillActive> specList, int tickRate)
        {
            switch (id)
            {
                // ── 플레이어 스킬 ──
                case 215362202: // 시이나: 침묵 (DamageCC 아키타입, CC타입 오버라이드)
                    p.CCType = CrowdControlType.Silence;
                    p.CCDurationFrames = SecondsToFrames(GetSpecRate(specList, 2, 3f), tickRate);
                    break;
                case 217243102: // 블린: 5×5 다이아몬드 AoE (범위 2)
                    p.Param0 = 2;
                    break;
                // ── 몬스터 스킬 ──
                case 230202003: // 1챕터 마법사: 십자가 범위 (맨허튼 1)
                case 230606003: // 2챕터 마법사: 십자가 범위 (맨허튼 1)
                    p.Param0 = 1;
                    break;
            }
        }

        // ──────────────────────────────────────────────
        // 5. SKL 애니메이션 키프레임 추출
        // ──────────────────────────────────────────────

        /// <summary>SKL 클립에서 Execute 이벤트 타이밍을 SkillParams에 추출 (프레임 단위로 변환)</summary>
        private static void ExtractSkillHitTimes(ref SkillParams p, int prefabId, int tickRate)
        {
            if (prefabId <= 0) return;

            // Back_SKL 기준 (시뮬레이션은 방향 무관, Back 사용)
            int sklKey = AnimKeyframeData.MakeKey(prefabId, false, AnimClipType.SKL);

            if (AnimKeyframeData.ClipEvents.TryGetValue(sklKey, out var events))
            {
                // Execute 이벤트만 필터링
                int count = 0;
                for (int i = 0; i < events.Length; i++)
                    if (events[i].key >= AnimationEventKey.Execute1Per1 &&
                        events[i].key <= AnimationEventKey.Execute1Per12)
                        count++;

                if (count > 0)
                {
                    p.SkillHitFrames = new int[count];
                    int idx = 0;
                    for (int i = 0; i < events.Length; i++)
                        if (events[i].key >= AnimationEventKey.Execute1Per1 &&
                            events[i].key <= AnimationEventKey.Execute1Per12)
                            p.SkillHitFrames[idx++] = SecondsToFrames(events[i].time, tickRate);
                }
            }

            if (AnimKeyframeData.ClipLengths.TryGetValue(sklKey, out float len))
                p.SkillClipFrames = SecondsToFrames(len, tickRate);
        }

        // ──────────────────────────────────────────────
        // 유틸리티
        // ──────────────────────────────────────────────

        private static int SecondsToFrames(float seconds, int tickRate)
        {
            return (int)(seconds * tickRate + 0.5f);
        }

        private static float GetSpecRate(List<SkillActive> specList, int index, float fallback = 0f)
        {
            if (specList != null && index < specList.Count)
                return specList[index].base_rate;
            return fallback;
        }
    }
}
