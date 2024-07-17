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

        public static async UniTask LoadResources(IGameStateUI gameStateUI, int id)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            GameObject ingameTextPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/DamageText.prefab");
            InGameText = ingameTextPrefab.GetComponent<InGameTextView>();

            if (gameStateUI is InGameMainStateUIStageUI)
            {
                var stageData = SpecDataManager.Instance.GetStageData(id);
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Stage{stageData.chapter_id}.prefab");
            }
            else if (gameStateUI is InGameMainStateTrialDungeonUI)
            {
                var dungeonData = SpecDataManager.Instance.GetSpecDungeonTrialData(id);
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Stage{dungeonData.dungeon_id}.prefab");
            }
        }

        public static async UniTask LoadLobbyResources(int chapter)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            GameObject ingameTextPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/DamageText.prefab");
            InGameText = ingameTextPrefab.GetComponent<InGameTextView>();

            //[TODO] 아웃게임 챕터 불러오기
            StagePrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Outgame/Outgame_Stage_{chapter}.prefab");
        }

        public static void UnloadResources()
        {
            Addressables.Release(HpBarView.gameObject);
            Addressables.Release(InGameText.gameObject);
            // unload stage
            Addressables.Release(StagePrefab);
            StagePrefab = null;
            // SpecStage = null;
        }
    }
}
