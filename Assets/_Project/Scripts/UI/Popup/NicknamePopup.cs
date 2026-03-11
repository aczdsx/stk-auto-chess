using System.Text;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class NicknamePopup : UILayerPopupBase
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _confirmButton;

        [Space(10)]
        [SerializeField] private TMP_InputField _nicknameInputField;


        [Header("Tutorial Toast Pop")]
        [SerializeField] private GameObject _tutorialToastObj;
        [SerializeField] private TextMeshProUGUI _tutoiralText;
        [SerializeField] private Animator _tutorialToastAnimator;

        private bool _isFirst;

        protected override void Awake()
        {
            base.Awake();
            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _confirmButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickConfirmButton()).AddTo(this);
            _confirmButton.DefaultClickSoundType =  DefaultClickSoundType.Confirm;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _isFirst = (bool)param;
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            _nicknameInputField.text = "";

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
            if (_isFirst)
            {
                ToastManager.Instance.ShowToastByTokenKey("FIRST_NICKNAME_ALERT");
                return;
            }
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

            // TODO: 닉네임 변경 API 호출
            // UserDataManager.Instance.ChangeNickname(_nicknameInputField.text);

            SceneUILayerManager.Instance.PopUILayer(this);

            // 로비 정보 갱신
            var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
            if (battleReadyMain != null) battleReadyMain.RefreshUI(LobbyMainRefreshType.CHARACTER_LAYER);
        }

        private void OnClickCloseButton()
        {
            if (_isFirst)
            {
                ToastManager.Instance.ShowToastByTokenKey("FIRST_NICKNAME_ALERT");
                return;
            }

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
