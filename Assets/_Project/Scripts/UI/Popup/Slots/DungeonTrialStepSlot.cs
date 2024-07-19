using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class DungeonTrialStepSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _slotButton;

        [Header("Step State - Normal")]
        [SerializeField] private GameObject _normalStateObject;
        [SerializeField] private GameObject _normalCompleteObject;

        [Header("Step State - Main")]
        [SerializeField] private GameObject _mainStateObject;
        [SerializeField] private GameObject _mainCompleteObject;
        [SerializeField] private GameObject _mainDimmedObject;
        [SerializeField] private Image _mainIcon;
        [SerializeField] private Image _mainDimmedIcon;

        [Header("Step State - Active")]
        [SerializeField] private GameObject _activeStateObject;

        private DungeonTrialPopup _parentPopup;

        private SpecDungeonTrial _specDungeonData;
        private UserTrialDungeonData _userDungeonData;

        private void Awake()
        {
            _slotButton.onClick.AddListener(OnClickSlotButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _slotButton.onClick.RemoveListener(OnClickSlotButton);
        }

        public void SetStepSlot(DungeonTrialPopup parent, SpecDungeonTrial specData, UserTrialDungeonData data)
        {
            if (data == null) return;
            if (specData == null) return;

            _parentPopup = parent;

            _specDungeonData = specData;
            _userDungeonData = data;

            bool isNormalTrialType = _specDungeonData.trial_type == TrialType.BATTLE_NORMAL;

            if (isNormalTrialType == false)
            {
                _mainIcon.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(_specDungeonData.trial_type, false);
                _mainDimmedIcon.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(_specDungeonData.trial_type, true);
            }

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            if (_specDungeonData == null) return;
            if (_userDungeonData == null) return;
            if (_parentPopup == null) return;

            bool isNormalTrialType = _specDungeonData.trial_type == TrialType.BATTLE_NORMAL;
            bool isComplete = _userDungeonData.DungeonStateType == (int)DungeonStateType.CLEAR;
            bool isCurrentSlot = _parentPopup.CurrentUserDungeonData.DungeonId == _specDungeonData.dungeon_id;

            _normalStateObject.SetActive(isNormalTrialType);
            _normalCompleteObject.SetActive(isNormalTrialType && isComplete);

            _mainStateObject.SetActive(!isNormalTrialType);
            _mainCompleteObject.SetActive(!isNormalTrialType && isComplete);
            _mainDimmedObject.SetActive(!isNormalTrialType && !isComplete);

            _activeStateObject.SetActive(isCurrentSlot);
        }

        private void OnClickSlotButton()
        {
            if (_parentPopup == null) return;
            if (_userDungeonData == null) return;

            _parentPopup.SetCurrentSelectedDungeonData(_specDungeonData.dungeon_id);
            _parentPopup.RefreshDungeonTrialPopup(DungeonTrialPopupRefreshType.ALL);
        }
    }
}

