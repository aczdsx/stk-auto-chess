using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class CharacterDetailPopupParam
    {
        public int CharacterID;
        public List<int> CharacterIdList;

        public CharacterDetailPopupParam(int characterID, List<int> characterIdList)
        {
            CharacterID = characterID;
            CharacterIdList = characterIdList;
        }
    }

    public class CharacterDetailMainLayerPopup : UILayerPopupBase
    {

        [SerializeField] private CAButton _backButton;
        [SerializeField] private CAButton _elementSynergyButton;
        [SerializeField] private CAButton _asterismSynergyButton;
        [SerializeField] private CAButton _characterObjLeftButton;
        [SerializeField] private CAButton _characterObjRightButton;

        [Space(10)]
        [SerializeField] private GameObject _characterIllustParentObject;
        [SerializeField] private GameObject _characterSDParentObject;
        [Space(10)]
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _classSynergyUI;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterLevelText;
        [SerializeField] private TextMeshProUGUI _characterPositionTypeText;

        [Space(10)]
        [SerializeField] private GameObject _characterGradeImageObject_R;
        [SerializeField] private GameObject _characterGradeImageObject_SR;
        [SerializeField] private GameObject _characterGradeImageObject_SSR;

        [Header("Category Toggle")]
        [SerializeField] private CAToggle _growLayerTabButton;
        [SerializeField] private CAToggle _skillLayerTabButton;
        [SerializeField] private CharacterDetailGrowLayer _characterGrowLayer;
        [SerializeField] private CharacterDetailSkillLayer _characterSkillLayer;
        [SerializeField] private GameObject _UnOwnedCharacterDim;

        private AsyncOperationHandle<GameObject> _illustHandle;
        private AsyncOperationHandle<GameObject> _sdHandle;
        private CharacterInfo _specCharacterData;

        private Material _illustMaterial;

        private CharacterIllust _characterIllust;

        // 탭 관리
        private CharacterCollectionPopupTabType _currentTabType = CharacterCollectionPopupTabType.GROW;
        private int _currentCharacterID;
        private List<int> _characterIdList;

        public CharacterCollectionPopupTabType CurrentTabType => _currentTabType;

        protected override void Awake()
        {
            base.Awake();
            _backButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBackButton()).AddTo(this);
            _elementSynergyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickElementSynergyButton()).AddTo(this);
            _asterismSynergyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickAsterismSynergyButton()).AddTo(this);
            _characterObjLeftButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickLeftButton()).AddTo(this);
            _characterObjRightButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickRightButton()).AddTo(this);
            _growLayerTabButton.onValueChanged.AddListener(isOn => { if (isOn) OnClickGrowLayerTabButton(); });
            _skillLayerTabButton.onValueChanged.AddListener(isOn => { if (isOn) OnClickSkillLayerTabButton(); });
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            if (param is CharacterDetailPopupParam popupParam)
            {
                _currentCharacterID = popupParam.CharacterID;
                _characterIdList = popupParam.CharacterIdList;
                _currentTabType = CharacterCollectionPopupTabType.GROW;
                InitLayer(_currentCharacterID);
            }
        }

        private void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);

            ClearLayer();

            ChangeTabType(CharacterCollectionPopupTabType.GROW, true);
            SetCharacterInfo();
            SetUserCharacterInfo();
        }

        public void RefreshLayer()
        {
            SetUserCharacterInfo();
        }

        private void ChangeTabType(CharacterCollectionPopupTabType tabType, bool force = false)
        {
            if (_currentTabType == tabType && !force) return;

            _currentTabType = tabType;
            SetTabState();
            UpdateLayerVisibility();
        }

        private void UpdateLayerVisibility()
        {
            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);
            bool isGrow = _currentTabType == CharacterCollectionPopupTabType.GROW;
            bool isSkill = _currentTabType == CharacterCollectionPopupTabType.SKILL;

            _characterGrowLayer.gameObject.SetActive(isGrow);
            _characterSkillLayer.gameObject.SetActive(isSkill && isHaveCharacter);
            _UnOwnedCharacterDim.SetActive(!isHaveCharacter);

            if (isGrow)
            {
                _characterGrowLayer.InitLayer(_currentCharacterID);
            }
            else if (isSkill && isHaveCharacter)
            {
                _characterSkillLayer.InitLayer(_currentCharacterID);
            }
        }

        private void OnClickGrowLayerTabButton()
        {
            ChangeTabType(CharacterCollectionPopupTabType.GROW);
        }

        private void OnClickSkillLayerTabButton()
        {
            if (ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_HAVE_CHARACTER");
                return;
            }

            ChangeTabType(CharacterCollectionPopupTabType.SKILL);
        }

        public void OnClickLeftButton()
        {
            if (_currentCharacterID == 0 || _characterIdList == null || _characterIdList.Count <= 1) return;

            int currentIndex = _characterIdList.IndexOf(_currentCharacterID);
            if (currentIndex < 0) return;

            int nextIndex = (currentIndex - 1 + _characterIdList.Count) % _characterIdList.Count;
            _currentCharacterID = _characterIdList[nextIndex];
            InitLayer(_currentCharacterID);
        }

        public void OnClickRightButton()
        {
            if (_currentCharacterID == 0 || _characterIdList == null || _characterIdList.Count <= 1) return;

            int currentIndex = _characterIdList.IndexOf(_currentCharacterID);
            if (currentIndex < 0) return;

            int nextIndex = (currentIndex + 1) % _characterIdList.Count;
            _currentCharacterID = _characterIdList[nextIndex];
            InitLayer(_currentCharacterID);
        }

        private void SetTabState()
        {
            _growLayerTabButton.isOn = _currentTabType == CharacterCollectionPopupTabType.GROW;
            _skillLayerTabButton.isOn = _currentTabType == CharacterCollectionPopupTabType.SKILL;

            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);
            _skillLayerTabButton.interactable = isHaveCharacter;
        }

        private async Task SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);

            // 캐릭터 일러스트 생성
            string illustPrefabName = ZString.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            _illustHandle = Addressables.InstantiateAsync(illustPrefabName, _characterIllustParentObject.transform);
            var newObject = await _illustHandle.ToUniTask();
            if (newObject == null)
            {
                Debug.LogColor($"CharacterDetailMainLayer.SetCharacterInfo() : {illustPrefabName} is null","red");
                return;
            }

            // 캐릭터 SD 캐릭터 생성
            string sdPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            _sdHandle = Addressables.InstantiateAsync(sdPrefabName, _characterSDParentObject.transform);

            _characterNameText.text = LanguageManager.Instance.GetDefaultText(_specCharacterData.name_token);
            _characterPositionTypeText.text = _specCharacterData.character_position_type.ToString();

            _characterGradeImageObject_R.SetActive(_specCharacterData.grade_type == GradeType.R);
            _characterGradeImageObject_SR.SetActive(_specCharacterData.grade_type == GradeType.SR);
            _characterGradeImageObject_SSR.SetActive(_specCharacterData.grade_type == GradeType.SSR);

            _elementSynergyUI.SetSynergyUI(_specCharacterData.character_element_type);
            _classSynergyUI.SetSynergyUI(_specCharacterData.character_stella_type);
        }

        private void SetUserCharacterInfo()
        {
            if (_specCharacterData == null) return;

            CharacterData userCharacterData = ServerDataManager.Instance.Character.GetCharacter(_specCharacterData.id);

            if (userCharacterData != null)
            {
                _characterLevelText.text = $"Lv.{userCharacterData.Level}";
            }
        }

        private void OnClickBackButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickElementSynergyButton()
        {
            var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_specCharacterData.character_element_type);
            if (specSynergyDataList != null && specSynergyDataList.Count > 0)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipPopup>(specSynergyDataList).Forget();
            }
        }

        private void OnClickAsterismSynergyButton()
        {
            var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_specCharacterData.character_stella_type);
            if (specSynergyDataList != null && specSynergyDataList.Count > 0)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipPopup>(specSynergyDataList).Forget();
            }
        }


        private void OnDestroy()
        {
            if (_illustHandle.IsValid())
                Addressables.ReleaseInstance(_illustHandle);
            if (_sdHandle.IsValid())
                Addressables.ReleaseInstance(_sdHandle);
        }

        private void ClearLayer()
        {
            if (_illustHandle.IsValid())
            {
                Addressables.ReleaseInstance(_illustHandle);
                _illustHandle = default;
            }
            if (_sdHandle.IsValid())
            {
                Addressables.ReleaseInstance(_sdHandle);
                _sdHandle = default;
            }
            BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);
            BMUtil.RemoveChildObjects(_characterSDParentObject.transform);
        }
    }
}
