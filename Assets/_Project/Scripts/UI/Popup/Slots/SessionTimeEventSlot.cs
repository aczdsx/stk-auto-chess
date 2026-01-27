using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class SessionTimeEventSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton getRewardButton;
        [SerializeField] private RewardItemSlot rewardItemSlot;
        [SerializeField] private TextMeshProUGUI sessionTimeText;
        [SerializeField] private Badge redDot;

        [Header("Slot State")]
        [SerializeField] private GameObject activeCircleObject;
        [SerializeField] private GameObject claimBGObject;
        [SerializeField] private GameObject claimOnObject;
        [SerializeField] private GameObject claimCheckObject;

        private EventCondition specEventConditionData;

        private EventData currentEventData;
        private EventConditionData currentEventConditionData;

        private void Awake()
        {
            getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetEventSlot(EventData eventData, EventConditionData serverConditionData, EventCondition specConditionData)
        {
            currentEventData = eventData;
            currentEventConditionData = serverConditionData;
            specEventConditionData = specConditionData;

            if (specEventConditionData == null)
            {
                Debug.LogWarning($"[SessionTimeEventSlot] specConditionData is null for EventConditionId: {serverConditionData?.EventConditionId}");
                return;
            }

            sessionTimeText.text = specEventConditionData.need_count.ToString();

            RefreshUI();

            var newRewardItem = new RewardItem(specEventConditionData.item_id, specEventConditionData.item_count);
            rewardItemSlot.SetRewardSlot(newRewardItem);
        }

        private void RefreshUI()
        {
            if (specEventConditionData == null || currentEventData == null) return;

            var isCleared = specEventConditionData.need_count <= currentEventData.CurrentCount;
            var isClaimed = currentEventConditionData.IsRewarded;
            var isAvailableGetReward = isCleared && !isClaimed;

            claimBGObject.SetActive(isAvailableGetReward);
            claimOnObject.SetActive(isAvailableGetReward);

            activeCircleObject.SetActive(isAvailableGetReward || isClaimed);
            claimCheckObject.SetActive(isClaimed);

            UpdateRedDot();
        }

        private void UpdateRedDot()
        {
            var path = $"Event/SessionTime/{currentEventConditionData.EventConditionId}";
            redDot.Clear();
            redDot.AddBadgePath(BadgeType.RedDot, path);
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            if (currentEventData == null)
                return;
            if (currentEventConditionData.IsRewarded)
                return;
            if(currentEventData.CurrentCount < specEventConditionData.need_count)
                return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            var response = await NetManager.Instance.Event.ClaimRewardAsync(
                currentEventData.EventId,
                currentEventConditionData.EventConditionId
            );

            if (response is not { IsSuccess: true })
            {
                return;
            }

            // 리워드 팝업 생성
            var rewardItemList = new List<RewardItem>(response.Rewards.Count);
            for (var i = 0; i < response.Rewards.Count; i++)
            {
                rewardItemList.Add(new RewardItem(response.Rewards[i]));
            }

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();

            UpdateEventConditionData(response.Event);
            RefreshUI();
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
