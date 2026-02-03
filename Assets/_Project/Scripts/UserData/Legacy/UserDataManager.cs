using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace CookApps.AutoBattler
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateUserDataInitializerAttribute : Attribute
    { }

    [GenerateUserDataInitializer]
    public partial class UserDataManager : SingletonMonoBehaviour<UserDataManager>
    {
        /// <summary>
        /// 저장 대기 중인 데이터들 (Category → Serialized Data)
        /// </summary>
        private readonly Dictionary<string, IMessage> _pendingSaveData = new Dictionary<string, IMessage>();

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
            var tryCount = 0;
            var allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>().ToList();
            var resp = await NetManager.Instance.PlayerData.ListAsync(allCategories.Select(x => x.ToCategoryString()));
            if (!resp.IsSuccess)
                return false;
            
            
            // 받은 정보 중에 없는 카테고리는 기본값으로 채워준다.
            Dictionary<DataCategory, string> userDatas = new();
            {
                foreach (var playerData in resp.Data.ItemList)
                {
                    if (playerData.Data.Length == 0) continue;
            
                    DataCategory category = DataCategory.None;
                    for (var i = 0; i < allCategories.Count; i++)
                    {
                        if (allCategories[i].ToCategoryString() == playerData.Category)
                        {
                            category = allCategories[i];
                            allCategories.RemoveAt(i);
                            break;
                        }
                    }
            
                    if (category != DataCategory.None)
                    {
                        var base64string = playerData.GetData();
                        userDatas.Add(category, base64string);
                    }
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

            // 대기 중인 저장 데이터도 클리어
            _pendingSaveData.Clear();
        }

        /// <summary>
        /// 데이터를 즉시 저장하지 않고 Dictionary에 추가 (LateUpdate에서 배치 처리)
        /// </summary>
        public void QueueSave(string category, IMessage data)
        {
            _pendingSaveData[category] = data;
        }

        /// <summary>
        /// LateUpdate에서 호출 - 모아둔 데이터를 한 번에 서버로 전송
        /// </summary>
        private void LateUpdate()
        {
            if (_pendingSaveData.Count == 0) return;

            var savingData = new Dictionary<string, string>();
            // Dictionary의 모든 데이터를 서버로 전송
            foreach (var kvp in _pendingSaveData)
            {
                savingData.Add(kvp.Key, MessageUtility.ToBase64String(kvp.Value));
            }
            NetManager.Instance.PlayerData.SetAsync(savingData);

            // 전송 후 Dictionary 클리어
            _pendingSaveData.Clear();
        }
    }
}