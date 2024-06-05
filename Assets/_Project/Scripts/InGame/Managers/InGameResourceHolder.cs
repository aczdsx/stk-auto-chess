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
        public static InGameTextView InGameText = null;
        // [TODO] specStage 관리 어디서 할까요?
        public static SpecStage SpecStage = null;

        public static async UniTask LoadResources(int chapter, int stageIdx, DifficultyType difficultyType)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            GameObject ingameTextPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/DamageText.prefab");
            InGameText = ingameTextPrefab.GetComponent<InGameTextView>();

            SpecStage = SpecDataManager.Instance.GetStageData(chapter, stageIdx, difficultyType);
            StagePrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Stage{chapter}.prefab");
        }

        public static void UnloadResources()
        {
            Addressables.Release(HpBarView.gameObject);
            Addressables.Release(InGameText.gameObject);
            // unload stage
            Addressables.Release(StagePrefab);
            StagePrefab = null;
            SpecStage = null;
        }
    }
}
