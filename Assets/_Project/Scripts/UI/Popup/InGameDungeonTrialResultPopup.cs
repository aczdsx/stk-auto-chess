using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class InGameDungeonTrialResultPopupParam
    {
        public bool IsVictory { get; }
        public CharacterInfo SpecCharacter { get; }
        public IReadOnlyList<Reward> Rewards { get; }

        public InGameDungeonTrialResultPopupParam(bool isVictory, CharacterInfo specCharacter, IReadOnlyList<Reward> rewards)
        {
            IsVictory = isVictory;
            SpecCharacter = specCharacter;
            Rewards = rewards;
        }
    }

    public class InGameDungeonTrialResultPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _nextStageButton;
        [SerializeField] private CAButton _retryStageButton;

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        [SerializeField] private TextMeshProUGUI _victoryStageText;
        [SerializeField] private TextMeshProUGUI _failStageText;
        [SerializeField] private TextMeshProUGUI _nextStageButtonText;

        [SerializeField] private GameObject _characterIllustParentObject;

        [Space]
        [SerializeField] private GameObject _rewardObj;
        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;

        [SerializeField] private GameObject _gradeUpObj;
        [SerializeField] private GameObject _afterObj;
        [SerializeField] private GameObject _beforeObj;
        [SerializeField] private Image _beforeGradeImage;
        [SerializeField] private SpriteLoader _beforeGradeSpriteLoader;
        [SerializeField] private Image _afterGradeImage;
        [SerializeField] private SpriteLoader _afterGradeSpriteLoader;
        [SerializeField] private GameObject _beforeLoseObj;
        [SerializeField] private GameObject _afterLoseObj;

        [SerializeField] private GameObject _beforeArrow;
        [SerializeField] private GameObject _afterArrow;
        [SerializeField] private TextMeshProUGUI _beforeGradeText;
        [SerializeField] private TextMeshProUGUI _afterGradeText;

        [Header("Dungeon Info")]


        private AsyncOperationHandle<GameObject> _illustHandle;
        private bool _isVictory = false;
        private CharacterInfo _specCharacter;
        private IReadOnlyList<Reward> _rewards;
        private DungeonBabelInfo _currentSpecDungeonTrial;

        protected override void Awake()
        {
            base.Awake();
            _exitButton?.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickExitButtonAsync(), AwaitOperation.Drop).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.StopBGM();

            var popupParam = (InGameDungeonTrialResultPopupParam)param;
            _isVictory = popupParam.IsVictory;
            _specCharacter = popupParam.SpecCharacter;
            _rewards = popupParam.Rewards;

            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);

            _afterLoseObj.SetActive(!_isVictory);
            _beforeLoseObj.SetActive(!_isVictory);
            if (_isVictory)
            {
                string successString = LanguageManager.Instance.GetDefaultText("TIER_UPGRADE_SUCCESS_MSG");
                _victoryStageText.text =
                    StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial) + " " + successString;

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);

                SetRewardInfo();
            }
            else
            {
                string failString = LanguageManager.Instance.GetDefaultText("TIER_UPGRADE_FAIL_MSG");
                _failStageText.text =
                    StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial) + " " + failString;

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            if (_specCharacter != null)
            {
                if (_illustHandle.IsValid())
                {
                    Addressables.ReleaseInstance(_illustHandle);
                    _illustHandle = default;
                }
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacter.prefab_id);
                _illustHandle = Addressables.InstantiateAsync(illustPrefabName, _characterIllustParentObject.transform);
            }

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
            // _rewardObj.SetActive(!InGameManager.Instance.SpecDungeonTrial.is_grade_up);

            _currentSpecDungeonTrial = SpecDataManager.Instance.GetSpecDungeonTrialData(InGameManager.Instance.SpecDungeonTrial.dungeon_id - 1);
            var nextDungeonTrialData = InGameManager.Instance.SpecDungeonTrial;

            _beforeArrow.SetActive(_isVictory);
            _afterObj.SetActive(_isVictory);
            _beforeObj.SetActive(_currentSpecDungeonTrial != null);
            if (_currentSpecDungeonTrial != null)
            {
                _beforeGradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_currentSpecDungeonTrial.trial_type, false)).Forget();
                _beforeGradeText.text = StringUtil.GetTrialDungeonString(_currentSpecDungeonTrial);
            }

            _afterGradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(nextDungeonTrialData.trial_type, false)).Forget();
            _afterGradeText.text = StringUtil.GetTrialDungeonString(nextDungeonTrialData);
            _afterArrow.SetActive(_beforeObj.activeSelf);

            // 앱이벤트 전송
            SendDungeonEndAppEvent(InGameManager.Instance.AppEventResult, InGameManager.Instance.AppEventReason);
        }

        private void OnDestroy()
        {
            if (_illustHandle.IsValid())
                Addressables.ReleaseInstance(_illustHandle);
        }

        private async UniTask OnClickExitButtonAsync()
        {
            int lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene("BattleReady", specLastStageData.chapter_id);

            var guideMission = ServerDataManager.Instance.GuideMission;
            if (!guideMission.IsCompleted)
                SceneUILayerManager.OnSceneLoadedEvent += OpenDungeonTrialPopupAction;
        }

        private void OpenDungeonTrialPopupAction(string scenename)
        {
            if (scenename == "BattleReady")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<DungeonTrialPopup>().Forget();

                SceneUILayerManager.OnSceneLoadedEvent -= OpenDungeonTrialPopupAction;
            }
        }

        private void SetRewardInfo()
        {
            if (InGameManager.Instance.SpecDungeonTrial == null) return;

            BMUtil.RemoveChildObjects(_rewardsTransform);

            _gradeUpObj.SetActive(_isVictory && InGameManager.Instance.SpecDungeonTrial.is_grade_up);
            if (!InGameManager.Instance.SpecDungeonTrial.is_grade_up && _rewards != null)
            {
                foreach (var reward in _rewards)
                {
                    GameObject newSlotObject = Instantiate(_rewardItemSlotObj, _rewardsTransform);
                    RewardItemSlot newSlot = newSlotObject.GetComponent<RewardItemSlot>();

                    RewardItem newRewardItem = new RewardItem(reward);
                    newSlot?.SetRewardSlot(newRewardItem);
                }
            }
        }

        // 앱이벤트 - 던전 종료
        private void SendDungeonEndAppEvent(string result, string reason)
        {
            // 앱 이벤트 처리
            var myDeck = ServerDataManager.Instance.Deck.GetDeck(InGameType.TRIAL);
            int myDeckPower = DeckModel.GetDeckBattlePower(myDeck);
            int enemyPower = (int)InGameObjectManager.Instance.GetStartingEnemiesAttr();

            int starNum1 = _isVictory ? 1 : 0;
            string clearCondition = AppEventManager.Instance.GetAppEventCustomDataList(starNum1);

            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;

            AppEventManager.Instance.DungeonEnd(InGameManager.Instance.SpecDungeonTrial.dungeon_type, InGameManager.Instance.SpecDungeonTrial.id, battleTime, myDeck?.CharacterPlacements.Count ?? 0,
                myDeckPower, enemyPower, result, reason, clearCondition);
        }
    }
}
