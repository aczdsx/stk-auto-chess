using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CookApps.AutoBattler
{
    public class InGameTextView : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text _txtDamage;
        [SerializeField] private TMP_Text _textCritDamage;
        [SerializeField] private TMP_Text _healDamage;

        [SerializeField] private Transform _root;
        [SerializeField] private Animator _animator;

        [SerializeField] private float _heightOffset = 0.3f;
        [SerializeField] private float _xOffset = 0.0f;

        private TMP_Text _damageText;
        private static readonly int Critical = Animator.StringToHash("Critical");
        private static readonly int Normal = Animator.StringToHash("Normal");
        private static readonly int Heal = Animator.StringToHash("Heal");

        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            _damageText = (isCritical) ? _textCritDamage : _txtDamage;
            _damageText.text = $"{damage}";

            _xOffset = Random.Range(-0.5f, 0.5f);

            Vector3 initialPosition = position + Vector3.up * (characterHeight  + _heightOffset);
            initialPosition.x += _xOffset;
            _root.position = initialPosition;
            _animator.SetTrigger(isCritical ? Critical : Normal);

            if (isCritical)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_normal1);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_critical1);
            }

            // await WaitForAnimationEnd();
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            _damageText = _healDamage;
            _damageText.text = $"+{healAmount}";

            _xOffset = Random.Range(-0.5f, 0.5f);

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            initialPosition.x += _xOffset;
            _root.position = initialPosition;
            _animator.SetTrigger(Heal);

            // await WaitForAnimationEnd();
        }

        public void ReturnTextView()
        {
            InGameTextViewPool.Instance.Return(this);
        }

        // private async UniTask WaitForAnimationEnd()
        // {
        //     AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        //     await UniTask.WaitUntil(() => stateInfo.normalizedTime >= 1f);
        // }
    }

    public class InGameTextViewPool : Singleton<InGameTextViewPool>
    {
        private UnityPool<InGameTextView> _textViewPool;
        private GameObject _instance;

        public void InitializePool(GameObject instance)
        {
            _instance = instance;
            _textViewPool = new UnityPool<InGameTextView>();
            _textViewPool.Initialize(_instance);
        }

        public void ReleasePool()
        {
            _textViewPool.ClearPool();
            _textViewPool = null;
        }

        public InGameTextView Get()
        {
            return _textViewPool.Get(null);
        }

        public void Return(InGameTextView textView)
        {
            if (_textViewPool != null)
                _textViewPool.Return(textView);
        }
    }
}
