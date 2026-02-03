using System.Collections.Generic;
using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientProgressData : ClientDataBase
    {
        public const string CategoryName = "client_progress";
        public override string Category => CategoryName;

        public static ClientProgressData Get() => ClientDataManager.Instance.GetData<ClientProgressData>(CategoryName);

        [MemoryPackOrder(0)] public MemoryPackList<int> completeDialogueIds = new();
        [MemoryPackOrder(1)] public bool hasRewardedFirstGachaTicket = false;
        [MemoryPackOrder(2)] public bool hasNicknameSet = false;
        [MemoryPackOrder(3)] public MemoryPackList<int> receivedRewardIds = new();

        public IReadOnlyList<int> GetCompleteDialogueIds() => completeDialogueIds;

        public bool HasDialogueId(int dialogueId) => completeDialogueIds.Contains(dialogueId);

        public void AddCompleteDialogueId(int dialogueId)
        {
            if (!completeDialogueIds.Contains(dialogueId))
            {
                completeDialogueIds.Add(dialogueId);
                SetDirty();
            }
        }
        
        public bool HasRewardedFirstGachaTicket()
        {
            return hasRewardedFirstGachaTicket;
        }
        
        public void SetRewardedFirstGachaTicket(bool rewarded)
        {
            hasRewardedFirstGachaTicket = rewarded;
            SetDirty();
        }

        public void SetNicknameSet(bool value)
        {
            hasNicknameSet = value;
            SetDirty();
        }
        
        public bool IsRewardReceived(int rewardId)
        {
            return receivedRewardIds.Contains(rewardId);
        }
        
        public void AddReceivedRewardId(int rewardId)
        {
            if (!receivedRewardIds.Contains(rewardId))
            {
                receivedRewardIds.Add(rewardId);
                SetDirty();
            }
        }
    }
}
