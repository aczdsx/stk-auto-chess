using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailMainLayer : CachedMonoBehaviour
    {
        public Material IllustMaterial => _characterIllust.IllustMaterial;

        [SerializeField] private CAButton _backButton;
        [SerializeField] private CAButton _elementSynergyButton;
        [SerializeField] private CAButton _asterismSynergyButton;

        [Space(10)]
        [SerializeField] private GameObject _characterIllustParentObject;
        [SerializeField] private GameObject _characterSDParentObject;
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _classSynergyUI;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterLevelText;
        [SerializeField] private TextMeshProUGUI _characterPositionTypeText;

        [Space(10)]
        [SerializeField] private GameObject _characterGradeImageObject_R;
        [SerializeField] private GameObject _characterGradeImageObject_SR;
        [SerializeField] private GameObject _characterGradeImageObject_SSR;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;

        [Header("Category Toggle")]
        [SerializeField] private CAToggle _growLayerTabButton;
        [SerializeField] private CAToggle _skillLayerTabButton;

        private CharacterCollectionPopup _parentCollectionPopup;

        private SpecCharacter _specCharacterData;

        private Material _illustMaterial;

        private CharacterIllust _characterIllust;

        private void Awake()
        {
            _backButton.onClick.AddListener(OnClickBackButton);
            _elementSynergyButton.onClick.AddListener(OnClickElementSynergyButton);
            _asterismSynergyButton.onClick.AddListener(OnClickAsterismSynergyButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _backButton.onClick.RemoveListener(OnClickBackButton);
            _elementSynergyButton.onClick.RemoveListener(OnClickElementSynergyButton);
            _asterismSynergyButton.onClick.RemoveListener(OnClickAsterismSynergyButton);
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
            if (UserDataManager.Instance.IsHaveCharacter(_specCharacterData.character_id) == false)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_HAVE_CHARACTER");
                return;
            }

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.SKILL);
        }
        
        public void OnClickLeftButton()
        {
            if (_parentCollectionPopup == null) return;

            var leftCharacterID = SpecDataManager.Instance.GetLeftCharacterID(_specCharacterData.character_id, CharacterType.CHARACTER);
            InitLayer(leftCharacterID, _parentCollectionPopup);
        }

        public void OnClickRightButton()
        {
            if (_parentCollectionPopup == null) return;

            var rightCharacterID = SpecDataManager.Instance.GetRightCharacterID(_specCharacterData.character_id, CharacterType.CHARACTER);
            InitLayer(rightCharacterID, _parentCollectionPopup);
        }

        private void SetTabState()
        {
            if (_parentCollectionPopup == null) return;

            _growLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.GROW;
            _skillLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.SKILL;

            bool isHaveCharacter = UserDataManager.Instance.IsHaveCharacter(_specCharacterData.character_id);
            _skillLayerTabButton.interactable = isHaveCharacter;
        }

        private void SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            bool isHaveCharacter = UserDataManager.Instance.IsHaveCharacter(_specCharacterData.character_id);

            // 캐릭터 일러스트 생성
            string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            var newObject = AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);

            _characterIllust = newObject.GetComponent<CharacterIllust>();
            _characterIllust.SetCharacterAnimation("idle");

            // 캐릭터 SD 캐릭터 생성
            string sdPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            AddressablesUtil.Instantiate(sdPrefabName, _characterSDParentObject.transform);

            _characterNameText.text = LanguageManager.Instance.GetLanguageText(_specCharacterData.name_token);
            _characterPositionTypeText.text = _specCharacterData.position_type.ToString();

            _characterGradeImageObject_R.SetActive(_specCharacterData.grade_type == GradeType.RARE);
            _characterGradeImageObject_SR.SetActive(_specCharacterData.grade_type == GradeType.EPIC);
            _characterGradeImageObject_SSR.SetActive(_specCharacterData.grade_type == GradeType.LEGEND);

            _elementSynergyUI.SetSynergyUI(_specCharacterData.element_type);
            _classSynergyUI.SetSynergyUI(_specCharacterData.asterism_type);

            SetStarObject(_specCharacterData.grade_type);
        }

        private void SetUserCharacterInfo()
        {
            if (_specCharacterData == null) return;

            UserCharacter userCharacterData = UserDataManager.Instance.GetUserCharacter(_specCharacterData.character_id);

            if (userCharacterData != null)
            {
                _characterLevelText.text = $"Lv.{userCharacterData.Level}";
            }
        }

        private void SetStarObject(GradeType gradeType)
        {
            for (int i = 0; i < _starObjectList.Count; i++)
            {
                _starObjectList[i].SetActive(i <= (int)gradeType);
            }
        }

        private void OnClickBackButton()
        {
            if (_parentCollectionPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.MAIN);
        }

        private void OnClickElementSynergyButton()
        {
            var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_specCharacterData.element_type);
            if (specSynergyDataList != null && specSynergyDataList.Count > 0)
            {
                var filteredSynergyDataList = specSynergyDataList.Where(l => l.grade != 0).ToList();
                SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipPopup>(filteredSynergyDataList).Forget();
            }
        }

        private void OnClickAsterismSynergyButton()
        {
            var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_specCharacterData.asterism_type);
            if (specSynergyDataList != null && specSynergyDataList.Count > 0)
            {
                var filteredSynergyDataList = specSynergyDataList.Where(l => l.grade != 0).ToList();
                SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipPopup>(filteredSynergyDataList).Forget();
            }
        }


        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);
            BMUtil.RemoveChildObjects(_characterSDParentObject.transform);
        }
    }
}
