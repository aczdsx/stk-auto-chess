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
        
        private async UniTask LoadElpis()
        {
            await NetManager.Instance.WaitForElpisInitializationAsync();
            elpisMainBlockHandle = Addressables.InstantiateAsync("Elpis/MainBlock.prefab");
            elpisBgHandle = Addressables.InstantiateAsync("Elpis/BG.prefab");
            await elpisMainBlockHandle.WaitUntilDone();
            await elpisBgHandle.WaitUntilDone();

            MainBlock = elpisMainBlockHandle.Result.GetComponent<ElpisMainBlock>();

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
