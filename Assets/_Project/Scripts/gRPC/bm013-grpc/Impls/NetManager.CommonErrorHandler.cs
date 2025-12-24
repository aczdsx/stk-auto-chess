using System;
using CookApps.NetLite.Feat.Grpc;

namespace CookApps.AutoBattler
{
    public partial class NetManager : IGrpcServiceInterceptor
    {
        public void ShowLoadingIndicator()
        {

        }

        public void HideLoadingIndicator()
        {

        }

        public bool HandleCommonError(Exception e)
        {
            return false;
        }
    }
}
