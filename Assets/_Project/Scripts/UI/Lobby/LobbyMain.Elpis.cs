using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
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
        private List<AsyncOperationHandle<GameObject>> characterHandles = new ();
        
        private async UniTask LoadElpis()
        {
            await NetManager.Instance.WaitForElpisInitializationAsync();
            elpisMainBlockHandle = Addressables.InstantiateAsync("Elpis/MainBlock.prefab");
            elpisBgHandle = Addressables.InstantiateAsync("Elpis/BG.prefab");
            await elpisMainBlockHandle.WaitUntilDone();
            await elpisBgHandle.WaitUntilDone();

            MainBlock = elpisMainBlockHandle.Result.GetComponent<ElpisMainBlock>();

            MainBlock.RebuildNavMesh();
            var characterHandle = Addressables.InstantiateAsync("SD_Characters/17513401/Elpis_17513401.prefab", new Vector3(-5, 0, -5), Quaternion.identity);
            characterHandles.Add(characterHandle);
            await characterHandle.WaitUntilDone();
            // var commandCenter = elpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            // if (commandCenter.Level >= 2)
            // {
            //     
            // }
        }
        
        public void UnloadElpis()
        {
            
        }
    }
}
