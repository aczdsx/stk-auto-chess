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

        [SerializeField] private float _normalDuration = 1.3f;
        [SerializeField] private Ease _normalEase;

        [SerializeField] private float _critDuration = 1.3f;
        [SerializeField] private Ease _critEase;

        private TMP_Text _damageText;
        private Ease _ease;
        private float _duration;

        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            CachedGo.SetActive(true);

            _damageObj.SetActive(!isCritical);
            _critDamageSpriteRenderer.gameObject.SetActive(isCritical);
            _damageText = (isCritical) ? _textCritDamage : _txtDamage;
            _duration = (isCritical) ? _critDuration : _normalDuration;
            _ease = (isCritical) ? _critEase : _normalEase;
            _damageText.text = $"{damage}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            Vector3 targetPosition = initialPosition + Vector3.up * 0.8f;

            var tcs = new UniTaskCompletionSource();

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (value) =>
                {
                    if (_root)
                    {
                        _root.position = value;
                        if (isCritical)
                        {
                            var color = _textCritDamage.color;
                            color.a = 1 - (value.y - initialPosition.y) / (targetPosition.y - initialPosition.y);
                            _textCritDamage.color = color;

                            var color1 = _critDamageSpriteRenderer.color;
                            color1.a = color.a;
                            _critDamageSpriteRenderer.color = color1;
                        }
                        else
                        {
                            var color = _txtDamage.color;
                            color.a = 1 - (value.y - initialPosition.y) / (targetPosition.y - initialPosition.y);
                            _txtDamage.color = color;
                        }
                    }
                },
                duration: _duration,
                ease: _ease
            ).OnComplete(this, target => tcs.TrySetResult());

            await tcs.Task;
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            CachedGo.SetActive(true);
            _damageText.text = $"Heal Amount : {healAmount}";

            Vector3 initialPosition = position + Vector3.up * characterHeight;
            _root.position = initialPosition;

            Vector3 targetPosition = initialPosition + Vector3.up * 0.5f;

            Tween.Custom(
                initialPosition,
                targetPosition,
                onValueChange: (Vector3 value) => { _root.position = value; },
                duration: _duration,
                ease: _ease
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
