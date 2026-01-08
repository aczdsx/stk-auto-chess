using System.Collections.Generic;
using CookApps.CypherPrefs;
using MemoryPack;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class AuthData
    {
        public AuthPlatform Platform { get; set; }
        public string Id { get; set; }
    }
    
    [MemoryPackable]
    public partial class LocalData
    {
        public List<AuthData> AuthDatas { get; set; } = new ();
        public uint LastPlayStageId { get; set; } = 10001;
    }
    
    public class LocalDataManager : Preference<LocalData>
    {
        public static LocalDataManager Instance { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            Instance = new LocalDataManager(PreferenceGetterSetter.Default);
        }

        public LocalDataManager(IPreferenceGetterSetter getterSetter) : base(getterSetter)
        {
            throttleSave = new ThrottleAction(0f);
            throttleSave.ThrottledEvent += Save;
        }

        public override string PreferenceKey => "AutoBattler_LocalData";
        ThrottleAction throttleSave;
        
        public bool HasAuthPlatform(AuthPlatform platform)
        {
            if (data.AuthDatas == null || data.AuthDatas.Count == 0)
                return false;

            foreach (var authData in data.AuthDatas)
            {
                if (authData.Platform == platform)
                    return true;
            }
            return false;
        }
        
        public string GetAuthId(AuthPlatform platform)
        {
            if (data.AuthDatas == null || data.AuthDatas.Count == 0)
                return string.Empty;

            foreach (var authData in data.AuthDatas)
            {
                if (authData.Platform == platform)
                    return authData.Id;
            }
            return string.Empty;
        }

        public AuthData GetRecentAuthData()
        {
            if (data.AuthDatas == null || data.AuthDatas.Count == 0)
                return null;
            return data.AuthDatas[^1];
        }

        public void AddAuth(AuthPlatform platform, string authId)
        {
            bool isExist = false;
            for (var i = 0; i < data.AuthDatas.Count; i++)
            {
                if (data.AuthDatas[i].Platform == platform)
                {
                    if (data.AuthDatas[i].Id == authId)
                        return;

                    data.AuthDatas[i].Id = authId;
                    isExist = true;
                    break;
                }
            }
            if (!isExist)
            {
                data.AuthDatas.Add(new AuthData
                {
                    Platform = platform,
                    Id = authId
                });
            }
            isDirty = true;
            throttleSave.ThrottleInvoke();
        }

        public uint GetLastPlayStageId()
        {
            return data.LastPlayStageId;
        }

        public void SetLastPlayStageId(uint stageId)
        {
            if (data.LastPlayStageId == stageId) return;

            data.LastPlayStageId = stageId;
            isDirty = true;
            throttleSave.ThrottleInvoke();
        }
    }
}
