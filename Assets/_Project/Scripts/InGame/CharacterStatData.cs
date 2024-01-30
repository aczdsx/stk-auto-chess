using CookApps.Obfuscator;
using CookApps.TeamBattle.BattleSystem;

namespace CookApps.SampleTeamBattle
{
    public class CharacterStatData : ICharacterStatData
    {
        public CharacterStatData(int characterId, int level)
        {
        }

        public EffectCodeInheritFlag DirtyFlags => throw new System.NotImplementedException();

        public void RemoveDirtyFlag(EffectCodeInheritFlag flag)
        {
            throw new System.NotImplementedException();
        }

        public ObfuscatorInt CharacterId => throw new System.NotImplementedException();

        public ObfuscatorDouble HP => throw new System.NotImplementedException();

        public ObfuscatorDouble AD => throw new System.NotImplementedException();

        public ObfuscatorDouble AP => throw new System.NotImplementedException();

        public ObfuscatorDouble DEF => throw new System.NotImplementedException();

        public ObfuscatorDouble RES => throw new System.NotImplementedException();

        public ObfuscatorDouble HPRecovery => throw new System.NotImplementedException();

        public ObfuscatorFloat CriticalProb => throw new System.NotImplementedException();

        public ObfuscatorFloat CriticalDamageRate => throw new System.NotImplementedException();

        public ObfuscatorFloat DoubleCriticalProb => throw new System.NotImplementedException();

        public ObfuscatorFloat DoubleCriticalDamageRate => throw new System.NotImplementedException();

        public ObfuscatorFloat MoveSpeed => throw new System.NotImplementedException();

        public ObfuscatorFloat AttackSpeed => throw new System.NotImplementedException();

        public ObfuscatorFloat AttackRange => throw new System.NotImplementedException();

        public ObfuscatorFloat SkillDamageRate => throw new System.NotImplementedException();

        public ObfuscatorFloat SkillCooltimeRate => throw new System.NotImplementedException();

        public ObfuscatorFloat AttackDamageRate => throw new System.NotImplementedException();

        public ObfuscatorFloat TakenDamageRate => throw new System.NotImplementedException();

        public ObfuscatorFloat GivenHealRate => throw new System.NotImplementedException();

        public ObfuscatorFloat TakenHealRate => throw new System.NotImplementedException();

        public AttackType AttackType => throw new System.NotImplementedException();
    }
}
