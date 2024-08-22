using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/ArenaPopup/ArenaPvpEndPopup.prefab")]
    public class ArenaPVPEndPopup : UILayer
    {
        [Header("Common")] [SerializeField] private CAButton _okButton;

        [Header("Tier Info Layer")] [SerializeField]
        private Image _tierImage;

        [SerializeField] private Image _tierAfterImage;
        [SerializeField] private Image _tierSecondImage;
        [SerializeField] private UICircle _tierSlider;
        [SerializeField] private TextMeshProUGUI _tierNameText;
        [SerializeField] private TextMeshProUGUI _tierPointText;
        [SerializeField] private TextMeshProUGUI _tierPointChangeText;
        [SerializeField] private List<GameObject> _tierLevelObjectList;

        [Header("Reward Layer")] [SerializeField]
        private GameObject _rewardContentObject;

        [SerializeField] private GameObject _rewardItemSlotObject;

        private bool _isVictory = false;
        private bool _isRevenge = false;

        private UserPVPBattleDetailData _detailData;
        private MatchPvpResponse _matchResultData;

        private void Awake()
        {
            _okButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _okButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            ClearPopup();

            SoundManager.Instance.StopBGM();

            (_isVictory, _detailData, _matchResultData) = ((bool, UserPVPBattleDetailData, MatchPvpResponse)) param;

            var specTierData = SpecDataManager.Instance.GetPVPTierData(_matchResultData.MyCurrentTier);

            var detailDeckData = InGameManager.Instance.UserPvpBattleDeckList;
            _isRevenge = string.IsNullOrEmpty(detailDeckData.MatchId) == false;

            // 일반 데이터 세팅
            _tierImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierSecondImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(specTierData.pvp_tier_type);
            _tierNameText.text = LanguageManager.Instance.GetPVPTierText(specTierData.pvp_tier_type);
            if (_matchResultData.MyDeltaScore > 0)
                _tierPointChangeText.text = $"(+{_matchResultData.MyDeltaScore.ToString("n0")})";
            else if (_matchResultData.MyDeltaScore < 0)
                _tierPointChangeText.text = $"({_matchResultData.MyDeltaScore.ToString("n0")})";

            float duration = 1.0f;
            AnimateSliderProgressAsync(_matchResultData.MyCurrentScore, _matchResultData.MyDeltaScore, duration)
                .Forget();

            for (int i = 0; i < specTierData.tier_order; i++)
            {
                _tierLevelObjectList[i].SetActive(true);
            }

            // 보상 데이터 세팅
            PvpRewardType targetRewardType =
                _isVictory ? PvpRewardType.PVP_REWARD_VICTORY : PvpRewardType.PVP_REWARD_LOSE;
            var pvpRewardList =
                SpecDataManager.Instance.GetRewardItemListByPVPRewardList(targetRewardType, specTierData.ranking_id);
            foreach (var rewardData in pvpRewardList)
            {
                GameObject newObject = Instantiate(_rewardItemSlotObject, _rewardContentObject.transform);
                var rewardSlot = newObject.GetComponent<RewardItemSlot>();

                rewardSlot.SetRewardSlot(rewardData);
            }

            // 보상 지급 처리
            if (pvpRewardList != null && pvpRewardList.Count > 0)
            {
                UserDataManager.Instance.IncreaseRewardItemList(pvpRewardList, true);
            }

            // 앱 이벤트 처리
            var myDeck = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.PVP);
            int myDeckPower = UserDataManager.Instance.GetDeckBattlePower(myDeck);

            string result = _isVictory ? "win" : "lose";

            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;

            AppEventManager.Instance.PVPEnd(1, _isRevenge, specTierData.pvp_tier_type, _matchResultData.MyCurrentRank,
                _matchResultData.MyCurrentScore, battleTime, result, myDeckPower, _detailData);
        }

        private async void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);

            //InGameManager.Instance.EndInGame();
            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);

            SceneUILayerManager.OnSceneLoadedEvent += OpenArenaMainPopupAction;
        }

        private void OpenArenaMainPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<ArenaMainPopup>().Forget();

                SceneUILayerManager.OnSceneLoadedEvent -= OpenArenaMainPopupAction;
            }
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardContentObject.transform);

            _tierLevelObjectList?.ForEach(obj => obj.SetActive(false));
        }

        private async UniTask AnimateSliderProgressAsync(int currentScore, int deltaScore, float totalDuration)
        {
            var beforeTierData =
                SpecDataManager.Instance.GetPVPTierDataByRankPoint(RankingType.SCORE, currentScore - deltaScore);
            var afterTierData = SpecDataManager.Instance.GetPVPTierData(_matchResultData.MyCurrentTier);

            bool isTierChange = false;
            if (afterTierData != null && beforeTierData != null)
            {
                isTierChange = beforeTierData.pvp_tier_type != afterTierData.pvp_tier_type;
            }

            string animKey = "";
            if (isTierChange)
            {
                animKey = _isVictory ? "SetTierUp" : "SetTierDown";
                _tierAfterImage.sprite = ImageManager.Instance.GetPVPTierIconSprite(afterTierData.pvp_tier_type);
            }
            else
            {
                animKey = _isVictory ? "SetUp" : "SetDown";
            }

            baseAnimator.SetTrigger(animKey);

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            float beforeRankingMin = Mathf.Max(950, beforeTierData.ranking_min);
            float afterRankingMin = Mathf.Max(950, afterTierData.ranking_min);

            float startValue = (float) (currentScore - deltaScore - beforeRankingMin) /
                               (beforeTierData.ranking_max - beforeRankingMin);
            float endValue = (float) (currentScore - afterRankingMin) /
                             (afterTierData.ranking_max - afterRankingMin);

            int startScore = currentScore - deltaScore;
            int endScore = currentScore;

            if (beforeTierData == afterTierData)
            {
                await AnimateSliderAndScore(startValue, endValue, startScore, endScore, totalDuration);
            }
            else
            {
                float firstSegmentProgress = 1.0f - startValue;
                float secondSegmentProgress = endValue;
                float totalProgress = firstSegmentProgress + secondSegmentProgress;

                float firstSegmentDuration = totalDuration * (firstSegmentProgress / totalProgress);
                float secondSegmentDuration = totalDuration * (secondSegmentProgress / totalProgress);

                await AnimateSliderAndScore(startValue, 1.0f, startScore, beforeTierData.ranking_max,
                    firstSegmentDuration);

                _tierNameText.text = LanguageManager.Instance.GetPVPTierText(afterTierData.pvp_tier_type);

                _tierSlider.Progress = 0f;
                await AnimateSliderAndScore(0f, endValue, afterTierData.ranking_min, endScore, secondSegmentDuration);
            }
        }

        private async UniTask AnimateSliderAndScore(float startValue, float endValue, int startScore, int endScore,
            float duration)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                _tierSlider.Progress = Mathf.Lerp(startValue, endValue, t);

                float currentScore = (int) Mathf.Lerp(startScore, endScore, t);
                _tierPointText.text = currentScore.ToString("n0");

                await UniTask.Yield();
            }

            _tierSlider.Progress = endValue; // 최종적으로 정확한 값 설정
            _tierPointText.text = endScore.ToString("n0"); // 최종 점수 설정
        }
    }
}