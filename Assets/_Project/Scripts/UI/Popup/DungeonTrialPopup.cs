using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum DungeonTrialPopupRefreshType
    {
        ALL,
        STEP_SLOT,
        DUNGEON_INFO,
    }

    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/DungeonTrialPopup.prefab")]
    public class DungeonTrialPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _EnterDungeonButton;

        [Space]
        [SerializeField] private TextMeshProUGUI _dungeonNameText;
        [SerializeField] private TextMeshProUGUI _totalMonsterBattlePointText;
        [SerializeField] private TextMeshProUGUI _needStageStarText;

        [Header("Top Dungeon Progress Layer")]
        [SerializeField] private ScrollRect _stepScrollRect;
        [SerializeField] private GameObject _stepSlotObject;

        private List<DungeonTrialStepSlot> _stepSlotList = new List<DungeonTrialStepSlot>();

        [Header("Dungeon Monster Info Layer")]
        [SerializeField] private ScrollRect _monsterInfoScrollRect;
        [SerializeField] private GameObject _monsterInfoSlotObject;

        [Header("Dungeon Reward Info Layer")]
        [SerializeField] private Transform _rewardInfoContentTransform;
        [SerializeField] private GameObject _rewardItemSlotObject;

        private SpecDungeonTrial _specDungeonTrialData;

        public UserTrialDungeonData CurrentUserDungeonData { get; private set; }

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
            _EnterDungeonButton.onClick.AddListener(OnClickEnterDungeonButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _EnterDungeonButton.onClick.RemoveListener(OnClickEnterDungeonButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            CurrentUserDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
            _specDungeonTrialData = SpecDataManager.Instance.GetSpecDungeonTrialData(CurrentUserDungeonData.DungeonId);

            InitDungeonPopup();
        }

        public void SetCurrentSelectedDungeonData(int dungeonID)
        {
            CurrentUserDungeonData = UserDataManager.Instance.GetTrialDungeonData(dungeonID);
            _specDungeonTrialData = SpecDataManager.Instance.GetSpecDungeonTrialData(CurrentUserDungeonData.DungeonId);
        }

        public void RefreshDungeonTrialPopup(DungeonTrialPopupRefreshType refreshType)
        {
            switch (refreshType)
            {
                case DungeonTrialPopupRefreshType.ALL:
                    SetCommonInfoLayer();
                    SetMonsterInfoLayer();
                    SetRewardInfoLayer();
                    RefreshStepSlotLayer();
                    break;
                case DungeonTrialPopupRefreshType.STEP_SLOT:
                    RefreshStepSlotLayer();
                    break;
                case DungeonTrialPopupRefreshType.DUNGEON_INFO:
                    SetCommonInfoLayer();
                    SetMonsterInfoLayer();
                    SetRewardInfoLayer();
                    break;
            }
        }

        private void InitDungeonPopup()
        {
            SetCommonInfoLayer();
            SetMonsterInfoLayer();
            SetRewardInfoLayer();
            SetStepSlotLayer();

            _stepScrollRect.horizontalNormalizedPosition = 0;
        }

        private void SetCommonInfoLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;

            _needStageStarText.text = _specDungeonTrialData.need_star.ToString();
        }

        private void SetMonsterInfoLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;

            BMUtil.RemoveChildObjects(_monsterInfoScrollRect.content);

            var monsterDataList = SpecDataManager.Instance.GetSpecDungeonMonsterDataList(DungeonType.TRIAL, CurrentUserDungeonData.DungeonId);

            foreach (var monsterData in monsterDataList)
            {
                GameObject newSlotObject = Instantiate(_monsterInfoSlotObject, _monsterInfoScrollRect.content);
                DungeonMonsterInfoSlot newSlot = newSlotObject.GetComponent<DungeonMonsterInfoSlot>();
                newSlot?.SetMonsterInfoSlot(monsterData);
            }
        }

        private void SetRewardInfoLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;

            BMUtil.RemoveChildObjects(_rewardInfoContentTransform);

            var rewardDataList = SpecDataManager.Instance.GetSpecDungeonRewardDataList(DungeonType.TRIAL, CurrentUserDungeonData.DungeonId);

            foreach (var rewardData in rewardDataList)
            {
                GameObject newSlotObject = Instantiate(_rewardItemSlotObject, _rewardInfoContentTransform);
                RewardItemSlot newSlot = newSlotObject.GetComponent<RewardItemSlot>();

                RewardItem newRewardItem = new RewardItem(rewardData.item_type, rewardData.item_key, rewardData.item_count);
                newSlot?.SetRewardSlot(newRewardItem);
            }
        }

        private void SetStepSlotLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;

            BMUtil.RemoveChildObjects(_stepScrollRect.content);
            _stepSlotList.Clear();

            var totalDungeonDataList = SpecDataManager.Instance.GetSpecDungeonTrialDataList(DungeonType.TRIAL);

            foreach (var dungeonData in totalDungeonDataList)
            {
                GameObject newSlotObject = Instantiate(_stepSlotObject, _stepScrollRect.content);
                DungeonTrialStepSlot newSlot = newSlotObject.GetComponent<DungeonTrialStepSlot>();
                newSlot?.SetStepSlot(this, dungeonData, CurrentUserDungeonData);

                _stepSlotList.Add(newSlot);
            }
        }

        private void RefreshStepSlotLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;
            if (_stepSlotList == null || _stepSlotList.Count <= 0) return;

            _stepSlotList.ForEach(slot => slot.RefreshSlot());
        }

        private void OnClickEnterDungeonButton()
        {
            // 던전 진입 가능 조건 검사
            if (UserDataManager.Instance.GetAllTotalChapterStarCount() < _specDungeonTrialData.need_star)
            {
                ToastManager.Instance.ShowToast("TEST - 스테이지 별 갯수 부족");
                return;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            // todo.. 던전 입장 처리

            SceneLoading.GoToNextScene("InGame",
                (InGameType.TRIAL, (IGameStateUI) new InGameMainStateTrialDungeonUI(), (int)_specDungeonTrialData.dungeon_id)).Forget();
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardInfoContentTransform);
            BMUtil.RemoveChildObjects(_monsterInfoScrollRect.content);
        }
    }
}
