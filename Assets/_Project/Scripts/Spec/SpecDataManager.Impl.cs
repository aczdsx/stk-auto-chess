// #define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.gRPC.Universal;
using CookApps.LocalData;
#endif
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class SpecDataManager : SingletonMonoBehaviour<SpecDataManager>
    {
        public async UniTask Initialize()
        {
            await UniTask.Yield();
#if USE_SERVER_SPEC
        var localData = new CookAppsLocalData(SecretKey.GetKey());
        string json;
        if (UniversalGrpcManager.Instance.IsSpecUpdated)
        {
            do
            {
                json = await UniversalGrpcManager.Instance.GetSpecDataAsync();
            } while (string.IsNullOrEmpty(json));

            localData.SaveString(json, "SpecData");
        }
        else
        {
            if (!localData.TryLoadString("SpecData", out json))
            {
                do
                {
                    json = await UniversalGrpcManager.Instance.GetSpecDataAsync();
                } while (string.IsNullOrEmpty(json));

                localData.SaveString(json, "SpecData");
            }
        }
#else
            string json = SpecDataResourceLoader.LoadSpecData();
            await UniTask.Yield();
#endif
            Load(json);
            await UniTask.Yield();
            CustomizeSpecData();
        }

        private Dictionary<int, List<SpecStage>> stageDict = new ();  // key : chapter, value : stage list
        private Dictionary<int, List<RewardItem>> chestDict = new (); // key : chest_id, value : chest list

        private void CustomizeSpecData()
        {
            stageDict.Clear();
            foreach (SpecStage stage in SpecStage.All)
            {
                if (!stageDict.TryGetValue(stage.chapter_id, out List<SpecStage> stageList))
                {
                    stageList = new List<SpecStage>();
                    stageDict.Add(stage.chapter_id, stageList);
                }

                stageList.Add(stage);
            }

            chestDict.Clear();
            foreach (SpecChest chest in SpecChest.All)
            {
                if (!chestDict.TryGetValue(chest.chest_id, out List<RewardItem> chestList))
                {
                    chestList = new List<RewardItem>();
                    chestDict.Add(chest.chest_id, chestList);
                }

                chestList.Add(chest.ToRewardItem());
            }
        }

        public int GetStageCount(int chapter)
        {
            if (stageDict.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList.Count;
            }

            return 0;
        }

        public SpecStage GetSpecStage(int chapter, int stageIdx)
        {
            if (stageDict.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList[stageIdx];
            }

            return null;
        }

        public int GetStageIndex(int chapter, int stageId)
        {
            if (stageDict.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                for (var i = 0; i < stageList.Count; i++)
                {
                    if (stageList[i].stage_id == stageId)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public List<RewardItem> GetChestList(int chestId)
        {
            return chestDict.GetValueOrDefault(chestId);
        }
    }
}
