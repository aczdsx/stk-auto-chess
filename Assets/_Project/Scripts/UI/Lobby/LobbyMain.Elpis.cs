using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public partial class LobbyMain
    {
        public ElpisMainBlock MainBlock { get; private set; }
        private AsyncOperationHandle<GameObject> elpisMainBlockHandle;
        private AsyncOperationHandle<GameObject> elpisBgHandle;
        private List<AsyncOperationHandle<GameObject>> characterHandles = new();

        // GC 최적화: 재사용 컬렉션
        private readonly HashSet<int> _unlockedBuildIdsCache = new();
        private readonly List<ElpisBuildingBase> _unlockedBuildingsCache = new();

        private async UniTask LoadElpis()
        {
            elpisDataBridge = new ElpisDataBridge();
            await NetManager.Instance.WaitForElpisInitializationAsync();

            // 해금된 건물들을 0 레벨로 추가
            AddUnlockedFacilities();

            elpisMainBlockHandle = Addressables.InstantiateAsync("Elpis/MainBlock.prefab");
            elpisBgHandle = Addressables.InstantiateAsync("Elpis/BG.prefab");
            await elpisMainBlockHandle.WaitUntilDone();
            await elpisBgHandle.WaitUntilDone();

            MainBlock = elpisMainBlockHandle.Result.GetComponent<ElpisMainBlock>();

            var characterHandle = Addressables.InstantiateAsync("SD/17513401/Elpis_17513401.prefab", new Vector3(-5, 0, -5), Quaternion.identity);
            characterHandles.Add(characterHandle);
            await characterHandle.WaitUntilDone();
            var commandCenter = elpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            await MainBlock.LoadAllSubBlocks();
            
            if (commandCenter.Level >= 2)
            {
                await MainBlock.AttachSubBlock(0, false);
            }
            if (commandCenter.Level >= 3)
            {
                await MainBlock.AttachSubBlock(1, false);
            }

            elpisDataBridge.OnFacilityChanged
                .Where(info => info.IsLevelChanged)
                .SubscribeAwait(this, (info, self, _) => self.OnFacilityLevelChanged(info))
                .AddTo(this);

            CreateWorldInteractionSlots(GetUnlockedBuildings(commandCenter.Level));

            MainBlock.RebuildNavMesh();
        }

        public void UnloadElpis()
        {
            elpisMainBlockHandle.Release();
            elpisBgHandle.Release();
            for (var i = 0; i < characterHandles.Count; i++)
            {
                characterHandles[i].Release();
            }
            characterHandles.Clear();
        }

        private async UniTask OnFacilityLevelChanged(FacilityChangeInfo info)
        {
            // 커맨드 센터 레벨업 시 처리
            if (info.Current.Type == ElpisFacilityType.FacilityTypeCommandCenter)
            {
                // 새로 해금된 건물 추가
                AddUnlockedFacilities();
                // WorldInteraction 슬롯 갱신
                //CreateWorldInteractionSlots(GetUnlockedBuildings(info.Current.Level));
            }
        }

        private void AddUnlockedFacilities()
        {
            // 커맨드 센터 레벨 확인
            var commandCenter = elpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            if (commandCenter == null)
            {
                Debug.LogWarning("[LobbyMain] 커맨드 센터가 없습니다.");
                return;
            }

            int commandCenterLevel = (int)commandCenter.Level;

            // 현재 레벨에서 해금된 build_id 목록 수집
            _unlockedBuildIdsCache.Clear();

            _unlockedBuildIdsCache.Add((int)commandCenter.BuildId);
            
            var benefits = SpecDataManager.Instance.ElpisCommandCenterBenefit.All;
            for (var i = 0; i < benefits.Count; i++)
            {
                var benefit = benefits[i];
                if (benefit.lv <= commandCenterLevel && benefit.build_id > 0)
                {
                    _unlockedBuildIdsCache.Add(benefit.build_id);
                }
            }

            // 해금된 건물 중 설치되지 않은 건물들을 0 레벨로 추가
            var buildInfos = SpecDataManager.Instance.ElpisBuildInfo.All;
            for (var i = 0; i < buildInfos.Count; i++)
            {
                var buildInfo = buildInfos[i];

                // 1레벨 건물만 확인 (신규 설치 대상)
                if (buildInfo.build_lv != 1) continue;

                // 이미 설치된 건물인지 확인
                var existingFacility = elpisDataBridge.GetFacilityByType(buildInfo.facility_type.ToServerType());
                if (existingFacility != null) continue;

                // 0 레벨 건물 데이터 생성 (UI 표시용)
                var newFacility = new ElpisFacility
                {
                    BuildId = (uint)buildInfo.build_id,
                    Type = buildInfo.facility_type.ToServerType(),
                    Level = 0,
                    MaxLevel = (uint)GetMaxLevelForFacility(buildInfo.build_id),
                    GridX = buildInfo.slot_index, // 스펙 데이터의 slot_index 사용
                    GridY = 0,
                    BuiltAt = 0
                };

                // 서버 데이터 매니저에 추가
                ServerDataManager.Instance.Elpis.UpdateFacility(newFacility);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[LobbyMain] 해금된 건물 추가: {buildInfo.facility_type} (레벨 0, 슬롯: {buildInfo.slot_index})");
#endif
            }
        }

        private int GetMaxLevelForFacility(int buildId)
        {
            // build_group_id로 최대 레벨 확인
            var buildInfos = SpecDataManager.Instance.ElpisBuildInfo.All;
            int maxLevel = 1;
            for (var i = 0; i < buildInfos.Count; i++)
            {
                var info = buildInfos[i];
                if (info.build_id == buildId && info.build_lv > maxLevel)
                {
                    maxLevel = info.build_lv;
                }
            }
            return maxLevel;
        }

        /// <summary>
        /// 해금된 시설 데이터를 추가합니다.
        /// </summary>
        public void RefreshUnlockedFacilities()
        {
            AddUnlockedFacilities();
        }

        /// <summary>
        /// 월드 인터랙션 UI 슬롯을 갱신합니다.
        /// </summary>
        public void RefreshWorldInteractionSlots(uint commandCenterLevel)
        {
            CreateWorldInteractionSlots(GetUnlockedBuildings(commandCenterLevel));
        }

        private IReadOnlyList<ElpisBuildingBase> GetUnlockedBuildings(uint commandCenterLevel)
        {
            _unlockedBuildingsCache.Clear();

            // 메인 블럭 건물들은 항상 해금
            var allBuildings = MainBlock.ElpisBuildings;
            var mainBlockBuildingCount = MainBlock.MainBlockBuildingCount;

            for (var i = 0; i < mainBlockBuildingCount; i++)
            {
                _unlockedBuildingsCache.Add(allBuildings[i]);
            }

            // 서브블럭 건물들은 커맨드 센터 레벨에 따라 해금
            // 레벨 2 이상: 서브블럭 0 해금
            // 레벨 3 이상: 서브블럭 1 해금
            var unlockedSubBlockCount = commandCenterLevel >= 3 ? 2 : commandCenterLevel >= 2 ? 1 : 0;

            for (var subBlockIndex = 0; subBlockIndex < unlockedSubBlockCount; subBlockIndex++)
            {
                var subBlock = MainBlock.GetSubBlockInfo(subBlockIndex).SubBlock;
                if (subBlock == null) continue;

                var subBlockBuildings = subBlock.ElpisBuildings;
                for (var i = 0; i < subBlockBuildings.Count; i++)
                {
                    _unlockedBuildingsCache.Add(subBlockBuildings[i]);
                }
            }

            return _unlockedBuildingsCache;
        }
    }
}
