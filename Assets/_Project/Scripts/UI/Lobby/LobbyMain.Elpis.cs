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

        private async UniTask LoadElpis()
        {
            elpisDataBridge = new ElpisDataBridge();
            await NetManager.Instance.WaitForElpisInitializationAsync();
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
            
            CreateWorldInteractionSlots(MainBlock.ElpisBuildings);
            
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
            // 함선 렙업 확장 연출
            if (info.Current.Type == ElpisFacilityType.FacilityTypeCommandCenter)
            {
                for (var i = 0; i < SpecDataManager.Instance.ElpisCommandCenterBenefit.All.Count; i++)
                {
                    var benefit = SpecDataManager.Instance.ElpisCommandCenterBenefit[i];
                    if (benefit.lv == info.Current.Level)
                    {
                        // TODO: Focusing
                        await MainBlock.AttachSubBlock(benefit.benefit_key, true);
                        MainBlock.RebuildNavMesh();
                        return;
                    }
                }
            }
        }
    }
}
