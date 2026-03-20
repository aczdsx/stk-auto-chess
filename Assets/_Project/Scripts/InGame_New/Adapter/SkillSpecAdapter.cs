using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 -> SkillParams 변환 어댑터.
    /// 스킬 ID별 아키타입 매핑과 파라미터 추출을 담당.
    /// </summary>
    public static class SkillSpecAdapter
    {
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

            ApplySkillSpecificParams(ref p, spec.skill_group_id, specList, tickRate);

            // SKL 클립에서 Execute 이벤트 타이밍 자동 추출
            ExtractSkillHitTimes(ref p, spec.prefab_id, tickRate);

            return p;
        }

        /// <summary>스킬 ID로 아키타입 분류</summary>
        public static SimSkillArchetype ClassifySkill(SkillActive spec)
        {
            int id = spec.skill_group_id;

            // 커스텀 플레이어 스킬
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
                    return SimSkillArchetype.Custom;
            }

            // 플레이어 스킬 아키타입 매핑
            switch (id)
            {
                case 215362202: return SimSkillArchetype.DamageCC;        // 시이나 (침묵)
                case 217243102: return SimSkillArchetype.DiamondAoE;       // 블린
                case 217513401: return SimSkillArchetype.LineDamage;      // 아트레시아
                case 1406031:   return SimSkillArchetype.Heal;            // 아란
                case 217353203: return SimSkillArchetype.Custom;          // 라키유
            }

            // 몬스터 스킬 분류
            if (IsMonsterSkill(id))
                return ClassifyMonsterSkill(id);

            return SimSkillArchetype.SingleDamage;
        }

        /// <summary>아키타입 -> SimSkillBase 인스턴스</summary>
        public static SimSkillBase CreateFromArchetype(SimSkillArchetype archetype)
        {
            switch (archetype)
            {
                case SimSkillArchetype.SingleDamage: return new SimSkillSingleDamage();
                case SimSkillArchetype.AoEDamage: return new SimSkillAoEDamage();
                case SimSkillArchetype.LineDamage: return new SimSkillLineDamage();
                case SimSkillArchetype.DamageCC: return new SimSkillDamageCC();
                case SimSkillArchetype.ConeDamage: return new SimSkillConeDamage();
                case SimSkillArchetype.PatternDamage: return new SimSkillPatternDamage();
                case SimSkillArchetype.MultiHit: return new SimSkillMultiHit();
                case SimSkillArchetype.Heal: return new SimSkillHeal();
                case SimSkillArchetype.MultiTargetHeal: return new SimSkillMultiTargetHeal();
                case SimSkillArchetype.TeleportStrike: return new SimSkillTeleportStrike();
                case SimSkillArchetype.Buff: return new SimSkillBuff();
                case SimSkillArchetype.Debuff: return new SimSkillDebuff();
                case SimSkillArchetype.Stun: return new SimSkillStun();
                case SimSkillArchetype.DiamondAoE: return new SimSkillDiamondAoE();
                default: return null;
            }
        }

        private static bool IsMonsterSkill(int id)
        {
            return id >= 1100000 || (id >= 20000 && id <= 40000);
        }

        private static SimSkillArchetype ClassifyMonsterSkill(int id)
        {
            switch (id)
            {
                // CC 스킬
                case 1102061: case 230404002: case 230505002: case 230606002:
                case 240107001: case 240407301: case 250208101:
                    return SimSkillArchetype.DamageCC;

                // 콘 데미지
                case 230101002: case 230404001: case 230505001: case 230606001:
                case 280109001:
                    return SimSkillArchetype.ConeDamage;

                // 패턴 AoE
                case 1103041: case 1203021: case 230505003: case 250608501:
                case 280109002:
                    return SimSkillArchetype.PatternDamage;

                // 관통 프로젝타일
                case 1104081: case 230404004: case 230505004: case 230606004:
                case 240107002:
                    return SimSkillArchetype.LineDamage;

                // 텔레포트
                case 1202091: case 240407302: case 250108002: case 250108003:
                    return SimSkillArchetype.TeleportStrike;

                // 멀티히트
                case 1105031: case 230404005: case 230505005:
                    return SimSkillArchetype.MultiHit;

                // 힐
                case 1106041: case 230404006: case 230505006: case 230606006:
                    return SimSkillArchetype.MultiTargetHeal;
            }

            return SimSkillArchetype.SingleDamage;
        }

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

        /// <summary>
        /// 아키타입 기반 스킬의 스펙 특수 파라미터 적용.
        /// 커스텀 스킬은 각 클래스의 InitializeFromSpec에서 직접 처리.
        /// </summary>
        private static void ApplySkillSpecificParams(ref SkillParams p, int id,
            List<SkillActive> specList, int tickRate)
        {
            switch (id)
            {
                case 215362202: // 시이나: 침묵 (DamageCC 아키타입)
                    p.CCType = CrowdControlType.Silence;
                    p.CCDurationFrames = SecondsToFrames(GetSpecRate(specList, 2, 3f), tickRate);
                    break;
                case 217243102: // 블린: 5×5 다이아몬드 AoE (DiamondAoE 아키타입)
                    p.Param0 = 2;
                    break;
                case 217513401: // 아트레시아: 3칸 폭 직선 관통 (LineDamage 아키타입)
                    p.Param2 = 3;
                    break;
            }
        }
    }
}
