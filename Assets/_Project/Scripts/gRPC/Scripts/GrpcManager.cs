using System;
using CookApps.gRPC;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public partial class GrpcManager : GrpcManagerBase
    {
        private static readonly Lazy<GrpcManager> _instance = new(() => new GrpcManager());
        public static GrpcManager Instance => _instance.Value;

        private GrpcManager()
        {
        }
    }
}