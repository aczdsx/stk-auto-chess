using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;
using UnityEditor.Localization.Plugins.XLIFF.V12;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 시이나 패시브
    /// 대상: 자기 자신
    /// 공격 대상의 인접한 셀(그리드)에 아군(적군)이 없다면 일반 공격이 {0}% 추가 피해를입힙니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive115362202 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 115362202;
        private float _damageRatePercent; // 반격 데미지 비율
        private float _increaseDamageRatePercent; // 이번 틱의 증분량
        private SkillPassive _specSkill;

        /// <summary>
        /// 아이콘 표시가 필요한 경우 true를 반환합니다.
        /// 이 값이 true이고 owner가 살아있는 경우, 이펙트 코드는 제거되지 않습니다.
        /// </summary>
        public override bool IsNeedToShowIcon => true;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _damageRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _specSkill = base.GetSpecSkillPassive(CodeId);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _damageRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
        }

        public override void OnAttack()
        {
            base.OnAttack();
            if (owner.Target == null)
                return;
            //일반 공격시 적용하고 빼자.
            var tiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.Target.CurrentTile, 1);
            foreach (var tile in tiles)
            {
                if(tile.OccupiedCharacter == null || tile.OccupiedCharacter == owner.Target || !tile.OccupiedCharacter.IsAlive )
                    continue;

                if (tile.CheckValidTile(owner.Target.AllianceType, true))
                {
                    _increaseDamageRatePercent = _damageRatePercent;
                    owner.GetEffectCodeContainer().SetDirtyFlag(this);
                    return;
                }
            }


        }

        public override void OnAttackEnd(CharacterController target)
        {
            base.OnAttackEnd(target);
            if(_increaseDamageRatePercent > 0f)
            {
                _increaseDamageRatePercent = 0f;
                owner.GetEffectCodeContainer().SetDirtyFlag(this);
                return;
            }
        }

        public override double GetIncrementPercentAD()
        {
            return _increaseDamageRatePercent;
        }
    }
        
}//115362202
