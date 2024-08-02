using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
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
        [SerializeField] private GameObject _dungeonClearBtnObj;

        [Header("Top Dungeon Progress Layer")]
        [SerializeField] private ScrollRect _stepScrollRect;
        [SerializeField] private GameObject _stepSlotObject;

        private List<DungeonTrialStepSlot> _stepSlotList = new List<DungeonTrialStepSlot>();

        [Header("Current Dungeon Info Layer")]
        [SerializeField] private Image _currentStepImage;
        [SerializeField] private TextMeshProUGUI _currentStepName;
        [SerializeField] private TextMeshProUGUI _currentStepAttr;
        [SerializeField] private GameObject _characterImageParentObject;
        [SerializeField] private GameObject _lightFxObj;
        [SerializeField] private GameObject _dimmedLightFxObj;
        [SerializeField] private UICharacter _uiCharacter;

        [Header("Dungeon Monster Info Layer")]
        [SerializeField] private ScrollRect _monsterInfoScrollRect;
        [SerializeField] private GameObject _monsterInfoSlotObject;

        [Header("Dungeon Reward Info Layer")]
        [SerializeField] private Transform _rewardInfoContentTransform;
        [SerializeField] private GameObject _rewardItemSlotObject;
        [SerializeField] private GameObject _gradeUpObj;
        [SerializeField] private GameObject _rewardObj;

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

            var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
            bool isDimmed = lastDungeonData.Order > _specDungeonTrialData.order;
            _dimmedLightFxObj.SetActive(isDimmed);
            _lightFxObj.SetActive(!isDimmed);
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
            
            var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
            _dungeonClearBtnObj.SetActive(lastDungeonData.Order > _specDungeonTrialData.order);
            _EnterDungeonButton.gameObject.SetActive(!_dungeonClearBtnObj.activeSelf);
            _needStageStarText.text = StringUtil.GetCompareString(UserDataManager.Instance.GetAllTotalChapterStarCount(), _specDungeonTrialData.need_star);
        }

        private void SetMonsterInfoLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;
            BMUtil.RemoveChildObjects(_monsterInfoScrollRect.content);
            var monsterDataList = SpecDataManager.Instance.GetSpecDungeonMonsterDataList(DungeonType.TRIAL, CurrentUserDungeonData.DungeonId);
            double attr = 0;
            CharacterStatData topStatData = null;
            foreach (var monsterData in monsterDataList)
            {
                var statData = new CharacterStatData(monsterData.monster_id, monsterData.monster_lv, monsterData.multiple_atk,
                    monsterData.multiple_hp);
                
                GameObject newSlotObject = Instantiate(_monsterInfoSlotObject, _monsterInfoScrollRect.content);
                DungeonMonsterInfoSlot newSlot = newSlotObject.GetComponent<DungeonMonsterInfoSlot>();
                newSlot?.SetMonsterInfoSlot(statData);
                attr += statData.GetAttrValue();

                if (topStatData == null || topStatData.GetAttrValue() < statData.GetAttrValue())
                    topStatData = statData;
            }
            
            _currentStepImage.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(_specDungeonTrialData.trial_type, false);
            _currentStepName.text = StringUtil.GetTrialDungeonString(_specDungeonTrialData);
            _currentStepAttr.text = attr.ToString("n0");

            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
            if (topStatData != null)
            {
                var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
                bool isDimmed = lastDungeonData.Order > _specDungeonTrialData.order;

                string characterPrefabName =
                    string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, topStatData.Spec.prefab_id);
                GameObject obj =
                    AddressablesUtil.Instantiate(characterPrefabName, _characterImageParentObject.transform);
                _uiCharacter = obj.GetComponent<UICharacter>();
                _uiCharacter.SetGrayCharacter(isDimmed);
            }
        }

        private void SetRewardInfoLayer()
        {
            if (CurrentUserDungeonData == null || _specDungeonTrialData == null) return;

            BMUtil.RemoveChildObjects(_rewardInfoContentTransform);

            _gradeUpObj.SetActive(_specDungeonTrialData.is_grade_up);
            _rewardObj.SetActive(!_specDungeonTrialData.is_grade_up);
            if (!_specDungeonTrialData.is_grade_up)
            {
                var rewardDataList = SpecDataManager.Instance.GetSpecDungeonRewardDataList(DungeonType.TRIAL, CurrentUserDungeonData.DungeonId);

                foreach (var rewardData in rewardDataList)
                {
                    GameObject newSlotObject = Instantiate(_rewardItemSlotObject, _rewardInfoContentTransform);
                    RewardItemSlot newSlot = newSlotObject.GetComponent<RewardItemSlot>();

                    RewardItem newRewardItem = new RewardItem(rewardData.item_type, rewardData.item_key, rewardData.item_count);
                    newSlot?.SetRewardSlot(newRewardItem);
                    
                    var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
                    newSlot?.SetCheckSlot(lastDungeonData.Order > _specDungeonTrialData.order);
                }
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
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            // 던전 진입 가능 조건 검사
            if (UserDataManager.Instance.GetAllTotalChapterStarCount() < _specDungeonTrialData.need_star)
            {
                ToastManager.Instance.ShowToast("TEST - 스테이지 별 갯수 부족.");
                return;
            }
            
            var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
            
            if (lastDungeonData.Order > _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToast("TEST - 이미 클리어한 던전입니다.");
                return;
            }
            
            if (lastDungeonData.Order < _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToast("TEST - 이전 단계를 클리어 해주세요.");
                return;
            }

            // todo.. 던전 입장 처리


            InGameManager.Instance.EndInGame();
            var transition = SceneTransition_FadeInOut.Create();

            InGameType inGameType = (_specDungeonTrialData.dungeon_map_id == 1) ? InGameType.TRIAL_BOSS: InGameType.TRIAL;
            SceneLoading.GoToNextScene("InGame",
                (inGameType, (IGameStateUI) new InGameMainStateTrialDungeonUI(), (int)_specDungeonTrialData.dungeon_id), transition).Forget();
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
