using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterCardSlot : CachedMonoBehaviour
    {
        [Header("BG Layer")]
        [SerializeField] private GameObject _lockBGLayerObject;
        [SerializeField] private GameObject _normalBGLayerObject;
        [SerializeField] private GameObject _SSRBGLayerObject;

        [Header("Character Info")]
        [SerializeField] private TextMeshProUGUI _chracterLevelText;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private SynergyUI _positionSynergyUI;

        [Space(10)]
        [SerializeField] private GameObject _outlineActiveObject;
        [SerializeField] private GameObject _outlineInactiveObject;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;


        private Character _characterData;
        private UserCharacter _userCharacterData;

        public Character CharacterData => _characterData;

        public void SetCharcacterSlot(Character characterData)
        {
            if (characterData == null) return;

            ClearCardSlot();

            _characterData = characterData;
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(_characterData.id);

            bool haveCharacter = _userCharacterData != null;

            // 기본 데이터 관련 세팅
            _gradeImage.sprite = ImageManager.Instance.GetGradeSprite(_characterData.grade, haveCharacter);

            _synergyUI.SetSynergyUI(_characterData.element_type, haveCharacter);
            _positionSynergyUI.SetPositionSynergyUI(_characterData.class_type, haveCharacter);

            _chracterLevelText.gameObject.SetActive(haveCharacter);
            if (haveCharacter)
            {
                _chracterLevelText.text = _userCharacterData.Level.ToString();
            }

            SetStarObject(_characterData.grade);

            // 캐릭터 보유 여부 관련 처리
            _outlineActiveObject.SetActive(haveCharacter);
            _outlineInactiveObject.SetActive(!haveCharacter);

            // BG Layer 세팅
            _lockBGLayerObject.SetActive(!haveCharacter);
            _normalBGLayerObject.SetActive(haveCharacter && _characterData.grade != Grade.LEGEND);
            _SSRBGLayerObject.SetActive(haveCharacter && _characterData.grade == Grade.LEGEND);
        }

        private void SetStarObject(Grade gradeType)
        {
            for (int i = 0; i < _starObjectList.Count; i++)
            {
                _starObjectList[i].SetActive(i <= (int)gradeType);
            }
        }

        private void ClearCardSlot()
        {
            //_starObjectList?.ForEach(star => star.SetActive(false));
        }
    }
}
