using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.SampleTeamBattle
{
    public static class InGameResourceHolder
    {
        public static GameObject StagePrefab { get; private set; }
        public static Dictionary<int, GameObject> PlayerCharacterPrefabs { get; private set; } = new ();
        public static Dictionary<int, GameObject> EnemyCharacterPrefabs { get; private set; } = new ();

        public static async UniTask LoadResources(int chapter, int stageIdx)
        {
            SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIdx);
            // load stage
            StagePrefab = await AddressableLoadHelper.LoadAssetAsync<GameObject>($"Prefabs/Stages/Stage{chapter}.prefab");
            // load player character
            List<int> deckCharacIds = ListPool<int>.Get();
            deckCharacIds.AddRange(UserDataManager.UserDeck.GetFront());
            deckCharacIds.AddRange(UserDataManager.UserDeck.GetMid());
            deckCharacIds.AddRange(UserDataManager.UserDeck.GetBack());
            foreach (int characId in deckCharacIds)
            {
                PlayerCharacterPrefabs.Add(characId, await AddressableLoadHelper.LoadAssetAsync<GameObject>($"Prefabs/Characters/{characId}.prefab"));
            }

            // load enemy character
            deckCharacIds.Clear();
            deckCharacIds.AddRange(specStage.GetFront().Select(x => x.id));
            deckCharacIds.AddRange(specStage.GetMid().Select(x => x.id));
            deckCharacIds.AddRange(specStage.GetBack().Select(x => x.id));
            foreach (int characId in deckCharacIds)
            {
                EnemyCharacterPrefabs.Add(characId, await AddressableLoadHelper.LoadAssetAsync<GameObject>($"Prefabs/Characters/{characId}.prefab"));
            }

            ListPool<int>.Release(deckCharacIds);
        }

        public static void UnloadResources()
        {
            // unload stage
            AddressableLoadHelper.ReleaseLoadedAsset(StagePrefab);
            StagePrefab = null;
            // unload player character
            foreach ((int _, GameObject prefab) in PlayerCharacterPrefabs)
            {
                AddressableLoadHelper.ReleaseLoadedAsset(prefab);
            }

            PlayerCharacterPrefabs = null;
            // unload enemy character
            foreach ((int _, GameObject prefab) in EnemyCharacterPrefabs)
            {
                AddressableLoadHelper.ReleaseLoadedAsset(prefab);
            }

            EnemyCharacterPrefabs = null;
        }
    }
}
