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
        [SerializeField] private SimpleImageSwapper _boardImageSwapper;

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

            if (_boardImageSwapper != null && spec is CookApps.AutoBattler.CharacterInfo charInfo)
            {
                var gradeSwapType = charInfo.grade_type switch
                {
                    GradeType.EPIC => SimpleSwapType.Grade_1,
                    GradeType.LEGENDARY => SimpleSwapType.Grade_2,
                    _ => SimpleSwapType.Grade_0, // Rare 및 기본값
                };
                _boardImageSwapper.Swap(gradeSwapType);
            }

            if (_cpText != null)
                _cpText.text = spec.stat_atk.ToString("n0");
        }
    }
}
