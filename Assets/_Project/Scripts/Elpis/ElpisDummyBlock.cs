using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
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
                    LMotion.Create(CachedTr.localPosition, blockPosRot.Pos, 2f)
                        .WithEase(Ease.Linear).BindToLocalPosition(CachedTr).AddTo(this).ToUniTask(),
                    LMotion.Create(CachedTr.localRotation, blockPosRot.Rot, 2f)
                        .WithEase(Ease.Linear).Bind(v => CachedTr.localRotation = v).AddTo(this).ToUniTask()
                );
                
                await UniTask.Delay(500);
            }

            CachedGo.SetActive(false);
        }
    }
}
