using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class InGameDungeonResultPopup : UILayerPopupBase
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

        [SerializeField] private GameObject _characterIllustParentObject;

        private AsyncOperationHandle<GameObject> _illustHandle;
        private bool _isVictory = false;

        private CharacterInfo _specCharacter;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.StopBGM();

            (_isVictory, _specCharacter) = ((bool, CharacterInfo))param;

            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);

            if (_isVictory)
                _victoryStageText.text = StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial);
            else
                _failStageText.text =  StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial);

            _exitButton?.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnExitButtonClickedAsync(), AwaitOperation.Drop).AddTo(this);
            _nextStageButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnNextStageButtonClicked()).AddTo(this);
            _retryStageButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickRetryStageButton()).AddTo(this);

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

            // 승리 시 보상 및 각종 데이터 처리
            if (_isVictory)
            {
                CreateRewardItems();

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
        }

        private void OnDestroy()
        {
            if (_illustHandle.IsValid())
                Addressables.ReleaseInstance(_illustHandle);
        }

        private async UniTask OnExitButtonClickedAsync()
        {
            int lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();
            SceneLoading.GoToNextScene("BattleReady",  specLastStageData.chapter_id);
        }

        private void OnNextStageButtonClicked()
        {

        }

        private void OnClickRetryStageButton()
        {

        }

        private void CreateRewardItems()
        {
            var rewardList = SpecDataManager.Instance.GetSpecStageReward(InGameManager.Instance.SpecStage.reward_id)
                .FindAll(l => l.difficulty_type == InGameManager.Instance.SpecStage.difficulty_type);

            List<RewardItem> resultItemList = new List<RewardItem>();   // 보상 지급용 리워드 리스트

            foreach (var rewardItem in rewardList)
            {
                bool shouldCreateRewardItemSlot = false;

                if (rewardItem.frequency_type == FrequencyType.ONCE)
                {
                    // todo.. 최초 획득 보상 처리

                    shouldCreateRewardItemSlot = true;
                }
                else if (rewardItem.frequency_type == FrequencyType.REPEAT)
                {
                    shouldCreateRewardItemSlot = true;
                }

                if (shouldCreateRewardItemSlot)
                {
                    // ItemType의 삭제로 인해 변경.(new RewardItem(rewardItem.item_type, rewardItem.item_key, rewardItem.item_count))
                    RewardItem newItem = new RewardItem(rewardItem.item_id, rewardItem.item_count);

                    var rewardItemSlot = Instantiate(_rewardItemSlotObj, _rewardsTransform).GetComponent<RewardItemSlot>();
                    rewardItemSlot.SetRewardSlot(newItem);

                    resultItemList.Add(newItem);
                }
            }

            // 보상 데이터 저장
            if (resultItemList.Count > 0)
            {
                // TODO: api 마이그레이션 (InGameResultPopup처럼)
                // UserDataManager.Instance.IncreaseRewardItemList(resultItemList, true);
            }
        }
    }
}
