using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Modal, "Prefabs/UI/InGame/BattleStatisticsPopup.prefab")]

    public class BattleStatisticsPopup : UILayer
    {
        private const int STATISTICS_UPDATE_TIME = 500; // 밀리 세컨즈

        [SerializeField] private GameObject _statisticsListParentObject;
        [SerializeField] private GameObject _statisticsListSlotObject;

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
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            ClearPopup();
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
