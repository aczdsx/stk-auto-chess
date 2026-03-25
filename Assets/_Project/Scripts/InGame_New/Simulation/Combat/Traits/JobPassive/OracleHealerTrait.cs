namespace CookApps.AutoChess
{
    /// <summary>
    /// ORACLE 패시브: 평타로 아군을 힐.
    /// 힐 파라미터 보관 + 힐량 계산 담당. 실행은 DamageSystem에서 분기 처리.
    /// </summary>
    public sealed class OracleHealerTrait : CombatTraitBase
    {
        public const int HealTargetHPThreshold = 50; // HP비율 이 값 미만인 아군만 힐 대상
        public const int HealRangeBonus = 0;         // 힐 타겟 탐색 시 사거리 보정

        private readonly int _healPercent; // 회복 비율 (정수 퍼센트)

        public OracleHealerTrait(int healPercent)
        {
            _healPercent = healPercent;
        }

        /// <summary>힐량: Attack * HealPercent / 100, 양쪽 HealPower 적용</summary>
        public int CalculateHealAmount(ref CombatUnit healer, ref CombatUnit target)
        {
            int amount = healer.Attack * _healPercent / 100;
            amount = amount * (100 + healer.HealPower) / 100;
            amount = amount * (100 + target.HealPower) / 100;
            if (amount < 1) amount = 1;
            return amount;
        }

        public override void Reset()
        {
        }
    }
}
