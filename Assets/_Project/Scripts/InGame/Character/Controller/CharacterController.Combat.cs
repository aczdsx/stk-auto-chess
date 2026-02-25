using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace CookApps.BattleSystem
{
    public partial class CharacterController
    {
        [Flags]
        public enum DamageTestFlags
        {
            None = 0,
            SkipAvoidTest = 1 << 0,           // 회피 테스트 스킵
            SkipCriticalTest = 1 << 1,        // 크리티컬 테스트 스킵
            SkipResistPierce = 1 << 2,        // 저항 관통 테스트 스킵
            SkipLevelDiffMul = 1 << 3,         // 레벨 차이 계산 스킵
        }

        public struct DamageInfo
        {
            public AttackerType attackerType;
            public ObfuscatorDouble damageAmount;
            public bool isAD;
            public bool isCritical;
            public bool isDoubleCritical;
            public bool isMissed;// 회피테스트 성공여부
            public long source;

            // // 데미지 계산 히스토리 (각 단계별 데미지 변경 추적)
            // public List<DamageCalculationStep> calculationHistory;

            // 외부에서 DamageInfo를 생성할 때 사용하는 함수
            public static DamageInfo Create(double damageAmount, long source, AttackerType attackerType, bool isAD = true, bool isCritical = false, bool isDoubleCritical = false)
            {
                return new DamageInfo
                {
                    damageAmount = damageAmount,
                    source = source,
                    attackerType = attackerType,
                    isAD = isAD,
                    isCritical = isCritical,
                    isDoubleCritical = isDoubleCritical,
                    isMissed = false,
                };
            }
        }


        /// <summary>
        /// 데미지 계산해서 벹는함수.
        /// 기존에 Pre,Post Calculate 연산이 모두 하나로 통합
        /// 비트 플래그를 사용하여 각 테스트를 선택적으로 스킵할 수 있음
        /// 일반공격시 source는 0이다.
        /// </summary>
        /// <param name="ad"></param>
        /// <param name="ap"></param>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="isSkill"></param>
        /// <param name="skipTests">스킵할 테스트 플래그 (비트 연산으로 조합 가능)</param>
        /// <returns></returns>
        public DamageInfo CalculateDamageAmount(double ad, double ap, CharacterController target, long source,
        bool isSkill, DamageTestFlags skipTests = DamageTestFlags.None)
        {
            var damageInfo = new DamageInfo();
            damageInfo.source = source;
            // damageInfo.calculationHistory = new List<DamageCalculationStep>();

            //일단 공격 타입 결정
            damageInfo.isAD = ad > 0d;
            damageInfo.attackerType = AttackerType.CHARCTER;
            //ad ap로 데미지 1차 결정
            double initialDamage = damageInfo.isAD ? ad : ap;
            damageInfo.damageAmount = initialDamage;

            if (initialDamage <= 0)
            {
                Debug.LogError($"데미지 계산 오류: 초기 데미지가 0 이하입니다. {initialDamage}");
            }

            if (isSkill)
            {
                //스킬이라면 스킬데미지 계수 적용
                damageInfo.damageAmount *= SkillDamageRate;
            }

            if (target.AllianceType == AllianceType.Neutral)
                return damageInfo;

            if ((skipTests & DamageTestFlags.SkipCriticalTest) == 0)
            {
                ProgressCriticalTest(ref damageInfo);
            }

            //저항 관통 테스트
            if ((skipTests & DamageTestFlags.SkipResistPierce) == 0)
            {
                ProgressResistPierce(ref damageInfo, target);
            }

            //레벨 차이 계산
            if ((skipTests & DamageTestFlags.SkipLevelDiffMul) == 0)
            {
                var targetLevel = target.GetCharacterStat().Level;
                var attackerLevel = GetCharacterStat().Level;
                ProgressLevelDiffMul(ref damageInfo, targetLevel, attackerLevel);
            }

            if (ecc != null)
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyDamageAmount);
                damageInfo.damageAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyDamageAmountLambda,
                damageInfo.damageAmount.Value);
            }

            damageInfo.damageAmount = Math.Floor(damageInfo.damageAmount);
            
            return damageInfo;
        }
        

        private void ProgressCriticalTest(ref DamageInfo damageInfo)
        {
            if (CriticalTest())
            {
                ApplyCriticalDamage(ref damageInfo);
            }
        }

        /// <summary>
        /// 크리티컬 데미지를 적용하는 메서드
        /// </summary>
        public void ApplyCriticalDamage(ref DamageInfo damageInfo)
        {
            damageInfo.isCritical = true;
            damageInfo.damageAmount *= CriticalDamageRate;

            damageInfo.isDoubleCritical = DoubleCriticalTest();
            if (damageInfo.isDoubleCritical)
            {
                damageInfo.damageAmount *= DoubleCriticalDamageRate;
            }
        }

        private void ProgressResistPierce(ref DamageInfo damageInfo, CharacterController target)
        {
            if (damageInfo.isAD)
            {
                var ResistRaw = target.ADReduce * 0.01f;
                var EffResist = Mathf.Clamp((float)(ResistRaw * (1f - ADPierce)), 0, RESIST_CAP);
                damageInfo.damageAmount *= 1f - EffResist;
            }
            else
            {
                var ResistRaw = target.APReduce * 0.01f;
                var EffResist = Mathf.Clamp((float)(ResistRaw * (1f - APPierce)), 0, RESIST_CAP);
                damageInfo.damageAmount *= 1f - EffResist;
            }
        }

        private void ProgressLevelDiffMul(ref DamageInfo damageInfo, int targetLevel, int attackerLevel)
        {
            var Gap = Mathf.Max(0, targetLevel - attackerLevel);
            var LevelMul = Mathf.Clamp(1 - 0.05f * Mathf.Floor(Gap / 5), 0.5f, 1.0f);
            damageInfo.damageAmount *= LevelMul;
        }


        /// <summary>
        /// 실제로 대미지를 입히는 함수
        /// </summary>
        /// <param name="damageInfo">계산된 대미지 정보</param>
        /// <param name="attacker">공격자</param>
        /// <param name="isFirstDamage">
        /// 스킬중에 피해를 받으면 받는 피해의 일부를 주변 동료에게 넘기는 스킬을 가진 캐릭터가 둘 있다면,
        /// 그 둘중 하나가 피헤를 받을 경우, 둘이서 피해를 무한으로 넘기고 받을 수 있을 텐데,
        /// 이를 막기위해 첫번째 피해인 것을 명시하여 이후의 피해는 스킬 효과를 적용하지 않도록 한다.
        /// </param>
        /// <returns>
        /// 대미지를 입힌 후 상태
        /// "스킬로 상대를 죽인 경우 쿨타임 초기화" 이런 것을 처리하기 위해 반환값을 사용
        /// </returns>
        public DamageReturnType GetDamaged(in DamageInfo damageInfo, CharacterController attacker,
        bool isFirstDamage = true, bool isNonHitFx = false)
        {
            if (!InGameManager.Instance.IsInGameCombat)
                return DamageReturnType.Damaging;

            // 로비 전투 상황일 때
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateLobbyCombat)
            {
                if (attacker != null && attacker.AllianceType == AllianceType.Enemy)
                    return DamageReturnType.Damaging;
            }
            // 같은 틱에 대미지를 줘서 여러번 죽이는 경우가 있어서 이미 죽었는지 체크
            if (_currHp <= 0 || _view == null)
            {
                return DamageReturnType.AlreadyDead;
            }

            var originDamageAmount = damageInfo.damageAmount.Value;
            var damageAmount = damageInfo;
            // effectCode에게 이벤트 전달
            {
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDamaged);
                damageAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDamagedLambda, damageInfo, attacker, isFirstDamage);
            }

            // effectCode에게 이벤트 전달
            if (damageInfo.isCritical && attacker != null)
            {
                var effectCodes = attacker.GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCritical);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnCriticalLambda, this);
            }

            if (!isNonHitFx)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_hit_01, SkillRootTransformFollowable);
            }

            GetCharacterView().OnHit();

            if (!damageInfo.isMissed)
            {

                ShowDamageText(damageAmount.damageAmount.Value, damageInfo.isCritical).Forget();
                
            }
            else
            {
                ShowMissText().Forget();
            }

            // 테스트 무적 체크 (데미지 텍스트는 표시되지만 HP 감소 없음)
            bool isInvincible = (AllianceType == AllianceType.Player && InGameManager.Instance.IsPlayerInvincible)
                             || (AllianceType == AllianceType.Enemy && InGameManager.Instance.IsEnemyInvincible);
            if (isInvincible)
            {
                return DamageReturnType.Damaging;
            }

            _currHp -= damageAmount.damageAmount.Value;
            Debug.Log($"damageAmount: {damageAmount.damageAmount.Value}, _currHp: {_currHp}");


            if (attacker != null)
            {
                InGameStatistics.Instance.AddCombatDamage(attacker, this, damageInfo.damageAmount, _currHp, damageInfo.source);
            }
            UpdateHpBar();

            if (_currHp <= 0)
            {
                _currHp = 0;
                IsAlive = false;

                if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat
                    || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateTrialDungeonCombat
                    || InGameMainFlowManager.Instance.CurrentFlowState is FlowStateInGameTestCombat)
                {
                    switch (damageInfo.attackerType)
                    {
                        case AttackerType.CHARCTER:
                            if (attacker != null)
                            {
                                InGameMain.GetInGameMain().AddKillLog(attacker, this, attacker.AllianceType == AllianceType.Player);
                            }
                            break;
                        case AttackerType.COMMANDER_SKILL:
                            var commanderSkill = SpecDataManager.Instance.GetCommanderSkillDataList((int)damageInfo.source)[0];
                            InGameMain.GetInGameMain().AddKillLog(commanderSkill, this, AllianceType != AllianceType.Player);
                            break;
                        case AttackerType.CHAPTER_RULE:
                            var chapterRule = SpecDataManager.Instance.GetChapterRuleData((int)damageInfo.source);
                            InGameMain.GetInGameMain().AddKillLog(chapterRule, this, AllianceType != AllianceType.Player);
                            break;
                        case AttackerType.SYNERGY_STAR_ASTERISM:
                            var synergyStarAsterism = SpecDataManager.Instance.GetSpecSynergyData((int)damageInfo.source);
                            InGameMain.GetInGameMain().AddKillLog(synergyStarAsterism, this, AllianceType != AllianceType.Player);
                            break;
                    }
                }

                var deathInfo = new DeathInfo { attacker = attacker };
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnDead);
                deathInfo = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallOnDeadLambda, deathInfo);

                if (!deathInfo.isUseCustomState)
                {
                    ForceSetNextState<CharacterStateDead>();

                    if (LobbyMain.GetLobbyMain() != null)
                    {
                        SpawnDropFx((targetLine) =>
                        {
                            if (targetLine != null)
                                targetLine.Remove();
                        });
                    }
                }

                // 튜토리얼 CHARACTER_DEAD 트리거 처리 (Dead 상태 전환 후 처리)
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
                {
                    var tutorialTarget = GetCharacterView()?.gameObject.GetComponent<TutorialTarget>();
                    if (tutorialTarget != null && !string.IsNullOrEmpty(tutorialTarget.TargetId))
                    {
                        Debug.LogColor($"튜토리얼 CHARACTER_DEAD 트리거 처리: {tutorialTarget.TargetId}", "green");
                        TutorialManager.Instance.HandleTutorialAction(
                            TutorialTriggerType.CHARACTER_DEAD,
                            tutorialTarget.TargetId.ToString()
                        );
                    }
                }

                return DamageReturnType.Killed;
            }

            return DamageReturnType.Damaging;
        }

        /// <summary>
        /// 회복량 계산
        /// </summary>
        /// <param name="recoveryAmount">기본 회복량</param>
        /// <param name="target">회복 대상</param>
        /// <param name="isSkill">스킬 힐인지 여부 (평타 힐은 false)</param>
        /// <returns>계산된 회복량</returns>
        public double PostCalculateHealAmount(double recoveryAmount, CharacterController target, bool isSkill = false)
        {
            // 오라클 캐릭터의 스킬 힐량 계산
            bool isOracleHealer = _stateTypeMap.TryGetValue(typeof(CharacterStateAttack), out Type concreteType)
                                   && concreteType == typeof(CharacterStateAttackHealer);

            if (isSkill && isOracleHealer)
            {
                recoveryAmount = EffectCodeJobPassiveRecovery.CalculateOracleSkillRecoveryAmount(this, target, recoveryAmount);
            }

            recoveryAmount = Math.Round(recoveryAmount * (1f + GivenHealRate) * (1f + target.GivenHealRate));

            // 속성, 크기, 종족에 따른 회복량 계산이 필요하다면 여기서 할 것

            var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyHealAmount);
            recoveryAmount = EffectCodeForLoopHelper.Passing(effectCodes, EffectCodeCharacterLambda.CallModifyHealAmountLambda, recoveryAmount);

            return recoveryAmount;
        }

        /// <summary>
        /// 회복
        /// </summary>
        /// <param name="amount">힐량</param>
        /// <param name="healer">힐을 준 캐릭터</param>
        /// <param name="source">힐이 사용된 effectCodeId</param>
        /// <param name="isFirstHeal">
        /// 스킬중에 힐을 받으면 주변 동료에게 받은 힐량의 절반을 회복하는 스킬을 가진 캐릭터가 둘 있다면,
        /// 그 둘중 하나가 힐을 받을 경우, 둘이서 회복을 계속 하면서 무한으로 주고 받을 수 있을텐데,
        /// 이를 막기위해 첫번째 힐인 것을 명시하여 이후의 힐은 스킬 효과를 적용하지 않도록 한다.
        /// </param>
        /// <returns>회복 됬는지 유무</returns>
        public bool GetHealed(double amount, CharacterController healer, long source, bool isFirstHeal = true)
        {
            // 죽어있으면 안준다.
            if (_currHp <= 0 || !IsAlive)
            {
                return false;
            }

            {
                // effectCode에게 이벤트 전달
                var effectCodes = ecc.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnHealed);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnHealedLambda, amount, isFirstHeal);
                _currHp += amount;
            }

            ShowHealText(amount).Forget();

            if (_currHp > HP)
            {
                _currHp = HP;
            }

            if (AllianceType != AllianceType.Enemy)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_heal01);
            }



            InGameStatistics.Instance.AddCombatHeal(healer, this, amount, _currHp, HP, source);

            UpdateHpBar();
            return true;
        }


        private async UniTask ShowDamageText(double amount, bool isCritical)
        {
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            if (AllianceType != AllianceType.Player)
            {
                textView.PlayDamageSound(isCritical);
            }

            await textView.ShowDamageText(GetCharacterView().CachedTr.position, _statData.Spec.height, amount, isCritical);
        }


        private async UniTask ShowHealText(double amount)
        {
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            await textView.ShowHealText(GetCharacterView().CachedTr.position, _statData.Spec.height, amount);
        }

        private async UniTask ShowMissText()
        {
            InGameTextView textView = InGameTextViewPool.Instance.Get();
            await textView.ShowMissText(GetCharacterView().CachedTr.position, _statData.Spec.height);
        }

        private bool CriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < (_fixedCriticalProb + CriticalProb) * 100; // OK
        }

        public void SetFixedCriticalProb(float fixedCriticalProb)
        {
            _fixedCriticalProb = fixedCriticalProb;
        }
        public void ResetFixedCriticalProb()
        {
            _fixedCriticalProb = 0f;
        }

        public bool PureDamageTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < PureDamageProb * 100; // OK
        }

        private bool DoubleCriticalTest()
        {
            return InGameRandomManager.GetUniversalRandomValue(0f, 100f) < DoubleCriticalProb; // OK
        }
    }
}
