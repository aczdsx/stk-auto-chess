using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]

    /// <summary>
    /// 배틀 아이템 공급
    /// 트러블슈터 시너지 사용 시 배틀아이템을 일정 시간마다 배포합니다.
    /// 
    /// </summary>
    public partial class EffectCodeBattleItemSupply : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.BATTLE_ITEM_SUPPLY;
        private float _duration;
        private float _elapsedTime;
        private enum TroubleshooterSynergyIdx
        {
            CHOCOBAR = 2,
            VITAMIN = 3,
            EMERGENCY_ARMOR = 4,
            EMP_BOMB = 5,
            ENERGY_DRINK = 6,
        }

        private InGameVfx _supplyVfx;// 보급품떨어지는 vfx
        private CharacterController _targetCharacter;// 보급품을 받는 캐릭터
        private InGameVfxNameType _supplyVfxNameType;// 부여되는 아이템 타입

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _elapsedTime = 0f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _duration = codeInfo.GetCodeStatToFloat(0);
            _elapsedTime = 0f;
        }

        public override void OnUpdate(float dt)
        {
            _elapsedTime += dt;
            if (_elapsedTime >= _duration)
            {
                _elapsedTime = 0f;
                SupplyBattleItem();
            }
        }

        private void SupplyBattleItem()
        {
            var randomValue = InGameRandomManager.GetUniversalRandomValue(0, 4);
            InGameVfxNameType effectVfxNameType = InGameVfxNameType.NONE;
            switch (randomValue)
            {
                case 0:
                    effectVfxNameType = InGameVfxNameType.fx_common_supply_chocobar;
                    break;
                case 1:

                    effectVfxNameType = InGameVfxNameType.fx_common_supply_vitamin;
                    break;
                case 2:
                    effectVfxNameType = InGameVfxNameType.fx_common_supply_emp_bomb;
                    break;
                case 3:
                    effectVfxNameType = InGameVfxNameType.fx_common_supply_energy_drink;
                    break;
                case 4:

                    effectVfxNameType = InGameVfxNameType.fx_common_supply_emergency_armor;
                    break;
                default:
                    break;
            }

            if (effectVfxNameType == InGameVfxNameType.NONE)
                return;

            var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Player);

            // TroubleShooter 캐릭터들에게 낙하산으로 배틀 아이템 공급
            List<CharacterController> troubleShooterCharacterList = new();
            foreach (var character in playerCharacterList)
            {
                if (character.GetCharacterStat().Spec.character_stella_type == SynergyType.TROUBLESHOOTER
                && character.IsAlive)
                {
                    troubleShooterCharacterList.Add(character);
                    break;
                }
            }
            if (troubleShooterCharacterList.Count == 0)
                return;

            int randomIndex = InGameRandomManager.GetUniversalRandomValue(0, troubleShooterCharacterList.Count - 1);
            SpawnSupplyVfx(troubleShooterCharacterList[randomIndex], effectVfxNameType);
        }

        private void SpawnSupplyVfx(CharacterController targetCharacter, InGameVfxNameType supplyVfxNameType)
        {
            _targetCharacter = targetCharacter;
            Vector3 startPos = targetCharacter.SkillRootTransformFollowable.GetPosition();
            _supplyVfxNameType = supplyVfxNameType;
            // VFX 생성
            _supplyVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_supply_varient, startPos);

            _supplyVfx.Initialize(false);
            if (_supplyVfx is InGameVfxWithAnimation)
            {
                var animatorVfx = _supplyVfx as InGameVfxWithAnimation;
                animatorVfx.SetOnVfxEndCallback(OnEndSupplyAnimation);
                animatorVfx.SetOnCustomAnimationEventCallback((eventKey, positions) => OnReachedTargetHandler(eventKey, positions));
            }
        }

        // 낙하산이 목표 지점에 도착했을 때 처리
        private void OnReachedTargetHandler(AnimationEventKey eventKey, IReadOnlyList<Transform> positions)
        {
            ApplySupplyEffectCode(_targetCharacter, _supplyVfxNameType);
        }

        private void OnEndSupplyAnimation(IReadOnlyList<Transform> positions)
        {
            _supplyVfx.Remove();
        }


        private void ApplySupplyEffectCode(CharacterController targetCharacter, InGameVfxNameType vfxNameType)
        {
            if (targetCharacter == null || targetCharacter.IsAlive == false)
                return;

            var tSData = SpecDataManager.Instance.GetSpecSynergyList(synergyType: SynergyType.TROUBLESHOOTER);
            if (tSData == null || tSData.Count == 0)
                return;
            InGameVfxManager.Instance.AddInGameVfx(vfxNameType, targetCharacter.SkillMiddleFXTransformFollowable.GetPosition());

            switch (vfxNameType)
            {
                case InGameVfxNameType.fx_common_supply_chocobar:
                    {
                        CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                        damageInfo.damageAmount = targetCharacter.HP * tSData[(int)TroubleshooterSynergyIdx.CHOCOBAR].effect_stat_value_1 * 0.01d;
                        damageInfo.damageAmount = Math.Floor(damageInfo.damageAmount);
                        targetCharacter.GetHealed(damageInfo.damageAmount, null, codeId);
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_chocolate_01, targetCharacter.Position3D);
                        break;
                    }
                case InGameVfxNameType.fx_common_supply_vitamin:
                    {//전투자극제
                        Span<double> stats = stackalloc double[3];
                        stats[0] = CodeId;
                        stats[1] = tSData[(int)TroubleshooterSynergyIdx.VITAMIN].effect_stat_value_1;
                        stats[2] = tSData[(int)TroubleshooterSynergyIdx.VITAMIN].effect_stat_value_2 * 0.01d;
                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_AD_PERCENT_UP, targetCharacter, stats, source);
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_stimpack_01, targetCharacter.Position3D);
                        break;
                    }
                case InGameVfxNameType.fx_common_supply_emp_bomb:
                    {
                        var targetTile = targetCharacter.CurrentTile;
                        if (targetTile != null)
                        {
                            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(targetTile, 1);
                            Span<double> eccStats = stackalloc double[3];
                            eccStats.Clear();
                            eccStats[0] = codeId;
                            eccStats[1] = tSData[(int)TroubleshooterSynergyIdx.EMP_BOMB].effect_stat_value_1;
                            eccStats[2] = 0;
                            foreach (var tile in tileList)
                            {
                                var empBombCharacter = tile.OccupiedCharacter;
                                if (empBombCharacter != null)
                                {
                                    if (empBombCharacter.AllianceType == AllianceType.Player)
                                    {
                                        continue;
                                    }
                                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_SILENCE, empBombCharacter, eccStats, source);
                                }
                            }
                            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_emp_01, targetTile.View.CachedTr.position);
                            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_shooter_emp);
                        }
                        break;
                    }
                case InGameVfxNameType.fx_common_supply_energy_drink:
                    {
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_ctdown_01, targetCharacter.SkillMiddleFXTransformFollowable);
                        targetCharacter.AddSkillCooltimeInECC(tSData[(int)TroubleshooterSynergyIdx.ENERGY_DRINK].effect_stat_value_1);
                        break;
                    }
                case InGameVfxNameType.fx_common_supply_emergency_armor:
                    {
                        Span<double> stats = stackalloc double[3];
                        stats.Clear();
                        stats[0] = 99999f;
                        stats[1] = targetCharacter.CurrentHp * tSData[(int)TroubleshooterSynergyIdx.EMERGENCY_ARMOR].effect_stat_value_1 * 0.01f;
                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.SHIELD, targetCharacter, stats, source);
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_ts_shield_01, targetCharacter.SkillMiddleFXTransformFollowable);
                        break;
                    }
                default:
                    break;
            }
        }
    }
}
