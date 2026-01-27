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

namespace CookApps.AutoBattler
{
    public class CharacterDetailMainLayer : CachedMonoBehaviour
    {
        public Material IllustMaterial => _characterIllust.IllustMaterial;

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

        private CharacterCollectionPopup _parentCollectionPopup;

        private CharacterInfo _specCharacterData;

        private Material _illustMaterial;

        private CharacterIllust _characterIllust;

        private void Awake()
        {
            _backButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickBackButton()).AddTo(this);
            _elementSynergyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickElementSynergyButton()).AddTo(this);
            _asterismSynergyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickAsterismSynergyButton()).AddTo(this);
            // ! TODO SpecDataManager.Instance.GetLeftCharacterID의 로직이 이상합니다! 둘이 서로 반대로 구현 되어 있어서 일단 작동 의도대로 넣기위해 반대로 넣었습니다!
            _characterObjLeftButton.OnClickAsObservable().Subscribe(this, (_, self) => _parentCollectionPopup.OnClickRightButton()).AddTo(this);
            // ! TODO SpecDataManager.Instance.GetRightCharacterID의 로직이 이상합니다! 둘이 서로 반대로 구현 되어 있어서 일단 작동 의도대로 넣기위해 반대로 넣었습니다!
            _characterObjRightButton.OnClickAsObservable().Subscribe(this, (_, self) => _parentCollectionPopup.OnClickLeftButton()).AddTo(this);
        }

        public void InitLayer(int characterID, CharacterCollectionPopup _parentPopup)
        {
            _parentCollectionPopup = _parentPopup;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);

            ClearLayer();

            SetTabState();
            SetCharacterInfo();
            SetUserCharacterInfo();
        }

        public void RefreshLayer()
        {
            SetUserCharacterInfo();
        }

        public void OnClickGrowLayerTabButton()
        {
            if (_parentCollectionPopup == null) return;

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.GROW);
        }

        public void OnClickSkillLayerTabButton()
        {
            if (_parentCollectionPopup == null) return;
            if (ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_HAVE_CHARACTER");
                return;
            }

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.SKILL);
        }
        
        public void OnClickLeftButton()
        {
            if (_parentCollectionPopup == null) return;

            var nextCharacterID = SpecDataManager.Instance.GetLeftOwnedCharacterId(_specCharacterData.id);
            if (nextCharacterID != -1)
            {
                InitLayer(nextCharacterID, _parentCollectionPopup);
            }
        }

        public void OnClickRightButton()
        {
            if (_parentCollectionPopup == null) return;

            var nextCharacterID = SpecDataManager.Instance.GetRightOwnedCharacterId(_specCharacterData.id);
            if (nextCharacterID != -1)
            {
                InitLayer(nextCharacterID, _parentCollectionPopup);
            }
        }

        private void SetTabState()
        {
            if (_parentCollectionPopup == null) return;

            _growLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.GROW;
            _skillLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.SKILL;

            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);
            _skillLayerTabButton.interactable = isHaveCharacter;
        }

        private async Task SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            bool isHaveCharacter = ServerDataManager.Instance.Character.HasCharacter(_specCharacterData.id);

            // 캐릭터 일러스트 생성
            string illustPrefabName = ZString.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            var newObject = await Addressables.InstantiateAsync(illustPrefabName, _characterIllustParentObject.transform).ToUniTask();
            if (newObject == null)
            {
                Debug.LogColor($"CharacterDetailMainLayer.SetCharacterInfo() : {illustPrefabName} is null","red");
                return;
            }
            _characterIllust = newObject.GetComponent<CharacterIllust>();
            _characterIllust.SetCharacterAnimation("idle");

            // 캐릭터 SD 캐릭터 생성
            string sdPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            AddressablesUtil.Instantiate(sdPrefabName, _characterSDParentObject.transform);

            _characterNameText.text = LanguageManager.Instance.GetDefaultText(_specCharacterData.name_token);
            _characterPositionTypeText.text = _specCharacterData.character_position_type.ToString();

            _characterGradeImageObject_R.SetActive(_specCharacterData.grade_type == GradeType.RARE);
            _characterGradeImageObject_SR.SetActive(_specCharacterData.grade_type == GradeType.EPIC);
            _characterGradeImageObject_SSR.SetActive(_specCharacterData.grade_type == GradeType.LEGENDARY);

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
            if (_parentCollectionPopup == null) return;

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.MAIN);
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


        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);
            BMUtil.RemoveChildObjects(_characterSDParentObject.transform);
        }
    }
}
