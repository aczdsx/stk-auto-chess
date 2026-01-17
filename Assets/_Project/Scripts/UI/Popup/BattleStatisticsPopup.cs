using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
            _dimImg.DOFade(0f, 0f);
            _canvasGroup.DOFade(0f, 0f);
            _dimImg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            _canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            _popup.transform.DOLocalMoveX(-100f, 0f).SetUpdate(true);
            _popup.transform.DOLocalMoveX(20f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            /*
            //Exit 버튼
            _circleImg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            _lineImg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.1f);
            _arrowImg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.2f);
            _circle.DOScale(1f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true);
            _line.DOScale(1f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true).SetDelay(0.1f);
            _arrow.DOScale(1f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true).SetDelay(0.2f);*/
        }

        public void PlayPopupCloseAnimation()
        {
            _dimImg.DOFade(1f, 0f);
            _canvasGroup.DOFade(1f, 0f);
            _dimImg.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
            _canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
            _popup.transform.DOLocalMoveX(-100f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            _popup.transform.DOLocalMoveX(20f, 0f).SetUpdate(true);
            /*
            //Exit 버튼
            _circleImg.DOFade(0f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            _lineImg.DOFade(0f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.1f);
            _arrowImg.DOFade(0f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.2f);
            _circle.DOScale(0f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true);
            _line.DOScale(0f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true).SetDelay(0.1f);
            _arrow.DOScale(0f, 0.3f).SetEase(Ease.InQuad).SetUpdate(true).SetDelay(0.2f);
            */
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
