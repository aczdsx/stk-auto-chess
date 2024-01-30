using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class InGameTextView : CachedMonoBehaviour, ITextView
    {
        public UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical)
        {
            throw new System.NotImplementedException();
        }

        public UniTask ShowHealText(Vector3 position, float characterHeight, double damage)
        {
            throw new System.NotImplementedException();
        }
    }

    public class InGameTextViewPool : ITextViewPool
    {
        public UniTask<ITextView> GetDamageTextView()
        {
            throw new System.NotImplementedException();
        }

        public void ReturnDamageTextView(ITextView textView)
        {
            throw new System.NotImplementedException();
        }
    }
}
