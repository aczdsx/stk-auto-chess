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

        private DungeonBabelInfo _specDungeonTrialData;

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
            CharacterStatData topStatData = null;//114333202
            
            List<CharacterStatData> dataList = new List<CharacterStatData>();
            foreach (var monsterData in monsterDataList)
            {
                var statData = new CharacterStatData(monsterData.monster_id, monsterData.monster_lv,
                    monsterData.multiple_atk, monsterData.multiple_hp);
                
                dataList.Add((statData));
            }

            dataList.Sort((a, b) => b.GetAttrValueCP().CompareTo(a.GetAttrValueCP()));
            foreach (var monsterData in dataList)
            {
                GameObject newSlotObject = Instantiate(_monsterInfoSlotObject, _monsterInfoScrollRect.content);
                DungeonMonsterInfoSlot newSlot = newSlotObject.GetComponent<DungeonMonsterInfoSlot>();
                newSlot?.SetMonsterInfoSlot(monsterData);
    
                double attrValue = monsterData.GetAttrValueCP();

                attr += attrValue;

                if (topStatData == null || topStatData.GetAttrValueCP() < attrValue)
                    topStatData = monsterData;
            }
            
            _currentStepImage.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(_specDungeonTrialData.trial_type, false);
            _currentStepName.text = StringUtil.GetTrialDungeonString(_specDungeonTrialData, true);
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
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_CONDITION_STAR_LACK");
                return;
            }
            
            var lastDungeonData = UserDataManager.Instance.GetLastTrialDungeonData();
            
            if (lastDungeonData.Order > _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_ALREADY_CLEAR");
                return;
            }
            
            if (lastDungeonData.Order < _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_NEED_BEFORE_STAGE");
                return;
            }

            // todo.. 던전 입장 처리

            InGameManager.Instance.EndInGame();
            SceneTransition.Create<SceneTransition_FadeInOut>();
            SceneTransition.FadeInAsync().Forget();

            InGameType inGameType = (_specDungeonTrialData.dungeon_map_id == 1) ? InGameType.TRIAL_BOSS: InGameType.TRIAL;
            SceneLoading.GoToNextScene("InGame",
                (inGameType, (IGameStateUICore) new InGameMainStateTrialDungeon(), _specDungeonTrialData.dungeon_id));
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
