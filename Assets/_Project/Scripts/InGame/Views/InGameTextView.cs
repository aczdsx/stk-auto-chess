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

        // [TODO] inGame 쪽 ui 관리 방법 필요
        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            CachedGo.SetActive(true);
            txtDamage.text = isDoubleCritical ? $"Double Critical! {damage}" : isCritical ? $"Critical! {damage}" : $"{damage}";

            Vector3 worldPosition = position + Vector3.up * characterHeight;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            _root.position = screenPosition;

            Vector3 initialPosition = _root.position;
            Vector3 targetPosition = initialPosition + Vector3.up * 50;

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (Vector2 value) => { CachedGo.transform.position = value; },
                duration: 1f,
                ease: Ease.OutCubic
            ).OnComplete(this, target => CachedGo.SetActive(false));
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            CachedGo.SetActive(true);
            txtDamage.text = $"Heal Amount : {healAmount}";

            Vector3 worldPosition = position + Vector3.up * characterHeight;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            _root.position = screenPosition;

            Vector3 initialPosition = _root.position;
            Vector3 targetPosition = initialPosition + Vector3.up * 50;

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (Vector2 value) => { CachedGo.transform.position = value; },
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
            if (textView == null)
            {
                throw new Exception("Can't return null textView");
            }

            _textViewPool.Return(textView);
        }
    }
}
