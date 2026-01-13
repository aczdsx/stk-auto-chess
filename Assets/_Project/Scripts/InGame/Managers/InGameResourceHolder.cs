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
        public static InGameBuffDebuff InGameBuffDebuff = null;
        public static InGameType InGameType;

        public static async UniTask LoadResources(InGameType inGameType, IGameStateUICore gameStateUI, int id)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            GameObject ingameTextPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/DamageText.prefab");
            InGameText = ingameTextPrefab.GetComponent<InGameTextView>();

            GameObject inGameBuffDebuffPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/BuffIcon.prefab");
            InGameBuffDebuff = inGameBuffDebuffPrefab.GetComponent<InGameBuffDebuff>();

            InGameType = inGameType;

            Debug.LogColor($"LoadResources: {inGameType} id: {id}", "blue");

            if (inGameType == InGameType.STAGE)
            {
                var stageData = SpecDataManager.Instance.GetStageData(id);
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Stage{stageData.chapter_id}.prefab");
            }
            else if (inGameType == InGameType.TRIAL)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Trial2.prefab");
            }
            else if (inGameType == InGameType.TRIAL_BOSS)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Trial1.prefab");
            }
            else if (inGameType == InGameType.PVP_DEFENSE)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/PvpSetting.prefab");
            }
            else if (inGameType == InGameType.PVP)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/PVP.prefab");
            }
            else if (inGameType == InGameType.PROLOGUE)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/Prologue.prefab");
            }
            else if (inGameType == InGameType.TEST)
            {
                StagePrefab =
                    await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Ingame/InGameTest.prefab");
            }
        }

        public static async UniTask LoadBattleReadyResources(int chapter)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            GameObject ingameTextPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/DamageText.prefab");
            InGameText = ingameTextPrefab.GetComponent<InGameTextView>();

            GameObject inGameBuffDebuffPrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/InGame/BuffIcon.prefab");
            InGameBuffDebuff = inGameBuffDebuffPrefab.GetComponent<InGameBuffDebuff>();

            //[TODO] 아웃게임 챕터 불러오기
            StagePrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Outgame/Outgame_Stage_{chapter}.prefab");
        }

        public static void UnloadResources()
        {
            Addressables.Release(HpBarView.gameObject);
            Addressables.Release(InGameText.gameObject);
            Addressables.Release(InGameBuffDebuff.gameObject);
            // unload stage
            Addressables.Release(StagePrefab);
            StagePrefab = null;
            // SpecStage = null;
        }

    }
}
