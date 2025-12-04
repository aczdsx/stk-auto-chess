/*
* Copyright (c) CookApps.
*/

using System.Threading;
using System.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.NetLite.Feat.Grpc
{
    [GrpcService(typeof(LobbyService.LobbyServiceClient))]
    public partial class GrpcLobbyService
    {
        public async Task<CheckVersionResponse> CheckVersionAsync(CancellationToken cancellationToken = default)
        {
            CheckVersionResponse resp = null; // await ExecuteAsync(ServiceClient.CheckVersionAsync, new CheckVersionRequest(), cancellationToken: cancellationToken);
            return resp;
        }
    }
}
