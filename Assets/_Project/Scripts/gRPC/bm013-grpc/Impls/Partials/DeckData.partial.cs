using System;
using Google.Protobuf;
using MemoryPack;

namespace Tech.Hive.V1
{
    [MemoryPackable]
    public partial class DeckAdditionalData
    {
        [MemoryPackOrder(0)] public int supernovaCharacterId = 0;
        
        public ByteString ToGrpcData()
        {
            return ByteString.CopyFrom(MemoryPackSerializer.Serialize(this));
        }
    }
    
    public partial class DeckData
    {
        private DeckAdditionalData AdditionalData { get; set; }
        
        public void ResetAdditionalData()
        {
            AdditionalData = null;
        }
        
        public DeckAdditionalData GetAdditionalData()
        {
            if (AdditionalData != null)
            {
                return AdditionalData;
            }
            
            if (ClientData == null)
            {
                return AdditionalData = new DeckAdditionalData();
            }

            try
            {
                AdditionalData = MemoryPackSerializer.Deserialize<DeckAdditionalData>(ClientData.Span);
                return AdditionalData ??= new DeckAdditionalData();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to deserialize DeckAdditionalData: {ex}");
                return new DeckAdditionalData();
            }
        }
    }
}
