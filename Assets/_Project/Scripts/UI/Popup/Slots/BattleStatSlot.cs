using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class BattleStatSlot : CachedMonoBehaviour
    {
        public int CharacterID => _currentCharacterID;
        public double BattleValue => _battleValue;
        [Header("Base Info")]
        [SerializeField] private Image _characterIconImage;
        [SerializeField] private TextMeshProUGUI _damageAmountText;
        [SerializeField] private Slider _damageAmountSlider;

        [Header("Buff Info")]
        [SerializeField] private GameObject _buffListParentObject;
        [SerializeField] private GameObject _buffListSlotObject;

        [SerializeField] private UIEffect _characterIconEffect;
        [SerializeField] Image _fillImage;
        
        [SerializeField] Color _fillGivenDamageColor;
        [SerializeField] Color _fillTakenDamageColor;
        [SerializeField] Color _fillHealColor;

        private int _currentCharacterID;
        private int _currentCharacterUID;
        private SpecCharacter _specCharacterData;
        private double _battleValue;

        public void SetDeadCharacter()
        {
            _characterIconEffect.effectMode = EffectMode.Grayscale;
            float grayColor = 154 / 255f;
            _characterIconImage.color = new Color(grayColor, grayColor, grayColor, 1);
        }

        public void SetBattleStatSlot(int targetCharacterID, int targetCharacterUID)
        {
            _currentCharacterID = targetCharacterID;
            _currentCharacterUID = targetCharacterUID;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(_currentCharacterID);

            _characterIconImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(_specCharacterData.prefab_id);

            bool isAlive = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Exists(l => l.CharacterId == targetCharacterID);
            if (!isAlive)
                SetDeadCharacter();
        }
        
        public async UniTask RefreshBattleStatSlotSmooth(BattleStatisticsTabType tabType, float duration)
        {
            if (InGameStatistics.Instance == null) return;

            double totalValue = 0;
            double value = 0;
            switch (tabType)
            {
                case BattleStatisticsTabType.GIVENDAMAGE:
                    totalValue = InGameStatistics.Instance.GetTotalAmount(ActionType.Damaged, true);
                    value = InGameStatistics.Instance.GetAttackDamageAmount(_currentCharacterUID);
                    _fillImage.color = _fillGivenDamageColor;
                    break;
                case BattleStatisticsTabType.TAKENDAMAGED:
                    totalValue = InGameStatistics.Instance.GetTotalAmount(ActionType.Damaged, false);
                    value = InGameStatistics.Instance.GetTakenDamageAmount(_currentCharacterUID);
                    _fillImage.color = _fillTakenDamageColor;
                    break;
                case BattleStatisticsTabType.HEAL:
                    totalValue = InGameStatistics.Instance.GetTotalAmount(ActionType.Healed, true);
                    value = InGameStatistics.Instance.GetGivenHealAmount(_currentCharacterUID);
                    _fillImage.color = _fillHealColor;
                    break;
            }
            
            float startTime = Time.time;
            float startAttackDamageAmount = (float)_battleValue;

            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                _battleValue = Mathf.Lerp(startAttackDamageAmount, (float)value, t);

                _damageAmountSlider.maxValue = (int)totalValue;
                _damageAmountSlider.value = (int)_battleValue;

                _damageAmountText.text = _battleValue.ToString("N0");

                await UniTask.Yield();
            }
            
            _battleValue = value;
            _damageAmountSlider.value = (int)_battleValue;
            _damageAmountText.text = _battleValue.ToString("N0");
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_buffListParentObject.transform);
        }
    }
}
