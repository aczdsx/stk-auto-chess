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
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private SpriteLoader _rewardIconSpriteLoader;
        [SerializeField] private UIShiny _rewardIconUIShiny;
        [SerializeField] private TextMeshProUGUI _clearAmountText;
        [SerializeField] private TextMeshProUGUI _rewardAmountText;

        [Space(10)]
        [SerializeField] private GameObject _activeFrameObject;
        [SerializeField] private GameObject _completeSymbolObject;
        [SerializeField] private Image _completeLayerImage_1;
        [SerializeField] private Image _completeLayerImage_2;

        [SerializeField] private Color _completedColor;

        private QuestInfo _specQuestData;
        private QuestData _questData;

        private QuestPopup _parentPopup;

        private List<RewardItem> _questRewardItemList = new List<RewardItem>();

        private bool _isAvailGetReward;
        private bool _isAlreadyGetReward;

        private void Awake()
        {
            _getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetQuestGaugeSlot(QuestPopup parent, QuestInfo data)
        {
            if (data == null) return;

            ClearSlot();

            _parentPopup = parent;

            _specQuestData = data;
            _questData = ServerDataManager.Instance.Quest.GetQuest(_specQuestData.quest_id);

            _clearAmountText.text = _specQuestData.need_count.ToString();

            _rewardIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specQuestData.item_id)).Forget();
            _rewardAmountText.text = $"x{_specQuestData.item_count}";

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(_specQuestData.item_id, _specQuestData.item_count);
            _questRewardItemList.Add(rewardItem);

            RefreshSlot(false);
        }

        public void RefreshSlot(bool needRefreshUserData)
        {
            if (needRefreshUserData)
            {
                _questData = ServerDataManager.Instance.Quest.GetQuest(_specQuestData.quest_id);
            }

            // 보상 수령 상태에 따른 분기처리
            _isAvailGetReward = _questData.State == QuestState.Completed && _questData.Rewards.Count > 0;
            _isAlreadyGetReward = _questData.State == QuestState.Completed && _questData.Rewards.Count == 0;

            _rewardIconUIShiny.Play(_isAvailGetReward && !_isAlreadyGetReward);
            _activeFrameObject.SetActive(_isAvailGetReward && !_isAlreadyGetReward);

            if (_isAlreadyGetReward)
            {
                _rewardIconImage.color = BMUtil.ChangeColorAlpha(_rewardIconImage.color, 60);

                _completeLayerImage_1.color = _completedColor;
                _completeLayerImage_2.color = _completedColor;
            }

            _completeLayerImage_1.gameObject.SetActive(_isAlreadyGetReward);
            _completeSymbolObject.SetActive(_isAlreadyGetReward);
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            // 보상 수령 가능 여부 체크
            if (_isAvailGetReward == false)
            {
                return;
            }

            // 이미 수령한 보상 체크
            if (_isAlreadyGetReward)
            {
                return;
            }

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

            RefreshSlot(false);

            await SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), null);

            _parentPopup?.RefreshPopup();
        }

        private void ClearSlot()
        {
            _questRewardItemList.Clear();
        }
    }
}
