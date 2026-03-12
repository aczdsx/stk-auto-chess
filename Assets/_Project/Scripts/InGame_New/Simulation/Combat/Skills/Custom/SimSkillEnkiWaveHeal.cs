namespace CookApps.AutoChess
{
    /// <summary>엔키: 파도 채널링 — 뒤에서 앞으로 보드 전체를 이동하며 아군 힐 + HoT.
    /// 힐/타일이펙트/VFX 모두 ProjectileSystem(HealAlly)에서 동기 처리.</summary>
    public class SimSkillEnkiWaveHeal : SimSkillBase
    {
        private int _hotDuration;
        private int _hotInterval;

        // 채널링 상태
        private int _phaseTimer;
        private int _channelFramesRemaining;
        private bool _fired;
        private bool _channeling;

        // Execute에서 캐싱 (OnChannelTick에서 사용)
        private int _cachedCasterCombatId;
        private int _cachedAttack;
        private int _startRow;
        private int _centerCol;
        private int _halfWidth;
        private int _waveDirRow;

        private const int DefaultMoveInterval = 24; // 24프레임마다 1행 이동 (~0.4초/행, speed≈5)
        private const int WaveWidth = 5;           // 파도 폭 5칸

        public override bool IsChanneling => true;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _hotDuration = p.Param0 > 0 ? p.Param0 : 180;
            _hotInterval = p.Param1 > 0 ? p.Param1 : 30;
        }

        public override int GetCastFrames() => 0;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            // 적이 존재할 때만 시전, 자기 자신을 반환하여 facing 유지
            int nearestEnemy = TargetingSystem.FindNearestEnemy(state, ref caster);
            return nearestEnemy != CombatUnit.InvalidId ? caster.CombatId : CombatUnit.InvalidId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 준비만: 상태 저장 + 타이머 설정 (LineDamage 패턴)
            _waveDirRow = caster.TeamIndex == 0 ? 1 : -1;
            _startRow = _waveDirRow > 0 ? 0 : BoardHelper.CombatHeight - 1;
            _centerCol = caster.GridCol;
            _halfWidth = WaveWidth / 2;
            _cachedCasterCombatId = caster.CombatId;
            _cachedAttack = caster.Attack;
            _fired = false;
            _channeling = true;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : DefaultMoveInterval;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (!_channeling) return false;

            // 발사 전: SkillHitFrames[0] 대기
            if (!_fired)
            {
                _phaseTimer--;
                if (_phaseTimer > 0) return true;

                _fired = true;
                SpawnWaveProjectiles(state);
                _channelFramesRemaining = (BoardHelper.CombatHeight - 1) * DefaultMoveInterval;
                return true;
            }

            // 발사 후: 투사체가 보드 끝까지 이동할 때까지 채널링 유지
            _channelFramesRemaining--;
            if (_channelFramesRemaining <= 0)
            {
                _channeling = false;
                return false;
            }

            return true;
        }

        private void SpawnWaveProjectiles(CombatMatchState state)
        {
            int healAmount = _cachedAttack * PowerPercent / 100;
            int hotPerTick = _cachedAttack * SecondaryPowerPercent / 100;

            int minCol = _centerCol - _halfWidth;
            int maxCol = _centerCol + _halfWidth;

            for (int c = minCol; c <= maxCol; c++)
            {
                if (!BoardHelper.IsValidCombatPosition(c, _startRow)) continue;

                ProjectileSystem.CreateLinearHealProjectile(
                    state, _cachedCasterCombatId,
                    (byte)c, (byte)_startRow,
                    0, (sbyte)_waveDirRow,
                    healAmount, DamageType.Physical,
                    DefaultMoveInterval, BoardHelper.CombatHeight, SkillId,
                    skillVfxIndex: 1,
                    hotPerTick: hotPerTick, hotDuration: _hotDuration, hotInterval: _hotInterval,
                    areaEffectHalfWidth: c == _centerCol ? (byte)_halfWidth : (byte)0);
            }
        }

        public override void Reset()
        {
            _hotDuration = 180;
            _hotInterval = 30;
            _channeling = false;
            _fired = false;
            _phaseTimer = 0;
            _channelFramesRemaining = 0;
        }
    }
}
