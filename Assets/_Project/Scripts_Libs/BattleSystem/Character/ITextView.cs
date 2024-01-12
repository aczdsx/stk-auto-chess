using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface ITextView : ICachedGameObject, ICachedTransform
    {
        UniTask ShowDamageText(Vector3 position, float characterHeight, double damage, bool isCritical, bool isDoubleCritical);
        UniTask ShowHealText(Vector3 position, float characterHeight, double damage);
    }

    public interface ITextViewPool
    {
        UniTask<ITextView> GetDamageTextView();
        void ReturnDamageTextView(ITextView textView);
    }

    public static class TextViewPool
    {
        private static ITextViewPool instance;

        public static ITextViewPool Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("TextViewPool is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(ITextViewPool instance)
        {
            TextViewPool.instance = instance;
        }
    }
}
