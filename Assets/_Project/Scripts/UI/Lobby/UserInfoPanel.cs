using CookApps.AutoBattler;
using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nickNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expRatioText;
    [SerializeField] private Slider expSlider;

    private PlayerDataModel playerDataModel;

    public void Initialize()
    {
        playerDataModel ??= ServerDataManager.Instance.PlayerData;

        SetInitialValues();
        InitializeEvents();
    }

    private void SetInitialValues()
    {
        SetNickNameText(playerDataModel.Nickname);
        SetLevelText(playerDataModel.Level);
        SetExp(playerDataModel.Exp);
    }

    private void InitializeEvents()
    {
        playerDataModel.OnNicknameChanged
            .Subscribe(this, (nickName, self) => self.SetNickNameText(nickName))
            .AddTo(this);
        
        playerDataModel.OnLevelChanged
            .Subscribe(this, (level, self) => self.SetLevelText(level))
            .AddTo(this);
        
        playerDataModel.OnExpChanged
            .Subscribe(this, (exp, self) => self.SetExp(exp))
            .AddTo(this);
    }

    private void SetNickNameText(string nickName)
    {
        nickNameText.text = nickName;
    }

    private void SetLevelText(uint level)
    {
        levelText.text = ZString.Format("Lv.{0}", level);
    }
    
    private void SetExp(ulong currentExp)
    {
        var currentProgress = playerDataModel.ExpProgress;
        
        expSlider.value = currentProgress;
        expRatioText.text = ZString.Format("{0}%", currentProgress * 100.0f);
    }
}