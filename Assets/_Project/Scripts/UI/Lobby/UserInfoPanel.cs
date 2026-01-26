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

    private PlayerDataBridge playerDataBridge;
    
    public void Initialize()
    {
        playerDataBridge ??= new PlayerDataBridge();

        SetInitialValues();
        InitializeEvents();
    }

    private void SetInitialValues()
    {
        SetNickNameText(playerDataBridge.Nickname);
        SetLevelText(playerDataBridge.Level);
        SetExp(playerDataBridge.Exp);
    }

    private void InitializeEvents()
    {
        playerDataBridge.OnNicknameChanged
            .Subscribe(this, (nickName, self) => self.SetNickNameText(nickName))
            .AddTo(this);
        
        playerDataBridge.OnLevelChanged
            .Subscribe(this, (level, self) => self.SetLevelText(level))
            .AddTo(this);
        
        playerDataBridge.OnExpChanged
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
        var currentProgress = playerDataBridge.ExpProgress;
        
        expSlider.value = currentProgress;
        expRatioText.text = ZString.Concat(currentProgress * 100.0f);
    }
}