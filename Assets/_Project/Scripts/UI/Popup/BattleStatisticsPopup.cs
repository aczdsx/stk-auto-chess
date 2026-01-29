using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum BattleStatisticsTabType
    {
        GIVENDAMAGE,
        TAKENDAMAGED,
        HEAL,
    }

    public class BattleStatisticsPopup : UILayerPopupBase
    {
        private const int STATISTICS_UPDATE_TIME = 300; // 밀리 세컨즈

        [SerializeField] private CAButton _closeButton;
        [SerializeField] private GameObject _statisticsListParentObject;
        [SerializeField] private GameObject _statisticsListSlotObject;
        [SerializeField] private Image _dimImg;
        [SerializeField] private GameObject _popup;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Animator _buttonAnimation;
        /*
        [Space(10)]
        [Header("Exit Button")]
        [SerializeField] private RectTransform _line;
        [SerializeField] private RectTransform _circle;
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private Image _lineImg;
        [SerializeField] private Image _circleImg;
        [SerializeField] private Image _arrowImg;
        */

        private List<BattleStatSlot> _battleStatSlotList = new List<BattleStatSlot>();

        private InGameBottomUI _parentUI;
        
        private BattleStatisticsTabType _currentTabType = BattleStatisticsTabType.GIVENDAMAGE;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _parentUI = param as InGameBottomUI;

            _currentTabType = BattleStatisticsTabType.GIVENDAMAGE;
            SetBattleStatisticsPopup();
            PlayPopupOpenAnimation();
            _parentUI?.ChangeStatisticsButtonActiveState(false);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            ClearPopup();

            PlayPopupCloseAnimation();

            _parentUI?.ChangeStatisticsButtonActiveState(true);
        }
        
        public void OnClickTabToggleButton(int tabIndex)
        {
            _currentTabType = (BattleStatisticsTabType)tabIndex;

            switch (_currentTabType)
            {
                case BattleStatisticsTabType.GIVENDAMAGE:
                    _buttonAnimation.SetTrigger("SetDamage");
                    break;
                case BattleStatisticsTabType.TAKENDAMAGED:
                    _buttonAnimation.SetTrigger("SetTank");
                    break;
                case BattleStatisticsTabType.HEAL:
                    _buttonAnimation.SetTrigger("SetHeal");
                    break;
            }

            StartBattleStatistcs().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());;
        }

        public void PlayPopupOpenAnimation()
        {
            //팝업
            var dimColor = _dimImg.color; dimColor.a = 0f; _dimImg.color = dimColor;
            _canvasGroup.alpha = 0f;
            LMotion.Create(0f, 1f, 0.3f).WithEase(Ease.OutQuad).BindToColorA(_dimImg).AddTo(this);
            LMotion.Create(0f, 1f, 0.3f).WithEase(Ease.OutQuad).BindToAlpha(_canvasGroup).AddTo(this);
            var pos = _popup.transform.localPosition; pos.x = -100f; _popup.transform.localPosition = pos;
            LMotion.Create(-100f, 20f, 0.3f)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalPositionX(_popup.transform)
                .AddTo(this);
        }

        public void PlayPopupCloseAnimation()
        {
            var dimColor = _dimImg.color; dimColor.a = 1f; _dimImg.color = dimColor;
            _canvasGroup.alpha = 1f;
            LMotion.Create(1f, 0f, 0.3f).WithEase(Ease.OutQuad).BindToColorA(_dimImg).AddTo(this);
            LMotion.Create(1f, 0f, 0.3f).WithEase(Ease.OutQuad).BindToAlpha(_canvasGroup).AddTo(this);
            LMotion.Create(20f, -100f, 0.3f)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalPositionX(_popup.transform)
                .AddTo(this);
        }

        public void SetDeadSlot(int characterID)
        {
            var slot = _battleStatSlotList.Find(l => l.CharacterID == characterID);
            slot.SetDeadCharacter();
        }

        private void SetBattleStatisticsPopup()
        {
            ClearPopup();

            var battleCharacterList = InGameObjectManager.Instance.StartingPlayerCharacters;
            foreach (var battleCharacter in battleCharacterList)
            {
                GameObject newSlot = Instantiate(_statisticsListSlotObject, _statisticsListParentObject.transform);
                BattleStatSlot battleStatSlot = newSlot.GetComponent<BattleStatSlot>();
                battleStatSlot.SetBattleStatSlot(battleCharacter.CharacterId, battleCharacter.CharacterUId);

                _battleStatSlotList.Add(battleStatSlot);
            }

            StartBattleStatistcs().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());;
        }

        private async UniTask StartBattleStatistcs()
        {
            while (InGameManager.Instance.IsInGamePlaying)
            {
                _battleStatSlotList.ForEach(slot => slot.RefreshBattleStatSlotSmooth(_currentTabType, STATISTICS_UPDATE_TIME * 0.001f).Forget());
                
                _battleStatSlotList.Sort((a, b) => b.BattleValue.CompareTo(a.BattleValue));

                for (int i = 0; i < _battleStatSlotList.Count; i++)
                {
                    _battleStatSlotList[i].gameObject.transform.SetSiblingIndex(i);
                }

                await UniTask.Delay(STATISTICS_UPDATE_TIME);
            }
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_statisticsListParentObject.transform);

            _battleStatSlotList.Clear();
        }

        private void OnClickCloseButton()
        {
            Preference.SavePreference(Pref.STATISTIC, false);
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
