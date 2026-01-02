using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Unity.AI.Navigation;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Prototypes.Movement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class ElpisMainBlock : CachedMonoBehaviour
    {
        [Serializable]
        private class SubBlockInfo
        {
            [SerializeField] private Vector3 worldPointSrc;
            [SerializeField] private Vector3 worldPointDest;
            [SerializeField] private Quaternion rotation;
            [SerializeField] private AssetReferenceGameObject assetRef;
            private AsyncOperationHandle<GameObject> loadedHandle;
            public ElpisSubBlock SubBlock { get; private set; }

            public async UniTask InstantiateAsync()
            {
                if (loadedHandle.IsValid())
                    return;

                loadedHandle = assetRef.InstantiateAsync();
                await loadedHandle.WaitUntilDone();
                if (!loadedHandle.IsValid())
                    return;

                SubBlock = loadedHandle.Result.GetComponent<ElpisSubBlock>();
                SubBlock.CachedTr.rotation = rotation;
            }

            public void UnloadAddressable()
            {
                if (loadedHandle.IsValid())
                {
                    loadedHandle.Release();
                    SubBlock = null;
                }
            }

            public async UniTask MoveBlock(ElpisDummyBlock dummyBlock, bool withAnimation)
            {
                if (!withAnimation)
                {
                    dummyBlock.CachedGo.SetActive(false);
                    SubBlock.CachedTr.position = worldPointDest;
                    return;
                }

                dummyBlock.AnimateExit().Forget();
                SubBlock.CachedTr.position = worldPointSrc;
                var tween = Tween.Position(SubBlock.CachedTr, worldPointDest, 2f, Ease.OutCirc);
                await tween;
            }
        }

        [SerializeField] private GameObject walkPath;
        [SerializeField] private ElpisDummyBlock[] dummySubBlocks;
        [SerializeField] private SubBlockInfo[] subBlockInfos;
        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private ElevatorLink[] elevatorLinks;

        private void Awake()
        {
            walkPath.layer = LayerMask.NameToLayer("ElpisGround");
            walkPath.AddComponent<BoxCollider>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                subBlockInfos[i].UnloadAddressable();
            }
        }

        public async UniTask AttachSubBlock(int index, bool withAnimation)
        {
            var token = destroyCancellationToken;
            var task = subBlockInfos[index].InstantiateAsync();
            await task;
            if (token.IsCancellationRequested)
                return;

            var dummyBlock = index < dummySubBlocks.Length ? dummySubBlocks[index] : null;
            await subBlockInfos[index].MoveBlock(dummyBlock, withAnimation);

            elevatorLinks[index].ActivateElevator();
        }

        public void ReleaseSubBlocks()
        {
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                subBlockInfos[i].UnloadAddressable();
            }
        }

        public void RebuildNavMesh()
        {
            navMeshSurface.BuildNavMesh();
        }
    }
}
