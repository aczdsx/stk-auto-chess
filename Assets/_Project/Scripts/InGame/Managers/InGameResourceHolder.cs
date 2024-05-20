using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public static class InGameResourceHolder
    {
        public static GameObject StagePrefab { get; private set; }
        public static HpBarView HpBarView = null;

        public static async UniTask LoadResources(int chapter, int stageIdx)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            // SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIdx);
            // load stage
            StagePrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Stage{chapter}.prefab");
        }

        public static void UnloadResources()
        {
            Addressables.Release(HpBarView);
            // unload stage
            Addressables.Release(StagePrefab);
            StagePrefab = null;
        }
    }
}
