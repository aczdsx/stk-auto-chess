using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisDummyBlock : CachedMonoBehaviour
    {
        [SerializeField] private BlockPosRot[] blockPosRots;

        [System.Serializable] 
        public class BlockPosRot
        {
            public Vector3 Pos;
            public Quaternion Rot;
        }

        public void SetExitPoint()
        {
            CachedTr.position = blockPosRots[^1].Pos;
            CachedTr.rotation = blockPosRots[^1].Rot;
        }

        public async UniTask AnimateExit()
        {
            // blockPosRots 순서대로 Pos와 Rot 애니메이션
            foreach (var blockPosRot in blockPosRots)
            {
                await UniTask.WhenAll(
                    Tween.LocalPosition(CachedTr, blockPosRot.Pos, 2f, Ease.Linear).ToUniTask(),
                    Tween.LocalRotation(CachedTr, blockPosRot.Rot, 2f, Ease.Linear).ToUniTask()
                );
                
                await UniTask.Delay(500);
            }

            CachedGo.SetActive(false);
        }
    }
}
