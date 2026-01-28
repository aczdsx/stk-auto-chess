using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class CommanderSkillPopup : UILayerPopupBase
    {
        public int Index => _index;
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private SkillTooltipPopup _skillTooltipPopup;

        [Space(10)]
        [SerializeField] private GameObject _skillListParentObject;
        [SerializeField] private GameObject _skillListSlotObject;

        private List<CommanderSkillSlot> _commanderSkillSlotList = new List<CommanderSkillSlot>();
        private int _index = 0;

        protected override void Awake()
        {
            base.Awake();
            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
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

        public void OpenSkillToolTipPopup(SkillCommander skillData)
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
            var specDataManagerInstance = SpecDataManager.Instance;
            var commanderSkillCodeIdList = specDataManagerInstance.GetCommanderSkillCodeIdList();
            foreach (var commanderSkillCodeId in commanderSkillCodeIdList)
            {
                // int userSkillLevel = 1;
                // {
                // ! MUST_REVERT 원래 주석이 아니였습니다
                int userSkillLevel = ServerDataManager.Instance.CommanderSkill.GetUserCommanderSkillLevel(commanderSkillCodeId);
                // }

                var specTargetCommanderSkill = specDataManagerInstance.GetCommanderSkillListByUserSkillLevel(commanderSkillCodeId, userSkillLevel);
                if (specTargetCommanderSkill != null)
                {
                    GameObject newSkillSlot = Instantiate(_skillListSlotObject, _skillListParentObject.transform);
                    CommanderSkillSlot commanderSkillSlot = newSkillSlot.GetComponent<CommanderSkillSlot>();
                    commanderSkillSlot.SetCommanderSkillSlot(this, specTargetCommanderSkill);
                    _commanderSkillSlotList.Add(commanderSkillSlot);
                }
            }
        }

        private void OnClickCloseButton()
        {
            InGameMain.GetInGameMain().SetCommanderSkillUI(_index, ServerDataManager.Instance.CommanderSkill.GetEquippedCommanderSkillId(_index));

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearList()
        {
            _commanderSkillSlotList.Clear();

            BMUtil.RemoveChildObjects(_skillListParentObject.transform);
        }
    }
}
