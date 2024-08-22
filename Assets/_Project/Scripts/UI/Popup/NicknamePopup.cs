using System.Collections;
using System.Collections.Generic;
using System.Text;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/NicknamePopup.prefab")]
    public class NicknamePopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _confirmButton;
        
        [Space(10)]
        [SerializeField] private TMP_InputField _nicknameInputField;
        
        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
            _confirmButton.onClick.AddListener(OnClickConfirmButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _confirmButton.onClick.RemoveListener(OnClickConfirmButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            _nicknameInputField.text = "";
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void OnClickConfirmButton()
        {
            // 닉네임 유효성 체크
            int minGuestIDLength = SpecDataManager.Instance.GetGameConfig<int>("min_user_name_length");
            int maxGuestIDLength = SpecDataManager.Instance.GetGameConfig<int>("max_user_name_length");
            
            int guestIDByte = Encoding.UTF8.GetByteCount(_nicknameInputField.text);
            
            if (guestIDByte < minGuestIDLength || guestIDByte > maxGuestIDLength)
            {
                ToastManager.Instance.ShowToastByTokenKey("ERROR_SERVER_NICKNAME_LENGTH");
                return;
            }
            
            UserDataManager.Instance.ChangeNickname(_nicknameInputField.text);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            SceneUILayerManager.Instance.PopUILayer(this);
            
            // 로비 정보 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}