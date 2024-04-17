using CookApps.Obfuscator;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface ICharacterStatData
    {
        EffectCodeInheritFlag DirtyFlags { get; }
        void RemoveDirtyFlag(EffectCodeInheritFlag flag);

        ObfuscatorInt CharacterId { get; }

        /// <summary>
        /// 체력
        /// </summary>
        ObfuscatorDouble HP { get; }
        /// <summary>
        /// 물리공격력
        /// </summary>
        ObfuscatorDouble AD { get; }
        /// <summary>
        /// 마법 공격력
        /// </summary>
        ObfuscatorDouble AP { get; }
        /// <summary>
        /// 방어력
        /// </summary>
        ObfuscatorDouble DEF { get; }
        /// <summary>
        /// 마법 저항력
        /// </summary>
        ObfuscatorDouble RES { get; }
        /// <summary>
        /// 방어구 관통력
        /// </summary>
        ObfuscatorDouble DEFPenetration { get; }
        /// <summary>
        /// 마법 관통력
        /// </summary>
        ObfuscatorDouble RESPenetration { get; }

        /// <summary>
        /// 체력회복량
        /// </summary>
        ObfuscatorDouble HPRecovery { get; }
        /// <summary>
        /// 크리티컬 확률
        /// </summary>
        ObfuscatorFloat CriticalProb { get; }
        /// <summary>
        /// 크리티컬 대미지 배율
        /// </summary>
        ObfuscatorFloat CriticalDamageRate { get; }
        /// <summary>
        /// 더블 크리티컬 확률
        /// </summary>
        ObfuscatorFloat DoubleCriticalProb { get; }
        /// <summary>
        /// 더블 크리티컬 대미지 배율
        /// </summary>
        ObfuscatorFloat DoubleCriticalDamageRate { get; }
        /// <summary>
        /// 이동속도
        /// </summary>
        ObfuscatorFloat MoveSpeed { get; }
        /// <summary>
        /// 공격속도
        /// </summary>
        ObfuscatorFloat AttackSpeed { get; }
        /// <summary>
        /// 공격 범위
        /// </summary>
        ObfuscatorFloat AttackRange { get; }

        /// <summary>
        /// 스킬 대미지 배율
        /// </summary>
        ObfuscatorFloat SkillDamageRate { get; }
        /// <summary>
        /// 스킬 쿨타임 감소 배율
        /// </summary>
        ObfuscatorFloat SkillCooltimeRate { get; }

        /// <summary>
        /// 주는 피해 배율
        /// </summary>
        ObfuscatorFloat AttackDamageRate { get; }
        /// <summary>
        /// 받는 피해 배율
        /// </summary>
        ObfuscatorFloat TakenDamageRate { get; }

        /// <summary>
        /// 주는 힐량 배율
        /// </summary>
        ObfuscatorFloat GivenHealRate { get; }

        /// <summary>
        /// 받는 힐량 배율
        /// </summary>
        ObfuscatorFloat TakenHealRate { get; }

        /// <summary>
        /// 공격 타입 (근거리, 원거리)
        /// </summary>
        AttackType AttackType { get; }

        /// <summary>
        /// 기본 공격 검색 타입 (가까운 적 탐색, 먼 적 탐색)
        /// </summary>
        ScanType ScanType { get; }
    }
}
