using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisDummyBlock : CachedMonoBehaviour
    {
        [SerializeField] private Vector3 exitPoint;

        public async UniTask AnimateExit()
        {
            await Tween.LocalPosition(CachedTr, exitPoint, 1f, Ease.InCirc);
            CachedGo.SetActive(false);
        }
    }
}
