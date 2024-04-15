using CookApps.Obfuscator;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface ICharacterStatData
    {
        EffectCodeInheritFlag DirtyFlags { get; }
        void RemoveDirtyFlag(EffectCodeInheritFlag flag);

        ObfuscatorInt CharacterId { get; }

        ObfuscatorDouble HP { get; }
        ObfuscatorDouble AD { get; }
        ObfuscatorDouble AP { get; }
        ObfuscatorDouble DEF { get; }
        ObfuscatorDouble RES { get; }
        ObfuscatorDouble DEFPenetration { get; }
        ObfuscatorDouble RESPenetration { get; }

        ObfuscatorDouble HPRecovery { get; }
        ObfuscatorFloat CriticalProb { get; }
        ObfuscatorFloat CriticalDamageRate { get; }
        ObfuscatorFloat DoubleCriticalProb { get; }
        ObfuscatorFloat DoubleCriticalDamageRate { get; }
        ObfuscatorFloat MoveSpeed { get; }
        ObfuscatorFloat AttackSpeed { get; }
        ObfuscatorFloat AttackRange { get; }

        // 스킬 관련 값들
        ObfuscatorFloat SkillDamageRate { get; }
        ObfuscatorFloat SkillCooltimeRate { get; }

        // 주는 피해량, 받는 피해량 값들
        ObfuscatorFloat AttackDamageRate { get; }
        ObfuscatorFloat TakenDamageRate { get; }

        // 주는 힐량, 받는 힐량 값들
        ObfuscatorFloat GivenHealRate { get; }
        ObfuscatorFloat TakenHealRate { get; }

        AttackType AttackType { get; }
    }
}
