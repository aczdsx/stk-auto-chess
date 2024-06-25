using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Modal, "Prefabs/UI/InGame/BattleStatisticsPopup.prefab")]

    public class BattleStatisticsPopup : UILayer
    {
        private const int STATISTICS_UPDATE_TIME = 500; // 밀리 세컨즈

        [SerializeField] private GameObject _statisticsListParentObject;
        [SerializeField] private GameObject _statisticsListSlotObject;
        [SerializeField] private Image _dimImg;
        [SerializeField] private GameObject _popup;
        [SerializeField] private CanvasGroup _canvasGroup;


        private List<BattleStatSlot> _battleStatSlotList = new List<BattleStatSlot>();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SetBattleStatisticsPopup();

            PlayPopupOpenAnimation();
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            ClearPopup();

            PlayPopupCloseAnimation();
        }

        public void PlayPopupOpenAnimation()
        {
            _dimImg.DOFade(0f, 0f);
            _canvasGroup.DOFade(0f, 0f);
            _dimImg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            _canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            _popup.transform.DOLocalMoveX(-100f, 0.3f).SetEase(Ease.OutQuad).From();
         
        }

        public void PlayPopupCloseAnimation()
        {
            _dimImg.DOFade(1f, 0f);
            _canvasGroup.DOFade(1f, 0f);
            _dimImg.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
            _canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
            _popup.transform.DOLocalMoveX(-100f, 0.3f).SetEase(Ease.OutQuad);
        }

        private void SetBattleStatisticsPopup()
        {
            ClearPopup();

            var battleCharacterList = InGameObjectManager.Instance.StartingPlayerCharacters;
            foreach (var battleCharacter in battleCharacterList)
            {
                GameObject newSlot = Instantiate(_statisticsListSlotObject, _statisticsListParentObject.transform);
                BattleStatSlot battleStatSlot = newSlot.GetComponent<BattleStatSlot>();
                battleStatSlot.SetBattleStatSlot(battleCharacter.CharacterId);

                _battleStatSlotList.Add(battleStatSlot);
            }

            StartBattleStatistcs().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());;
        }

        private async UniTask StartBattleStatistcs()
        {
            while (InGameManager.Instance.IsInGamePlaying)
            {
                _battleStatSlotList.ForEach(slot => slot.RefreshBattleStatSlot());

                await UniTask.Delay(STATISTICS_UPDATE_TIME);
            }
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_statisticsListParentObject.transform);

            _battleStatSlotList.Clear();
        }
    }
}
