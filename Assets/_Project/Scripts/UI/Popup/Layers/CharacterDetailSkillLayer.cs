using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailSkillLayer : CachedMonoBehaviour
    {
        [SerializeField] private Image _normalSkillIconImage;
        [SerializeField] private TextMeshProUGUI _normalSkillNameText;

        private SpecCharacter _specCharacterData;
        private UserCharacter _userCharacterData;

        public void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);

            SetSkillLayer();
        }

        public void RefreshLayer()
        {

        }

        private void SetSkillLayer()
        {
            if (_specCharacterData == null) return;
            if (_userCharacterData == null) return;

            var specSkillList = SpecDataManager.Instance.GetSkillDataListByPrefabID(_specCharacterData.prefab_id);
            if (specSkillList != null && specSkillList.Count > 0)
            {
                _normalSkillIconImage.sprite = ImageManager.Instance.GetCharacterSkillSprite(specSkillList[0].skill_id);
                _normalSkillNameText.text = LanguageManager.Instance.GetLanguageText(specSkillList[0].skill_name_token);
            }
        }
    }
}
