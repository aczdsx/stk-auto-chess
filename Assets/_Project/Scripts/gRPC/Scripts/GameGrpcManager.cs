using CookApps.gRPC;

namespace GrpcGame
{
    /// <summary>
    /// RealServer와 연동시 사용하는 매니저입니다. (Client Only / HatcheryServer일때는 사용하지 않습니다.)
    /// </summary>
    [CookAppsGrpcManager(typeof(CookApps.gRPC.LocalServer))]
    public partial class GameGrpcManager    //partial class를 만들어서 사용하세요
    {
        protected override void HandleInitializeParam(IGrpcParam param)
        {
            // 초기화시 추가적인 로직을 이곳에 추가합니다
        }
    }
}
