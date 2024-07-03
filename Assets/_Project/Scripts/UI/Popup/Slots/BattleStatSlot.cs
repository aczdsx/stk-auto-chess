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
        public double AttackDamageAmount => _attackDamageAmount;
        [Header("Base Info")]
        [SerializeField] private Image _characterIconImage;
        [SerializeField] private TextMeshProUGUI _damageAmountText;
        [SerializeField] private Slider _damageAmountSlider;

        [Header("Buff Info")]
        [SerializeField] private GameObject _buffListParentObject;
        [SerializeField] private GameObject _buffListSlotObject;

        [SerializeField] private UIEffect _characterIconEffect;

        private int _currentCharacterID;
        private SpecCharacter _specCharacterData;
        private double _attackDamageAmount;

        public void SetDeadCharacter()
        {
            _characterIconEffect.effectMode = EffectMode.Grayscale;
            float grayColor = 154 / 255f;
            _characterIconImage.color = new Color(grayColor, grayColor, grayColor, 1);
        }

        public void SetBattleStatSlot(int targetCharacterID)
        {
            _currentCharacterID = targetCharacterID;

            _specCharacterData = SpecDataManager.Instance.GetCharacterData(_currentCharacterID);

            _characterIconImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(_specCharacterData.prefab_id);

            bool isAlive = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player).Exists(l => l.CharacterId == targetCharacterID);
            if (!isAlive)
                SetDeadCharacter();
        }

        public async UniTask RefreshBattleStatSlotSmooth(float duration)
        {
            if (InGameStatistics.Instance == null) return;

            var totalDamageAmount = InGameStatistics.Instance.GetTotalAttackDamageAmount();
            var targetAttackDamageAmount = InGameStatistics.Instance.GetAttackDamageAmount(_currentCharacterID);

            float startTime = Time.time;
            float startAttackDamageAmount = (float)_attackDamageAmount;

            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                _attackDamageAmount = Mathf.Lerp(startAttackDamageAmount, (float)targetAttackDamageAmount, t);

                _damageAmountSlider.maxValue = (int)totalDamageAmount;
                _damageAmountSlider.value = (int)_attackDamageAmount;

                _damageAmountText.text = _attackDamageAmount.ToString("N0");

                await UniTask.Yield();
            }

            _attackDamageAmount = targetAttackDamageAmount;
            _damageAmountSlider.value = (int)_attackDamageAmount;
            _damageAmountText.text = _attackDamageAmount.ToString("N0");
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_buffListParentObject.transform);
        }
    }
}
