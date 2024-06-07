using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterCardSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _characterCardButton;

        [Header("BG Layer")]
        [SerializeField] private GameObject _lockBGLayerObject;
        [SerializeField] private GameObject _normalBGLayerObject;
        [SerializeField] private GameObject _SSRBGLayerObject;

        [Header("Character Info")]
        [SerializeField] private GameObject _characterImageParentObject;
        [SerializeField] private TextMeshProUGUI _chracterLevelText;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private SynergyUI _positionSynergyUI;

        [Space(10)]
        [SerializeField] private GameObject _outlineActiveObject;
        [SerializeField] private GameObject _outlineInactiveObject;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;


        private SpecCharacter _characterData;
        private UserCharacter _userCharacterData;

        private CharacterCollectionPopup _parentCollectionPopup;

        public SpecCharacter CharacterData => _characterData;

        private void Awake()
        {
            _characterCardButton.onClick.AddListener(OnClickCardSlot);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _characterCardButton.onClick.RemoveListener(OnClickCardSlot);
        }

        public void SetCharcacterSlot(SpecCharacter characterData, CharacterCollectionPopup _parentPopup)
        {
            if (characterData == null) return;

            ClearCardSlot();

            _parentCollectionPopup = _parentPopup;

            _characterData = characterData;
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(_characterData.character_id);

            bool haveCharacter = _userCharacterData != null;

            // 기본 데이터 관련 세팅
            string characterPrefabName = string.Format(Defines.CHARACTER_UI_PREFEAB_NAME_FORMAT, _characterData.character_id);
            AddressablesUtil.Instantiate(characterPrefabName, _characterImageParentObject.transform);

            _gradeImage.sprite = ImageManager.Instance.GetGradeTypeSprite(_characterData.grade_type, haveCharacter);

            _synergyUI.SetSynergyUI(_characterData.element_type, haveCharacter);
            _positionSynergyUI.SetPositionSynergyUI(_characterData.character_position_type, haveCharacter);

            _chracterLevelText.gameObject.SetActive(haveCharacter);
            if (haveCharacter)
            {
                _chracterLevelText.text = _userCharacterData.Level.ToString();
            }

            SetStarObject(_characterData.grade_type);

            // 캐릭터 보유 여부 관련 처리
            _outlineActiveObject.SetActive(haveCharacter);
            _outlineInactiveObject.SetActive(!haveCharacter);

            // BG Layer 세팅
            _lockBGLayerObject.SetActive(!haveCharacter);
            _normalBGLayerObject.SetActive(haveCharacter && _characterData.grade_type != GradeType.LEGEND);
            _SSRBGLayerObject.SetActive(haveCharacter && _characterData.grade_type == GradeType.LEGEND);
        }

        private void SetStarObject(GradeType gradeType)
        {
            for (int i = 0; i < _starObjectList.Count; i++)
            {
                _starObjectList[i].SetActive(i <= (int)gradeType);
            }
        }

        private void OnClickCardSlot()
        {
            if (_parentCollectionPopup == null) return;

            _parentCollectionPopup.SelectCharacterCard(_characterData.character_id);
        }

        private void ClearCardSlot()
        {
            //_starObjectList?.ForEach(star => star.SetActive(false));

            BMUtil.RemoveChildObjects(_characterImageParentObject.transform);
        }
    }
}
