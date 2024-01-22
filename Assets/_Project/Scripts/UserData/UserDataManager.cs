using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Com.Cookapps.Playgrounds.Heroidle;
using CookApps.gRPC.Common;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using GetUserDataResponse = Com.Cookapps.Tech.GetUserDataResponse;

public class UserDataManager : Singleton<UserDataManager>
{
    private Dictionary<string, IUserData> userDataDict = new ();
    public static UserDataStage Stage => Instance.userDataDict[DataCategory.UserStage.ToCategoryString()] as UserDataStage;

    public async UniTask<bool> Initialize()
    {
        var tryCount = 0;
        Type[] impls = InheritHelper.GetAllImplementations<IUserData>();
        Dictionary<string, Func<IUserData>> constructorDict = new ();
        foreach (Type userDataImpl in impls)
        {
            FieldInfo categoryFieldInfo = userDataImpl.GetField("Category");
            var dataCategory = (DataCategory) categoryFieldInfo.GetValue(null);
            NewExpression constructorExpression = Expression.New(userDataImpl);
            Expression<Func<IUserData>> lambdaExpression = Expression.Lambda<Func<IUserData>>(constructorExpression);
            Func<IUserData> constructorFunc = lambdaExpression.Compile();
            constructorDict.Add(dataCategory.ToCategoryString(), constructorFunc);
        }

        GetUserDataResponse resp;
        do
        {
            IEnumerable<DataCategory> allCategories = Enum.GetValues(typeof(DataCategory)).Cast<DataCategory>();
            resp = await CommonGrpcManager.Instance.GetUserDatasAsync(allCategories.Select(x => x.ToCategoryString()));
            tryCount++;
        } while (resp.IsError && tryCount < 3);

        if (resp.IsError)
        {
            return false;
        }

        foreach ((string category, string data) in resp.UserDatas)
        {
            if (string.IsNullOrEmpty(data))
            {
                continue;
            }

            userDataDict.Add(category, constructorDict[category].Invoke());
        }

        return true;
    }
}
