using Tech.Universal.V2;
using Cookapps.Stkauto.V1;

namespace CookApps.gRPC
{
    // TODO : `LocalServer`는 `game_service.proto`를 통해 생성된 GameService.GameServiceClient를 상속받습니다.
    public class LocalServer : GameService.GameServiceClient
    {
        public LocalServer(Grpc.Core.CallInvoker invoker) : base(invoker) { }

        //───────────────────────────────────────────────────────────────────────────────────────

        // https://docs.tech.cookapps.com/release/com.cookapps.grpc/manual/localserver.html 를 참고하세요.
        // TODO : LocalServer는 RealServer와 연동할 때 사용하세요.
    }
}
