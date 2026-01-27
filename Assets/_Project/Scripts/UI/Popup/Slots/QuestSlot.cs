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
    public class QuestSlot : CachedMonoBehaviour
    {
        [SerializeField] private Badge redDot;
        [SerializeField] private QuestRewardSlot questRewardSlot;
        [SerializeField] private TextMeshProUGUI questTitleText;
        [SerializeField] private TextMeshProUGUI questDescText;

        [Space]
        [SerializeField] private Slider questProgressSlider;
        [SerializeField] private TextMeshProUGUI questSliderText;

        [Space]
        [SerializeField] private GameObject claimBGObject;
        [SerializeField] private GameObject claimButtonObject;
        [SerializeField] private CAButton claimButton;
        [SerializeField] private GameObject completeLayerObject;
        [SerializeField] private GameObject completeButtonObject;

        private QuestInfo specQuestData;
        private QuestData questData;

        private List<RewardItem> questRewardItemList = new List<RewardItem>();

        private QuestPopup parentPopup;

        private void Awake()
        {
            claimButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetQuestSlot(QuestPopup parent, QuestInfo data)
        {
            if (data == null) return;

            ClearSlot();

            parentPopup = parent;
            specQuestData = data;
            questData = ServerDataManager.Instance.Quest.GetQuest(specQuestData.quest_id);

            questTitleText.text = LanguageManager.Instance.GetDefaultText(specQuestData.name_token);
            questDescText.text = LanguageManager.Instance.GetDefaultText(specQuestData.desc_token);

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(specQuestData.item_id, specQuestData.item_count);
            questRewardSlot.SetRewardSlot(rewardItem);
            questRewardItemList.Add(rewardItem);

            RefreshQuestSlot(false);
        }

        private void ClearSlot()
        {
            questRewardItemList.Clear();
        }

        public void RefreshQuestSlot(bool needRefreshData)
        {
            if (specQuestData == null) return;

            if (needRefreshData)
            {
                questData = ServerDataManager.Instance.Quest.GetQuest(specQuestData.quest_id);
            }

            if (questData == null)
            {
                // 데이터 없으면 기본 상태로 설정
                questSliderText.text = $"0/{specQuestData.need_count}";
                questProgressSlider.maxValue = specQuestData.need_count;
                questProgressSlider.value = 0;

                claimBGObject.SetActive(false);
                claimButtonObject.SetActive(false);
                completeLayerObject.SetActive(false);
                completeButtonObject.SetActive(false);
                return;
            }

            // 슬라이더 세팅
            questSliderText.text = $"{questData.CurrentCount}/{specQuestData.need_count}";
            questProgressSlider.maxValue = specQuestData.need_count;
            questProgressSlider.value = questData.CurrentCount;

            // 버튼 상태 세팅
            var isCleared = questData.CurrentCount >= specQuestData.need_count;
            var isClaimable = isCleared && !questData.IsRewarded;
            var isAlreadyClaimed = isCleared && questData.IsRewarded;

            claimBGObject.SetActive(isClaimable);
            claimButtonObject.SetActive(isClaimable);

            completeLayerObject.SetActive(isAlreadyClaimed);
            completeButtonObject.SetActive(isAlreadyClaimed);

            UpdateRedDot();
        }

        private void UpdateRedDot()
        {
            var path = $"Quest/{specQuestData.quest_id}";
            redDot.Clear();
            redDot.AddBadgePath(BadgeType.RedDot, path);
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            if (questData == null) return;
            if (questData.CurrentCount < specQuestData.need_count) return;
            if (questData.IsRewarded) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Quest.ClaimQuestRewardAsync(
                questData.QuestId,
                specQuestData.term_type.ToServerType());

            if (response == null || !response.IsSuccess)
            {
                return;
            }

            // 보상 결과 표시
            var rewardItemList = new List<RewardItem>(response.Rewards.Count);
            for (int i = 0; i < response.Rewards.Count; i++)
            {
                rewardItemList.Add(new RewardItem(response.Rewards[i]));
            }

            // 로컬 데이터 갱신
            if (response.Quest != null)
            {
                questData = response.Quest;
            }

            RefreshQuestSlot(false);

            await SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), null);

            parentPopup?.RefreshPopup();
        }
    }
}
