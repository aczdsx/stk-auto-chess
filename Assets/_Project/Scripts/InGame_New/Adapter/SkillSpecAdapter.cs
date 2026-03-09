using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 -> SkillParams 변환 어댑터.
    /// 스킬 ID별 아키타입 매핑과 파라미터 추출을 담당.
    /// </summary>
    public static class SkillSpecAdapter
    {
        /// <summary>SkillActive 스펙에서 SkillParams 생성</summary>
        public static SkillParams BuildParams(SkillActive spec)
        {
            var archetype = ClassifySkill(spec);
            var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;
            int powerPercent = Mathf.RoundToInt(spec.base_rate * 100f);

            var p = new SkillParams
            {
                SkillId = spec.id,
                PowerPercent = powerPercent > 0 ? powerPercent : 200,
                DamageType = dmgType,
                CastFrames = 0,
                TargetCount = 1,
                HitCount = 1,
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

            ApplySkillSpecificParams(ref p, spec.id);
            return p;
        }

        /// <summary>스킬 ID로 아키타입 분류</summary>
        public static SimSkillArchetype ClassifySkill(SkillActive spec)
        {
            int id = spec.id;

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
                    return SimSkillArchetype.Custom;
            }

            // 플레이어 스킬 아키타입 매핑
            switch (id)
            {
                case 215532401: return SimSkillArchetype.SingleDamage;    // 필리아
                case 215362202: return SimSkillArchetype.DamageCC;        // 시이나 (침묵)
                case 217433303: return SimSkillArchetype.DamageCC;        // 하티 (넉백)
                case 217323201: return SimSkillArchetype.DamageCC;        // 미사 (기절)
                case 217243102: return SimSkillArchetype.AoEDamage;       // 블린
                case 217513401: return SimSkillArchetype.LineDamage;      // 아트레시아
                case 1406031:   return SimSkillArchetype.Heal;            // 아란
                case 215322201: return SimSkillArchetype.PatternDamage;   // 메이
                case 217523403: return SimSkillArchetype.PatternDamage;   // 아드리아
                case 217613501: return SimSkillArchetype.PatternDamage;   // 오데트
                case 217663506: return SimSkillArchetype.MultiHit;        // 시라유키
                case 215642501: return SimSkillArchetype.AoEDamage;       // 엘리스
                case 217353203: return SimSkillArchetype.AoEDamage;       // 라키유
                case 217263103: return SimSkillArchetype.Buff;            // 루키다
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

        private static void ApplySkillSpecificParams(ref SkillParams p, int id)
        {
            switch (id)
            {
                case 215362202: // 시이나: 침묵
                    p.CCType = CrowdControlType.Silence;
                    p.CCDurationFrames = 90;
                    break;
                case 217433303: // 하티: 넉백 2타일
                    p.CCType = CrowdControlType.Knockback;
                    p.CCDurationFrames = 2;
                    break;
                case 215252102: // 유니: 3명 힐 + 디버프 2개 제거
                    p.TargetCount = 3;
                    p.Param0 = 2;
                    break;
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
                case 217413301: // 테토라: 넉백 4타일 + AoE 스턴
                    p.Param0 = 4;
                    p.Param1 = 1;
                    p.CCType = CrowdControlType.Stun;
                    p.CCDurationFrames = 60;
                    break;
                case 217553404: // 클레이: 채널링 존 (3초, 6틱)
                    p.Param0 = 100; // healPercent (총량, 틱당 /6)
                    p.Param1 = 80;  // damagePercent (총량, 틱당 /6)
                    p.Param2 = 50;  // healReductionPercent
                    p.Param3 = 2;   // zoneRange (맨해튼 거리 2)
                    break;
                case 217563405: // 마리에: 다단히트
                    p.HitCount = 4;
                    break;
                case 215422301: // 멘샤: 실드
                    p.Param0 = 180; // shieldDurationFrames
                    break;
                case 217653505: // 엔키: 전체 힐 + HoT
                    p.Param0 = 180; // HoT 지속 프레임
                    p.Param1 = 30;  // HoT 틱 간격
                    p.SecondaryPowerPercent = 50; // HoT 틱당 배율
                    break;
                case 217333202: // 에이프릴: 채널링 다단히트
                    p.HitCount = 10;     // 총 타수
                    p.Param0 = 100;      // 근거리 배율 (1~2행)
                    p.Param1 = 75;       // 중거리 배율 (3행)
                    p.Param2 = 50;       // 원거리 배율 (4+행)
                    p.CastFrames = 90;   // 총 채널링 프레임 (3초 @ 30fps)
                    break;
            }
        }
    }
}
