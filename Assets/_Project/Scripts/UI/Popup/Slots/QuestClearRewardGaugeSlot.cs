using System.Collections.Generic;
using Coffee.UIEffects;
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
    public class QuestClearRewardGaugeSlot : CachedMonoBehaviour
    {
        [SerializeField] private Badge redDot;
        [SerializeField] private CAButton getRewardButton;
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private SpriteLoader rewardIconSpriteLoader;
        [SerializeField] private UIShiny rewardIconUIShiny;
        [SerializeField] private TextMeshProUGUI clearAmountText;
        [SerializeField] private TextMeshProUGUI rewardAmountText;

        [Space(10)]
        [SerializeField] private GameObject activeFrameObject;
        [SerializeField] private GameObject completeSymbolObject;
        [SerializeField] private Image completeLayerImage1;
        [SerializeField] private Image completeLayerImage2;

        [SerializeField] private Color completedColor;

        private QuestInfo specQuestData;
        private QuestData questData;
    
        private QuestPopup parentPopup;

        private List<RewardItem> questRewardItemList = new List<RewardItem>();

        private bool isAvailGetReward;
        private bool isAlreadyGetReward;

        private void Awake()
        {
            getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetQuestGaugeSlot(QuestPopup parent, QuestInfo data)
        {
            if (data == null) return;

            ClearSlot();

            parentPopup = parent;

            specQuestData = data;
            questData = ServerDataManager.Instance.Quest.GetQuest(specQuestData.quest_id);

            clearAmountText.text = specQuestData.need_count.ToString();

            rewardIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(specQuestData.item_id)).Forget();
            rewardAmountText.text = $"x{specQuestData.item_count}";

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(specQuestData.item_id, specQuestData.item_count);
            questRewardItemList.Add(rewardItem);

            RefreshSlot(false);
        }

        public void RefreshSlot(bool needRefreshUserData)
        {
            if (specQuestData == null) return;

            if (needRefreshUserData)
            {
                questData = ServerDataManager.Instance.Quest.GetQuest(specQuestData.quest_id);
            }

            if (questData == null)
            {
                isAvailGetReward = false;
                isAlreadyGetReward = false;
            }
            else
            {
                // 보상 수령 상태에 따른 분기처리
                var isCleared = questData.CurrentCount >= specQuestData.need_count;
                isAvailGetReward = isCleared && !questData.IsRewarded;
                isAlreadyGetReward = isCleared && questData.IsRewarded;
            }

            rewardIconUIShiny.Play(isAvailGetReward && !isAlreadyGetReward);
            activeFrameObject.SetActive(isAvailGetReward && !isAlreadyGetReward);

            if (isAlreadyGetReward)
            {
                rewardIconImage.color = BMUtil.ChangeColorAlpha(rewardIconImage.color, 60);

                completeLayerImage1.color = completedColor;
                completeLayerImage2.color = completedColor;
            }

            completeLayerImage1.gameObject.SetActive(isAlreadyGetReward);
            completeSymbolObject.SetActive(isAlreadyGetReward);

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
            // 보상 수령 가능 여부 체크
            if (isAvailGetReward == false)
            {
                return;
            }

            // 이미 수령한 보상 체크
            if (isAlreadyGetReward)
            {
                return;
            }

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

            RefreshSlot(false);

            await SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), null);

            parentPopup?.RefreshPopup();
        }

        private void ClearSlot()
        {
            questRewardItemList.Clear();
        }
    }
}
