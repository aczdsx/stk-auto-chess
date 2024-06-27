using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CommanderSkillSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _equipButton;
        [SerializeField] private CAButton _skillInfoButton;

        [Space(10)]
        [SerializeField] private GameObject _disabledLayerObject;
        [SerializeField] private GameObject _equipLayerObject;
        [SerializeField] private GameObject _equippedLayerObject;

        [Space(10)]
        [SerializeField] private Image _skillIconImage;
        [SerializeField] private TextMeshProUGUI _skillNameText;

        private CommanderSkillPopup _parentPopup;
        private SpecCommanderSkill _specCommanderSkillData;

        private void Awake()
        {
            _equipButton.onClick.AddListener(OnClickEquipButton);
            _skillInfoButton.onClick.AddListener(OnClickSkillInfoButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _equipButton.onClick.RemoveListener(OnClickEquipButton);
            _skillInfoButton.onClick.RemoveListener(OnClickSkillInfoButton);
        }

        public void SetCommanderSkillSlot(CommanderSkillPopup parent, SpecCommanderSkill skillData)
        {
            if (skillData == null) return;

            _parentPopup = parent;

            _specCommanderSkillData = skillData;

            _skillIconImage.sprite = ImageManager.Instance.GetCommanderSkillSprite(_specCommanderSkillData.commander_skill_id);
            _skillNameText.text = LanguageManager.Instance.GetLanguageText(_specCommanderSkillData.name_token);

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            bool isOpenSkill = UserDataManager.Instance.IsOpenedCommanderSkill(_specCommanderSkillData.commander_skill_id);
            bool isEquippedSkill = UserDataManager.Instance.GetEquippedCommanderSkill() == _specCommanderSkillData.commander_skill_id;

            _disabledLayerObject.SetActive(!isOpenSkill);

            _equipButton.gameObject.SetActive(isOpenSkill);
            _equipLayerObject.SetActive(isOpenSkill && !isEquippedSkill);
            _equippedLayerObject.SetActive(isOpenSkill && isEquippedSkill);
        }

        private void OnClickEquipButton()
        {
            if (_parentPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            UserDataManager.Instance.SetEquippedCommanderSkill(_specCommanderSkillData.commander_skill_id);

            _parentPopup.RefreshSkillSlot();
        }

        private void OnClickSkillInfoButton()
        {
            if (_parentPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            _parentPopup.OpenSkillToolTipPopup(_specCommanderSkillData);
        }
    }
}
