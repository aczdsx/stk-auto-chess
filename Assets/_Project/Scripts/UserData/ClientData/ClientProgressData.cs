using System.Collections.Generic;
using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientProgressData : ClientDataBase
    {
        
        
        public const string CategoryName = "client_progress";
        public override string Category => CategoryName;

        [MemoryPackOrder(0)] public MemoryPackList<int> _completeDialogueIds = new();
        [MemoryPackOrder(1)] public bool hasRewardedFirstGachaTicket = false;

        public IReadOnlyList<int> GetCompleteDialogueIds() => _completeDialogueIds;

        public bool HasDialogueId(int dialogueId) => _completeDialogueIds.Contains(dialogueId);

        public void AddCompleteDialogueId(int dialogueId)
        {
            if (!_completeDialogueIds.Contains(dialogueId))
            {
                _completeDialogueIds.Add(dialogueId);
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
    }
}
