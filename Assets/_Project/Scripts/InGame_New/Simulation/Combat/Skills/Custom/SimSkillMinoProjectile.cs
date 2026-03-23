using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 미노 (217433302): 최저HP 적 3명에게 순차 미사일 발사 + Plus(+) 형태 스플래시.
    /// 적이 3명 미만이면 첫 번째 적에게 반복 발사.
    /// 스펙: {0}=쿨타임, {1}=데미지배율(%) → PowerPercent (메인+스플래시 동일)
    /// vfx[0]=미사일(베지어, Homing 투사체), vfx[1]=폭발(타겟 위치)
    /// </summary>
    public class SimSkillMinoProjectile : SimSkillBase
    {
        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        private const int MaxMissiles = 3;
        private const float LaunchIntervalSec = 0.3f;
        private const float TravelTimeSec = 0.5f;
        private const int DamageDelayFrames = 3; // 도착 VFX 후 데미지 지연 (시각 동기화)

        private int _launchIntervalFrames;
        private int _travelFrames;

        private readonly int[] _targetIds = new int[MaxMissiles];
        private readonly int[] _targetCols = new int[MaxMissiles];
        private readonly int[] _targetRows = new int[MaxMissiles];
        private readonly int[] _arrivalTimers = new int[MaxMissiles];
        private int _missileCount;

        private int _nextLaunchIdx;
        private int _launchTimer;
        private int _pendingArrivals;
        private int _clipEndTimer;
        private bool _allLaunched;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            TargetCount = 3;
            _launchIntervalFrames = SkillSpecHelper.SecondsToFrames(LaunchIntervalSec, tickRate);
            _travelFrames = SkillSpecHelper.SecondsToFrames(TravelTimeSec, tickRate);
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            CollectTargets(state, caster.TeamIndex);

            _nextLaunchIdx = 0;
            _pendingArrivals = 0;
            _allLaunched = false;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : 120;

            // 첫 발사는 SkillHitFrames[0] 타이밍
            _launchTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 1;

            for (int i = 0; i < MaxMissiles; i++)
                _arrivalTimers[i] = -1;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            // 미사일 발사
            if (!_allLaunched)
            {
                _launchTimer--;
                if (_launchTimer <= 0 && _nextLaunchIdx < _missileCount)
                {
                    LaunchMissile(state, ref caster, _nextLaunchIdx);
                    _nextLaunchIdx++;

                    if (_nextLaunchIdx >= _missileCount)
                        _allLaunched = true;
                    else
                        _launchTimer = _launchIntervalFrames;
                }
            }

            // 도착 처리 (스플래시 + vfx[1])
            for (int i = 0; i < _missileCount; i++)
            {
                if (_arrivalTimers[i] < 0) continue;
                _arrivalTimers[i]--;
                if (_arrivalTimers[i] <= 0)
                {
                    OnMissileArrival(state, ref caster, i);
                    _arrivalTimers[i] = -1;
                    _pendingArrivals--;
                }
            }

            if (_allLaunched && _pendingArrivals <= 0)
                return _clipEndTimer > 0;

            return true;
        }

        private void CollectTargets(CombatMatchState state, byte myTeam)
        {
            int found = 0;

            for (int c = 0; c < MaxMissiles; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex == myTeam || !u.IsAlive) continue;

                    // 이미 선택된 타겟인지 선형 탐색 (MaxMissiles=3이라 O(3))
                    bool alreadyUsed = false;
                    for (int j = 0; j < found; j++)
                    {
                        if (_targetIds[j] == u.CombatId) { alreadyUsed = true; break; }
                    }
                    if (alreadyUsed) continue;

                    if (u.CurrentHP < bestHP) { bestHP = u.CurrentHP; bestIdx = i; }
                }
                if (bestIdx < 0) break;
                _targetIds[found] = state.Units[bestIdx].CombatId;
                found++;
            }

            // 적이 3명 미만이면 첫 번째 적에게 반복
            if (found > 0)
            {
                for (int i = found; i < MaxMissiles; i++)
                    _targetIds[i] = _targetIds[0];
                _missileCount = MaxMissiles;
            }
            else
            {
                _missileCount = 0;
            }
        }

        private void LaunchMissile(CombatMatchState state, ref CombatUnit caster, int idx)
        {
            int targetIdx = state.FindUnitIndex(_targetIds[idx]);
            if (targetIdx < 0) return;

            ref var target = ref state.Units[targetIdx];
            byte col = target.GridCol;
            byte row = target.GridRow;
            _targetCols[idx] = col;
            _targetRows[idx] = row;

            // Homing 투사체: 베지어 VFX 추적 전용 (damage=0, 실제 데미지는 OnMissileArrival에서 처리)
            ProjectileSystem.CreateHomingProjectile(
                state, caster.CombatId, _targetIds[idx],
                damage: 0, isCrit: false, DamageType, _travelFrames,
                skillSpecId: SkillId, skillVfxIndex: 0, useBezier: true, arrivalVfxIndex: 1);

            _arrivalTimers[idx] = _travelFrames + DamageDelayFrames;
            _pendingArrivals++;
        }

        private void OnMissileArrival(CombatMatchState state, ref CombatUnit caster, int idx)
        {
            int targetId = _targetIds[idx];
            int targetIdx = state.FindUnitIndex(targetId);

            // 도착 위치 (생존 시 현재 위치, 사망 시 발사 시 기록 위치)
            int col, row;
            if (targetIdx >= 0 && state.Units[targetIdx].IsAlive)
            {
                col = state.Units[targetIdx].GridCol;
                row = state.Units[targetIdx].GridRow;
            }
            else
            {
                col = _targetCols[idx];
                row = _targetRows[idx];
            }

            // vfx[1] 폭발은 뷰에서 투사체 도착 시 자동 스폰 (ArrivalVfxPrefab)

            int attack = caster.Attack;
            int power = PowerPercent;
            var dmgType = DamageType;
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0) return;

            // 메인 타겟 데미지
            if (targetIdx >= 0 && state.Units[targetIdx].IsValidTarget)
            {
                int raw = attack * power / 100;
                int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref state.Units[casterIdx], ref state.Units[targetIdx]);
                DamageSystem.ApplyDamage(state, ref state.Units[targetIdx], dmg);
                DamageSystem.ChargeMana(ref state.Units[targetIdx], state.Units[targetIdx].ManaGainOnHit);
            }

            // Plus(+) 형태 스플래시 (메인 타겟 제외)
            SkillAreaHelper.ForEachEnemyInPlus(state, caster.TeamIndex, col, row, 1,
                (ref CombatUnit u, int i) =>
                {
                    if (u.CombatId == targetId) return;
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref state.Units[casterIdx], ref u);
                    DamageSystem.ApplyDamage(state, ref u, dmg);
                    DamageSystem.ChargeMana(ref u, u.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _nextLaunchIdx = 0;
            _launchTimer = 0;
            _pendingArrivals = 0;
            _clipEndTimer = 0;
            _allLaunched = false;
            _missileCount = 0;
            for (int i = 0; i < MaxMissiles; i++)
            {
                _targetIds[i] = CombatUnit.InvalidId;
                _arrivalTimers[i] = -1;
            }
        }
    }
}
