using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
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
        [SerializeField] private GameObject _mainGrayObject;
        [SerializeField] private GameObject _mainCompleteObject;
        [SerializeField] private GameObject _mainDimmedObject;
        [SerializeField] private Image _mainIcon;
        [SerializeField] private SpriteLoader _mainIconSpriteLoader;
        [SerializeField] private Image _mainDimmedIcon;
        [SerializeField] private SpriteLoader _mainDimmedIconSpriteLoader;

        [Header("Step State - Active")]
        [SerializeField] private GameObject _activeStateObject;

        private DungeonTrialPopup _parentPopup;

        private DungeonBabelInfo _specDungeonData;
        
        // Removed: private UserTrialDungeonData _selectedDungeonData;
        // Removed: private UserTrialDungeonData _dungeonData;
        
        private uint _currentUserOrder;

        private void Awake()
        {
            _slotButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickSlotButton()).AddTo(this);
        }

        public void SetStepSlot(DungeonTrialPopup parent, DungeonBabelInfo specData, uint currentOrder)
        {
            if (specData == null) return;

            _parentPopup = parent;
            _specDungeonData = specData;
            _currentUserOrder = currentOrder;

            if (_specDungeonData.is_grade_up)
            {
                _mainIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specDungeonData.trial_type, false)).Forget();
                _mainDimmedIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_specDungeonData.trial_type, true)).Forget();
            }

            RefreshSlot(currentOrder);
        }

        public void RefreshSlot(uint currentOrder)
        {
            _currentUserOrder = currentOrder;
            RefreshSlot();
        }

        public void RefreshSlot()
        {
            if (_specDungeonData == null) return;
            if (_parentPopup == null) return;

            bool isNormalTrialType = !_specDungeonData.is_grade_up;
            
            // Logic:
            // Cleared if UserOrder > SlotOrder
            // Current if UserOrder == SlotOrder
            
            bool isComplete = _currentUserOrder > _specDungeonData.order;
            bool isCurrentSlot = _parentPopup.CurrentSelectedDungeonId == _specDungeonData.dungeon_id;

            _activeStateObject.SetActive(isCurrentSlot);

            _normalStateObject.SetActive(isNormalTrialType);
            _normalCompleteObject.SetActive(isNormalTrialType && isComplete);

            _mainStateObject.SetActive(!isNormalTrialType);
            _mainCompleteObject.SetActive(!isNormalTrialType && isComplete);
            
            // Gray is usually for "Locked" or "Not Reached" if it's not complete?
            // Original: !isNormalTrialType && (isCurrentSlot || isComplete) -> wait, logic seems like "Active" or "Complete"
            // Let's interpret "Gray" as "Active" based on typical UI logic if "ActiveStateObject" is specifically for selection highlight.
            // Actually, based on previous code: `_mainGrayObject.SetActive(!isNormalTrialType && (isCurrentSlot || isComplete));`
            // Wait, isCurrentSlot is SELECTION.
            // Let's stick to previous display logic but using new boolean flags.
            
            _mainGrayObject.SetActive(!isNormalTrialType && (isCurrentSlot || isComplete));
            _mainDimmedObject.SetActive(!isNormalTrialType && !_mainGrayObject.activeSelf);
        }

        private void OnClickSlotButton()
        {
            if (_parentPopup == null) return;
            if (_specDungeonData == null) return;

            _parentPopup.SetCurrentSelectedDungeonData(_specDungeonData.dungeon_id);
            _parentPopup.RefreshDungeonTrialPopup(DungeonTrialPopupRefreshType.ALL);
        }
    }
}

