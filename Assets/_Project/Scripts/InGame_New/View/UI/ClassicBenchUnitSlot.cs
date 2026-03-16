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
        [SerializeField] private SimpleImageColorSwapper _atkTypeColorSwapper;
        [SerializeField] private TMP_Text _cpText;
        [SerializeField] private SimpleImageSwapper _boardImageSwapper;
        [SerializeField] private GameObject _selectedDim;

        protected override void UpdateVisual()
        {
            base.UpdateVisual();

            if (_currentChampSpecId <= 0) return;

            var spec = SpecDataManager.Instance.GetSpecCharacter(_currentChampSpecId);
            if (spec == null) return;

            if (_characterPositionTypeText != null)
                _characterPositionTypeText.text = spec.character_position_type.ToString();

            if (_positionColorSwapper != null)
                _positionColorSwapper.Swap(spec.atk_type == AtkType.AD
                    ? SimpleSwapType.AD
                    : SimpleSwapType.AP);

            if (_atkTypeColorSwapper != null)
            {
                _atkTypeColorSwapper.Swap(spec.atk_type == AtkType.AD
                    ? SimpleSwapType.AD
                    : SimpleSwapType.AP);
            }

            if (_boardImageSwapper != null && spec is  AutoBattler.CharacterInfo charInfo)
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
            {
                // ISpecCharacterInfo → 정수 스탯 변환 + 별 배율 적용 → CP 계산
                int starMul = _currentStarLevel switch { 2 => 180, 3 => 320, _ => 100 };
                int hp = spec.stat_hp * starMul / 100;
                int atk = spec.stat_atk * starMul / 100;
                int armor = spec.stat_def;
                int magicResist = (int)spec.ap_reduce;
                int atkSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                int critRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                int critPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                int atkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                if (critRate <= 0) critRate = 25;
                if (critPower <= 0) critPower = 150;

                int cp = CombatPowerCalculator.CalculateFromOldSpec(
                    hp, atk, armor, magicResist, atkSpeed, critRate, critPower, atkPierce);
                _cpText.text = cp.ToString("n0");
            }
        }

        protected override void OnSelected()
        {
            if (_selectedDim != null)
                _selectedDim.SetActive(true);
        }

        protected override void OnDeselected()
        {
            if (_selectedDim != null)
                _selectedDim.SetActive(false);
        }
    }
}
