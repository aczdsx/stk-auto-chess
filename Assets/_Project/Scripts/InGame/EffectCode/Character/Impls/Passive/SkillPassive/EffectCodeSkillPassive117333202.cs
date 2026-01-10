using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 에이프릴릴 패시브
    /// 대상: 자기 자신
    /// 에이프릴이 공격위치를 유지할수록 {0}초당 공격속도가 {1}% 상승합니다. 이 효과는 최대 60%를 넘길 수 없습니다.
    /// 이동할 경우 이 증가분은 천천히 하락합니다.
    /// </summary>
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeSkillPassive117333202 : EffectCodeSkillPassiveBase
    {
        public const int CodeId = 117333202;

        private float _increaseTime;
        private float _elapsedTime;
        private float _attackSpeedIncreaseRate;
        private float _currentAttackSpeedIncreaseRate;

        private static readonly float _maxAttackSpeedIncreaseRate = 0.6f;

        private InGameTile _prevTile;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _increaseTime = codeInfo.GetCodeStatToFloat(0);
            _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _currentAttackSpeedIncreaseRate = 0f;
            _elapsedTime = 0f;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _increaseTime = codeInfo.GetCodeStatToFloat(0);
            _attackSpeedIncreaseRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        }


        public override void OnCombatStart()
        {
            _prevTile = owner.CurrentTile;
        }

        public override void OnUpdate(float dt)
        {
            if (_prevTile == null || owner.CurrentTile == null)
                return;

            _elapsedTime += dt;
            if (_elapsedTime < _increaseTime)
                return;

            _elapsedTime = 0f;
            var prevAttackSpeedIncreaseRate = _currentAttackSpeedIncreaseRate;

            if (_prevTile == owner.CurrentTile)
            {
                _currentAttackSpeedIncreaseRate += _attackSpeedIncreaseRate;
            }
            else
            {
                _currentAttackSpeedIncreaseRate -= _attackSpeedIncreaseRate * 0.5f;
            }

            _currentAttackSpeedIncreaseRate = Mathf.Clamp(_currentAttackSpeedIncreaseRate, 0f, _maxAttackSpeedIncreaseRate);
            if(prevAttackSpeedIncreaseRate != _currentAttackSpeedIncreaseRate)
            {
                owner.GetEffectCodeContainer().SetDirtyFlag(this);
            }

            _prevTile = owner.CurrentTile;
        }

        public override float GetIncrementPercentAttackSpeed()
        {
            return _currentAttackSpeedIncreaseRate;
        }
    }
}//117333202
