using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ChapterListStarGaugeSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private SpriteLoader _rewardIconSpriteLoader;
        [SerializeField] private UIShiny _rewardIconUIShiny;
        [SerializeField] private TextMeshProUGUI _starAmountText;
        [SerializeField] private TextMeshProUGUI _rewardAmountText;

        [Space(10)]
        [SerializeField] private GameObject _activeFrameObject;
        [SerializeField] private GameObject _completeSymbolObject;
        [SerializeField] private Image _completeLayerImage_1;
        [SerializeField] private Image _completeLayerImage_2;

        [SerializeField] private Color _completedColor;

        private StageMilestoneReward _specRewardInfo;

        private bool _isAvailGetReward;
        private bool _isAlreadyGetReward;

        private void Awake()
        {
            _getRewardButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickGetRewardButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        public void SetStarGaugeSlot(StageMilestoneReward rewardInfo)
        {
            if (rewardInfo == null) return;

            ClearSlot();

            _specRewardInfo = rewardInfo;

            _starAmountText.text = _specRewardInfo.sub_value.ToString();

            _rewardIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specRewardInfo.item_id)).Forget();
            _rewardAmountText.text = $"x{_specRewardInfo.item_count}";

            // 보상 수령 상태에 따른 분기처리
            int totalStarCount = (int)ServerDataManager.Instance.Battle.GetTotalChapterStarCount((uint)_specRewardInfo.content_key_value, _specRewardInfo.difficulty_type);
            _isAvailGetReward = totalStarCount >= _specRewardInfo.sub_value;

            _isAlreadyGetReward = ServerDataManager.Instance.Battle.IsGetStageAccReward(_specRewardInfo.content_key_value,
                _specRewardInfo.difficulty_type, _specRewardInfo.sub_value);

            _rewardIconUIShiny.Play(_isAvailGetReward && !_isAlreadyGetReward);
            _activeFrameObject.SetActive(_isAvailGetReward && !_isAlreadyGetReward);

            if (_isAlreadyGetReward)
            {
                _rewardIconImage.color = BMUtil.ChangeColorAlpha(_rewardIconImage.color, 60);
                //
                // _completeLayerImage_1.color = _completedColor;
                // _completeLayerImage_2.color = _completedColor;
            }

            _completeLayerImage_1.gameObject.SetActive(_isAlreadyGetReward);
            _completeSymbolObject.SetActive(_isAlreadyGetReward);
        }

        private async UniTask OnClickGetRewardButtonAsync()
        {
            // 보상 수령 가능 여부 체크
            if (_isAvailGetReward == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_LOCK_STAGE_MILESTONE_REWARD");
                return;
            }

            // 이미 수령한 보상 체크
            if (_isAlreadyGetReward)
            {
                return;
            }

            // 서버에 보상 수령 요청
            var response = await NetManager.Instance.Battle.ClaimChapterMilestoneRewardAsync(
                (uint)_specRewardInfo.content_key_value,
                (uint)_specRewardInfo.reward_id
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

            // 보상 수령 데이터 처리
            ServerDataManager.Instance.Battle.SetStageAccRewardState(_specRewardInfo.content_key_value, _specRewardInfo.difficulty_type, _specRewardInfo.sub_value);

            // 챕터 리스트 팝업 갱신
            var chapterListPopup = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>().StageMilestonePanel;
            if (chapterListPopup != null)
            {
                chapterListPopup.RefreshRewardLayer();
            }
        }

        private void ClearSlot()
        {

        }
    }
}
