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

        private Character _characterData;
        private UserCharacter _userCharacterData;

        public void SetCharcacterSlot(Character characterData)
        {
            if (characterData == null) return;

            _characterData = characterData;
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(_characterData.id);

            // 기본 데이터 관련 세팅
            _gradeImage.sprite = ImageManager.Instance.GetGradeSprite(_characterData.grade);

            _synergyUI.SetSynergyUI(_characterData.type);
            _positionSynergyUI.SetPositionSynergyUI(_characterData.position);

            // BG Layer 세팅
            bool haveCharacter = _userCharacterData != null;

            _lockBGLayerObject.SetActive(!haveCharacter);
            _normalBGLayerObject.SetActive(haveCharacter && _characterData.grade != Grade.LEGEND);
            _SSRBGLayerObject.SetActive(haveCharacter && _characterData.grade == Grade.LEGEND);
        }
    }
}
