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
        {
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
                    continue;
                }

                var afterInitializeAttribute = method.GetCustomAttribute<AfterInitializeAttribute>(false);
                if (afterInitializeAttribute != null) afterInitializeMethods.Add(method);
            }

            var playerId = await GetServerPlayerId();
            if (string.IsNullOrEmpty(playerId))
            {
                // todo : grpc 플레이어 정보 초기화 실패
            }
            var tryCount = 0;
            PlayerDataListResponse resp;
            var allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>().ToList();
            do
            {
                resp = await GrpcManager.Instance.PlayerData.ListAsync(allCategories.Select(x => x.ToCategoryString()));
                tryCount++;
            } while (resp.IsError && tryCount < 3);

            if (resp.IsError) return false;

            // 받은 정보 중에 없는 카테고리는 기본값으로 채워준다.
            Dictionary<string, string> userDatas = new();
            {
                foreach (var palyerData in resp.Data.ItemList)
                {
                    if (string.IsNullOrEmpty(palyerData.Data)) continue;

                    for (var i = 0; i < allCategories.Count; i++)
                        if (allCategories[i].ToCategoryString() == palyerData.Category)
                        {
                            allCategories.RemoveAt(i);
                            break;
                        }

                    userDatas.Add(palyerData.Category, palyerData.Data);
                }

                foreach (var category in allCategories) userDatas.Add(category.ToCategoryString(), string.Empty);
            }

            methods.Sort((x, y) => x.priority - y.priority);
            foreach (var (category, _, method) in methods)
                if (category == DataCategory.None)
                {
                    if (method.Invoke(this, Array.Empty<object>()) is UniTask task) await task;
                }
                else
                {
                    if (method.Invoke(this, new object[] { userDatas[category.ToCategoryString()] }) is UniTask task) await task;
                }

            foreach (var method in afterInitializeMethods) method.Invoke(this, null);

            return true;
        }
        
        /*
         * todo : grpc 상황에 맞게 처리하세요
         * 1. 서버 리스트를 받아온다.
         * 2. 서버 리스트에 플레이어 정보가 있는지 확인한다.
         * 3. (플레이어 정보가 있으면 선택, 없으면 서버에 플레이어를 생성한다.)
         * 4. 서버에 플레이어를 조인한다.
         */
        // Server Player 처리
        private async UniTask<string> GetServerPlayerId()
        {
            // 서버 리스트를 받아온다.
            var serverListResponse = await GrpcManager.Instance.Server.ListAsync();
            if (serverListResponse.IsError) 
                return string.Empty;
            
            // 서버의 유저 정보에서 첫번째 선택 ( 서버에 플레이어가 여러명일 경우 처리 방법은 다를 수 있음 )
            UserServerData userServerData = serverListResponse.Data.UserServerList.FirstOrDefault();
            // 서버에서 유저 정보가 있으면 해당 서버의 플레이어 ServerJoin 이후 Id 반환
            if (userServerData != null)
            {
                return await ServerJoin(userServerData.ServerId, userServerData.PlayerId);   
            }
            
            // IsJoinable 가능한 첫번째 서버 ( 서버 선택 UI를 통해 선택하게 할 수도 있음 )
            var firstServer = serverListResponse.Data.ServerList.FirstOrDefault(x => x.IsJoinable);
            if (firstServer == null)
            {   
                // 서버가 없음 서버팀에 문의 해 주세요!!!
                return string.Empty;
            }
            
            uint selectedServerId = firstServer.ServerId;
            // 없으면 첫번째 서버에 플레이어를 생성
            string nickname = Guid.NewGuid().ToString(); // 닉네임이 없는 게임이면 랜덤으로 생성
            PlayerCreateResponse createResponse = await GrpcManager.Instance.Player.CreateAsync(firstServer.ServerId, nickname);
            if(createResponse.IsError) 
                return string.Empty;
            return await ServerJoin(selectedServerId, createResponse.PlayerId);

            //----------------------------------------------------------------------
            async UniTask<string> ServerJoin(uint serverId, string playerId)
            {
                ServerJoinResponse joinResponse = await GrpcManager.Instance.Server.JoinAsync(serverId, playerId);
                return joinResponse.IsError ? string.Empty : playerId;
            }
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