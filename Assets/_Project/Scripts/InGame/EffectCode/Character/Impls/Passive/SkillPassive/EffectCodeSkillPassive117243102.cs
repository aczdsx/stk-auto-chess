using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Unity.VisualScripting;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 블린 패시브
    /// 범위: 자기 자신
    /// 에스퍼의 효과로 폭격(3*3)피해를 입힌 대상에 따라 ‘열기’중첩을 획득합니다. 
    /// 열기 중첩이 {0}회가 되면 다음 공격이 강화 됩니다.
    /// #폭염: 3*3 범위에 블린에 공격력의 {1}%에 해당하는 피해를 입히고 {2}초간 {3}%의 위력의 지속피해를 입히는 불지대를 만듭니다. 
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117243102 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117243102;
        private int _overheatMaxCount; // 최대 중첩 횟수
        private int _currentOverheatCount; // 현재 중첩 횟수


        private float _overheatDamageRatePercent; // 폭염 데미지 비율
        private float _overheatDuration; // 지속 시간
        private float _overheatDurationElapsedTime; // 지속 시간 경과 시간
        private float _overheatDamageRate; // 지속 데미지 비율
        private float _overheatDamageElapsedTime; // 지속 데미지 경과 시간

        private List<InGameTile> _overHeatTiles = new List<InGameTile>();

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _overheatMaxCount = codeInfo.GetCodeStatToInt(0);
            _overheatDamageRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _overheatDuration = codeInfo.GetCodeStatToFloat(2);
            _overheatDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;

            _currentOverheatCount = 0;
            var jobPassiveEsper = owner.GetEffectCodeContainer().GetEffectCode((int)EffectCodeNameType.JOBS_ESPER) as EffectCodeJobPassiveEsper;
            jobPassiveEsper.OnExplosionDamage += OnExplosionDamage;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _overheatMaxCount = codeInfo.GetCodeStatToInt(0);
            _overheatDamageRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _overheatDuration = codeInfo.GetCodeStatToFloat(2);
            _overheatDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        }

        public override void OnUpdate(float dt)
        {
            if (_overHeatTiles.Count == 0)
                return;

            // 지속 시간 경과 시간 증가
            _overheatDurationElapsedTime += dt;
            
            // 지속 시간이 지나면 불지대 제거
            if (_overheatDurationElapsedTime >= _overheatDuration)
            {
                _overHeatTiles.Clear();
                _overheatDurationElapsedTime = 0f;
                _overheatDamageElapsedTime = 0f;
                return;
            }

            // 지속 데미지 경과 시간 증가
            _overheatDamageElapsedTime += dt;
            
            // 1초마다 지속 데미지 적용
            if (_overheatDamageElapsedTime >= 1f)
            {
                _overheatDamageElapsedTime -= 1f;
                
                foreach (var overHeatTile in _overHeatTiles)
                {
                    // 타일 VFX 표시
                    InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, overHeatTile);
                    
                    // 타일이 유효하고 적군이 있으면 데미지 적용
                    if (overHeatTile.CheckValidTile(owner.AllianceType, false))
                    {
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                            overHeatTile.OccupiedCharacter.SkillRootTransformFollowable);

                        // 지속 데미지 계산 (공격력의 _overheatDamageRate%만큼)
                        var defaultDamage = owner.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AP;
                        var damage = owner.CalculateDamageAmount(defaultDamage * _overheatDamageRate, 0,
                            overHeatTile.OccupiedCharacter, codeId, true);

                        overHeatTile.OccupiedCharacter.GetDamaged(damage, owner);
                    }
                }
            }
        }

        private void OnExplosionDamage(int damagedTargetCount, InGameTile explosionStartTile)
        {
            _currentOverheatCount += damagedTargetCount;
            _currentOverheatCount = Math.Min(_currentOverheatCount, _overheatMaxCount);
            
            if (_currentOverheatCount < _overheatMaxCount)
            {
                return;
            }
            
            // 열기 중첩이 최대치에 도달하면 폭염 발동
            _overHeatTiles.Clear();
            _overHeatTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(explosionStartTile, 1);
        }

    }
}//117243102
