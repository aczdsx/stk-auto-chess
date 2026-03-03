using CookApps.AutoBattler;
using CookApps.TeamBattle.Utility;
using TMPro;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    public class ClassicBenchUnitSlot : BenchUnitSlot
    {
        [Header("Classic Display")]
        [SerializeField] private TMP_Text _characterPositionTypeText;
        [SerializeField] private SimpleImageColorSwapper _positionColorSwapper;
        [SerializeField] private TMP_Text _cpText;

        protected override void UpdateVisual()
        {
            base.UpdateVisual();

            if (_currentChampSpecId <= 0) return;

            var spec = SpecDataManager.Instance.GetSpecCharacter(_currentChampSpecId);
            if (spec == null) return;

            if (_characterPositionTypeText != null)
                _characterPositionTypeText.text = spec.character_position_type.ToString();

            if (_positionColorSwapper != null)
            {
                _positionColorSwapper.Swap(spec.atk_type == AtkType.AD
                    ? SimpleSwapType.AD
                    : SimpleSwapType.AP);
            }

            if (_cpText != null)
                _cpText.text = spec.stat_atk.ToString("n0");
        }
    }
}
