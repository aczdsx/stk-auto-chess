using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.TeamBattle.BattleSystem;

namespace CookApps.SampleTeamBattle
{
    public class CharacterStatData : IEffectCodeSource, ICharacterStatData
    {
        public EffectCodeContainer EffectCodeContainer { get; }

        private int characterId;
        private SpecCharacter spec;

        public CharacterStatData(int characterId, int level, List<EffectCodeInfo> globalEffectCodeInfos = null)
        {
            this.characterId = characterId;
            EffectCodeContainer = new EffectCodeContainer(this);
            spec = SpecDataManager.Instance.SpecCharacter.Get(characterId);
            // TODO: level에 따른 스탯 증가 적용! 이펙트 코드로 적용되어야 함

            var flags = EffectCodeInheritFlag.None;
            if (globalEffectCodeInfos != null)
            {
                foreach (EffectCodeInfo effectCodeInfo in globalEffectCodeInfos)
                {
                    EffectCodeBase code = EffectCodeContainer.AddEffectCode(effectCodeInfo, this);
                    if (code is EffectCodeStatBase statCode)
                    {
                        flags.AddFlag(statCode.GetFlag());
                    }
                }
            }

            UpdateStats(flags);
        }

        public void AddOrUpdateEffectCode(EffectCodeInfo codeInfo)
        {
            EffectCodeBase effectCode = EffectCodeContainer.AddOrMergeEffectCode(codeInfo, this);
            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                UpdateStats(statEffectCode.GetFlag());
            }
        }

        public void RemoveEffectCode(int codeId)
        {
            EffectCodeBase effectCode = EffectCodeContainer.RemoveEffectCode(codeId);
            if (effectCode is EffectCodeStatBase statEffectCode)
            {
                UpdateStats(statEffectCode.GetFlag());
            }
        }

        private EffectCodeInheritFlag dirtyFlags = EffectCodeInheritFlag.None;
        public EffectCodeInheritFlag DirtyFlags => dirtyFlags;

        public void RemoveDirtyFlag(EffectCodeInheritFlag flag)
        {
            dirtyFlags.RemoveFlag(flag);
        }

        private void UpdateStats(EffectCodeInheritFlag flags)
        {
            dirtyFlags.AddFlag(flags);

            if (flags.HasFlag(EffectCodeInheritFlag.StatHP))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHP);
                HP = codes.CalculateHP(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAD))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAD);
                AD = codes.CalculateAD(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAP))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAP);
                AP = codes.CalculateAP(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDEF))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEF);
                DEF = codes.CalculateDEF(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRES))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRES);
                RES = codes.CalculateRES(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDEFPenetration))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEFPenetration);
                DEFPenetration = codes.CalculateDEFPenetration(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRESPenetration))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRESPenetration);
                RESPenetration = codes.CalculateRESPenetration(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRecoveryHP))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRecoveryHP);
                HPRecovery = codes.CalculateRecoveryHP(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatMoveSpeed))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatMoveSpeed);
                MoveSpeed = codes.CalculateMoveSpeed(1 /*spec.move_speed*/);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatCriticalProb))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalProb);
                CriticalProb = codes.CalculateCriticalProb(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatCriticalDamageRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                CriticalDamageRate = codes.CalculateCriticalDamageRate(1);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalProb))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                DoubleCriticalProb = codes.CalculateDoubleCriticalProb(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                DoubleCriticalDamageRate = codes.CalculateDoubleCriticalDamageRate(1);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackSpeed))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackSpeed);
                AttackSpeed = codes.CalculateAttackSpeed(spec.atkSpd);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackRange))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
                AttackRange = codes.CalculateAttackRange(spec.atkRange);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackRangeShape))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
                AttackRangeShape = codes.CalculateAttackRangeShape(spec.atkRangeShape.ToInGameAttackRangeShape());
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatSkillDamageRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                SkillDamageRate = codes.CalculateSkillDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatSkillCooltimeRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                SkillCooltimeRate = codes.CalculateSkillCooltimeRate(0f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackDamageRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                AttackDamageRate = codes.CalculateAttackDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatTakenDamageRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                TakenDamageRate = codes.CalculateTakenDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatGivenHealRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatGivenHealRate);
                GivenHealRate = codes.CalculateGivenHealRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatTakenHealRate))
            {
                List<EffectCodeStatBase> codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
                TakenHealRate = codes.CalculateTakenHealRate(1f);
            }
        }

        public ObfuscatorInt CharacterId => characterId;

        public ObfuscatorDouble HP { get; private set; }

        public ObfuscatorDouble AD { get; private set; }

        public ObfuscatorDouble AP { get; private set; }

        public ObfuscatorDouble DEF { get; private set; }

        public ObfuscatorDouble RES { get; private set; }

        public ObfuscatorDouble DEFPenetration { get; private set; }

        public ObfuscatorDouble RESPenetration { get; private set; }

        public ObfuscatorDouble HPRecovery { get; private set; }

        public ObfuscatorFloat CriticalProb { get; private set; }

        public ObfuscatorFloat CriticalDamageRate { get; private set; }

        public ObfuscatorFloat DoubleCriticalProb { get; private set; }

        public ObfuscatorFloat DoubleCriticalDamageRate { get; private set; }

        public ObfuscatorFloat MoveSpeed { get; private set; }

        public ObfuscatorFloat AttackSpeed { get; private set; }

        public ObfuscatorInt AttackRange { get; private set; }

        public TeamBattle.BattleSystem.AttackRangeShape AttackRangeShape { get; private set; }

        public ObfuscatorFloat SkillDamageRate { get; private set; }

        public ObfuscatorFloat SkillCooltimeRate { get; private set; }

        public ObfuscatorFloat AttackDamageRate { get; private set; }

        public ObfuscatorFloat TakenDamageRate { get; private set; }

        public ObfuscatorFloat GivenHealRate { get; private set; }

        public ObfuscatorFloat TakenHealRate { get; private set; }

        public AttackType AttackType { get; private set; }

        public ScanType ScanType { get; private set; }
    }
}
