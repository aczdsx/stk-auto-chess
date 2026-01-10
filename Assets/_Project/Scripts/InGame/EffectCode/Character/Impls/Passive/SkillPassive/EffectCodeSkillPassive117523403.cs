using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Mono.Cecil.Cil;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 아드리아 패시브
    /// 대상: 자기 자신
    /// 3*3 범위에 있는 적들의 수에 따라 자신의 물리/마법 저항력이 {0}% 상승하고, {1}% 만큼 치유력이 올라갑니다.
    /// </summary>
    [UseEffectCodeIds((int)CodeId)]
    public partial class EffectCodeSkillPassive117523403 : EffectCodeSkillPassiveBase
    {
        public const long CodeId = 117523403;
        private float _adReduceApReduceRatePercent; // 물리/마법 저항력 증가 비율
        private float _healRatePercent; // 치유력 증가 비율
        private SkillPassive _specSkill;
        private int _currentTargetCount;


        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _adReduceApReduceRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _healRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _specSkill = base.GetSpecSkillPassive(CodeId);

        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _adReduceApReduceRatePercent = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _healRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            int targetCount = 0;
            var targettiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
            foreach (var tile in targettiles)
            {
                if (tile.CheckValidTile(owner.AllianceType, false))
                {
                    targetCount++;
                }
            }

            if (targetCount != _currentTargetCount)
            {
                _currentTargetCount = targetCount;
                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }
        }

        public override float GetIncrementPercentGivenHealRate()
        {
            return _healRatePercent * _currentTargetCount;
        }

        public override double GetIncrementPercentADReduce()
        {
            return _adReduceApReduceRatePercent * _currentTargetCount;
        }
        
        public override double GetIncrementPercentAPReduce()
        {
            return _adReduceApReduceRatePercent * _currentTargetCount;
        }
    }
}//117523403
