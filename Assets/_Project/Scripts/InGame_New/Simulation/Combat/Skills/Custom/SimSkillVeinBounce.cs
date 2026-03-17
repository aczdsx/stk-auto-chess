using System;
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 베인 (217363204): 바운스 투사체 — 타겟 간 순차 이동, 데미지 감소, 공속 버프.
    /// Channeling + Homing 투사체 패턴 (미노 기반).
    /// 스펙: {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=감소율%, {3}=버프시간(초),
    ///       {4}=공속증가%(1명당), {5}=최대바운스
    /// vfx[0]=투사체(베지어, Homing), vfx[1]=히트 이펙트
    /// </summary>
    public class SimSkillVeinBounce : SimSkillBase
    {
        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;
        public override bool HasProjectile => true;
        public override int GetCastFrames() => 0;

        private const float TravelTimeSec = 0.3f;
        private const int DamageDelayFrames = 3;
        private const int MaxBounces = 8; // hitIds 배열 상한

        private int _travelFrames;
        private int _decayPercent;

        // 상태
        private enum Phase { Launch, WaitArrival, Done }
        private Phase _phase;
        private int _launchTimer;
        private int _arrivalTimer;
        private int _clipEndTimer;

        // 바운스 추적
        private int _currentTargetId;
        private int _currentPower;
        private int _bounceIdx;
        private readonly int[] _hitIds = new int[MaxBounces];
        private int _hitCount;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=감소율%, {3}=버프시간(초),
            // {4}=공속증가%(1명당), {5}=최대바운스
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            TargetCount = SkillSpecHelper.GetInt(specList, 5, 5f);
            SecondaryPowerPercent = SkillSpecHelper.GetInt(specList, 2, 20f);
            BuffStat = StatModType.AttackSpeed;
            BuffDurationFrames = SkillSpecHelper.GetFrames(specList, 3, 3f, tickRate);
            BuffValue = SkillSpecHelper.GetInt(specList, 4, 30f);
            _travelFrames = SkillSpecHelper.SecondsToFrames(TravelTimeSec, tickRate);
            _decayPercent = SecondaryPowerPercent;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _currentTargetId = targetCombatId;
            _currentPower = PowerPercent;
            _bounceIdx = 0;
            _hitCount = 0;
            _phase = Phase.Launch;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : 120;

            // 첫 발사는 SKL 키프레임 타이밍 대기
            _launchTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 1;

            for (int i = 0; i < MaxBounces; i++)
                _hitIds[i] = CombatUnit.InvalidId;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            switch (_phase)
            {
                case Phase.Launch:
                    _launchTimer--;
                    if (_launchTimer <= 0)
                        LaunchBounce(state, ref caster);
                    break;

                case Phase.WaitArrival:
                    _arrivalTimer--;
                    if (_arrivalTimer <= 0)
                        OnBounceArrival(state, ref caster);
                    break;

                case Phase.Done:
                    return _clipEndTimer > 0;
            }

            return true;
        }

        private void LaunchBounce(CombatMatchState state, ref CombatUnit caster)
        {
            int targetIdx = state.FindUnitIndex(_currentTargetId);
            if (targetIdx < 0 || !state.Units[targetIdx].IsAlive)
            {
                ApplyAtkSpeedBuff(state, ref caster);
                _phase = Phase.Done;
                return;
            }

            // Homing 투사체 (damage=0, 실제 데미지는 OnBounceArrival에서)
            ProjectileSystem.CreateHomingProjectile(
                state, caster.CombatId, _currentTargetId,
                damage: 0, isCrit: false, DamageType, _travelFrames,
                skillSpecId: SkillId, skillVfxIndex: 0, useBezier: true, arrivalVfxIndex: 1);

            _arrivalTimer = _travelFrames + DamageDelayFrames;
            _phase = Phase.WaitArrival;
        }

        private void OnBounceArrival(CombatMatchState state, ref CombatUnit caster)
        {
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0)
            {
                _phase = Phase.Done;
                return;
            }

            int targetIdx = state.FindUnitIndex(_currentTargetId);

            // 타겟 생존 시 데미지 적용
            if (targetIdx >= 0 && state.Units[targetIdx].IsValidTarget)
            {
                int raw = caster.Attack * _currentPower / 100;
                int dmg = DamageSystem.CalculateDamage(raw, DamageType,
                    ref state.Units[casterIdx], ref state.Units[targetIdx]);
                DamageSystem.ApplyDamage(state, ref state.Units[targetIdx], dmg);
                DamageSystem.ChargeMana(ref state.Units[targetIdx], state.Units[targetIdx].ManaGainOnHit);

                if (_hitCount < MaxBounces)
                    _hitIds[_hitCount++] = _currentTargetId;
            }

            _bounceIdx++;
            _currentPower = _currentPower * (100 - _decayPercent) / 100;

            // 최대 바운스 도달 또는 다음 타겟 없음 → 종료
            if (_bounceIdx >= TargetCount)
            {
                ApplyAtkSpeedBuff(state, ref caster);
                _phase = Phase.Done;
                return;
            }

            // 다음 타겟 실시간 탐색 (도착 시점 기준)
            int nextTarget = FindNextBounceTarget(state, caster.TeamIndex,
                targetIdx >= 0 ? targetIdx : -1);

            if (nextTarget == CombatUnit.InvalidId)
            {
                ApplyAtkSpeedBuff(state, ref caster);
                _phase = Phase.Done;
                return;
            }

            _currentTargetId = nextTarget;
            _launchTimer = 1; // 즉시 재발사
            _phase = Phase.Launch;
        }

        private void ApplyAtkSpeedBuff(CombatMatchState state, ref CombatUnit caster)
        {
            if (_hitCount <= 0 || BuffValue <= 0 || BuffDurationFrames <= 0) return;
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx >= 0)
            {
                int totalBuff = BuffValue * _hitCount;
                SkillBuffHelper.ApplyTimedBuff(state, casterIdx, BuffStat, totalBuff, BuffDurationFrames);
            }
        }

        private int FindNextBounceTarget(CombatMatchState state, byte myTeam, int currentIdx)
        {
            int bestId = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;

            // currentIdx가 유효하지 않으면 거리 기반 탐색 불가 → 첫 번째 미피격 적 반환
            int refCol = 0, refRow = 0;
            if (currentIdx >= 0)
            {
                refCol = state.Units[currentIdx].GridCol;
                refRow = state.Units[currentIdx].GridRow;
            }

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex == myTeam || !u.IsAlive) continue;

                bool alreadyHit = false;
                for (int j = 0; j < _hitCount; j++)
                    if (_hitIds[j] == u.CombatId) { alreadyHit = true; break; }
                if (alreadyHit) continue;

                if (currentIdx < 0)
                    return u.CombatId; // 참조 위치 없으면 첫 번째 가능한 적

                int dist = Math.Abs(u.GridCol - refCol) + Math.Abs(u.GridRow - refRow);
                if (dist < bestDist) { bestDist = dist; bestId = u.CombatId; }
            }
            return bestId;
        }

        public override void Reset()
        {
            _phase = Phase.Done;
            _launchTimer = 0;
            _arrivalTimer = 0;
            _clipEndTimer = 0;
            _currentTargetId = CombatUnit.InvalidId;
            _currentPower = 0;
            _bounceIdx = 0;
            _hitCount = 0;
            for (int i = 0; i < MaxBounces; i++)
                _hitIds[i] = CombatUnit.InvalidId;
        }
    }
}
