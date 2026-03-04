using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum DungeonTrialPopupRefreshType
    {
        ALL,
        STEP_SLOT,
        DUNGEON_INFO,
    }

    public class DungeonTrialPopup : UILayerPopupBase
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
        [SerializeField] private SpriteLoader _currentStepSpriteLoader;
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

        private AsyncOperationHandle<GameObject> _characterHandle;
        private DungeonBabelInfo _specDungeonTrialData;

        // Current Selected Dungeon Spec ID for viewing
        public int CurrentSelectedDungeonId { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _EnterDungeonButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickEnterDungeonButtonAsync(), AwaitOperation.Drop).AddTo(this);
            
            // Subscribe to Server Data Changes
            ServerDataManager.Instance.TrialDungeon.OnChanged
                .Subscribe(this, (_, self) => self.OnServerDataChanged())
                .AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            var currentOrder = ServerDataManager.Instance.TrialDungeon.Order;
            if (currentOrder == 0) currentOrder = 1;

            var spec = SpecDataManager.Instance.GetSpecDungeonTrialDataByOrder((int)currentOrder);
            if (spec != null)
            {
                CurrentSelectedDungeonId = spec.dungeon_id;
                _specDungeonTrialData = spec;
            }
            else
            {
                // Fallback
                var list = SpecDataManager.Instance.GetSpecDungeonTrialDataList(DungeonType.TRIAL);
                if (list != null && list.Count > 0)
                {
                    CurrentSelectedDungeonId = list[0].dungeon_id;
                    _specDungeonTrialData = list[0];
                }
            }

            InitDungeonPopup();
        }
        
        private void OnServerDataChanged()
        {
            // If the popup is open, we might want to refresh. 
            // Primarily useful if the user clears a dungeon and comes back, 
            // but usually we close/reopen or have a result popup.
            // For now, let's just refresh visual states if needed.
            RefreshDungeonTrialPopup(DungeonTrialPopupRefreshType.ALL);
        }

        public void SetCurrentSelectedDungeonData(int dungeonID)
        {
            CurrentSelectedDungeonId = dungeonID;
            _specDungeonTrialData = SpecDataManager.Instance.GetSpecDungeonTrialData(dungeonID);

            var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
            
            // If 0, it means not started (essentially order 1 is next)
            uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;
            
            // Logic: Dimmed if this dungeon is already cleared (Order > Spec.order)
            // Wait, Order 1 is current. If completed 1, user has Order 2 (assuming).
            // Usually Order represents "Current Stage to Clear".
            // So if ServerOrder > SpecOrder, it is cleared (Dimmed).
            // If ServerOrder == SpecOrder, it is Current (Light).
            // If ServerOrder < SpecOrder, it is Future (Light/Locked visually elsewhere).
            
            bool isCleared = effectiveServerOrder > _specDungeonTrialData.order;
            
            _dimmedLightFxObj.SetActive(isCleared);
            _lightFxObj.SetActive(!isCleared);
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
            
            // Scroll to current selected
            // Implementation omitted for brevity, logic exists in slots usually
        }

        private void SetCommonInfoLayer()
        {
            if (_specDungeonTrialData == null) return;

            var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
            uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;

            // Clear Button Active if User Order > Selected Dungeon Order
            bool isCleared = effectiveServerOrder > _specDungeonTrialData.order;
            
            _dungeonClearBtnObj.SetActive(isCleared);
            _EnterDungeonButton.gameObject.SetActive(!_dungeonClearBtnObj.activeSelf);
            _needStageStarText.text = StringUtil.GetCompareString((int)ServerDataManager.Instance.Battle.TotalStarCount, _specDungeonTrialData.need_star);
        }

        private void SetMonsterInfoLayer()
        {
            if (_specDungeonTrialData == null) return;
            BMUtil.RemoveChildObjects(_monsterInfoScrollRect.content);
            var monsterDataList = SpecDataManager.Instance.GetSpecDungeonMonsterDataList(DungeonType.TRIAL, _specDungeonTrialData.dungeon_id);
            double attr = 0;
            CharacterStatData topStatData = null;

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

            _currentStepSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specDungeonTrialData.trial_type, false)).Forget();
            _currentStepName.text = StringUtil.GetTrialDungeonString(_specDungeonTrialData, true);
            _currentStepAttr.text = attr.ToString("n0");

            if (_characterHandle.IsValid())
            {
                Addressables.ReleaseInstance(_characterHandle);
                _characterHandle = default;
            }
            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
            if (topStatData != null)
            {
                var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
                uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;
                bool isCleared = effectiveServerOrder > _specDungeonTrialData.order;

                string characterPrefabName =
                    string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, topStatData.Spec.prefab_id);
                _characterHandle = Addressables.InstantiateAsync(characterPrefabName, _characterImageParentObject.transform);
                GameObject obj = _characterHandle.WaitForCompletion();
                _uiCharacter = obj.GetComponent<UICharacter>();
                _uiCharacter.SetGrayCharacter(isCleared);
            }
        }

        private void SetRewardInfoLayer()
        {
            if (_specDungeonTrialData == null) return;

            BMUtil.RemoveChildObjects(_rewardInfoContentTransform);

            _gradeUpObj.SetActive(_specDungeonTrialData.is_grade_up);
            _rewardObj.SetActive(!_specDungeonTrialData.is_grade_up);
            if (!_specDungeonTrialData.is_grade_up)
            {
                var rewardDataList = SpecDataManager.Instance.GetSpecDungeonRewardDataList(DungeonType.TRIAL, _specDungeonTrialData.dungeon_id);

                foreach (var rewardData in rewardDataList)
                {
                    GameObject newSlotObject = Instantiate(_rewardItemSlotObject, _rewardInfoContentTransform);
                    RewardItemSlot newSlot = newSlotObject.GetComponent<RewardItemSlot>();

                    RewardItem newRewardItem = new RewardItem(rewardData.item_id, rewardData.item_count);
                    newSlot?.SetRewardSlot(newRewardItem);

                    var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
                    uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;
                    
                    newSlot?.SetCheckSlot(effectiveServerOrder > _specDungeonTrialData.order);
                }
            }
        }

        private void SetStepSlotLayer()
        {
            BMUtil.RemoveChildObjects(_stepScrollRect.content);
            _stepSlotList.Clear();

            var totalDungeonDataList = SpecDataManager.Instance.GetSpecDungeonTrialDataList(DungeonType.TRIAL);

            var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
            uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;

            foreach (var dungeonData in totalDungeonDataList)
            {
                GameObject newSlotObject = Instantiate(_stepSlotObject, _stepScrollRect.content);

                DungeonTrialStepSlot newSlot = newSlotObject.GetComponent<DungeonTrialStepSlot>();
                // Pass order instead of user data
                newSlot?.SetStepSlot(this, dungeonData, effectiveServerOrder);

                _stepSlotList.Add(newSlot);
            }
        }

        private void RefreshStepSlotLayer()
        {
            if (_stepSlotList == null || _stepSlotList.Count <= 0) return;

            var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
            uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;

            _stepSlotList.ForEach(slot => slot.RefreshSlot(effectiveServerOrder));
        }

        private async UniTask OnClickEnterDungeonButtonAsync()
        {
            //Check Star Condition
            if (ServerDataManager.Instance.Battle.TotalStarCount < _specDungeonTrialData.need_star)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_CONDITION_STAR_LACK");
                return;
            }

            var serverOrder = ServerDataManager.Instance.TrialDungeon.Order;
            uint effectiveServerOrder = serverOrder == 0 ? 1 : serverOrder;

            if (effectiveServerOrder > _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_ALREADY_CLEAR");
                return;
            }
            
            if (effectiveServerOrder < _specDungeonTrialData.order)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_TRIAL_ENTRANCE_NEED_BEFORE_STAGE");
                return;
            }

            // Request Enter to Server
            var response = await NetManager.Instance.TrialDungeon.EnterAsync();
            if (response is not { IsSuccess: true })
            {
                return;
            }

            //InGameManager.Instance.EndInGame();
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();

            var inGameParams = new InGameMainParams(
                _specDungeonTrialData.dungeon_map_id == 1 ? InGameType.TRIAL_BOSS : InGameType.TRIAL,
                new InGameMainStateTrialDungeon(),
                _specDungeonTrialData.dungeon_id,
                response.BattleSessionId,
                response.BattleSeed
            );
            
            SceneLoading.GoToNextScene("InGame", inGameParams);
        }

        private void OnDestroy()
        {
            if (_characterHandle.IsValid())
                Addressables.ReleaseInstance(_characterHandle);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            if (_characterHandle.IsValid())
            {
                Addressables.ReleaseInstance(_characterHandle);
                _characterHandle = default;
            }
            BMUtil.RemoveChildObjects(_rewardInfoContentTransform);
            BMUtil.RemoveChildObjects(_monsterInfoScrollRect.content);
        }
    }
}
