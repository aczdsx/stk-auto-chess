using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxArtesiaCharge : InGameVfx
    {
        [SerializeField] private GameObject _buff;
        [SerializeField] private GameObject _explosion;

        private bool _isFinished;
        private bool _isTriggered;

        public bool IsFinished => _isFinished;

        public override void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            base.Initialize(isFlipX, movementBase);
            _isFinished = false;
            _isTriggered = false;
            _buff.SetActive(true);
            _explosion.SetActive(false);
        }

        public void TriggerExplosion()
        {
            if (_isTriggered) return;

            _isTriggered = true;
            _buff.SetActive(false);
            _explosion.SetActive(true);
            _isFinished = true;
        }

        public override void Clear()
        {
            base.Clear();
            _isFinished = false;
            _isTriggered = false;
        }
    }
}