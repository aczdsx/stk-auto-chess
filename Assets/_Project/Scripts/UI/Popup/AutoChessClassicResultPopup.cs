using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public readonly struct AutoChessClassicResultPopupParam
    {
        public readonly bool IsVictory;
        public readonly bool IsStarTime;
        public readonly bool IsStarNoDeath;
        public readonly CharacterInfo SpecCharacter;
        public readonly IReadOnlyList<Reward> Rewards;
        public readonly int StageId;
        public readonly InGameType InGameType;

        public AutoChessClassicResultPopupParam(
            bool isVictory, bool isStarTime, bool isStarNoDeath,
            CharacterInfo specCharacter, IReadOnlyList<Reward> rewards,
            int stageId, InGameType inGameType)
        {
            IsVictory = isVictory;
            IsStarTime = isStarTime;
            IsStarNoDeath = isStarNoDeath;
            SpecCharacter = specCharacter;
            Rewards = rewards;
            StageId = stageId;
            InGameType = inGameType;
        }
    }

    public class AutoChessClassicResultPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _nextStageButton;
        [SerializeField] private CAButton _retryStageButton;

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        [SerializeField] private TextMeshProUGUI _victoryStageText;
        [SerializeField] private TextMeshProUGUI _failStageText;
        [SerializeField] private TextMeshProUGUI _nextStageButtonText;

        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;

        [SerializeField] private List<GameObject> _starList;
        [SerializeField] private List<InGameResultStarCondition> _starConditionList;

        [SerializeField] private GameObject _characterIllustParentObject;

        private AsyncOperationHandle<GameObject> _illustHandle;
        private AutoChessClassicResultPopupParam _popupParam;
        private StageInfo _specStage;

        private bool _isPlayingLastStage;
        private bool _isEndChapter;

        protected override void Awake()
        {
            base.Awake();

            SetupButton(_exitButton, self => self.OnExitButtonClickedAsync());
            SetupButton(_nextStageButton, self => self.OnNextStageButtonClickedAsync());
            SetupButton(_retryStageButton, self => self.OnClickRetryStageButtonAsync());
        }

        protected override void OnBackButton(ref bool offPrevUI) { }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SoundManager.Instance.StopBGM();

            _popupParam = (AutoChessClassicResultPopupParam)param;
            _specStage = SpecDataManager.Instance.GetStageData(_popupParam.StageId);

            _failObj.SetActive(!_popupParam.IsVictory);
            _victoryObj.SetActive(_popupParam.IsVictory);

            if (_specStage != null)
            {
                if (_popupParam.IsVictory)
                    _victoryStageText.text = StringUtil.GetStageString(_specStage);
                else
                    _failStageText.text = StringUtil.GetStageString(_specStage);
            }

            // MVP 캐릭터 일러스트
            if (_popupParam.SpecCharacter != null && _characterIllustParentObject != null)
            {
                if (_illustHandle.IsValid())
                {
                    Addressables.ReleaseInstance(_illustHandle);
                    _illustHandle = default;
                }
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(
                    Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT,
                    _popupParam.SpecCharacter.prefab_id);
                _illustHandle = Addressables.InstantiateAsync(illustPrefabName, _characterIllustParentObject.transform);
            }

            // 별 표시
            int starCount = CalculateStar();
            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetActive(starCount > i);
            }

            // 별 조건 텍스트
            if (_popupParam.IsVictory)
            {
                for (int i = 0; i < _starConditionList.Count; i++)
                {
                    string resultToken = string.Format("STAGE_STAR_CONDITON_DESC_{0}", i + 1);
                    _starConditionList[i]._conditionText.text = LanguageManager.Instance.GetDefaultText(resultToken);
                }

                _starConditionList[0]._starObject.SetActive(_popupParam.IsVictory);
                _starConditionList[1]._starObject.SetActive(_popupParam.IsStarTime);
                _starConditionList[2]._starObject.SetActive(_popupParam.IsStarNoDeath);
            }
            else
            {
                for (int i = 0; i < _starConditionList.Count; i++)
                {
                    _starConditionList[i]._starObject.SetActive(false);

                    string resultToken = string.Format("STAGE_LOSE_GUIDE_DESC_{0}", i + 1);
                    _starConditionList[i]._conditionText.text = LanguageManager.Instance.GetDefaultText(resultToken);
                }
            }

            // 마지막 스테이지 / 챕터 체크
            if (_specStage != null)
            {
                var lastSpecStage = SpecDataManager.Instance.GetLastStageData(
                    _specStage.chapter_id, _specStage.difficulty_type);
                _isPlayingLastStage = lastSpecStage != null && lastSpecStage.stage_id == _specStage.stage_id;
                if (_isPlayingLastStage)
                {
                    int nextChapterId = lastSpecStage.chapter_id + 1;
                    var nextChapterData = SpecDataManager.Instance.GetStageData(
                        nextChapterId, 1, lastSpecStage.difficulty_type);
                    _isEndChapter = nextChapterData == null;
                }
            }

            // 승리 시 보상
            if (_popupParam.IsVictory)
            {
                CreateRewardItemSlots();
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            // 버튼 상태
            _retryStageButton.gameObject.SetActive(true);
            _nextStageButton.gameObject.SetActive(!_isEndChapter && _popupParam.IsVictory);
            _exitButton.gameObject.SetActive(!(_isPlayingLastStage && _popupParam.IsVictory));

            string buttonStringKey = _isPlayingLastStage ? "UI_CHAPTER_NEXT_MOVE" : "UI_STAGE_NEXT_MOVE";
            _nextStageButtonText.text = LanguageManager.Instance.GetDefaultText(buttonStringKey);

            // 애니메이션
            string animKey = _popupParam.IsVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
        }

        private void SetupButton(CAButton button, Func<AutoChessClassicResultPopup, UniTask> handler,
            AwaitOperation operation = AwaitOperation.Drop)
        {
            if (button == null) return;
            button.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => handler(self), operation).AddTo(this);
            button.DefaultClickSoundType = DefaultClickSoundType.Confirm;
        }

        private int CalculateStar()
        {
            int star = _popupParam.IsVictory ? 1 : 0;
            if (_popupParam.IsStarTime) star++;
            if (_popupParam.IsStarNoDeath) star++;
            return star;
        }

        private async UniTask OnExitButtonClickedAsync()
        {
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();

            if (_popupParam.IsVictory && _specStage != null)
            {
                SceneLoading.GoToNextSceneWithStageClearTrigger(
                    "BattleReady", _specStage.stage_id, _specStage.chapter_id);
            }
            else
            {
                SceneLoading.GoToNextScene("BattleReady", _specStage?.chapter_id ?? 1);
            }
        }

        private async UniTask OnNextStageButtonClickedAsync()
        {
            if (_isEndChapter || _specStage == null) return;

            // 챕터 마지막 스테이지 → BattleReady로 이동
            if (_isPlayingLastStage)
            {
                SceneTransition.Create<SceneTransition_FadeInOut>();
                await SceneTransition.FadeInAsync();
                int nextChapterId = _specStage.chapter_id + 1;
                SceneLoading.GoToNextSceneWithStageClearTrigger(
                    "BattleReady", _specStage.stage_id, nextChapterId);
                return;
            }

            int targetStageNumber = _specStage.stage_number + 1;

            var nextStageData = SpecDataManager.Instance.GetStageData(
                _specStage.chapter_id, targetStageNumber, _specStage.difficulty_type);
            if (nextStageData == null) return;

            // 행동력 검사
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(
                    IdMap.Item.ActionPoint, (ulong)nextStageData.need_ap))
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_AP");
                return;
            }

            // 서버에 전투 시작 요청
            var inGameParams = await NetManager.Instance.Battle.StartAsync(
                nextStageData.chapter_id,
                nextStageData.stage_id,
                0,
                Array.Empty<string>());
            if (inGameParams == null) return;

            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextSceneWithStageClearAndEnterTrigger(
                "InGame_New", _specStage.stage_id, nextStageData.stage_id, inGameParams);
        }

        private async UniTask OnClickRetryStageButtonAsync()
        {
            if (_specStage == null) return;

            // 행동력 검사
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(
                    IdMap.Item.ActionPoint, (ulong)_specStage.need_ap))
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_AP");
                return;
            }

            // 서버에 전투 시작 요청
            var inGameParams = await NetManager.Instance.Battle.StartAsync(
                _specStage.chapter_id,
                _specStage.stage_id,
                0,
                Array.Empty<string>());
            if (inGameParams == null) return;

            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();

            if (_popupParam.IsVictory)
            {
                SceneLoading.GoToNextSceneWithStageClearTrigger(
                    "InGame_New", _specStage.stage_id, inGameParams);
            }
            else
            {
                SceneLoading.GoToNextScene("InGame_New", inGameParams);
            }
        }

        private void CreateRewardItemSlots()
        {
            if (_popupParam.Rewards == null || _rewardItemSlotObj == null) return;

            for (int i = 0; i < _popupParam.Rewards.Count; i++)
            {
                var rewardItem = _popupParam.Rewards[i];
                RewardItem newItem = new RewardItem((int)rewardItem.ItemId, (int)rewardItem.Count);

                var rewardItemSlot = Instantiate(_rewardItemSlotObj, _rewardsTransform)
                    .GetComponent<RewardItemSlot>();
                rewardItemSlot.SetRewardSlot(newItem);
            }
        }

        private void OnDestroy()
        {
            if (_illustHandle.IsValid())
                Addressables.ReleaseInstance(_illustHandle);
        }

        private void OnStarAnimationEndSound(int starIdx)
        {
            int starCount = CalculateStar();
            if (starIdx > starCount - 1) return;
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_clear_star);
        }
    }
}
