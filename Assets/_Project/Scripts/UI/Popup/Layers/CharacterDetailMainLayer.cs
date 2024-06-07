using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailMainLayer : CachedMonoBehaviour
    {
        [SerializeField] private Image _characterIllustImage;
        [SerializeField] private SynergyUI _elementSynergyUI;
        [SerializeField] private SynergyUI _classSynergyUI;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterGradeText;

        [Space(10)]
        [SerializeField] private List<GameObject> _starObjectList;

        private CharacterCollectionPopup _parentCollectionPopup;

        private SpecCharacter _specCharacterData;

        public void InitLayer(int characterID, CharacterCollectionPopup _parentPopup)
        {
            _parentCollectionPopup = _parentPopup;

            _specCharacterData = SpecDataManager.Instance.SpecCharacter.Get(characterID);

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

        private void SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            var targetSprite = ImageManager.Instance.GetCharacterIllustSprite(_specCharacterData.prefab_id);
            _characterIllustImage.sprite = targetSprite;
            _characterIllustImage.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);

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
    }
}
