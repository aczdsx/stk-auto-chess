using CookApps.Obfuscator;
using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

public class LobbyMain : UILayer
{
    [SerializeField] private CAButton btnStart;

    protected override void Awake()
    {
        base.Awake();
        btnStart.onClick.AddListener(OnClickStart);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        btnStart.onClick.RemoveListener(OnClickStart);
    }

    public override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Bread, TopPanelType.Coin, TopPanelType.Jewel, TopPanelType.Menu);
    }

    private void OnClickStart()
    {
        int currentStageId = UserDataManager.UserStage.GetCurrentStageId();
        SceneUIManager.Instance.PushUILayer("ChapterMain", currentStageId);
    }
}
