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
        [SerializeField] private Badge redDot;
        [SerializeField] private CAButton getRewardButton;
        [SerializeField] private RewardItemSlot rewardItemSlot;

        [SerializeField] private Image needItemImage;
        [SerializeField] private SpriteLoader needItemSpriteLoader;
        [SerializeField] private TextMeshProUGUI needItemAmountText;

        [Header("Slot State")]
        [SerializeField] private GameObject claimBGObject;
        [SerializeField] private GameObject claimOnObject;
        [SerializeField] private GameObject claimCheckObject;


        private EventInfo specEventData;
        private EventCondition specEventConditionData;

        private EventData currentEventData;
        private EventConditionData currentEventConditionData;

        private void Awake()
        {
            getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetEventSlot(EventData eventData, EventConditionData conditionData)
        {
            if (eventData == null) return;
            if (conditionData == null) return;

            currentEventData = eventData;
            currentEventConditionData = conditionData;

            specEventData = SpecDataManager.Instance.GetSpecEventData((int)currentEventData.EventId);
            specEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData((int)currentEventData.EventId, (int)currentEventConditionData.EventConditionId);

            SetNeedItemImage();
            needItemAmountText.text = $"x{specEventConditionData.need_count}";

            // 리워드 데이터 세팅
            RewardItem newRewardItem = new RewardItem(specEventConditionData.item_id, specEventConditionData.item_count);
            rewardItemSlot.SetRewardSlot(newRewardItem);

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            if (specEventConditionData == null || currentEventData == null) return;

            var isCleared = specEventConditionData.need_count <= currentEventData.CurrentCount;
            var isClaimed = currentEventConditionData.IsRewarded;
            var isAvailableGetReward = isCleared && !isClaimed;

            claimBGObject.SetActive(isAvailableGetReward);
            claimOnObject.SetActive(isAvailableGetReward);
            claimCheckObject.SetActive(isClaimed);

            UpdateRedDot();
        }

        private void UpdateRedDot()
        {
            var path = $"Event/ConsumeAP/{currentEventConditionData.EventConditionId}";
            redDot.Clear();
            redDot.AddBadgePath(BadgeType.RedDot, path);
        }

        private void SetNeedItemImage()
        {
            if (specEventData == null) return;

            switch (specEventData.event_type)
            {
                case EventType.USE_AP:
                    needItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(IdMap.Item.ActionPoint)).Forget();
                    break;
                case EventType.USE_GOLD:
                    needItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(IdMap.Item.Gold)).Forget();
                    break;
            }
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            if (currentEventData == null) return;
            if (currentEventConditionData.IsRewarded) return;
            if (currentEventData.CurrentCount < specEventConditionData.need_count) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Event.ClaimRewardAsync(
                currentEventData.EventId,
                currentEventConditionData.EventConditionId
            );

            if (response == null || !response.IsSuccess)
            {
                return;
            }

            // 리워드 팝업 생성
            var rewardItemList = new List<RewardItem>(response.Rewards.Count);
            for (int i = 0; i < response.Rewards.Count; i++)
            {
                rewardItemList.Add(new RewardItem(response.Rewards[i]));
            }

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();

            // 서버 응답의 EventConditionData로 갱신
            UpdateEventConditionData(response.Event);
            RefreshSlot();
        }

        private void UpdateEventConditionData(EventData updatedEventData)
        {
            if (updatedEventData == null) return;

            currentEventData = updatedEventData;

            // 현재 조건 데이터 찾아서 갱신
            for (int i = 0; i < updatedEventData.Conditions.Count; i++)
            {
                var condition = updatedEventData.Conditions[i];
                if (condition.EventConditionId == currentEventConditionData.EventConditionId)
                {
                    currentEventConditionData = condition;
                    break;
                }
            }
        }
    }
}
