using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hatchery.V2;

namespace CookApps.AutoBattler
{
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
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 으로 선언할 것!
        /// </summary>
        private class AfterInitializeAttribute : Attribute
        { }

        /// <summary>
        /// 해당 어트리뷰트 사용하는 함수는 반드시 private 으로 선언할 것!
        /// </summary>
        private class ClearAttribute : Attribute
        { }

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
                    continue;
                }

                var afterInitializeAttribute = method.GetCustomAttribute<AfterInitializeAttribute>(false);
                if (afterInitializeAttribute != null)
                {
                    afterInitializeMethods.Add(method);
                }
            }

            var tryCount = 0;
            GetPlayerDataResponse resp;
            List<DataCategory> allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>().ToList();
            do
            {
                resp = await HatcheryGrpcManager.Instance.GetPlayerDatasAsync(allCategories.Select(x => x.ToCategoryString()));
                tryCount++;
            } while (resp.IsError && tryCount < 3);

            if (resp.IsError)
            {
                return false;
            }

            // 받은 정보 중에 없는 카테고리는 기본값으로 채워준다.
            Dictionary<string, string> userDatas = new ();
            {
                foreach ((string category, string respData) in resp.PlayerDatas)
                {
                    string data = respData;
                    if (string.IsNullOrEmpty(data))
                    {
                        continue;
                    }

                    for (var i = 0; i < allCategories.Count; i++)
                    {
                        if (allCategories[i].ToCategoryString() == category)
                        {
                            allCategories.RemoveAt(i);
                            break;
                        }
                    }

                    userDatas.Add(category, data);
                }

                foreach (DataCategory category in allCategories)
                {
                    userDatas.Add(category.ToCategoryString(), string.Empty);
                }
            }

            methods.Sort((x, y) => x.priority - y.priority);
            foreach ((var category, var _, var method) in methods)
            {
                if (category == DataCategory.None)
                {
                    if (method.Invoke(this, Array.Empty<object>()) is UniTask task)
                    {
                        await task;
                    }
                }
                else
                {
                    if (method.Invoke(this, new object[] {userDatas[category.ToCategoryString()]}) is UniTask task)
                    {
                        await task;
                    }
                }
            }

            foreach (var method in afterInitializeMethods)
            {
                method.Invoke(this, null);
            }

            return true;
        }

        public void Clear()
        {
            var allMethods = typeof(UserDataManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var methods = new List<MethodInfo>();
            foreach (var method in allMethods)
            {
                var attributes = method.GetCustomAttributes(typeof(ClearAttribute), false);
                if (attributes.Length > 0)
                {
                    methods.Add(method);
                }
            }

            foreach (var method in methods)
            {
                method.Invoke(this, null);
            }
        }
    }
}
