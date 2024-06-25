using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class BattleStatSlot : CachedMonoBehaviour
    {
        [Header("Base Info")]
        [SerializeField] private Image _characterIconImage;
        [SerializeField] private TextMeshProUGUI _damageAmountText;
        [SerializeField] private Slider _damageAmountSlider;

        [Header("Buff Info")]
        [SerializeField] private GameObject _buffListParentObject;
        [SerializeField] private GameObject _buffListSlotObject;

        private int _currentCharacterID;
        private SpecCharacter _specCharacterData;

        public void SetBattleStatSlot(int targetCharacterID)
        {
            _currentCharacterID = targetCharacterID;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(_currentCharacterID);

            _characterIconImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(_specCharacterData.prefab_id);
        }

        public void RefreshBattleStatSlot()
        {
            if (InGameStatistics.Instance == null) return;

            var totalDamageAmount = InGameStatistics.Instance.GetTotalAttackDamageAmount();
            var attackDamageAmount = InGameStatistics.Instance.GetAttackDamageAmount(_currentCharacterID);

            _damageAmountSlider.maxValue = (int)totalDamageAmount;
            _damageAmountSlider.value = (int)attackDamageAmount;

            _damageAmountText.text = attackDamageAmount.ToString("N0");
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_buffListParentObject.transform);
        }
    }
}
