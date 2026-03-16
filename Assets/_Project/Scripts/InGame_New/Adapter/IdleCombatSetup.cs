using System.Collections.Generic;
using UnityEngine;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// Idle 전투용 CombatMatchState 생성 유틸리티.
    /// BattleReady 씬에서 GameWorld 없이 전투 프리뷰를 구성할 때 사용.
    /// playerA=0, playerB=0xFF (PvE 적팀).
    /// </summary>
    public static class IdleCombatSetup
    {
        private const byte PlayerA = 0;
        private const byte PlayerB = 0xFF;
        private const int MaxPlayerUnits = 5;

        /// <summary>
        /// 아군 챔피언 specId 리스트로 CombatMatchState를 생성한다.
        /// 아군은 row 0-3에, 적군은 TryAddEnemy로 동적 추가.
        /// </summary>
        /// <param name="playerChampionSpecIds">아군 챔피언 specId 리스트 (최대 5개)</param>
        /// <param name="eventQueue">SimEventQueue (View 연동용, null 가능)</param>
        /// <param name="rng">결정론적 RNG (struct, ref 전달)</param>
        /// <param name="tickRate">시뮬레이션 틱레이트</param>
        /// <returns>초기화된 CombatMatchState</returns>
        public static CombatMatchState CreateMatchState(
            List<int> playerChampionSpecIds,
            SimEventQueue eventQueue,
            ref DeterministicRNG rng,
            int tickRate)
        {
            var state = CombatMatchState.Create(0, PlayerA, PlayerB);
            state.EventQueue = eventQueue;

            // SkillFactory 초기화
            SkillFactory.Initialize(tickRate);

            // 아군 유닛 (team 0, row 0-3, 최대 5개)
            if (playerChampionSpecIds != null)
            {
                int count = playerChampionSpecIds.Count;
                if (count > MaxPlayerUnits) count = MaxPlayerUnits;

                for (int i = 0; i < count; i++)
                {
                    int specId = playerChampionSpecIds[i];
                    if (specId <= 0) continue;
                    if (state.UnitCount >= CombatMatchState.MaxCombatUnits) break;

                    if (!FindEmptyTile(state, 0, 3, ref rng, out int col, out int row))
                        break;

                    SpawnUnit(state, specId, teamIndex: 0, ownerIndex: PlayerA, col, row, tickRate);
                }
            }

            // 스킬 설정
            SetupSkillsForRange(state, 0, state.UnitCount, tickRate);

            // 생존 수 카운트
            state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
            state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);
            state.IgnoreEndCondition = true;

            return state;
        }

        /// <summary>
        /// 적 유닛 1체를 동적으로 추가. row 4-7에 빈 타일을 찾아 스폰.
        /// </summary>
        /// <param name="matchState">전투 상태</param>
        /// <param name="enemyChampionSpecId">적 챔피언 specId</param>
        /// <param name="rng">결정론적 RNG (struct, ref 전달)</param>
        /// <param name="tickRate">시뮬레이션 틱레이트</param>
        /// <returns>스폰 성공 여부</returns>
        public static bool TryAddEnemy(CombatMatchState matchState, int enemyChampionSpecId,
            float multipleAtk, float multipleHp,
            ref DeterministicRNG rng, int tickRate)
        {
            if (matchState.UnitCount >= CombatMatchState.MaxCombatUnits)
                return false;

            if (enemyChampionSpecId <= 0)
                return false;

            if (!FindEmptyTile(matchState, 4, 7, ref rng, out int col, out int row))
                return false;

            int unitIndex = matchState.UnitCount; // 스폰될 유닛의 인덱스
            SpawnUnit(matchState, enemyChampionSpecId, teamIndex: 1, ownerIndex: PlayerB, col, row, tickRate, multipleAtk, multipleHp);

            // 이 유닛의 스킬 설정
            SetupSkillForUnit(matchState, unitIndex, tickRate);

            // 생존 수 갱신
            matchState.AliveCountB = CombatSetupSystem.CountAliveByTeam(matchState, 1);

            return true;
        }

        /// <summary>
        /// 지정 row 범위(minRow~maxRow 포함)에서 빈 타일을 랜덤으로 하나 찾는다.
        /// </summary>
        private static bool FindEmptyTile(CombatMatchState state, int minRow, int maxRow,
            ref DeterministicRNG rng, out int outCol, out int outRow)
        {
            // 최대 7열 * 4행 = 28 타일. 초기화 시에만 호출되므로 소규모 배열 허용.
            int maxTiles = CombatGrid.Width * (maxRow - minRow + 1);
            var emptyCols = new int[maxTiles];
            var emptyRows = new int[maxTiles];
            int emptyCount = 0;

            for (int r = minRow; r <= maxRow; r++)
            {
                for (int c = 0; c < CombatGrid.Width; c++)
                {
                    if (state.GetUnitAtGrid(c, r) == CombatUnit.InvalidId)
                    {
                        emptyCols[emptyCount] = c;
                        emptyRows[emptyCount] = r;
                        emptyCount++;
                    }
                }
            }

            if (emptyCount == 0)
            {
                outCol = 0;
                outRow = 0;
                return false;
            }

            int chosen = rng.Range(0, emptyCount);
            outCol = emptyCols[chosen];
            outRow = emptyRows[chosen];
            return true;
        }

        /// <summary>유닛 1체를 지정 좌표에 스폰 (SpawnTutorialUnit 패턴 기반)</summary>
        private static void SpawnUnit(CombatMatchState state, int champSpecId,
            byte teamIndex, byte ownerIndex, int col, int row, int tickRate,
            float multipleAtk = 1f, float multipleHp = 1f)
        {
            int combatId = state.NextCombatId++;
            int slotIndex = state.UnitCount++;

            ref var unit = ref state.Units[slotIndex];
            unit.CombatId = combatId;
            unit.SourceEntityId = -1;
            unit.ChampionSpecId = champSpecId;
            unit.StarLevel = 1;
            unit.OwnerIndex = ownerIndex;
            unit.TeamIndex = teamIndex;
            unit.GridCol = (byte)col;
            unit.GridRow = (byte)row;
            unit.SizeW = 1;
            unit.SizeH = 1;
            unit.State = CombatState.Idle;
            unit.IsAlive = true;

            // 스펙 데이터 조회
            var charInfo = SpecDataManager.Instance.CharacterInfo.Get(champSpecId);

            if (charInfo != null)
            {
                ISpecCharacterInfo spec = charInfo;
                unit.MaxHP = (int)(spec.stat_hp * multipleHp);
                unit.CurrentHP = unit.MaxHP;
                unit.Attack = (int)(spec.stat_atk * multipleAtk);
                unit.Def = spec.stat_def;
                unit.AdReduce = (int)(spec.ad_reduce * 100);
                unit.ApReduce = (int)(spec.ap_reduce * 100);
                unit.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                unit.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                unit.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                unit.MaxMana = 100;
                unit.CurrentMana = 0;

                // 관통/크리
                unit.AtkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                unit.ResPierce = Mathf.Clamp((int)(spec.stat_res_pierce * 100), 0, 100);
                unit.CritRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                if (unit.CritRate <= 0) unit.CritRate = 25;
                unit.CritPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                if (unit.CritPower <= 0) unit.CritPower = 150;
                unit.HitChance = 100;
                unit.HealPower = (int)(spec.heal_power * 100);

                // 마나 리젠
                unit.ManaGainOnAttack = 10;
                unit.ManaGainOnHit = 5;

                // 스킬 ID
                unit.SkillSpecId = GetPrimarySkillId(spec);

                // ATK 키프레임 지연
                unit.AtkHitDelay = ExtractAtkHitDelay(spec.prefab_id, tickRate);

                // 범위 기본공격
                unit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(champSpecId, out _);
            }
            else
            {
                // 스펙 없으면 최소 기본값 (SpawnTutorialUnit 패턴)
                unit.MaxHP = 100;
                unit.CurrentHP = 100;
                unit.Attack = 10;
                unit.Def = 0;
                unit.AdReduce = 0;
                unit.ApReduce = 0;
                unit.AttackSpeed = 100;
                unit.AttackRange = 1;
                unit.MoveSpeed = 100;
                unit.MaxMana = 100;
                unit.CurrentMana = 0;
                unit.AtkPierce = 0;
                unit.ResPierce = 0;
                unit.CritRate = 25;
                unit.CritPower = 150;
                unit.HitChance = 100;
                unit.ManaGainOnAttack = 10;
                unit.ManaGainOnHit = 5;
                unit.AtkHitDelay = 1;
            }

            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.PendingAtkTargetId = CombatUnit.InvalidId;
            unit.PendingAtkTimer = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;
            unit.SkillCastTimer = 0;

            // 그리드에 등록
            state.SetGridMulti(col, row, unit.SizeW, unit.SizeH, combatId);

            // UnitSpawned 이벤트 발행 (View에서 시각 오브젝트 생성에 사용)
            state.EventQueue?.Push(new SimEvent
            {
                Type = SimEventType.UnitSpawned,
                EntityId = combatId,
                Value0 = champSpecId,
                Col = (byte)col,
                Row = (byte)row,
            });

            if (CombatLogger.Enabled)
                CombatLogger.LogSpawn(combatId, teamIndex, col, row, unit.MaxHP, unit.Attack, unit.AttackRange);
        }

        /// <summary>유닛 범위에 대해 스킬 인스턴스 생성 (GameWorld 없이)</summary>
        private static void SetupSkillsForRange(CombatMatchState state, int startIndex, int endIndex, int tickRate)
        {
            SkillFactory.Initialize(tickRate);

            for (int i = startIndex; i < endIndex; i++)
            {
                SetupSkillForUnit(state, i, tickRate);
            }
        }

        /// <summary>단일 유닛에 대해 스킬 인스턴스 생성</summary>
        private static void SetupSkillForUnit(CombatMatchState state, int unitIndex, int tickRate)
        {
            SkillFactory.Initialize(tickRate);

            ref var unit = ref state.Units[unitIndex];
            if (unit.SkillSpecId <= 0) return;

            var skill = SkillFactory.Create(unit.SkillSpecId);
            if (skill == null) return;

            if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
            {
                skill.Initialize(skillParams);
            }
            else
            {
                skill.Initialize(new SkillParams
                {
                    SkillId = unit.SkillSpecId,
                    PowerPercent = 200,
                    DamageType = DamageType.Magical,
                });
            }
            state.Skills[unitIndex] = skill;
        }

        /// <summary>ISpecCharacterInfo에서 첫 번째 스킬 ID 추출</summary>
        private static int GetPrimarySkillId(ISpecCharacterInfo c)
        {
            var ids = c.skill_ids;
            if (ids != null && ids.Length > 0 && ids[0] > 0)
                return ids[0];
            return 0;
        }

        /// <summary>ATK Execute 키프레임 지연 프레임 추출</summary>
        private static int ExtractAtkHitDelay(int prefabId, int tickRate)
        {
            if (prefabId <= 0) return 1;
            int atkKey = AnimKeyframeData.MakeKey(prefabId, false, AnimClipType.ATK);
            if (AnimKeyframeData.ExecuteTimes.TryGetValue(atkKey, out float execTime))
            {
                int frames = (int)(execTime * tickRate + 0.5f);
                return frames > 0 ? frames : 1;
            }
            return 1;
        }

    }
}
