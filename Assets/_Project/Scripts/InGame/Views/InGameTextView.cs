using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameTextView : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text txtDamage;
        [SerializeField] private Transform _root;

        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            CachedGo.SetActive(true);
            txtDamage.text = isDoubleCritical ? $"Double Critical! {damage}" : isCritical ? $"Critical! {damage}" : $"{damage}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            Vector3 targetPosition = initialPosition + Vector3.up * 1.0f;

            var tcs = new UniTaskCompletionSource();

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (value) =>
                {
                    if (_root)
                        _root.position = value;
                },
                duration: 1f,
                ease: Ease.OutCubic
            ).OnComplete(this, target => tcs.TrySetResult());

            await tcs.Task;
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            CachedGo.SetActive(true);
            txtDamage.text = $"Heal Amount : {healAmount}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            Vector3 targetPosition = initialPosition + Vector3.up * 0.5f;

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (Vector3 value) => { _root.position = value; },
                duration: 1f,
                ease: Ease.OutCubic
            ).OnComplete(this, target => CachedGo.SetActive(false));
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

        public InGameTextView GetDamageTextView()
        {
            return _textViewPool.Get(null);
        }

        public void ReturnDamageTextView(InGameTextView textView)
        {
            if (_textViewPool != null)
                _textViewPool.Return(textView);
        }
    }
}
