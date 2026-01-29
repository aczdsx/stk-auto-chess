using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Prototypes.Movement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class ElpisMainBlock : CachedMonoBehaviour
    {
        [Serializable]
        public class SubBlockInfo
        {
            [SerializeField] private Vector3 worldPointSrc;
            [SerializeField] private Vector3 worldPointDest;
            [SerializeField] private Quaternion rotation;
            [SerializeField] private AssetReferenceGameObject assetRef;
            
            private AsyncOperationHandle<GameObject> loadedHandle;

            public ElpisSubBlock SubBlock { get; private set; }
            public Vector3 LastAnimationPosition => worldPointDest;

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

                await dummyBlock.AnimateExit();
                SubBlock.CachedTr.position = worldPointSrc;
                await LMotion.Create(SubBlock.CachedTr.position, worldPointDest, 3f)
                    .WithEase(Ease.OutCubic)
                    .BindToPosition(SubBlock.CachedTr)
                    .ToUniTask();
            }
        }

        [SerializeField] private GameObject walkPath;
        [SerializeField] private ElpisDummyBlock[] dummySubBlocks;
        [SerializeField] private SubBlockInfo[] subBlockInfos;
        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private ElevatorLink[] elevatorLinks;
        [SerializeField] private ElpisBuildingBase[] elpisBuildings;

        private ElpisBuildingBase[] cachedElpisBuildings;
        public IReadOnlyList<ElpisBuildingBase> ElpisBuildings => cachedElpisBuildings;
        public int MainBlockBuildingCount => elpisBuildings.Length;
        
        private void Awake()
        {
            walkPath.layer = LayerMask.NameToLayer("ElpisGround");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                subBlockInfos[i].UnloadAddressable();
            }
        }

        public SubBlockInfo GetSubBlockInfo(int index)
        {
            return subBlockInfos[index];
        }

        public async UniTask AttachSubBlock(int index, bool withAnimation)
        {
            var dummyBlock = index < dummySubBlocks.Length ? dummySubBlocks[index] : null;
            subBlockInfos[index].SubBlock.gameObject.SetActive(true);
            await subBlockInfos[index].MoveBlock(dummyBlock, withAnimation);

            elevatorLinks[index].ActivateElevator();
        }
        
        public async UniTask LoadAllSubBlocks()
        {
            // 크기가 고정된 배열 사용으로 GC 최적화
            var loadTasks = new UniTask[subBlockInfos.Length];
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                loadTasks[i] = subBlockInfos[i].InstantiateAsync();
            }

            await UniTask.WhenAll(loadTasks);

            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                subBlockInfos[i].SubBlock.gameObject.SetActive(false);
            }

            // 전체 건물 수 미리 계산하여 capacity 설정
            var totalBuildingCount = elpisBuildings.Length;
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                totalBuildingCount += subBlockInfos[i].SubBlock.ElpisBuildings.Count;
            }

            var allBuildings = new List<ElpisBuildingBase>(totalBuildingCount);
            for (var i = 0; i < elpisBuildings.Length; i++)
            {
                if (elpisBuildings[i] != null)
                    allBuildings.Add(elpisBuildings[i]);
            }
            for (var i = 0; i < subBlockInfos.Length; i++)
            {
                var subBuildings = subBlockInfos[i].SubBlock.ElpisBuildings;
                for (var j = 0; j < subBuildings.Count; j++)
                {
                    if (subBuildings[j] != null)
                        allBuildings.Add(subBuildings[j]);
                }
            }

            cachedElpisBuildings = allBuildings.ToArray();
            for (var i = 0; i < cachedElpisBuildings.Length; i++)
            {
                cachedElpisBuildings[i].Initialize(i);
            }
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
