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
    public class UserDataManager : Singleton<UserDataManager>
    {
        private Dictionary<string, IUserData> userDataDict = new ();
        public static UserDataStage Stage => Instance.userDataDict[DataCategory.UserStage.ToCategoryString()] as UserDataStage;
        public static UserDataWallet Wallet => Instance.userDataDict[DataCategory.UserWallet.ToCategoryString()] as UserDataWallet;

        public async UniTask<bool> Initialize()
        {
            var tryCount = 0;
            Type[] impls = InheritHelper.GetAllImplementations<IUserData>();
            Dictionary<string, Func<IUserData>> constructorDict = new ();
            foreach (Type userDataImpl in impls)
            {
                var attribute = userDataImpl.GetCustomAttribute<UserDataInitializeInfoAttribute>();
                // var dataCategory = (DataCategory) categoryFieldInfo.GetValue(null);
                NewExpression constructorExpression = Expression.New(userDataImpl);
                Expression<Func<IUserData>> lambdaExpression = Expression.Lambda<Func<IUserData>>(constructorExpression);
                Func<IUserData> constructorFunc = lambdaExpression.Compile();
                constructorDict.Add(attribute.DataCategory.ToCategoryString(), constructorFunc);
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

                IUserData userData = constructorDict[category].Invoke();
                userData.Initialize(data);
                userDataDict.Add(category, userData);
            }

            foreach (DataCategory category in allCategories)
            {
                IUserData userData = constructorDict[category.ToCategoryString()].Invoke();
                userData.Initialize(UserDataDefault.Get(category));
                userDataDict.Add(category.ToCategoryString(), userData);
            }

            return true;
        }

        public static event Action<int> OnBreadChanged
        {
            add => Wallet.OnBreadChanged += value;
            remove => Wallet.OnBreadChanged -= value;
        }
    }
}
