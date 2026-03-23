namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 초기화 시스템. 보드 유닛을 CombatUnit으로 복제하고,
    /// 상대 유닛을 미러 배치하여 전투 그리드를 세팅.
    /// </summary>
    public static class CombatSetupSystem
    {
        /// <summary>매치 하나의 전투 세팅. 양쪽 보드 유닛을 CombatUnit으로 복제.</summary>
        public static CombatMatchState SetupMatch(GameWorld world, byte matchIndex, byte playerA, byte playerB)
        {
            var state = CombatMatchState.Create(matchIndex, playerA, playerB);
            state.EventQueue = world.EventQueue;

            // PlayerA 유닛: 하단 배치
            SpawnTeamUnits(world, state, playerA, teamIndex: 0, mirrorGrid: false);

            // PlayerB 유닛: 상단 배치 (미러링)
            SpawnTeamUnits(world, state, playerB, teamIndex: 1, mirrorGrid: true);

            state.AliveCountA = CountAliveByTeam(state, 0);
            state.AliveCountB = CountAliveByTeam(state, 1);

            return state;
        }

        /// <summary>한 팀의 보드 유닛을 CombatUnit으로 복제하여 전투 그리드에 배치</summary>
        private static void SpawnTeamUnits(GameWorld world, CombatMatchState state,
            byte playerIndex, byte teamIndex, bool mirrorGrid)
        {
            var boardSlots = world.BoardSlots[playerIndex];

            for (int i = 0; i < world.BoardSize; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                int unitIndex = world.FindUnitIndex(entityId);
                if (unitIndex < 0) continue;

                ref var srcUnit = ref world.Units[unitIndex];
                if (!srcUnit.IsValid) continue;

                // 멀티타일 중복 스폰 방지: anchor 좌표(BoardCol, BoardRow)의 슬롯 인덱스와 일치할 때만 스폰
                int anchorIndex = BoardHelper.ToIndex(srcUnit.BoardCol, srcUnit.BoardRow);
                if (i != anchorIndex) continue;

                // 그리드 좌표 계산
                BoardHelper.FromIndex(i, out int col, out int row);

                int gridCol, gridRow;
                if (mirrorGrid)
                {
                    BoardHelper.MirrorPosition(col, row, out gridCol, out gridRow);
                }
                else
                {
                    gridCol = col;
                    gridRow = row;
                }

                // 그리드 범위 밖이면 스폰 스킵 (그리드 축소 시 보호)
                byte sizeW = srcUnit.SizeW > 0 ? srcUnit.SizeW : (byte)1;
                byte sizeH = srcUnit.SizeH > 0 ? srcUnit.SizeH : (byte)1;
                if (!BoardHelper.IsValidCombatFootprint(gridCol, gridRow, sizeW, sizeH))
                    continue;

                // CombatUnit 생성
                int combatId = state.NextCombatId++;
                int slotIndex = state.UnitCount++;
                state.CombatIdToUnitIndex[combatId] = slotIndex;

                ref var combatUnit = ref state.Units[slotIndex];
                int prefabId = FindPrefabId(world, srcUnit.ChampionSpecId);

                InitCombatUnitCommon(ref combatUnit,
                    combatId, entityId, srcUnit.ChampionSpecId,
                    srcUnit.StarLevel, playerIndex, teamIndex,
                    (byte)gridCol, (byte)gridRow,
                    srcUnit.SizeW > 0 ? srcUnit.SizeW : (byte)1,
                    srcUnit.SizeH > 0 ? srcUnit.SizeH : (byte)1,
                    srcUnit.MaxHP, srcUnit.Attack, srcUnit.AttackSpeed,
                    srcUnit.AttackRange, srcUnit.MoveSpeed, srcUnit.MaxMana,
                    srcUnit.Def, srcUnit.AdReduce, srcUnit.ApReduce,
                    world.Config.DefaultManaRegenPerSec,
                    world.Config.DefaultManaGainOnAttack,
                    world.Config.DefaultManaGainOnHit,
                    srcUnit.AtkPierce, srcUnit.ResPierce,
                    srcUnit.CritRate, srcUnit.CritPower,
                    srcUnit.HealPower, srcUnit.ImmuneType, srcUnit.TraitFlags,
                    ExtractAtkHitDelay(prefabId, world.TickRate),
                    ExtractAttackActionFrames(prefabId, world.TickRate),
                    FindSkillId(world, srcUnit.ChampionSpecId),
                    AreaAttackRegistry.TryGetPattern(srcUnit.ChampionSpecId, out _));

                // 아이템 스탯 적용 (기본 스탯 → 아이템 순서)
                ItemSystem.ApplyItemStats(world, ref combatUnit, ref srcUnit);

                // 그리드에 등록 (multi-tile)
                state.SetGridMulti(gridCol, gridRow, combatUnit.SizeW, combatUnit.SizeH, combatId);

                if (CombatLogger.Enabled) CombatLogger.LogSpawn(combatId, teamIndex, gridCol, gridRow, combatUnit.MaxHP, combatUnit.Attack, combatUnit.AttackRange);
            }
        }

        /// <summary>
        /// CombatUnit 공통 필드 초기화. SpawnTeamUnits, SpawnPvEEnemies, SpawnTutorialUnit에서 공유.
        /// 타이머/상태 필드를 기본값으로, 전투 스탯을 파라미터 값으로 설정.
        /// </summary>
        private static void InitCombatUnitCommon(ref CombatUnit unit,
            int combatId, int sourceEntityId, int championSpecId,
            byte starLevel, byte ownerIndex, byte teamIndex,
            byte gridCol, byte gridRow, byte sizeW, byte sizeH,
            int maxHP, int attack, int attackSpeed, int attackRange, int moveSpeed,
            int maxMana, int def, int adReduce, int apReduce,
            int manaRegenPerSec, int manaGainOnAttack, int manaGainOnHit,
            int atkPierce, int resPierce, int critRate, int critPower,
            int healPower, int immuneType, int traitFlags,
            int atkHitDelay, int attackActionFrames, int skillSpecId, bool hasAreaAttack)
        {
            // 식별자
            unit.CombatId = combatId;
            unit.SourceEntityId = sourceEntityId;
            unit.ChampionSpecId = championSpecId;
            unit.StarLevel = starLevel;
            unit.OwnerIndex = ownerIndex;
            unit.TeamIndex = teamIndex;
            unit.GridCol = gridCol;
            unit.GridRow = gridRow;
            unit.SizeW = sizeW;
            unit.SizeH = sizeH;
            unit.State = CombatState.Idle;
            unit.IsAlive = true;

            // 전투 스탯
            unit.MaxHP = maxHP;
            unit.CurrentHP = maxHP;
            unit.Attack = attack;
            unit.AttackSpeed = attackSpeed;
            unit.AttackRange = attackRange;
            unit.MoveSpeed = moveSpeed;
            unit.MaxMana = maxMana;
            unit.CurrentMana = 0;
            unit.Def = def;
            unit.AdReduce = adReduce;
            unit.ApReduce = apReduce;

            // 퍼센트 버프 기준값
            unit.BaseMaxHP = maxHP;
            unit.BaseAttack = attack;
            unit.BaseDef = def;
            unit.BaseAttackSpeed = attackSpeed;
            unit.BaseAdReduce = adReduce;
            unit.BaseApReduce = apReduce;

            // 마나 리젠
            unit.ManaRegenPerSec = manaRegenPerSec;
            unit.ManaGainOnAttack = manaGainOnAttack;
            unit.ManaGainOnHit = manaGainOnHit;
            unit.ManaRegenRateBonus = 0;

            // 특수 능력
            unit.AtkPierce = atkPierce;
            unit.ResPierce = resPierce;
            unit.CritRate = critRate > 0 ? critRate : 25;
            unit.CritPower = critPower > 0 ? critPower : 150;
            unit.HitChance = 100;
            unit.HealPower = healPower;
            unit.ImmuneType = immuneType;
            unit.TraitFlags = traitFlags;

            // 공격/스킬
            unit.AtkHitDelay = atkHitDelay;
            unit.AttackActionFrames = attackActionFrames;
            unit.SkillSpecId = skillSpecId;
            unit.HasAreaAttack = hasAreaAttack;

            // 타이머 초기화
            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.PendingAtkTargetId = CombatUnit.InvalidId;
            unit.PendingAtkTimer = 0;
            unit.ActionLockTimer = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;
            unit.SkillCastTimer = 0;
        }

        /// <summary>ChampionSpec 전체 조회</summary>
        private static ChampionSpec FindChampionSpec(GameWorld world, int championSpecId)
        {
            if (world.Pool == null) return default;
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == championSpecId)
                    return world.Pool.Specs[i];
            }
            return default;
        }

        /// <summary>ChampionSpec에서 SkillId 조회</summary>
        private static int FindSkillId(GameWorld world, int championSpecId)
        {
            if (world.Pool == null) return 0;
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == championSpecId)
                    return world.Pool.Specs[i].SkillId;
            }
            return 0;
        }

        /// <summary>ChampionSpec에서 PrefabId 조회</summary>
        private static int FindPrefabId(GameWorld world, int championSpecId)
        {
            if (world.Pool == null) return 0;
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == championSpecId)
                    return world.Pool.Specs[i].PrefabId;
            }
            return 0;
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

        /// <summary>공격 모션 전체 프레임 추출. ATK/ATK2/CRIT, front/back 중 최장 길이를 사용.</summary>
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

            if (maxLength <= 0f)
                return 1;

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

        /// <summary>팀별 생존 유닛 수</summary>
        public static int CountAliveByTeam(CombatMatchState state, byte teamIndex)
        {
            int count = 0;
            for (int i = 0; i < state.UnitCount; i++)
            {
                if (state.Units[i].TeamIndex == teamIndex && state.Units[i].IsAlive)
                    count++;
            }
            return count;
        }

        // ── PvE 매치 셋업 ──

        /// <summary>PvE 매치 세팅. 플레이어 보드 유닛 + PvE 적 데이터로 전투 구성.</summary>
        public static CombatMatchState SetupPvEMatch(GameWorld world, byte matchIndex, byte playerIndex)
        {
            var state = CombatMatchState.Create(matchIndex, playerIndex, 0xFF);
            state.EventQueue = world.EventQueue;

            // 플레이어 유닛 (team 0, 하단)
            SpawnTeamUnits(world, state, playerIndex, teamIndex: 0, mirrorGrid: false);

            // PvE 적 (team 1, 상단, 미러링)
            SpawnPvEEnemies(world, state);

            state.AliveCountA = CountAliveByTeam(state, 0);
            state.AliveCountB = CountAliveByTeam(state, 1);

            return state;
        }

        /// <summary>PvE 적 유닛을 CombatUnit으로 직접 생성 (보드 거치지 않음)</summary>
        private static void SpawnPvEEnemies(GameWorld world, CombatMatchState state)
        {
            for (int i = 0; i < world.PvEEnemyCount; i++)
            {
                ref var enemy = ref world.PvEEnemies[i];

                int gridCol = enemy.GridCol;
                int gridRow = enemy.GridRow;

                int combatId = state.NextCombatId++;
                int slotIndex = state.UnitCount++;
                state.CombatIdToUnitIndex[combatId] = slotIndex;

                ref var unit = ref state.Units[slotIndex];
                int enemyPrefabId = enemy.PrefabId > 0 ? enemy.PrefabId : enemy.ChampionSpecId;

                InitCombatUnitCommon(ref unit,
                    combatId, -1, enemy.ChampionSpecId,
                    1, 0xFF, 1,
                    (byte)gridCol, (byte)gridRow, enemy.SizeW, enemy.SizeH,
                    enemy.MaxHP, enemy.Attack, enemy.AttackSpeed,
                    enemy.AttackRange, enemy.MoveSpeed, enemy.MaxMana,
                    enemy.Def, enemy.AdReduce, enemy.ApReduce,
                    world.Config.DefaultManaRegenPerSec,
                    world.Config.DefaultManaGainOnAttack,
                    world.Config.DefaultManaGainOnHit,
                    enemy.AtkPierce, enemy.ResPierce,
                    enemy.CritRate, enemy.CritPower,
                    enemy.HealPower, enemy.ImmuneType, enemy.TraitFlags,
                    ExtractAtkHitDelay(enemyPrefabId, world.TickRate),
                    ExtractAttackActionFrames(enemyPrefabId, world.TickRate),
                    enemy.SkillSpecId,
                    AreaAttackRegistry.TryGetPattern(enemy.ChampionSpecId, out _));

                state.SetGridMulti(gridCol, gridRow, enemy.SizeW, enemy.SizeH, combatId);

                if (CombatLogger.Enabled) CombatLogger.LogSpawn(combatId, 1, gridCol, gridRow, unit.MaxHP, unit.Attack, unit.AttackRange);
            }
        }

        /// <summary>튜토리얼 적 유닛 1체를 전투 중 동적 스폰 (TeamB, 적팀)</summary>
        public static void SpawnTutorialUnit(ref CombatMatchState state, int monsterSpecId, int col, int row)
        {
            if (state.UnitCount >= CombatMatchState.MaxCombatUnits) return;

            // 그리드 범위 밖이면 스폰 스킵
            if (!BoardHelper.IsValidCombatFootprint(col, row, 1, 1)) return;

            int combatId = state.NextCombatId++;
            int slotIndex = state.UnitCount++;
            state.CombatIdToUnitIndex[combatId] = slotIndex;

            ref var unit = ref state.Units[slotIndex];

            // TODO: 하드코딩 스탯 → 스펙 테이블 조회로 교체 필요 (튜토리얼 데이터에서 monsterSpecId 기반 조회)
            InitCombatUnitCommon(ref unit,
                combatId, -1, monsterSpecId,
                1, 0xFF, 1,
                (byte)col, (byte)row, 1, 1,
                maxHP: 100, attack: 10, attackSpeed: 100,
                attackRange: 1, moveSpeed: 100, maxMana: 100,
                def: 0, adReduce: 0, apReduce: 0,
                manaRegenPerSec: 10, manaGainOnAttack: 0, manaGainOnHit: 0,
                atkPierce: 0, resPierce: 0, critRate: 25, critPower: 150,
                healPower: 0, immuneType: 0, traitFlags: 0,
                atkHitDelay: 1, attackActionFrames: 1, skillSpecId: 0,
                hasAreaAttack: false);

            // 그리드에 등록
            state.SetGridMulti(col, row, 1, 1, combatId);

            // 생존 수 갱신
            state.AliveCountB = CountAliveByTeam(state, 1);

            // UnitSpawned 이벤트 발행 (View에서 시각 오브젝트 생성에 사용)
            state.EventQueue?.Push(new SimEvent
            {
                Type = SimEventType.UnitSpawned,
                EntityId = combatId,
                Value0 = monsterSpecId,
                Col = (byte)col,
                Row = (byte)row,
            });

            if (CombatLogger.Enabled) CombatLogger.LogSpawn(combatId, 1, col, row, unit.MaxHP, unit.Attack, unit.AttackRange);
        }

        /// <summary>매치메이킹: 4인 → 2개 1v1 매치 배정 (간단한 라운드 로빈)</summary>
        public static void AssignMatches(GameWorld world)
        {
            // 생존 플레이어 목록
            int aliveCount = 0;
            byte[] alivePlayers = new byte[world.MaxPlayers];
            for (int i = 0; i < world.MaxPlayers; i++)
            {
                if (i < world.Config.PlayerCount && world.Players[i].IsAlive)
                    alivePlayers[aliveCount++] = (byte)i;
            }

            if (aliveCount <= 1) return;

            // 셔플 (매칭 다양성)
            world.RNG.Shuffle(alivePlayers, aliveCount);

            // 매치 배정
            if (aliveCount >= 4)
            {
                // 4인: 2개 매치
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch
                {
                    PlayerA = alivePlayers[2], PlayerB = alivePlayers[3],
                    Winner = 0xFF
                };
            }
            else if (aliveCount == 3)
            {
                // 3인: 1 실제 매치 + 1 고스트 매치
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch
                {
                    PlayerA = alivePlayers[2], PlayerB = alivePlayers[0], // 고스트: P0 보드 복제
                    IsGhostMatch = true,
                    Winner = 0xFF
                };
            }
            else // 2인
            {
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = alivePlayers[0], PlayerB = alivePlayers[1],
                    Winner = 0xFF
                };
                world.Matches[1] = new CombatMatch { IsFinished = true, Winner = 0xFF };
            }
        }
    }
}
