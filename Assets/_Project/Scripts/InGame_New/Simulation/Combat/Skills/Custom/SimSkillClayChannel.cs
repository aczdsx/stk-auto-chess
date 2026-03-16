namespace CookApps.AutoChess
{
    /// <summary>
    /// 클레이 (217553404): 3초 채널링 존.
    /// Execute 즉시 호출 → SkillHitFrames[0] 타이밍에 첫 틱 시작 → 이후 틱 간격으로 반복.
    /// 매 틱 아군 힐 + CC제거, 적 데미지 + 회복감소 디버프.
    /// 스펙: {0}=쿨타임, {1}=힐배율(%) → PowerPercent, {2}=데미지배율(%), {3}=회복감소(%), {4}=디버프지속(초)
    /// Params: Param0=damagePercent, Param1=healReductionPercent, Param2=debuffDurationFrames, Param3=zoneRange
    /// </summary>
    public class SimSkillClayChannel : SimSkillBase
    {
        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        private int _damagePercent;
        private int _healReductionPercent;
        private int _debuffDurationFrames;
        private int _zoneRange;

        private int _remainingTicks;
        private int _tickInterval;
        private int _tickTimer;
        private bool _started; // SkillHitFrames[0] 대기 완료 여부
        private int _startDelay;
        private int _tickIndex; // 타일이펙트 주기 제어용 (짝수 틱에서만 발행)

        // 틱 간격: 0.5초 (30fps 기준 15프레임). 틱 수는 SkillClipFrames에서 동적 계산.
        private const int DefaultTickInterval = 15;
        private const int FallbackTickCount = 6; // SkillClipFrames 없을 때 폴백

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _damagePercent = p.Param0 > 0 ? p.Param0 : 80;
            _healReductionPercent = p.Param1 > 0 ? p.Param1 : 50;
            _debuffDurationFrames = p.Param2 > 0 ? p.Param2 : 90;
            _zoneRange = p.Param3 > 0 ? p.Param3 : 2;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _tickInterval = DefaultTickInterval;
            // SKL 클립 길이에서 틱 수 계산 (SkillHitFrames[0] 이후 채널링 구간 기준)
            int startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0 ? SkillHitFrames[0] : 15;
            int channelFrames = SkillClipFrames > startDelay ? SkillClipFrames - startDelay : 0;
            _remainingTicks = channelFrames > 0 ? (channelFrames / _tickInterval) + 1 : FallbackTickCount;
            _tickTimer = 0;
            _tickIndex = 0;
            _started = false;
            _startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15; // fallback
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_remainingTicks <= 0) return false;

            // SkillHitFrames[0] 타이밍까지 대기
            if (!_started)
            {
                _startDelay--;
                if (_startDelay > 0) return true;

                _started = true;

                // 영역 VFX 발행 (vfx[0])
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);

                // 첫 틱 즉시 실행
                DoZoneTick(state, ref caster);
                _remainingTicks--;
                _tickTimer = _tickInterval;
                return _remainingTicks > 0;
            }

            // 이후 틱 간격 대기
            _tickTimer--;
            if (_tickTimer > 0) return true;

            _tickTimer = _tickInterval;
            DoZoneTick(state, ref caster);
            _remainingTicks--;

            return _remainingTicks > 0;
        }

        private void DoZoneTick(CombatMatchState state, ref CombatUnit caster)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            var type = DamageType;
            byte team = caster.TeamIndex;

            // 타일 이펙트 이벤트 — 짝수 틱에서만 (기존 동작: 2틱에 1번)
            if (_tickIndex % 2 == 0)
            {
                state.EventQueue?.PushSkillAreaEffect(
                    caster.SourceEntityId, (byte)col, (byte)row, _zoneRange);
            }
            _tickIndex++;

            // 아군 힐 (틱당 배율 = PowerPercent / 틱수) + CC제거
            int healPct = PowerPercent;
            SkillAreaHelper.ForEachAllyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit ally, int i) =>
                {
                    int heal = attack * healPct / 100;
                    SkillDamageHelper.Heal(state, ref ally, heal);
                    int allyIdx = state.FindUnitIndex(ally.CombatId);
                    if (allyIdx >= 0)
                        StatusEffectSystem.RemoveAllDebuffs(state, allyIdx);
                });

            // 적 데미지 (틱당 배율 = damagePercent / 틱수) + 회복감소 디버프
            int dmgPct = _damagePercent;
            int healRedPct = _healReductionPercent;
            int debuffDur = _debuffDurationFrames;
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit enemy, int i) =>
                {
                    int raw = attack * dmgPct / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref enemy);
                    DamageSystem.ApplyDamage(state, ref enemy, dmg);
                    DamageSystem.ChargeMana(ref enemy, enemy.ManaGainOnHit);

                    // 회복감소 디버프
                    int enemyIdx = state.FindUnitIndex(enemy.CombatId);
                    if (enemyIdx >= 0 && healRedPct > 0)
                        StatusEffectSystem.AddEffect(state, enemyIdx,
                            StatusEffectType.HealReduction, healRedPct, debuffDur);
                });
        }

        public override void Reset()
        {
            _damagePercent = 0;
            _healReductionPercent = 0;
            _debuffDurationFrames = 0;
            _zoneRange = 2;
            _remainingTicks = 0;
            _tickTimer = 0;
            _tickIndex = 0;
            _started = false;
            _startDelay = 0;
        }
    }
}
