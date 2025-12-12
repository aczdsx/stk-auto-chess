using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using Google.Protobuf.Collections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class CharacterStatData : IEffectCodeSource
    {
        public EffectCodeContainer EffectCodeContainer { get; }
        public ISpecCharacterInfo Spec => _spec;
        public int Level => _level;
        public int CharacterID => _spec?.GetId() ?? 0;
        private ISpecCharacterInfo _spec;
        private int _level;

        public CharacterStatData(int characterId, int level, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null) : this(characterId, level, 1, 1, globalEffectCodeInfos)
        { }

        public double GetAttrValue()
        {
            double physicalPenetration = DEFPenetration; // 물리관통
            double magicPenetration = RESPenetration; // 마법관통
            double physicalDefense = DEF; // 물리방어
            double magicDefense = RES; // 마법방어
            double hpWeight = 1; // 체력 가중치, 이 값은 게임의 규칙에 따라 변경될 수 있습니다.

            double CP = (AD * AttackSpeed * (1 + CriticalProb * CriticalDamageRate)) *
                        (1 + ((physicalPenetration + magicPenetration) / (physicalPenetration + magicPenetration + 100) +
                              (physicalDefense + magicDefense) / (physicalDefense + magicDefense + 100))) +
                        hpWeight * Math.Sqrt(HP);

            return CP;
        }

        public CharacterStatData(int characterId, int level, float multiAd, float multiHp, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null)
        {
            Debug.LogColor("characterID : " + characterId);
            EffectCodeContainer = new EffectCodeContainer(this);
            _spec = SpecDataManager.Instance.GetSpecCharacter(characterId);
            _level = level;

            var levelBonusRate = 0f;
            for (var i = 1; i <= level; i++)
            {
                if (i % 10 == 0)
                    levelBonusRate += _spec.inc_lv_bonus_rate;
                else
                    levelBonusRate += _spec.inc_lv_rate;
            }

            {
                var adBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AD_PERCENT_UP, 0, levelBonusRate, 0);
                var apBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AP_PERCENT_UP, 0, levelBonusRate, 0);
                var hpBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.HP_PERCENT_UP, 0, levelBonusRate, 0);
                EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(apBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(hpBonusCodeInfo, this);
            }

            if (!Mathf.Approximately(multiAd, 1f))
            {
                {
                    var codeId = EffectCodeIdGenerator.GetStatCode(EffectCodeNameType.AD_PERCENT_UP,
                        GlobalEffectProviderType.MONSTER_MULTIPLE, 0);
                    var adBonusCodeInfo = new EffectCodeInfo(codeId, 0, multiAd, 0);
                    EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
                }

                {
                    var codeId = EffectCodeIdGenerator.GetStatCode(EffectCodeNameType.AP_PERCENT_UP,
                        GlobalEffectProviderType.MONSTER_MULTIPLE, 0);
                    var adBonusCodeInfo = new EffectCodeInfo(codeId, 0, multiAd, 0);
                    EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
                }
            }
            if (!Mathf.Approximately(multiHp, 1f))
            {
                var codeId = EffectCodeIdGenerator.GetStatCode(EffectCodeNameType.HP_PERCENT_UP,
                    GlobalEffectProviderType.MONSTER_MULTIPLE, 0);
                var adBonusCodeInfo = new EffectCodeInfo(codeId, 0, multiHp, 0);
                EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
            }

            if (globalEffectCodeInfos != null)
            {
                foreach (EffectCodeInfo effectCodeInfo in globalEffectCodeInfos)
                {
                    EffectCodeBase code = EffectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, this);
                }
            }

            UpdateStats(EffectCodeInheritFlag.StatAll);
        }

        // PVP 전용
        public CharacterStatData(int characterId, int level, RepeatedField<EffectCodeInfoProto> protos, RepeatedField<EffectCodeInfoProto> globalEffectCodeInfos = null)
        {
            Debug.LogColor("characterID : " + characterId);
            EffectCodeContainer = new EffectCodeContainer(this);
            _spec = SpecDataManager.Instance.GetSpecCharacter(characterId);
            _level = level;

            var levelBonusRate = 0f;
            for (var i = 1; i <= level; i++)
            {
                if (i % 10 == 0)
                    levelBonusRate += _spec.inc_lv_bonus_rate;
                else
                    levelBonusRate += _spec.inc_lv_rate;
            }

            {
                var adBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AD_PERCENT_UP, 0, levelBonusRate, 0);
                var apBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AP_PERCENT_UP, 0, levelBonusRate, 0);
                var hpBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.HP_PERCENT_UP, 0, levelBonusRate, 0);
                EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(apBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(hpBonusCodeInfo, this);
            }

            if (globalEffectCodeInfos != null)
            {
                foreach (EffectCodeInfoProto effectCodeInfo in globalEffectCodeInfos)
                {
                    List<double> statsList = new List<double>();
                    foreach (var stat in effectCodeInfo.Stat)
                    {
                        statsList.Add(stat);
                    }
                    ReadOnlySpan<double> stats = statsList.ToArray().AsSpan();
                    EffectCodeInfo eccInfo = new EffectCodeInfo(effectCodeInfo.Id, 0, stats);
                    EffectCodeBase code = EffectCodeContainer.AddOrMergeEffectCode(eccInfo, this);
                }
            }

            UpdateStats(EffectCodeInheritFlag.StatAll);
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
            EffectCodeContainer.RemoveEffectCode(codeId, out var effectCode);
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
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHP);
                HP = codes.CalculateHP(_spec.stat_hp);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAD))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAD);
                AD = codes.CalculateAD(_spec.stat_atk);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAP))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAP);
                AP = codes.CalculateAP(_spec.stat_atk);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDEF))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEF);
                DEF = codes.CalculateDEF(_spec.stat_def);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRES))
            {
                // var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRES);
                // RES = codes.CalculateRES(_spec.stat_res);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDEFPenetration))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEFPenetration);
                DEFPenetration = codes.CalculateDEFPenetration(_spec.stat_atk_pierce);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRESPenetration))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRESPenetration);
                RESPenetration = codes.CalculateRESPenetration(_spec.stat_res_pierce);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatRecoveryHP))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRecoveryHP);
                HPRecovery = codes.CalculateRecoveryHP(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatMoveSpeed))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatMoveSpeed);
                MoveSpeed = codes.CalculateMoveSpeed(_spec.move_speed);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatCriticalProb))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalProb);
                CriticalProb = codes.CalculateCriticalProb(_spec.crit_rate);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatCriticalDamageRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                CriticalDamageRate = codes.CalculateCriticalDamageRate(_spec.crit_power);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalProb))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                DoubleCriticalProb = codes.CalculateDoubleCriticalProb(0);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                DoubleCriticalDamageRate = codes.CalculateDoubleCriticalDamageRate(1);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackSpeed))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackSpeed);
                AttackSpeed = codes.CalculateAttackSpeed(_spec.atk_speed);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackRange))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
                AttackRange = codes.CalculateAttackRange(_spec.atk_range);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatSkillDamageRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                SkillDamageRate = codes.CalculateSkillDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatSkillCooltimeRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                SkillCooltimeRate = codes.CalculateSkillCooltimeRate(0f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAttackDamageRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                AttackDamageRate = codes.CalculateTotalDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatTakenDamageRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                TakenDamageRate = codes.CalculateTakenDamageRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatGivenHealRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatGivenHealRate);
                GivenHealRate = codes.CalculateGivenHealRate(1f);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatTakenHealRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
                TakenHealRate = codes.CalculateTakenHealRate(1f);
            }
        }

        public ObfuscatorInt CharacterId => _spec?.GetId() ?? 0;

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

        public ObfuscatorFloat PureDamageProb { get; private set; }

        public ObfuscatorFloat MoveSpeed { get; private set; }

        public ObfuscatorFloat AttackSpeed { get; private set; }

        public ObfuscatorInt AttackRange { get; private set; }

        public BattleSystem.AttackRangeShape AttackRangeShape { get; private set; }

        public ObfuscatorFloat SkillDamageRate { get; private set; }

        public ObfuscatorFloat SkillCooltimeRate { get; private set; }

        public ObfuscatorFloat AttackDamageRate { get; private set; }

        public ObfuscatorFloat TakenDamageRate { get; private set; }

        public ObfuscatorFloat GivenHealRate { get; private set; }

        public ObfuscatorFloat TakenHealRate { get; private set; }

        public ScanType ScanType { get; private set; }
    }
}
