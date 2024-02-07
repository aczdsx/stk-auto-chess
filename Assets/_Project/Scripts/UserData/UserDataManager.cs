using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Common;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using GetUserDataResponse = Com.Cookapps.Tech.GetUserDataResponse;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager : Singleton<UserDataManager>
    {
        private Dictionary<string, IUserData> userDataDict = new ();

        public static T Get<T>(DataCategory dataCategory) where T : IUserData
        {
            return (T) Instance.userDataDict[dataCategory.ToCategoryString()];
        }

        public async UniTask<bool> Initialize()
        {
            var tryCount = 0;
            Type[] impls = InheritHelper.GetAllImplementations<IUserData>();
            List<IUserData> userDataList = new ();
            foreach (Type userDataImpl in impls)
            {
                NewExpression constructorExpression = Expression.New(userDataImpl);
                Expression<Func<IUserData>> lambdaExpression = Expression.Lambda<Func<IUserData>>(constructorExpression);
                Func<IUserData> constructorFunc = lambdaExpression.Compile();
                userDataList.Add(constructorFunc.Invoke());
            }

            GetUserDataResponse resp;
            List<DataCategory> allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>().ToList();
            do
            {
                resp = await CommonGrpcManager.Instance.GetUserDatasAsync(allCategories.Select(x => x.ToCategoryString()));
                tryCount++;
            } while (resp.IsError && tryCount < 3);

            if (resp.IsError)
            {
                return false;
            }

            // 받은 정보 중에 없는 카테고리는 기본값으로 채워준다.
            Dictionary<string, string> userDatas = new ();
            {
                foreach ((string category, string respData) in resp.UserDatas)
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
                    userDatas.Add(category.ToCategoryString(), UserDataDefault.Get(category));
                }
            }

            userDataList.Sort((x, y) => x.Priority - y.Priority);
            foreach (IUserData userData in userDataList)
            {
                userData.SetDataFromServer(userDatas[userData.DataCategory.ToCategoryString()]);
                userDataDict.Add(userData.DataCategory.ToCategoryString(), userData);
            }

            return true;
        }
    }
}
