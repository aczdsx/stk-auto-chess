namespace CookApps.AutoChess
{
    /// <summary>엔키: 파도 채널링 — 뒤에서 앞으로 이동하며 아군 힐 + HoT</summary>
    public class SimSkillEnkiWaveHeal : SimSkillBase
    {
        private int _hotDuration;
        private int _hotInterval;

        // 채널링 상태
        private int _currentWaveRow;
        private int _waveEndRow;
        private int _waveDirRow;
        private int _waveHalfWidth;
        private int _waveCenterCol;
        private int _tickInterval;
        private int _tickTimer;
        private int _cachedAttack;
        private int _cachedCasterId;
        private byte _cachedTeam;
        private bool _channeling;

        private const int DefaultMoveInterval = 3; // 3프레임마다 1행 이동
        private const int WaveWidth = 5;           // 파도 폭 5칸

        public override bool IsChanneling => _channeling;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _hotDuration = p.Param0 > 0 ? p.Param0 : 180;
            _hotInterval = p.Param1 > 0 ? p.Param1 : 30;
        }

        public override int GetCastFrames() => BoardHelper.CombatHeight * DefaultMoveInterval;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 전방 방향 결정
            _waveDirRow = caster.TeamIndex == 0 ? 1 : -1;

            // 파도 시작: 엔키 뒤 2칸
            int startRow = caster.GridRow - _waveDirRow * 2;
            if (startRow < 0) startRow = 0;
            if (startRow >= BoardHelper.CombatHeight) startRow = BoardHelper.CombatHeight - 1;

            _currentWaveRow = startRow;
            _waveEndRow = _waveDirRow > 0 ? BoardHelper.CombatHeight : -1;
            _waveCenterCol = caster.GridCol;
            _waveHalfWidth = WaveWidth / 2;
            _tickInterval = DefaultMoveInterval;
            _tickTimer = _tickInterval;
            _cachedAttack = caster.Attack;
            _cachedCasterId = caster.SourceEntityId;
            _cachedTeam = caster.TeamIndex;
            _channeling = true;

            // 첫 행 힐 적용
            HealRow(state, _currentWaveRow);
            AdvanceWave();
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (!_channeling) return false;

            _tickTimer--;
            if (_tickTimer > 0) return true;

            _tickTimer = _tickInterval;

            // 현재 행 힐 적용
            HealRow(state, _currentWaveRow);

            // 다음 행으로 전진
            if (!AdvanceWave())
            {
                _channeling = false;
                return false;
            }

            return true;
        }

        private void HealRow(CombatMatchState state, int row)
        {
            if (row < 0 || row >= BoardHelper.CombatHeight) return;

            // 타일 이펙트 이벤트
            state.EventQueue?.PushSkillAreaEffect(
                _cachedCasterId, (byte)_waveCenterCol, (byte)row, _waveHalfWidth, isRow: true);

            int healAmount = _cachedAttack * PowerPercent / 100;
            int hotPerTick = _cachedAttack * SecondaryPowerPercent / 100;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex != _cachedTeam) continue;

                // 파도 폭 범위 내의 같은 행에 있는 아군만
                if (unit.GridRow != row) continue;
                int colDiff = unit.GridCol - _waveCenterCol;
                if (colDiff < -_waveHalfWidth || colDiff > _waveHalfWidth) continue;

                SkillDamageHelper.Heal(state, ref unit, healAmount);

                if (hotPerTick > 0)
                {
                    SkillBuffHelper.ApplyHOT(state, i, hotPerTick, _hotDuration, _hotInterval);
                }
            }
        }

        private bool AdvanceWave()
        {
            _currentWaveRow += _waveDirRow;
            return _currentWaveRow != _waveEndRow &&
                   _currentWaveRow >= 0 &&
                   _currentWaveRow < BoardHelper.CombatHeight;
        }

        public override void Reset()
        {
            _hotDuration = 180;
            _hotInterval = 30;
            _channeling = false;
            _tickTimer = 0;
        }
    }
}
