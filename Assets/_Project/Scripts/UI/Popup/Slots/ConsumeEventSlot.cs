using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ConsumeEventSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private RewardItemSlot _rewardItemSlot;

        [SerializeField] private Image _needItemImage;
        [SerializeField] private SpriteLoader _needItemSpriteLoader;
        [SerializeField] private TextMeshProUGUI _needItemAmountText;

        [Header("Slot State")]
        [SerializeField] private GameObject _claimBGObject;
        [SerializeField] private GameObject _claimOnObject;
        [SerializeField] private GameObject _claimCheckObject;


        private EventInfo _specEventData;
        private EventCondition _specEventConditionData;

        private EventData _currentEventData;
        private EventConditionData _currentEventConditionData;

        private void Awake()
        {
            _getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetEventSlot(EventData eventData, EventConditionData conditionData)
        {
            if (eventData == null) return;
            if (conditionData == null) return;

            _currentEventData = eventData;
            _currentEventConditionData = conditionData;

            _specEventData = SpecDataManager.Instance.GetSpecEventData((int)_currentEventData.EventId);
            _specEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData((int)_currentEventData.EventId, (int)_currentEventConditionData.EventConditionId);

            SetNeedItemImage();
            _needItemAmountText.text = $"x{_specEventConditionData.need_count}";

            // 리워드 데이터 세팅
            RewardItem newRewardItem = new RewardItem(_specEventConditionData.item_id, _specEventConditionData.item_count);
            _rewardItemSlot.SetRewardSlot(newRewardItem);

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            // TODO: 상태에 따른 UI 갱신 필요
            // 보상 수령 가능: COMPLETED 상태이고 보상이 남아있는 경우
            // bool isAvailGetReward = _currentEventConditionData.State == EventConditionState.Completed
            //                         && _currentEventConditionData.Rewards.Count > 0;
            // 이미 수령 완료: COMPLETED 상태이고 보상이 없는 경우
            // bool isAlreadyGetReward = _currentEventConditionData.State == EventConditionState.Completed
            //                           && _currentEventConditionData.Rewards.Count == 0;

            // 클레임 상태 세팅
            // _claimBGObject.SetActive(isAvailGetReward);
            // _claimOnObject.SetActive(isAvailGetReward);
            //
            // _claimCheckObject.SetActive(isAlreadyGetReward);
        }

        private void SetNeedItemImage()
        {
            if (_specEventData == null) return;

            switch (_specEventData.event_type)
            {
                case EventType.USE_AP:
                    _needItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(IdMap.Item.ActionPoint)).Forget();
                    break;
                case EventType.USE_GOLD:
                    _needItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(IdMap.Item.Gold)).Forget();
                    break;
            }
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            if (_currentEventData == null) return;
            if (_currentEventConditionData.IsRewarded) return;
            // TODO: 이벤트 조건 체크 필요

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Event.ClaimRewardAsync(
                _currentEventData.EventId,
                _currentEventConditionData.EventConditionId
            );

            if (response == null || !response.IsSuccess)
            {
                return;
            }

            // 리워드 팝업 생성
            List<RewardItem> rewardItemList = new List<RewardItem>();
            for (int i = 0; i < response.Rewards.Count; i++)
            {
                var reward = response.Rewards[i];
                rewardItemList.Add(new RewardItem(reward));
            }

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();

            // 서버 응답의 EventConditionData로 갱신
            UpdateEventConditionData(response.Event);
            RefreshSlot();
        }

        private void UpdateEventConditionData(EventData updatedEventData)
        {
            if (updatedEventData == null) return;

            _currentEventData = updatedEventData;

            // 현재 조건 데이터 찾아서 갱신
            for (int i = 0; i < updatedEventData.Conditions.Count; i++)
            {
                var condition = updatedEventData.Conditions[i];
                if (condition.EventConditionId == _currentEventConditionData.EventConditionId)
                {
                    _currentEventConditionData = condition;
                    break;
                }
            }
        }
    }
}
