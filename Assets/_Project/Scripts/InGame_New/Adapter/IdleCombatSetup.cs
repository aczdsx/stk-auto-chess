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
        // 스테이지 보드 크기 (CreateMatchState에서 설정, FindEmptyTile/TryAddEnemy에서 사용)
        private static int _boardWidth = 7;
        private static int _boardHeight = 4;

        public static CombatMatchState CreateMatchState(
            List<int> playerChampionSpecIds,
            SimEventQueue eventQueue,
            ref DeterministicRNG rng,
            int tickRate,
            int boardWidth = 7,
            int boardHeight = 4)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;

            int halfHeight = _boardHeight / 2;
            BoardHelper.Setup(_boardWidth, halfHeight, _boardHeight);

            var state = CombatMatchState.Create(0, PlayerA, PlayerB);
            state.EventQueue = eventQueue;

            // SkillFactory 초기화
            SkillFactory.Initialize(tickRate);

            int playerMaxRow = halfHeight - 1;
            int enemyMinRow = halfHeight;

            // 아군 유닛 (team 0, row 0 ~ playerMaxRow, 최대 5개)
            if (playerChampionSpecIds != null)
            {
                int count = playerChampionSpecIds.Count;
                if (count > MaxPlayerUnits) count = MaxPlayerUnits;

                for (int i = 0; i < count; i++)
                {
                    int specId = playerChampionSpecIds[i];
                    if (specId <= 0) continue;
                    if (state.UnitCount >= CombatMatchState.MaxCombatUnits) break;

                    if (!FindEmptyTile(state, 0, playerMaxRow, ref rng, out int col, out int row))
                        break;

                    SpawnUnit(state, specId, teamIndex: 0, ownerIndex: PlayerA, col, row, tickRate);
                }
            }

            // idle 모드: 스킬 사용 안 함 (SetupSkillsForRange 스킵)

            // 생존 수 카운트
            state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
            state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);
            state.IgnoreEndCondition = true;

            return state;
        }

        /// <summary>적 유닛 1체를 동적으로 추가. row 4-7에 빈 타일을 찾아 스폰.</summary>
        public static bool TryAddEnemy(CombatMatchState matchState, int enemyChampionSpecId,
            float multipleAtk, float multipleHp,
            ref DeterministicRNG rng, int tickRate, int monsterLevel = 1)
        {
            if (enemyChampionSpecId <= 0)
                return false;

            int halfHeight = _boardHeight / 2;
            int enemyMinRow = halfHeight;
            int enemyMaxRow = _boardHeight - 1;
            if (!FindEmptyTile(matchState, enemyMinRow, enemyMaxRow, ref rng, out int col, out int row))
                return false;

            // 죽은 적 슬롯 재사용 시도
            int reuseSlot = FindDeadEnemySlot(matchState);
            if (reuseSlot >= 0)
            {
                RespawnUnit(matchState, reuseSlot, enemyChampionSpecId, col, row, tickRate, multipleAtk, multipleHp, monsterLevel);
            }
            else if (matchState.UnitCount < CombatMatchState.MaxCombatUnits)
            {
                SpawnUnit(matchState, enemyChampionSpecId, teamIndex: 1, ownerIndex: PlayerB, col, row, tickRate, multipleAtk, multipleHp, monsterLevel);
            }
            else
            {
                return false; // 슬롯 없음
            }

            // 생존 수 갱신
            matchState.AliveCountB = CombatSetupSystem.CountAliveByTeam(matchState, 1);

            return true;
        }

        /// <summary>죽은 적(team 1) 유닛 슬롯 인덱스 반환. 없으면 -1.</summary>
        private static int FindDeadEnemySlot(CombatMatchState state)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (unit.TeamIndex == 1 && !unit.IsAlive)
                    return i;
            }
            return -1;
        }

        /// <summary>죽은 슬롯을 새 유닛으로 재사용</summary>
        private static void RespawnUnit(CombatMatchState state, int slotIndex, int champSpecId,
            int col, int row, int tickRate, float multipleAtk, float multipleHp, int monsterLevel)
        {
            int combatId = state.NextCombatId++;

            ref var unit = ref state.Units[slotIndex];
            // 기존 데이터 초기화
            unit = default;

            unit.CombatId = combatId;
            unit.SourceEntityId = -1;
            unit.ChampionSpecId = champSpecId;
            unit.StarLevel = 1;
            unit.OwnerIndex = PlayerB;
            unit.TeamIndex = 1;
            unit.GridCol = (byte)col;
            unit.GridRow = (byte)row;
            unit.SizeW = 1;
            unit.SizeH = 1;
            unit.State = CombatState.Idle;
            unit.IsAlive = true;

            ApplySpecStats(ref unit, champSpecId, multipleAtk, multipleHp, tickRate, monsterLevel);

            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.PendingAtkTargetId = CombatUnit.InvalidId;
            unit.PendingAtkTimer = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;
            unit.SkillCastTimer = 0;

            // CombatId → 슬롯 인덱스 매핑 등록
            state.CombatIdToUnitIndex[combatId] = slotIndex;

            // 그리드 등록
            state.SetGridMulti(col, row, unit.SizeW, unit.SizeH, combatId);

            // UnitSpawned 이벤트
            state.EventQueue?.Push(new SimEvent
            {
                Type = SimEventType.UnitSpawned,
                EntityId = combatId,
                Value0 = champSpecId,
                Col = (byte)col,
                Row = (byte)row,
            });
        }

        /// <summary>
        /// 지정 row 범위(minRow~maxRow 포함)에서 빈 타일을 랜덤으로 하나 찾는다.
        /// </summary>
        private static bool FindEmptyTile(CombatMatchState state, int minRow, int maxRow,
            ref DeterministicRNG rng, out int outCol, out int outRow)
        {
            if (maxRow >= _boardHeight) maxRow = _boardHeight - 1;
            if (minRow < 0) minRow = 0;

            int maxTiles = _boardWidth * (maxRow - minRow + 1);
            var emptyCols = new int[maxTiles];
            var emptyRows = new int[maxTiles];
            int emptyCount = 0;

            for (int r = minRow; r <= maxRow; r++)
            {
                for (int c = 0; c < _boardWidth; c++)
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
            float multipleAtk = 1f, float multipleHp = 1f, int monsterLevel = 1)
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

            ApplySpecStats(ref unit, champSpecId, multipleAtk, multipleHp, tickRate, monsterLevel);

            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.PendingAtkTargetId = CombatUnit.InvalidId;
            unit.PendingAtkTimer = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;
            unit.SkillCastTimer = 0;

            // CombatId → 슬롯 인덱스 매핑 등록
            state.CombatIdToUnitIndex[combatId] = slotIndex;

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

            state.Skills[unitIndex] = SkillFactory.Create(unit.SkillSpecId);
            ref var skill = ref state.Skills[unitIndex];

            if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
            {
                SkillFactory.TryGetSpecList(unit.SkillSpecId, out var specList);
                SkillDispatcher.InitializeFromSpec(ref skill, skillParams, specList, tickRate);
            }
            else
            {
                skill.InitializeBase(new SkillParams
                {
                    SkillId = unit.SkillSpecId,
                    PowerPercent = 200,
                    DamageType = DamageType.Magical,
                });
            }
        }

        /// <summary>ISpecCharacterInfo에서 첫 번째 스킬 ID 추출</summary>
        private static int GetPrimarySkillId(ISpecCharacterInfo c)
        {
            var ids = c.skill_ids;
            if (ids != null && ids.Length > 0 && ids[0] > 0)
                return ids[0];
            return 0;
        }

        /// <summary>스펙 데이터 기반 스탯 적용 (SpawnUnit / RespawnUnit 공용)</summary>
        private static void ApplySpecStats(ref CombatUnit unit, int champSpecId,
            float multipleAtk, float multipleHp, int tickRate, int monsterLevel = 1)
        {
            var spec = SpecDataManager.Instance.GetSpecCharacter(champSpecId);

            if (spec != null)
            {
                float bonusRate = CharacterGrowthHelper.CalculateLevelBonusRate(spec, monsterLevel);
                float growthMult = 1f + bonusRate;

                unit.MaxHP = (int)(spec.stat_hp * growthMult * multipleHp);
                unit.CurrentHP = unit.MaxHP;
                unit.Attack = (int)(spec.stat_atk * growthMult * multipleAtk);
                unit.Def = (int)(spec.stat_def * growthMult);
                unit.AdReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ad_reduce);
                unit.ApReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ap_reduce);
                unit.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                unit.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                unit.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                unit.MaxMana = 100;
                unit.CurrentMana = 0;

                unit.AtkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                unit.ResPierce = Mathf.Clamp((int)(spec.stat_res_pierce * 100), 0, 100);
                unit.CritRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                if (unit.CritRate <= 0) unit.CritRate = 25;
                unit.CritPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                if (unit.CritPower <= 0) unit.CritPower = 150;
                unit.HitChance = 100;
                unit.HealPower = (int)(spec.heal_power * 100);

                unit.ManaGainOnAttack = 0;
                unit.ManaGainOnHit = 0;

                unit.SkillSpecId = GetPrimarySkillId(spec);
                unit.AtkHitDelay = ExtractAtkHitDelay(spec.prefab_id, tickRate);
                unit.AttackActionFrames = ExtractAttackActionFrames(spec.prefab_id, tickRate);
                unit.ActionLockTimer = 0;
                unit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(champSpecId, out _);
            }
            else
            {
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
                unit.ManaGainOnAttack = 0;
                unit.ManaGainOnHit = 0;
                unit.AtkHitDelay = 1;
                unit.AttackActionFrames = 1;
                unit.ActionLockTimer = 0;
            }
        }

        /// <summary>공격 Execute 키프레임 지연 프레임 추출. ATK/ATK2/CRIT, front/back 중 최댓값 사용.</summary>
        private static int ExtractAtkHitDelay(int prefabId, int tickRate)
        {
            if (prefabId <= 0) return 1;

            float maxExecTime = 0f;
            maxExecTime = MaxExecuteTime(prefabId, true, AnimClipType.ATK, maxExecTime);
            maxExecTime = MaxExecuteTime(prefabId, false, AnimClipType.ATK, maxExecTime);
            maxExecTime = MaxExecuteTime(prefabId, true, AnimClipType.ATK2, maxExecTime);
            maxExecTime = MaxExecuteTime(prefabId, false, AnimClipType.ATK2, maxExecTime);
            maxExecTime = MaxExecuteTime(prefabId, true, AnimClipType.CRIT, maxExecTime);
            maxExecTime = MaxExecuteTime(prefabId, false, AnimClipType.CRIT, maxExecTime);

            if (maxExecTime <= 0f) return 1;

            int frames = (int)(maxExecTime * tickRate + 0.5f);
            return frames > 0 ? frames : 1;
        }

        private static int ExtractAttackActionFrames(int prefabId, int tickRate)
        {
            if (prefabId <= 0) return 1;

            float maxLength = 0f;
            maxLength = MaxClipLength(prefabId, true, AnimClipType.ATK, maxLength);
            maxLength = MaxClipLength(prefabId, false, AnimClipType.ATK, maxLength);
            maxLength = MaxClipLength(prefabId, true, AnimClipType.ATK2, maxLength);
            maxLength = MaxClipLength(prefabId, false, AnimClipType.ATK2, maxLength);
            maxLength = MaxClipLength(prefabId, true, AnimClipType.CRIT, maxLength);
            maxLength = MaxClipLength(prefabId, false, AnimClipType.CRIT, maxLength);

            if (maxLength <= 0f) return 1;

            int frames = (int)(maxLength * tickRate + 0.5f);
            return frames > 0 ? frames : 1;
        }

        private static float MaxClipLength(int prefabId, bool isFront, AnimClipType clipType, float currentMax)
        {
            int key = AnimKeyframeData.MakeKey(prefabId, isFront, clipType);
            return AnimKeyframeData.ClipLengths.TryGetValue(key, out float clipLength) && clipLength > currentMax
                ? clipLength
                : currentMax;
        }

        private static float MaxExecuteTime(int prefabId, bool isFront, AnimClipType clipType, float currentMax)
        {
            int key = AnimKeyframeData.MakeKey(prefabId, isFront, clipType);
            return AnimKeyframeData.ExecuteTimes.TryGetValue(key, out float execTime) && execTime > currentMax
                ? execTime
                : currentMax;
        }

    }
}
