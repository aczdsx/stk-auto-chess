/*
* Copyright (c) CookApps.
*/

using LiteDB;
using Tech.Hive.V1;

namespace CookApps.NetLite.Feat.DB
{
    /// Spec 정보 테이블
    [BsonCollection("dbs")]
    internal class CommonDBSpec
    {
        [BsonId(false)]
        public SpecType SpecType { get; init; }
        [BsonField("ver")]
        public uint Version { get; init; }
        [BsonField("data")]
        public byte[] Data { get; init; }
    }

    /// Platform 정보 테이블
    [BsonCollection("dbpf")]
    internal class DBPlatform
    {
        [BsonId(false)]
        public AuthPlatform AuthPlatform { get; init; }
        [BsonField("ai")]
        public string AuthId { get; init; }
    }
}
