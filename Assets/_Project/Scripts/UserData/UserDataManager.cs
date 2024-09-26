using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateUserDataInitializerAttribute : Attribute
    { }

    [GenerateUserDataInitializer]
    public partial class UserDataManager : Singleton<UserDataManager>
    {
        /// <summary>
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
        /// </summary>
        private class InitializeAttribute : Attribute
        {
            public int Priority { get; }
            public DataCategory Category { get; }

            public InitializeAttribute(DataCategory category)
            {
                Category = category;
                Priority = 0;
            }

            public InitializeAttribute(DataCategory category, int priority)
            {
                Category = category;
                Priority = priority;
            }
        }

        /// <summary>
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
        /// UniTask 비동기 반환 필요합니다.
        /// Initialize 다음에 호출됩니다.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public class InitializeEffectCodeAttribute : Attribute
        {
            public int Priority { get; }
            public InitializeEffectCodeAttribute(int priority = 0)
            {
                Priority = priority;
            }
        }

        /// <summary>
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
        /// InitializeEffectCode 다음에 호출됩니다.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public class InitializeOwnContentsAttribute : Attribute
        {
            public int Priority { get; }

            public InitializeOwnContentsAttribute(int priority = 0)
            {
                Priority = priority;
            }
        }

        /// <summary>
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 으로 선언할 것!
        /// </summary>
        private class ClearAttribute : Attribute
        {
        }

        public async UniTask<bool> Initialize()
        {
            // Get All Initialize Method
            var allMethods = typeof(UserDataManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var methods = new List<(DataCategory category, int priority, MethodInfo methodInfo)>();
            var afterInitializeMethods = new List<MethodInfo>();
            foreach (var method in allMethods)
            {
                var initializeAttribute = method.GetCustomAttribute<InitializeAttribute>(false);
                if (initializeAttribute != null)
                {
                    var category = initializeAttribute.Category;
                    var priority = initializeAttribute.Priority;

                    methods.Add((category, priority, method));
                }
            }

            var tryCount = 0;
            var allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>().ToList();
            var resp = await GrpcManager.Instance.PlayerData.ListAsync(allCategories.Select(x => x.ToCategoryString()));
            if (resp.IsError)
                return false;

            // 받은 정보 중에 없는 카테고리는 기본값으로 채워준다.
            Dictionary<DataCategory, string> userDatas = new();
            {
                foreach (var playerData in resp.Data.ItemList)
                {
                    if (string.IsNullOrEmpty(playerData.Data)) continue;

                    DataCategory category = DataCategory.None;
                    for (var i = 0; i < allCategories.Count; i++)
                    {
                        if (allCategories[i].ToCategoryString() == playerData.Category)
                        {
                            allCategories.RemoveAt(i);
                            category = allCategories[i];
                            break;
                        }
                    }

                    if (category != DataCategory.None)
                        userDatas.Add(category, playerData.Data);
                }

                foreach (var category in allCategories) userDatas.Add(category, string.Empty);
            }

            CallAllInitialize(userDatas);
            CallAllInitializeEffectCode();
            CallAllInitializeEffectCode();
            return true;
        }
        
        public void Clear()
        {
            var allMethods = typeof(UserDataManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var methods = new List<MethodInfo>();
            foreach (var method in allMethods)
            {
                var attributes = method.GetCustomAttributes(typeof(ClearAttribute), false);
                if (attributes.Length > 0) methods.Add(method);
            }

            foreach (var method in methods) method.Invoke(this, null);
        }
    }
}