using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private SpriteLoader _skillIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _skillNameText;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _skillLevelText;

        private CommanderSkillPopup _parentPopup;
        private SkillCommander _specCommanderSkillData;

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

        public void SetCommanderSkillSlot(CommanderSkillPopup parent, SkillCommander skillData)
        {
            if (skillData == null) return;

            _parentPopup = parent;

            _specCommanderSkillData = skillData;

            _skillIconSpriteLoader.SetSprite(SpriteNameParser.GetCommanderSkillSprite(_specCommanderSkillData.commander_skill_id)).Forget();
            _skillNameText.text = LanguageManager.Instance.GetLanguageText(_specCommanderSkillData.name_token);
            _skillLevelText.text = _specCommanderSkillData.level.ToString();

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            bool isOpenSkill = ServerDataManager.Instance.CommanderSkill.IsOpenedCommanderSkill(_specCommanderSkillData.commander_skill_id);
            bool isEquippedSkill = ServerDataManager.Instance.CommanderSkill.IsEquippedCommanderSkill(_specCommanderSkillData.commander_skill_id);

            _disabledLayerObject.SetActive(!isOpenSkill);

            _equipButton.gameObject.SetActive(isOpenSkill);
            _equipLayerObject.SetActive(isOpenSkill && !isEquippedSkill);
            _equippedLayerObject.SetActive(isOpenSkill && isEquippedSkill);
        }

        private void OnClickEquipButton()
        {
            if (_parentPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            ServerDataManager.Instance.CommanderSkill.SetEquippedCommanderSkill(_parentPopup.Index, _specCommanderSkillData.commander_skill_id);

            _parentPopup.RefreshSkillSlot();

            string msg = LanguageManager.Instance.GetLanguageText("MSG_EQUIP_COMMAND_SKILL");
            string skillNameText = LanguageManager.Instance.GetLanguageText(_specCommanderSkillData.name_token);

            ToastManager.Instance.ShowToast(string.Format(msg, skillNameText));
        }

        private void OnClickSkillInfoButton()
        {
            if (_parentPopup == null) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            _parentPopup.OpenSkillToolTipPopup(_specCommanderSkillData);
        }
    }
}
