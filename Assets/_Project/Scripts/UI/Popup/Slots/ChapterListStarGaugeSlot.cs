using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace CookApps.AutoBattler
{
    public class ChapterListStarGaugeSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private UIShiny _rewardIconUIShiny;
        [SerializeField] private TextMeshProUGUI _starAmountText;
        [SerializeField] private TextMeshProUGUI _rewardAmountText;

        [Space(10)]
        [SerializeField] private GameObject _activeFrameObject;
        [SerializeField] private GameObject _completeSymbolObject;
        [SerializeField] private Image _completeLayerImage_1;
        [SerializeField] private Image _completeLayerImage_2;

        [SerializeField] private Color _completedColor;

        private SpecRewardInfo _specRewardInfo;

        private bool _isAvailGetReward;
        private bool _isAlreadyGetReward;

        private void Awake()
        {
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            _getRewardButton.onClick.RemoveListener(OnClickGetRewardButton);
        }

        public void SetStarGaugeSlot(SpecRewardInfo rewardInfo)
        {
            if (rewardInfo == null) return;

            ClearSlot();

            _specRewardInfo = rewardInfo;

            _starAmountText.text = _specRewardInfo.sub_value.ToString();

            _rewardIconImage.sprite = ImageManager.Instance.GetItemSprite(_specRewardInfo.item_type);
            _rewardAmountText.text = $"x{_specRewardInfo.item_count}";

            // 보상 수령 상태에 따른 분기처리
            int totalStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_specRewardInfo.content_key_value, _specRewardInfo.difficulty_type);
            _isAvailGetReward = totalStarCount >= _specRewardInfo.sub_value;

            _isAlreadyGetReward = UserDataManager.Instance.IsGetStageAccReward(_specRewardInfo.content_key_value,
                _specRewardInfo.difficulty_type, _specRewardInfo.sub_value);

            _rewardIconUIShiny.Play(_isAvailGetReward && !_isAlreadyGetReward);
            _activeFrameObject.SetActive(_isAvailGetReward && !_isAlreadyGetReward);

            _completeSymbolObject.SetActive(_isAlreadyGetReward);
            if (_isAlreadyGetReward)
            {
                _rewardIconImage.color = BMUtil.ChangeColorAlpha(_rewardIconImage.color, 60);

                _completeLayerImage_1.color = _completedColor;
                _completeLayerImage_2.color = _completedColor;
            }
        }

        private void OnClickGetRewardButton()
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

            // 리워드 팝업 생성
            List<RewardItem> rewardItemList = new List<RewardItem>();
            rewardItemList.Add(new RewardItem(_specRewardInfo.item_type, _specRewardInfo.item_key, _specRewardInfo.item_count));

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(rewardItemList).Forget();

            // 보상 수령 데이터 처리
            UserDataManager.Instance.SetStageAccRewardState(_specRewardInfo.content_key_value, _specRewardInfo.difficulty_type, _specRewardInfo.sub_value);

            // 챕터 리스트 팝업 갱신
            var chapterListPopup = SceneUILayerManager.Instance.GetUILayer<ChapterListPopup>();
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
