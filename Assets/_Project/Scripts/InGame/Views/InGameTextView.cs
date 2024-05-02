using System;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class InGameTextView : CachedMonoBehaviour
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

    public class InGameTextViewPool : Singleton<InGameTextViewPool>
    {
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

        public InGameTextView GetDamageTextView()
        {
            return textViewPool.Get(null);
        }

        public void ReturnDamageTextView(InGameTextView textView)
        {
            if (textView == null)
            {
                throw new Exception("Can't return null textView");
            }

            textViewPool.Return(textView);
        }
    }
}
