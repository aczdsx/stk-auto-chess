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
    public readonly struct InGameResultPopupParam
    {
        public readonly bool IsVictory;
        public readonly bool IsStarTime;
        public readonly bool IsStarNoDeath;
        public readonly CharacterInfo SpecCharacter;
        public readonly IReadOnlyList<Reward> Rewards;

        public InGameResultPopupParam(bool isVictory, bool isStarTime, bool isStarNoDeath, CharacterInfo specCharacter, IReadOnlyList<Reward> rewards)
        {
            IsVictory = isVictory;
            IsStarTime = isStarTime;
            IsStarNoDeath = isStarNoDeath;
            SpecCharacter = specCharacter;
            Rewards = rewards;
        }
    }

    [Serializable]
    public class InGameResultStarCondition
    {
        public GameObject _starObject;
        public TextMeshProUGUI _conditionText;
    }

    public class InGameResultPopup : UILayerPopupBase
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
        private InGameResultPopupParam _popupParam;

        private bool _isPlayingTutorialStage = false;   // 튜토리얼 진행 여부 체크
        private bool _isClearTutorialStage = false;     // 튜토리얼 스테이지 클리어 여부 체크
        private bool _isPlayingLastStage = false;   // 챕터의 마지막 스테이지 체크용
        private bool _isEndChapter = false;         // 게임의 마지막 챕터인지 확인용
        private bool _isEndStage = false;         // 게임의 가장 마지막 스테이지 체크
        private bool _isWaitGuideMissionReward = false;         // 현재 가이드 미션을 클리어한 상태인지 체크


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
            OnPreEnterAsync(param).Forget();
        }

        private async UniTask OnPreEnterAsync(object param)
        {
            SoundManager.Instance.StopBGM();

            _popupParam = (InGameResultPopupParam)param;

            _failObj.SetActive(!_popupParam.IsVictory);
            _victoryObj.SetActive(_popupParam.IsVictory);

            if (_popupParam.IsVictory)
                _victoryStageText.text = StringUtil.GetStageString(InGameManager.Instance.SpecStage);
            else
                _failStageText.text = StringUtil.GetStageString(InGameManager.Instance.SpecStage);


            if (_popupParam.SpecCharacter != null)
            {
                if (_illustHandle.IsValid())
                {
                    Addressables.ReleaseInstance(_illustHandle);
                    _illustHandle = default;
                }
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _popupParam.SpecCharacter.prefab_id);
                _illustHandle = Addressables.InstantiateAsync(illustPrefabName, _characterIllustParentObject.transform);
            }

            var calculateStar = CalculateStar();
            // 상단 별 상태 갱신
            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetActive(calculateStar > i);
            }

            // 하단 별+조건 상태 갱신
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


            // 챕터 1 (튜토리얼 스테이지) 관련 처리
            if (InGameManager.Instance.SpecStage.chapter_id == 1)
            {
                _isPlayingTutorialStage = true;
            }

            // 마지막 스테이지 체크
            var lastSpecStage = SpecDataManager.Instance.GetLastStageData(InGameManager.Instance.SpecStage.chapter_id, InGameManager.Instance.SpecStage.difficulty_type);
            _isPlayingLastStage = lastSpecStage != null && lastSpecStage.stage_id == InGameManager.Instance.SpecStage.stage_id;
            if (_isPlayingLastStage)
            {
                int nextChpaterID = lastSpecStage.chapter_id + 1;
                // 다음 챕터 존재 여부 확인
                var nextChapterData = SpecDataManager.Instance.GetStageData(nextChpaterID, 1, lastSpecStage.difficulty_type);
                _isEndChapter = nextChapterData == null;    // 다음 챕터 데이터 없음 (게임의 마지막 챕터)
            }

            _isClearTutorialStage = _isPlayingTutorialStage && _isPlayingLastStage && _popupParam.IsVictory;

            // 승리 시 보상 및 각종 데이터 처리
            if (_popupParam.IsVictory)
            {
                CheckLatestStageClear();
                CreateRewardItemSlots();

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            await NetManager.Instance.GuideMission.GetAsync();
            var guideMission = ServerDataManager.Instance.GuideMission;
            // 가이드 미션이 완료되어 보상 수령 대기상태일 경우 처리
            _isWaitGuideMissionReward = guideMission.IsGoalReached || guideMission.IsCompleted;

            // 애니메이션 연출 적용
            string animKey = _popupParam.IsVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);

            // 버튼 상태 처리
            if (_isWaitGuideMissionReward)
            {
                _retryStageButton.gameObject.SetActive(false);
                _nextStageButton.gameObject.SetActive(false);
                _exitButton.gameObject.SetActive(true);
            }
            else
            {
                _retryStageButton.gameObject.SetActive(!_isPlayingTutorialStage || !_popupParam.IsVictory);
                _nextStageButton.gameObject.SetActive(!_isEndChapter && !_isClearTutorialStage && _popupParam.IsVictory);
                _exitButton.gameObject.SetActive(!_isPlayingTutorialStage || _isClearTutorialStage);
            }

            string buttonStringKey = _isPlayingLastStage ? "UI_CHAPTER_NEXT_MOVE" : "UI_STAGE_NEXT_MOVE";
            _nextStageButtonText.text = LanguageManager.Instance.GetDefaultText(buttonStringKey);

            // 앱이벤트 전송
            SendStageEndAppEvent(InGameManager.Instance.AppEventResult, InGameManager.Instance.AppEventReason);
        }

        private void SetupButton(CAButton button, Func<InGameResultPopup, UniTask> handler, AwaitOperation operation = AwaitOperation.Drop)
        {
            if (button == null) return;
            button.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => handler(self), operation).AddTo(this);
            button.DefaultClickSoundType = DefaultClickSoundType.Confirm;
        }

        private int CalculateStar()
        {
            var star = _popupParam.IsVictory ? 1 : 0;
            if (_popupParam.IsStarTime)
                star++;
            if (_popupParam.IsStarNoDeath)
                star++;

            return star;
        }

        private async UniTask OnExitButtonClickedAsync()
        {
            int lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();

            string nextSceneName = "BattleReady"; // (specLastStageData.chapter_id == 1) ? "Lobby" : "BattleReady";

            if (_popupParam.IsVictory)
            {
                // 승리 시 스테이지 클리어 나니노벨 트리거 검사
                SceneLoading.GoToNextSceneWithStageClearTrigger(nextSceneName, InGameManager.Instance.SpecStage.stage_id, InGameManager.Instance.SpecStage.chapter_id);
            }
            else
            {
                SceneLoading.GoToNextScene(nextSceneName, InGameManager.Instance.SpecStage.chapter_id);
            }
        }

        private async UniTask OnNextStageButtonClickedAsync()
        {
            // 최종 챕터/스테이지 여부 체크
            if (_isEndChapter) return;

            int targetChapterID = InGameManager.Instance.SpecStage.chapter_id;
            int targetStageNumber = InGameManager.Instance.SpecStage.stage_number;
            if (_isPlayingLastStage)
            {
                targetChapterID++;
                targetStageNumber = 1;
            }
            else
            {
                targetStageNumber++;
            }

            var nextStageData = SpecDataManager.Instance.GetStageData(targetChapterID, targetStageNumber, InGameManager.Instance.SpecStage.difficulty_type);

            // 행동력 검사
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(IdMap.Item.ActionPoint, (ulong)nextStageData.need_ap))
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
            if (inGameParams == null)
            {
                // ToastManager.Instance.ShowToastByTokenKey("ERROR_UNKNOWN");
                return;
            }

            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextSceneWithStageClearAndEnterTrigger("InGame_New", InGameManager.Instance.SpecStage.stage_id, nextStageData.stage_id, inGameParams);
        }

        private async UniTask OnClickRetryStageButtonAsync()
        {
            // 행동력 검사
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(IdMap.Item.ActionPoint, (ulong)InGameManager.Instance.SpecStage.need_ap))
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_AP");
                return;
            }

            // 서버에 전투 시작 요청
            var inGameParams = await NetManager.Instance.Battle.StartAsync(
                InGameManager.Instance.SpecStage.chapter_id,
                InGameManager.Instance.SpecStage.stage_id,
                0,
                Array.Empty<string>());
            if (inGameParams == null)
            {
                // ToastManager.Instance.ShowToastByTokenKey("ERROR_UNKNOWN");
                return;
            }

            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();

            if (_popupParam.IsVictory)
            {
                // 승리 시 스테이지 클리어 나니노벨 트리거 검사
                SceneLoading.GoToNextSceneWithStageClearTrigger("InGame_New", InGameManager.Instance.SpecStage.stage_id, inGameParams);
            }
            else
            {
                SceneLoading.GoToNextScene("InGame_New", inGameParams);
            }
        }

        // 가장 높은 스테이지 클리어 여부 체크
        private static void CheckLatestStageClear()
        {
            var latestStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            var latestStageData = SpecDataManager.Instance.GetStageData(latestStageID);

            if (latestStageData == null) return;

            if (InGameManager.Instance.SpecStage.chapter_id == latestStageData.chapter_id)
            {
                if (InGameManager.Instance.SpecStage.stage_number > latestStageData.stage_number)
                {
                    var nextStageData = SpecDataManager.Instance.GetNextStageData(InGameManager.Instance.SpecStage.stage_id);

                    if (nextStageData != null)
                    {
                        LocalDataManager.Instance.SetLastPlayStageId((uint)nextStageData.stage_id);
                    }
                }
            }
            else if (InGameManager.Instance.SpecStage.chapter_id > latestStageData.chapter_id)
            {
                var nextStageData = SpecDataManager.Instance.GetNextStageData(InGameManager.Instance.SpecStage.stage_id);

                if (nextStageData != null)
                {
                    LocalDataManager.Instance.SetLastPlayStageId((uint)nextStageData.stage_id);
                }
            }
        }

        private void CreateRewardItemSlots()
        {
            for (var i = 0; i < _popupParam.Rewards.Count; i++)
            {
                var rewardItem = _popupParam.Rewards[i];
                RewardItem newItem = new RewardItem((int)rewardItem.ItemId, (int)rewardItem.Count);

                var rewardItemSlot = Instantiate(_rewardItemSlotObj, _rewardsTransform).GetComponent<RewardItemSlot>();
                rewardItemSlot.SetRewardSlot(newItem);
            }
        }

        // 앱이벤트 - 스테이지 종료
        private void SendStageEndAppEvent(string result, string reason)
        {
            // 앱 이벤트 처리
            var myDeck = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);
            int myDeckPower = DeckModel.GetDeckBattlePower(myDeck);
            int enemyPower = (int)InGameObjectManager.Instance.GetStartingEnemiesAttr();

            int[] starNums = new int[] { 0, 0, 0 };
            if (_popupParam.IsVictory)
                starNums[0] = 1;
            if (_popupParam.IsStarTime)
                starNums[1] = 1;
            if (_popupParam.IsStarNoDeath)
                starNums[2] = 1;
            string clearCondition = AppEventManager.Instance.GetAppEventCustomDataList(starNums);

            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;

            AppEventManager.Instance.StageEnd(InGameManager.Instance.SpecStage.id, InGameManager.Instance.SpecStage.stage_id, battleTime, myDeck?.CharacterPlacements.Count ?? 0,
                myDeckPower, enemyPower, result, reason, clearCondition);
        }

        private void OnDestroy()
        {
            if (_illustHandle.IsValid())
                Addressables.ReleaseInstance(_illustHandle);
        }

        private void OnStarAnimationEndSound(int starIdx)
        {
            //인덱스는 0,1,2로 올것
            var calculateStar = CalculateStar();
            if(starIdx > calculateStar - 1)
                return;
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_clear_star);

        }
    }
}
