using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class InGameTextView : CachedMonoBehaviour, ITextView
    {
        // [SerializeField] private TMP_Text txtDamage;
        // private Sequence _damageTweener;
        // [SerializeField] private Transform _root;

        public UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            throw new NotImplementedException();
        }

        public UniTask ShowHealText(Vector3 position, float characterHeight, double damage)
        {
            throw new NotImplementedException();
        }
    }

    public class InGameTextViewPool : ITextViewPool
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            TextViewPool.Initialize(new InGameTextViewPool());
        }

        private UnityPool<InGameTextView> textViewPool;

        public async UniTask InitializePool()
        {
            // TODO: load prefab from addressable
            // textViewPool.Initialize(prefab);
        }

        public void ReleasePool()
        {
            textViewPool.ClearPool();
            textViewPool = null;
        }

        public ITextView GetDamageTextView()
        {
            return textViewPool.Get(null);
        }

        public void ReturnDamageTextView(ITextView textView)
        {
            var view = textView as InGameTextView;
            if (view == null)
            {
                throw new Exception("Invalid type of ITextView");
            }

            textViewPool.Return(view);
        }
    }
}
