using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    // TODO: 레벨업 시 AP 최대치 증가 및 보상 지급 로직 구현 필요
    public class AccountLevelUpWindowPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton expandButton;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            
            Debug.Log("TODO!! 레벨업 시 AP 최대치 증가 및 보상 지급 로직 구현 필요");

            expandButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => SceneUILayerManager.Instance.PopUILayer(self))
                .AddTo(this);
        }
    }
}
