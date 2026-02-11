using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum CharacterCollectionPopupTabType
    {
        MAIN,
        MAIN_DETAIL,
        GROW,
        SKILL,
    }

    public class CharacterCollectionPopup : UILayerPopupBase
    {
        [SerializeField] private CharacterCollectionMainLayer _collectionMainLayer;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.Char_User_Exp_Item, TopPanelType.Char_User_Exp_Item_2);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 캐릭터 리셋 데이터 업데이트
            UserDataManager.Instance.UpdateResetCharacterCount();

            _collectionMainLayer.InitLayer();
        }
    }
}
