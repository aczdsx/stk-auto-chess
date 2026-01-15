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
        [SerializeField] private QuestRewardSlot _questRewardSlot;
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescText;

        [Space]
        [SerializeField] private Slider _questProgressSlider;
        [SerializeField] private TextMeshProUGUI _questSliderText;

        [Space]
        [SerializeField] private GameObject _claimBGObject;
        [SerializeField] private GameObject _claimButtonObject;
        [SerializeField] private CAButton _claimButton;
        [SerializeField] private GameObject _completeLayerObject;
        [SerializeField] private GameObject _completeButtonObject;

        private QuestInfo _specQuestData;
        private QuestData _questData;

        private List<RewardItem> _questRewardItemList = new List<RewardItem>();

        private QuestPopup _parentPopup;

        private void Awake()
        {
            _claimButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetQuestSlot(QuestPopup parent, QuestInfo data)
        {
            if (data == null) return;

            _parentPopup = parent;

            _specQuestData = data;
            _questData = ServerDataManager.Instance.Quest.GetQuest(_specQuestData.quest_id);

            _questTitleText.text = LanguageManager.Instance.GetDefaultText(_specQuestData.name_token);
            _questDescText.text = LanguageManager.Instance.GetDefaultText(_specQuestData.desc_token);

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(_specQuestData.item_id, _specQuestData.item_count);
            _questRewardSlot.SetRewardSlot(rewardItem);

            _questRewardItemList.Add(rewardItem);

            RefreshQuestSlot(false);
        }

        public void RefreshQuestSlot(bool needRefreshData)
        {
            if (_specQuestData == null) return;
            if (_questData == null) return;

            if (needRefreshData)
            {
                _questData = ServerDataManager.Instance.Quest.GetQuest(_specQuestData.quest_id);
            }

            // 슬라이더 세팅
            _questSliderText.text = $"{_questData.CurrentCount}/{_specQuestData.need_count}";
            _questProgressSlider.maxValue = _specQuestData.need_count;
            _questProgressSlider.value = _questData.CurrentCount;

            // 버튼 상태 세팅 (State == Completed && Rewards.Count > 0 이면 보상 수령 가능)
            bool isClaimable = _questData.State == QuestState.Completed && _questData.Rewards.Count > 0;
            bool isAlreadyClaimed = _questData.State == QuestState.Completed && _questData.Rewards.Count == 0;

            _claimBGObject.SetActive(isClaimable);
            _claimButtonObject.SetActive(isClaimable);

            _completeLayerObject.SetActive(isAlreadyClaimed);
            _completeButtonObject.SetActive(isAlreadyClaimed);
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Quest.ClaimQuestRewardAsync(
                _questData.QuestId,
                _specQuestData.quest_type.ToServerType());

            if (response == null || !response.IsSuccess)
            {
                return;
            }

            // 보상 결과 표시
            List<RewardItem> rewardItemList = new List<RewardItem>();
            for (int i = 0; i < response.Rewards.Count; i++)
            {
                rewardItemList.Add(new RewardItem(response.Rewards[i]));
            }

            // 로컬 데이터 갱신
            if (response.Quest != null)
            {
                _questData = response.Quest;
            }

            RefreshQuestSlot(false);

            await SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), null);

            _parentPopup?.RefreshPopup();
        }
    }
}
