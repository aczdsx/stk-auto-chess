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
            int powerPercent = Mathf.RoundToInt(spec.base_rate * 100f);

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
                    return SimSkillArchetype.Custom;
            }

            // 플레이어 스킬 아키타입 매핑
            switch (id)
            {
                case 215532401: return SimSkillArchetype.Custom;          // 필리아
                case 215362202: return SimSkillArchetype.DamageCC;        // 시이나 (침묵)
                case 217433303: return SimSkillArchetype.DamageCC;        // 하티 (넉백)
                case 217323201: return SimSkillArchetype.Custom;          // 미사 (봉인+기절)
                case 217243102: return SimSkillArchetype.DiamondAoE;       // 블린
                case 217513401: return SimSkillArchetype.LineDamage;      // 아트레시아
                case 1406031:   return SimSkillArchetype.Heal;            // 아란
                case 215322201: return SimSkillArchetype.PatternDamage;   // 메이
                case 217523403: return SimSkillArchetype.Custom;          // 아드리아
                case 217663506: return SimSkillArchetype.MultiHit;        // 시라유키
                case 215642501: return SimSkillArchetype.AoEDamage;       // 엘리스
                case 217353203: return SimSkillArchetype.AoEDamage;       // 라키유
                case 217263103: return SimSkillArchetype.Custom;          // 루키다
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

        private static void ApplySkillSpecificParams(ref SkillParams p, int id,
            List<SkillActive> specList, int tickRate)
        {
            switch (id)
            {
                case 215362202: // 시이나: 침묵
                    p.CCType = CrowdControlType.Silence;
                    p.CCDurationFrames = 90;
                    break;
                case 217433303: // 하티: 가장 먼 적 + 넉백 2타일
                    p.TargetType = SkillTargetType.FarthestEnemy;
                    p.CCType = CrowdControlType.Knockback;
                    p.CCDurationFrames = 2;
                    break;
                case 217243102: // 블린: 5×5 다이아몬드 AoE
                    p.Param0 = 2; // 맨해튼 거리 2 (5×5 다이아몬드)
                    break;
                case 215532401: // 필리아: 가장 먼 적 단일강타
                    p.TargetType = SkillTargetType.FarthestEnemy;
                    break;
                case 215252102: // 유니: 체력 최저 아군 3명 힐 + 디버프 제거
                {
                    // {0}=쿨타임(초), {1}=힐배율(%) → PowerPercent로 자동 반영, {2}=디버프 제거 수
                    p.TargetCount = 3;
                    p.Param0 = Mathf.RoundToInt(GetSpecRate(specList, 2, 2f)); // 디버프 제거 수
                    break;
                }
                case 217433302: // 미노: 3발 + 스플래시
                    p.TargetCount = 3;
                    break;
                case 217363204: // 베인: 5회 바운스 + 공속 버프
                    p.TargetCount = 5;
                    p.SecondaryPowerPercent = 20;
                    p.BuffStat = StatModType.AttackSpeed;
                    p.BuffValue = 30;
                    p.BuffDurationFrames = 180;
                    break;
                case 217413301: // 테토라: 넉백 + AoE 스턴
                {
                    // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=마방계수(미사용),
                    // {3}=후속데미지배율(%)
                    p.Param0 = 4;  // 넉백 거리 (고정)
                    p.Param1 = 1;  // AoE 범위 (고정)
                    p.CCType = CrowdControlType.Stun;
                    p.CCDurationFrames = SecondsToFrames(1f, tickRate); // 넉백 스턴 1초 고정
                    p.SecondaryPowerPercent = Mathf.RoundToInt(GetSpecRate(specList, 3, 200f));
                    break;
                }
                case 217553404: // 클레이: 채널링 존 (3초, 6틱)
                {
                    // {0}=쿨타임(초), {1}=힐배율(%) → PowerPercent로 자동 반영,
                    // {2}=데미지배율(%), {3}=회복감소(%), {4}=디버프지속(초)
                    p.Param0 = Mathf.RoundToInt(GetSpecRate(specList, 2, 80f));  // damagePercent
                    p.Param1 = Mathf.RoundToInt(GetSpecRate(specList, 3, 50f));  // healReductionPercent
                    float debuffDurSec = GetSpecRate(specList, 4, 3f);
                    p.Param2 = SecondsToFrames(debuffDurSec, tickRate);          // debuffDurationFrames
                    p.Param3 = 2;                                                // zoneRange (고정)
                    break;
                }
                case 217563405: // 마리에: 공격력 최대 적 뒤 순간이동 + 다단히트
                {
                    // {0}=쿨타임(초), {1}=히트수, {2}=데미지배율(%) → PowerPercent 자동 반영,
                    // {3}=디버프지속(초), {4}=디버프율(%)
                    p.TargetType = SkillTargetType.HighestAttackEnemy;
                    int hitCount = Mathf.RoundToInt(GetSpecRate(specList, 1, 4f));
                    p.HitCount = hitCount > 0 ? hitCount : 4;
                    float debuffDurSec = GetSpecRate(specList, 3, 3f);
                    p.Param0 = SecondsToFrames(debuffDurSec, tickRate); // debuffDurationFrames
                    p.Param1 = Mathf.RoundToInt(GetSpecRate(specList, 4, 30f)); // debuffPercent
                    break;
                }
                case 215422301: // 멘샤: 실드
                    p.Param0 = 180; // shieldDurationFrames
                    break;
                case 217653505: // 엔키: 전체 힐 + HoT
                    p.Param0 = 180; // HoT 지속 프레임
                    p.Param1 = 30;  // HoT 틱 간격
                    p.SecondaryPowerPercent = 50; // HoT 틱당 배율
                    break;
                case 217333202: // 에이프릴: 채널링 다단히트 (AnimEvent 기반)
                    p.HitCount = 10;     // 총 타수
                    p.Param0 = 100;      // 근거리 배율 (1~2행)
                    p.Param1 = 75;       // 중거리 배율 (3행)
                    p.Param2 = 50;       // 원거리 배율 (4+행)
                    break;
                case 217513401: // 아트레시아: 3칸 폭 직선 관통
                    p.Param2 = 3;  // width (진행 방향 수직 3칸)
                    break;
                case 217613501: // 오데트: 2단계 채널링 (L자형 + 3×3 범위공격 + 순간이동)
                    p.Param0 = 90;       // 공속감소 디버프 지속 프레임 (3초 @ 30fps)
                    p.Param1 = 30;       // 공속 감소량
                    break;
                case 217523403: // 아드리아: 3단계 확장 패턴 AoE + 방어력 비례 데미지 + 스턴
                {
                    // {0}=쿨타임(초), {1}=데미지배율(%) → PowerPercent 자동 반영,
                    // {2}=방어력계수, {3}=스턴시간(초)
                    p.Param0 = Mathf.RoundToInt(GetSpecRate(specList, 2, 100f)); // defScaleValue
                    float stunSec = GetSpecRate(specList, 3, 2f);
                    p.Param1 = SecondsToFrames(stunSec, tickRate); // stunDurationFrames
                    break;
                }
                case 217323201: // 미사: 봉인(관) + 스턴 — 데미지 없음
                {
                    // {0}=쿨타임(초), {1}=봉인지속(초)
                    p.TargetType = SkillTargetType.HighestAttackEnemy;
                    p.CCType = CrowdControlType.Stun;
                    float sealDurSec = GetSpecRate(specList, 1, 3f);
                    p.CCDurationFrames = SecondsToFrames(sealDurSec, tickRate);
                    p.PowerPercent = 0; // 데미지 없음
                    break;
                }
                case 217263103: // 루키다: 여우불 추가 + 공속 버프 (스펙 데이터 기반)
                {
                    // {0}=쿨타임(초), {1}=여우불 증가량, {2}=공속버프 지속(초), {3}=공속증가율(%)
                    int foxFireIncrease = Mathf.RoundToInt(GetSpecRate(specList, 1, 2f));
                    float buffDurationSec = GetSpecRate(specList, 2, 3f);
                    int atkSpeedPercent = Mathf.RoundToInt(GetSpecRate(specList, 3, 10f));
                    p.Param0 = foxFireIncrease;
                    p.Param1 = SecondsToFrames(buffDurationSec, tickRate);
                    p.Param2 = atkSpeedPercent;
                    break;
                }
            }
        }
    }
}
