using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailMainLayer : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _backButton;

        [Space(10)]
        [SerializeField] private GameObject _characterIllustParentObject;
        [SerializeField] private GameObject _characterSDParentObject;
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _classSynergyUI;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterGradeText;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;

        [Header("Category Toggle")]
        [SerializeField] private CAToggle _growLayerTabButton;
        [SerializeField] private CAToggle _skillLayerTabButton;

        private CharacterCollectionPopup _parentCollectionPopup;

        private SpecCharacter _specCharacterData;

        private void Awake()
        {
            _backButton.onClick.AddListener(OnClickBackButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _backButton.onClick.RemoveListener(OnClickBackButton);
        }

        public void InitLayer(int prefabID, CharacterCollectionPopup _parentPopup)
        {
            _parentCollectionPopup = _parentPopup;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(prefabID);

            ClearLayer();

            SetTabState();
            SetCharacterInfo();
        }

        public void OnClickGrowLayerTabButton()
        {
            if (_parentCollectionPopup == null) return;

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.GROW);
        }

        public void OnClickSkillLayerTabButton()
        {
            if (_parentCollectionPopup == null) return;

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.SKILL);
        }

        private void SetTabState()
        {
            if (_parentCollectionPopup == null) return;

            _growLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.GROW;
            _skillLayerTabButton.isOn = _parentCollectionPopup.CurrentTabType == CharacterCollectionPopupTabType.SKILL;
        }

        private void SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            // 캐릭터 일러스트 생성
            string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);

            // 캐릭터 SD 캐릭터 생성
            string sdPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
            AddressablesUtil.Instantiate(sdPrefabName, _characterSDParentObject.transform);

            _characterNameText.text = _specCharacterData.name_token;
            _characterGradeText.text = LanguageManager.Instance.GetGradeText(_specCharacterData.grade_type);

            _elementSynergyUI.SetSynergyUI(_specCharacterData.element_type);
            _classSynergyUI.SetPositionSynergyUI(_specCharacterData.character_position_type);

            SetStarObject(_specCharacterData.grade_type);
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

            _parentCollectionPopup.ChangeTabType(CharacterCollectionPopupTabType.MAIN);
        }

        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);
            BMUtil.RemoveChildObjects(_characterSDParentObject.transform);
        }
    }
}
