using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public class CommanderSkillPopup : UILayer
    {
        public int Index => _index;
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;

        [Space(10)]
        [SerializeField] private GameObject _skillListParentObject;
        [SerializeField] private GameObject _skillListSlotObject;

        private List<CommanderSkillSlot> _commanderSkillSlotList = new List<CommanderSkillSlot>();
        private int _index = 0;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _index = (int)param;
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _skillTooltipPopup.gameObject.SetActive(false);

            InitSkillPopup();
        }

        public void RefreshSkillSlot()
        {
            if (_commanderSkillSlotList == null || _commanderSkillSlotList.Count <= 0) return;

            _commanderSkillSlotList.ForEach(slot => slot.RefreshSlot());
        }

        public void OpenSkillToolTipPopup(SpecCommanderSkill skillData)
        {
            _skillTooltipPopup.SetCommanderSkillToolTipPopup(skillData);

            _skillTooltipPopup.gameObject.SetActive(true);
        }

        private void InitSkillPopup()
        {
            SetSkillList();
        }

        private void SetSkillList()
        {
            ClearList();

            var commanderSkillList = SpecDataManager.Instance.SpecCommanderSkill.All;

            foreach (var commanderSkill in commanderSkillList)
            {
                if (commanderSkill.skill_value_type == SkillValueType.COOL)
                {
                    GameObject newSkillSlot = Instantiate(_skillListSlotObject, _skillListParentObject.transform);
                    CommanderSkillSlot commanderSkillSlot = newSkillSlot.GetComponent<CommanderSkillSlot>();

                    commanderSkillSlot.SetCommanderSkillSlot(this, commanderSkill);

                    _commanderSkillSlotList.Add(commanderSkillSlot);
                }
            }
        }

        private void OnClickCloseButton()
        {
            InGameMain.GetInGameMain().SetCommanderSkillUI(_index, UserDataManager.Instance.GetEquippedCommanderSkill(_index));

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearList()
        {
            _commanderSkillSlotList.Clear();

            BMUtil.RemoveChildObjects(_skillListParentObject.transform);
        }
    }
}
