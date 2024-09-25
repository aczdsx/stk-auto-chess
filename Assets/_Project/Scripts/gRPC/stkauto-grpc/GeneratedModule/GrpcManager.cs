// GrpcGenerator에서 만들어진 파일입니다. 수정하지 마세요.
// Copyright (c) CookApps.
// ReSharper disable All
namespace CookApps.AutoBattler
{
    public partial class GrpcManager : CookApps.gRPC.GrpcManagerBase
    {
        public GrpcPlayerDataService PlayerData { get; }

        public GrpcPlayerService Player { get; }

        public GrpcPremiumCurrencyService PremiumCurrency { get; }

        public GrpcServerService Server { get; }

        public GrpcServerRankingService ServerRanking { get; }

        public GrpcShopService Shop { get; }

        public GrpcSpecService Spec { get; }

        public GrpcStkautoPvpService StkautoPvp { get; }
    }
}