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
    public class CharacterDetailSkillLayer : CachedMonoBehaviour
    {
        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;
        [SerializeField] private CAButton _skillInfoButton;
        [SerializeField] private Image _normalSkillIconImage;
        [SerializeField] private TextMeshProUGUI _normalSkillNameText;

        private SpecCharacter _specCharacterData;
        private UserCharacter _userCharacterData;

        private SpecSkill _specSkillBaseData;

        private void Awake()
        {
            _skillInfoButton.onClick.AddListener(OnClickSkillInfoButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _skillInfoButton.onClick.RemoveListener(OnClickSkillInfoButton);
        }

        public void InitLayer(int characterID)
        {
            _skillTooltipPopup.gameObject.SetActive(false);

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
                _specSkillBaseData = specSkillList[0];

                _normalSkillIconImage.sprite = ImageManager.Instance.GetCharacterSkillSprite(specSkillList[0].skill_id);
                _normalSkillNameText.text = LanguageManager.Instance.GetLanguageText(specSkillList[0].skill_name_token);
            }
        }

        private void OnClickSkillInfoButton()
        {
            if (_skillTooltipPopup == null) return;
            if (_specSkillBaseData == null) return;

            _skillTooltipPopup.SetSkillToolTipPopup(_specSkillBaseData);

            _skillTooltipPopup.gameObject.SetActive(true);
        }
    }
}
