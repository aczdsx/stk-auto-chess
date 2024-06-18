using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/Popup/CommanderSkillPopup.prefab")]
    public class CommanderSkillPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;

        [Space(10)]
        [SerializeField] private GameObject _skillListParentObject;
        [SerializeField] private GameObject _skillListSlotObject;

        private List<CommanderSkillSlot> _commanderSkillSlotList = new List<CommanderSkillSlot>();

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
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            InitSkillPopup();
        }

        public void RefreshSkillSlot()
        {
            if (_commanderSkillSlotList == null || _commanderSkillSlotList.Count <= 0) return;

            _commanderSkillSlotList.ForEach(slot => slot.RefreshSlot());
        }

        private void InitSkillPopup()
        {
            SetSkillList();
        }

        private void SetSkillList()
        {
            ClearList();

            int lastStageID = UserDataManager.Instance.GetLastUserStageID();
            var lastStageData = SpecDataManager.Instance.SpecStage.Get(lastStageID);

            var commanderSkillList = SpecDataManager.Instance.GetCommanderSkillList(lastStageData.chapter_id);

            foreach (var commanderSkill in commanderSkillList)
            {
                if (commanderSkill.skill_value_type == SkillValueType.COOL) continue;

                GameObject newSkillSlot = Instantiate(_skillListSlotObject, _skillListParentObject.transform);
                CommanderSkillSlot commanderSkillSlot = newSkillSlot.GetComponent<CommanderSkillSlot>();

                commanderSkillSlot.SetCommanderSkillSlot(this, commanderSkill);

                _commanderSkillSlotList.Add(commanderSkillSlot);
            }
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearList()
        {
            _commanderSkillSlotList.Clear();

            BMUtil.RemoveChildObjects(_skillListParentObject.transform);
        }
    }
}
