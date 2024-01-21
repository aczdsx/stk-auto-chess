// #define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.gRPC.Universal;
using CookApps.LocalData;
#endif
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;

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
    }
}
