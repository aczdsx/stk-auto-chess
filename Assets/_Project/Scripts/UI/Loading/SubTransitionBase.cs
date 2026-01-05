using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public abstract class SubTransitionBase : CachedMonoBehaviour
    {
        public abstract UniTask FadeInAsync();
        public abstract UniTask FadeOutAsync();
    }
}
