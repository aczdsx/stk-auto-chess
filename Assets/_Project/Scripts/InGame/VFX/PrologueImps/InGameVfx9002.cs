using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Unity.VisualScripting;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfx9002 : InGameVfx
    {
        [SerializeField] private GameObject _orbGroup;
        [SerializeField] private GameObject _explosion;
        [SerializeField] private AnimationCurve _curve;
        [SerializeField] private float _duration = 1f;

        private float _elapsedTime;
        private bool _isPlaying = true;

        public float CurrentValue { get; private set; }

        public override void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            base.Initialize(isFlipX, movementBase);
            _elapsedTime = 0f;
            CurrentValue = _curve?.Evaluate(0f) ?? 0f;
        }

        public override void ManagedUpdate(float dt)
        {
            base.ManagedUpdate(dt);
            if (!_isPlaying){
                return;
            }
            _elapsedTime += dt;
            float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);
            CurrentValue = _curve?.Evaluate(normalizedTime) ?? 0f;

            OnCurveUpdate(CurrentValue);

            if (_elapsedTime >= _duration)
            {
                _isPlaying = false;
                OnCurveComplete();
            }
        }

        protected virtual void OnCurveUpdate(float value)
        {
            // 커브 값에 따라 업데이트 처리
            _orbGroup.transform.localPosition = Vector3.up * value;
        }

        protected virtual void OnCurveComplete()
        {
            // 커브 완료 시 처리
            _orbGroup.SetActive(false);
            _explosion.SetActive(true);
        }

        public override void Clear()
        {
            base.Clear();
            _elapsedTime = 0f;
            _isPlaying = false;
        }

        public void Play()
        {
            _elapsedTime = 0f;
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
        }
    }
}