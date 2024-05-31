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
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _characterGradeText;

        private SpecCharacter _specCharacterData;

        public void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.SpecCharacter.Get(characterID);

            SetCharacterInfo();
        }

        private void SetCharacterInfo()
        {
            if (_specCharacterData == null) return;

            _characterIllustImage.sprite = ImageManager.Instance.GetCharacterIllustSprite(_specCharacterData.id);
            _characterNameText.text = _specCharacterData.name_token;
            _characterGradeText.text = LanguageManager.Instance.GetGradeText(_specCharacterData.grade);
        }
    }
}
