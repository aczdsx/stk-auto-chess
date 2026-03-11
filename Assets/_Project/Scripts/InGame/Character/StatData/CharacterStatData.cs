using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Google.Protobuf.Collections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class CharacterStatData : IEffectCodeSource
    {
        public EffectCodeContainer EffectCodeContainer { get; }
        public ISpecCharacterInfo Spec => _spec;
        public int Level => _level;
        public int CharacterID => _spec.id;
        private ISpecCharacterInfo _spec;
        private int _level;

        public CharacterStatData(int characterId, int level, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null) :
        this(characterId, level, 1, 1, globalEffectCodeInfos)
        { }

        public double GetAttrValueCP()
        {
            var w_OP = 7;
            var w_DP = 5;
            var w_UP = 1;

            var CP = w_OP * Math.Sqrt(GetAttrValueOP()) + w_DP * Math.Sqrt(GetAttrValueDP()) + w_UP * GetAttrValueUP();

            return CP;
        }

        public double GetAttrValueOP()
        {
            var APS = AttackSpeed;
            var CriticalMul = 1 + CriticalDamageRate * (1 - CriticalProb);
            

            //CP용 관통 배율(딜 증가로 근사)
            var Pierce = Math.Clamp(ADPierce.Value, 0, 0.7);

            var K_Pierce = 0.6f;
            var PierceMul = 1 + K_Pierce * Pierce;
            var returnvaleu = AD * APS * CriticalMul * PierceMul;

            return returnvaleu;
        }

        public double GetAttrValueDP()
        {
            //방어 배율
            var K = 100;
            var DefMul = K / (K + DEF);

            //저항 배율
            var AvgResist = Math.Clamp(((ADReduce + APReduce) / 2) / 100, 0, 0.60);
            var ResMul = 1 - AvgResist;
            
            //최종 받는 피해 비율: ---> 방어 능력치를 HP로 환산해서 얼마나 더 버틸수 이쓴ㄴ지에 대한 개념
            var TakenMul = DefMul * ResMul ;

            if (TakenMul <= 0)
            {
                Debug.LogError("TakenMul is less than 0");
                return 0f;
            }
            return HP / TakenMul;
        }

        /// <summary>
        ///  차후 추가 예정 SkillPointEFF
        /// </summary>
        /// <returns></returns>
        public double GetAttrValueUP()
        {
            return 1;
        }


        public CharacterStatData(int characterId, int level, float multiAd, float multiHp, IEnumerable<EffectCodeInfo> globalEffectCodeInfos = null)
        {
            Debug.LogColor("characterID : " + characterId);
            EffectCodeContainer = new EffectCodeContainer(this);
            _spec = SpecDataManager.Instance.GetSpecCharacter(characterId);
            if (_spec == null)
            {
                Debug.LogError("CharacterStatData: _spec is null for characterId: " + characterId);
                return;
            }

            _level = level;

            var levelBonusRate = CalculateLevelBonusRate(level);
            InjectFixedValueByElpisCoreLabs();

            {
                var adBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AD_UP, 0, _spec.stat_atk * levelBonusRate, 0);
                var apBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AP_UP, 0, _spec.stat_atk * levelBonusRate, 0);

                var hpBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.HP_UP, 0, _spec.stat_hp * levelBonusRate, 0);

                var defBonusCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.DEF_UP, 0, _spec.stat_def * levelBonusRate, 0);

                EffectCodeContainer.AddOrMergeEffectCode(adBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(apBonusCodeInfo, this);

                EffectCodeContainer.AddOrMergeEffectCode(hpBonusCodeInfo, this);
                EffectCodeContainer.AddOrMergeEffectCode(defBonusCodeInfo, this);
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

        protected virtual void InjectFixedValueByElpisCoreLabs()
        {
            if (_spec is not CharacterInfo characterInfo)
            {
                return;
            }
            var elpisModel = ServerDataManager.Instance.Elpis;
            var cachedElpisCoreLabs = elpisModel.CachedElpisDimensionLabs;

            float fixedAD = 0f;
            float fixedHP = 0f;
            float fixedDEF = 0f;

            foreach (var coreLab in cachedElpisCoreLabs)
            {
                // upgrade_cover_type에 따라 적용 범위 결정
                switch (coreLab.upgrade_cover_type)
                {
                    case SynergyCoverType.KNIGHT_ALL:
                        // KNIGHT_ALL이면 전부 적용 (공, 방, 체)
                        ApplyKnightStats(coreLab, ref fixedAD, ref fixedDEF, ref fixedHP);
                        break;

                    case SynergyCoverType.SYNERGY_ELEMENTAL:
                        // SYNERGY_ELEMENTAL이면 _spec의 엘리먼트와 비교
                        if (IsElementMatch(coreLab.core_research_type, _spec.character_element_type))
                        {
                            // 속성은 방, 체 적용
                            ApplyElementalStats(coreLab, ref fixedDEF, ref fixedHP);
                        }
                        break;

                    case SynergyCoverType.SYNERGY_STELLA:
                        // SYNERGY_STELLA이면 _spec의 character_stella_type과 비교
                        if (IsStellaMatch(coreLab.core_research_type, _spec.character_stella_type))
                        {
                            // 성군은 공, 방 적용
                            ApplyStellaStats(coreLab, ref fixedAD, ref fixedDEF);
                        }
                        break;
                }
            }

            // 계산된 고정값을 EffectCode로 적용
            if (fixedAD > 0f)
            {
                var adCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.AD_UP, 0, fixedAD, 0);
                EffectCodeContainer.AddOrMergeEffectCode(adCodeInfo, this);
            }

            if (fixedDEF > 0f)
            {
                var defCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.DEF_UP, 0, fixedDEF, 0);
                EffectCodeContainer.AddOrMergeEffectCode(defCodeInfo, this);
            }

            if (fixedHP > 0f)
            {
                var hpCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.HP_UP, 0, fixedHP, 0);
                EffectCodeContainer.AddOrMergeEffectCode(hpCodeInfo, this);
            }
        }

        /// <summary>
        /// 기사 타입 스탯 적용 (공, 방, 체)
        /// </summary>
        private void ApplyKnightStats(ElpisDimensionLab coreLab, ref float fixedAD, ref float fixedDEF, ref float fixedHP)
        {
            switch (coreLab.core_research_type)
            {
                case CoreResearchType.KnightAttack:
                    fixedAD += coreLab.effect_stat_value01;
                    break;
                case CoreResearchType.KnightDefense:
                    fixedDEF += coreLab.effect_stat_value01;
                    break;
                case CoreResearchType.KnightHealth:
                    fixedHP += coreLab.effect_stat_value01;
                    break;
            }
        }

        /// <summary>
        /// 속성 타입 스탯 적용 (방, 체)
        /// </summary>
        private void ApplyElementalStats(ElpisDimensionLab coreLab, ref float fixedDEF, ref float fixedHP)
        {
            // effect_stat_value01이 방어, effect_stat_value02가 체력일 것으로 예상
            // 실제 데이터 구조에 맞게 조정 필요
            fixedDEF += coreLab.effect_stat_value01;
            fixedHP += coreLab.effect_stat_value02;
        }

        /// <summary>
        /// 성군 타입 스탯 적용 (공, 방)
        /// </summary>
        private void ApplyStellaStats(ElpisDimensionLab coreLab, ref float fixedAD, ref float fixedDEF)
        {
            // effect_stat_value01이 공격, effect_stat_value02가 방어일 것으로 예상
            // 실제 데이터 구조에 맞게 조정 필요
            fixedAD += coreLab.effect_stat_value01;
            fixedDEF += coreLab.effect_stat_value02;
        }

        /// <summary>
        /// CoreResearchType을 SynergyType으로 변환하여 엘리먼트 매칭 확인
        /// </summary>
        private bool IsElementMatch(CoreResearchType coreResearchType, SynergyType characterElementType)
        {
            switch (coreResearchType)
            {
                case CoreResearchType.Fire:
                    return characterElementType == SynergyType.FIRE;
                case CoreResearchType.Wind:
                    return characterElementType == SynergyType.WIND;
                case CoreResearchType.Earth:
                    return characterElementType == SynergyType.EARTH;
                case CoreResearchType.Lightning:
                    return characterElementType == SynergyType.LIGHTNING;
                case CoreResearchType.Water:
                    return characterElementType == SynergyType.WATER;
                default:
                    return false;
            }
        }

        /// <summary>
        /// CoreResearchType을 SynergyType으로 변환하여 성군 매칭 확인
        /// </summary>
        private bool IsStellaMatch(CoreResearchType coreResearchType, SynergyType characterStellaType)
        {
            switch (coreResearchType)
            {
                case CoreResearchType.Noblesse:
                    return characterStellaType == SynergyType.NOBLESSE;
                case CoreResearchType.Supernova:
                    return characterStellaType == SynergyType.SUPERNOVA;
                case CoreResearchType.Troubleshooter:
                    return characterStellaType == SynergyType.TROUBLESHOOTER;
                // VIGILANTE, ARCANA, ECLIPSE는 현재 SynergyType enum에 없음
                // 추후 추가될 경우 아래 주석을 해제
                // case CoreResearchType.Vigilante:
                //     return characterStellaType == SynergyType.VIGILANTE;
                // case CoreResearchType.Arcana:
                //     return characterStellaType == SynergyType.ARCANA;
                // case CoreResearchType.Eclipse:
                //     return characterStellaType == SynergyType.ECLIPSE;
                default:
                    return false;
            }
        }

        //아래 함수는 곱연산 계산 함수
        protected virtual double CalculateLevelBonusRate(int level)
        {
            // 레벨보너스랑 돌파만 몬스터에게 적용되고 초월은 안붙음.
            var levelMultiplier = (1f + _spec.inc_lv_rate * (level - 1)) * (1f + _spec.inc_lv_bonus_rate * Mathf.FloorToInt((level - 1) * 0.1f));
            float breakthroughMultiplier = 1f;
            float transcendenceMultiplier = 1f;
            if (_spec is CharacterInfo characterInfo)
            {
                //돌파
                var BT = level / 10;
                breakthroughMultiplier += characterInfo.inc_exceed * BT;

                var userCharacterData = ServerDataManager.Instance.Character.GetCharacter(characterInfo.id);
                var currentStar = (int)(userCharacterData?.TranscendLevel ?? 0) - characterInfo.init_star;
                var transcendenceData = SpecDataManager.Instance.GetCharacterTranscendenceData(characterInfo.grade_type, currentStar);
                var TR = transcendenceData != null ? transcendenceData.star - transcendenceData.init_star : 0;
                transcendenceMultiplier += characterInfo.inc_trancendence * TR;
            }

            return (levelMultiplier * breakthroughMultiplier * transcendenceMultiplier) - 1f;
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

            if (flags.HasFlag(EffectCodeInheritFlag.StatADReduce))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatADReduce);
                ADReduce = codes.CalculateADReduce(_spec.ad_reduce);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAPReduce))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAPReduce);
                APReduce = codes.CalculateAPReduce(_spec.ap_reduce);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatADPierce))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatADPierce);
                ADPierce = codes.CalculateADPierce(_spec.stat_atk_pierce);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatAPPierce))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAPPierce);
                APPierce = codes.CalculateAPPierce(_spec.stat_res_pierce);
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
                GivenHealRate = codes.CalculateGivenHealRate(_spec.heal_power);
            }

            if (flags.HasFlag(EffectCodeInheritFlag.StatTakenHealRate))
            {
                var codes = EffectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
                TakenHealRate = codes.CalculateTakenHealRate(1f);
            }
        }

        public ObfuscatorInt CharacterId => _spec?.id ?? 0;

        public ObfuscatorDouble HP { get; private set; }

        public ObfuscatorDouble AD { get; private set; }

        public ObfuscatorDouble AP { get; private set; }

        public ObfuscatorDouble ADReduce { get; private set; }

        public ObfuscatorDouble APReduce { get; private set; }

        public ObfuscatorDouble DEF { get; private set; }

        public ObfuscatorDouble ADPierce { get; private set; }

        public ObfuscatorDouble APPierce { get; private set; }

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
