using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    public class InGameTextView : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text _txtDamage;
        [SerializeField] private TMP_Text _textCritDamage;

        [SerializeField] private GameObject _damageObj;
        [SerializeField] private SpriteRenderer _critDamageSpriteRenderer;

        [SerializeField] private Transform _root;

        [SerializeField] private Animator _animator;

        private TMP_Text _damageText;
        private Ease _ease;
        private float _duration;
        private static readonly int IsCritical = Animator.StringToHash("IsCritical");

        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            CachedGo.SetActive(true);

            _damageObj.SetActive(!isCritical);
            _critDamageSpriteRenderer.gameObject.SetActive(isCritical);
            _damageText = (isCritical) ? _textCritDamage : _txtDamage;
            _damageText.text = $"{damage}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            _animator.SetBool(IsCritical, isCritical);
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            CachedGo.SetActive(true);
            _damageText.text = $"Heal Amount : {healAmount}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            _animator.SetBool(IsCritical, false);
        }
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
