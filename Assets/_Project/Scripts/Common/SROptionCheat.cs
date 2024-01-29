#if !RELEASE && ENABLE_CHEAT
using System.ComponentModel;
using CookApps.SampleTeamBattle;

public partial class SROptions
{
    [Category("Reward")] [Sort(0)]
    public RewardType RewardType { get; set; }

    [Category("Reward")] [Sort(1)]
    public int RewardId { get; set; }

    [Category("Reward")] [Sort(2)]
    public int RewardAmount { get; set; }

    [Category("Reward")] [Sort(3)]
    public void AddReward()
    {
        if (false) //itemType == ItemType.Equipment)
        {
            // var spec = SpecDataManager.Instance.GetEquipmentSpec(itemId);
            // if (spec == null)
            // {
            //     ItemResultMessage = $"{itemId}에 해당하는 장비가 없습니다.";
            // }
            // else
            // {
            //     UserDataManager.Instance.AddEquipment(itemId, itemAmount);
            //     ItemResultMessage = $"장비를 추가하였습니다. id:{itemId}, amount:{itemAmount}";
            // }
        }
        else if (RewardType == RewardType.KNIGHT_PIECE)
        {
            SpecCharacter spec = SpecDataManager.Instance.SpecCharacter.Get(RewardId);
            if (spec == null)
            {
                ItemResultMessage = $"{RewardId}에 해당하는 영웅이 없습니다.";
            }
            else
            {
                UserDataManager.UserCharacter.AddCharacter(RewardId, RewardAmount);
                UserDataManager.UserCharacter.Save();
                ItemResultMessage = $"영웅을 추가하였습니다. id:{RewardId}, amount:{RewardAmount}";
            }
        }

        OnPropertyChanged(nameof(ItemResultMessage));
    }

    [Category("Reward")] [Sort(4)]
    public string ItemResultMessage { get; private set; }

    [Category("Reward")] [Sort(10)]
    public void AddAllCharacters()
    {
        foreach (SpecCharacter spec in SpecDataManager.Instance.SpecCharacter.All)
        {
            if (spec.seq <= 0)
            {
                continue;
            }

            UserDataManager.UserCharacter.AddCharacter(spec.id, 1);
        }

        UserDataManager.UserCharacter.Save();
    }
}
#endif
