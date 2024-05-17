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
        public static Dictionary<int, GameObject> PlayerCharacterPrefabs { get; private set; } = new ();
        public static Dictionary<int, GameObject> EnemyCharacterPrefabs { get; private set; } = new ();

        public static HpBarView HpBarView = null;

        public static async UniTask LoadResources(int chapter, int stageIdx)
        {
            GameObject hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>($"FloatingHpBar.prefab");
            HpBarView = hpBarPrefab.GetComponent<HpBarView>();

            SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIdx);
            // load stage
            StagePrefab = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/Stages/Stage{chapter}.prefab");
            // load player character
            List<int> deckCharacIds = ListPool<int>.Get();
            deckCharacIds.Add(30001);
            // deckCharacIds.AddRange(UserDataManager.Instance.GetFront());
            // deckCharacIds.AddRange(UserDataManager.Instance.GetMid());
            // deckCharacIds.AddRange(UserDataManager.Instance.GetBack());PlayerCharacterPrefabs
            foreach (int characId in deckCharacIds)
            {
                PlayerCharacterPrefabs.Add(characId, await Addressables.LoadAssetAsync<GameObject>($"Characters/{characId}/{characId}.prefab"));
            }
            // load enemy character
            deckCharacIds.Clear();
            deckCharacIds.Add(30002);
            // deckCharacIds.AddRange(specStage.GetFront().Select(x => x.id));
            // deckCharacIds.AddRange(specStage.GetMid().Select(x => x.id));
            // deckCharacIds.AddRange(specStage.GetBack().Select(x => x.id));
            foreach (int characId in deckCharacIds)
            {
                EnemyCharacterPrefabs.Add(characId, await Addressables.LoadAssetAsync<GameObject>($"Characters/{characId}/{characId}.prefab"));
            }

            ListPool<int>.Release(deckCharacIds);
        }

        public static void UnloadResources()
        {
            Addressables.Release(HpBarView);
            // unload stage
            Addressables.Release(StagePrefab);
            StagePrefab = null;
            // unload player character
            foreach ((int _, GameObject prefab) in PlayerCharacterPrefabs)
            {
                Addressables.Release(prefab);
            }

            PlayerCharacterPrefabs = null;
            // unload enemy character
            foreach ((int _, GameObject prefab) in EnemyCharacterPrefabs)
            {
                Addressables.Release(prefab);
            }

            EnemyCharacterPrefabs = null;
        }
    }
}
