/*
 * Copyright (c) CookApps.
 */

using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// 사용자 데이터 (서버 동기화 항목)
    public class UserData
    {
        /*
         * 동기화 되는 목록
         */
        public readonly CategoryLevel LevelData = new(); // 레벨 카테고리
        public readonly CategoryExp ExpData = new();     // 경험치 카테고리

        /// 동기화 카테고리 이름 목록
        public static IEnumerable<string> GetSyncCategoryNames()
        {
            yield return nameof(LevelData);
            yield return nameof(ExpData);
        }

        /// 동기화 데이터 생성
        public IReadOnlyDictionary<string, string> GetSyncData()
        {
            var dict = new Dictionary<string, string>
            {
                { nameof(LevelData), JsonUtility.ToJson(LevelData) },
                { nameof(ExpData), JsonUtility.ToJson(ExpData) },
            };
            return dict;
        }

        /// 서버에서 받은 동기화 데이터 적용
        public void SetSyncData(IReadOnlyDictionary<string, string> dict)
        {
            if (dict.TryGetValue(nameof(LevelData), out string levelJson))
            {
                JsonUtility.FromJsonOverwrite(levelJson, LevelData);
            }
            if (dict.TryGetValue(nameof(ExpData), out string expJson))
            {
                JsonUtility.FromJsonOverwrite(expJson, ExpData);
            }
        }

        public void Reset()
        {
            LevelData.Level = 0;
            ExpData.Exp = 0;
        }

        //----------------------------------------------------------------------
        // 기타 데이터 항목

        public static UserData Instance { get; private set; }

        public long UserId;                    // 서버에서 할당 받은 유저 ID
        public string PlayerId = string.Empty; // 서버에서 할당 받은 플레이어 ID

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInit()
        {
            Instance = new UserData();
        }

        //----------------------------------------------------------------------
        // 관리되는 카테고리

        public class CategoryLevel
        {
            public int Level;
        }

        public class CategoryExp
        {
            public int Exp;
        }
    }
}
