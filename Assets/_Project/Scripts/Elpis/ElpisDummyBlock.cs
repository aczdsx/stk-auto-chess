using CookApps.TeamBattle;
using PrimeTween;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
