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
        private const int DefaultTickRate = 30;

        /// <summary>
        /// 아군 챔피언 specId 배열로 CombatMatchState를 생성하고,
        /// 적군을 enemySpecIds 배열로 스폰한다.
        /// </summary>
        /// <param name="playerSpecIds">아군 챔피언 specId 배열 (최대 ~7개, row 0-3 배치)</param>
        /// <param name="enemySpecIds">적군 챔피언 specId 배열 (row 4-7 배치)</param>
        /// <param name="rng">결정론적 RNG (struct, ref 전달)</param>
        /// <returns>초기화된 CombatMatchState</returns>
        public static CombatMatchState Create(int[] playerSpecIds, int[] enemySpecIds, ref DeterministicRNG rng)
        {
            var state = CombatMatchState.Create(0, PlayerA, PlayerB);

            // 아군 유닛 (team 0, row 0-3)
            SpawnUnitsFromSpecIds(state, playerSpecIds, teamIndex: 0, ownerIndex: PlayerA, startRow: 0, mirrorGrid: false);

            // 적군 유닛 (team 1, row 4-7, 미러링)
            SpawnUnitsFromSpecIds(state, enemySpecIds, teamIndex: 1, ownerIndex: PlayerB, startRow: 0, mirrorGrid: true);

            // 스킬 설정 (GameWorld 없이 SkillFactory 직접 사용)
            SetupSkillsWithoutWorld(state);

            // 생존 수 카운트
            state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
            state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);

            return state;
        }

        /// <summary>
        /// specId 배열로 유닛을 스폰하여 CombatMatchState에 배치.
        /// SpawnTutorialUnit 패턴 기반, 스펙 데이터에서 스탯을 조회하여 채움.
        /// </summary>
        private static void SpawnUnitsFromSpecIds(
            CombatMatchState state, int[] specIds,
            byte teamIndex, byte ownerIndex, int startRow, bool mirrorGrid)
        {
            if (specIds == null) return;

            int boardWidth = CombatGrid.Width; // 7

            for (int i = 0; i < specIds.Length; i++)
            {
                int specId = specIds[i];
                if (specId <= 0) continue;
                if (state.UnitCount >= CombatMatchState.MaxCombatUnits) break;

                // 보드 좌표 계산 (좌→우, 행 순서)
                int col = i % boardWidth;
                int row = startRow + i / boardWidth;

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

                // 스펙 데이터 조회
                var charInfo = CharacterInfo.Get(specId);
                ISpecCharacterInfo spec = charInfo;

                int combatId = state.NextCombatId++;
                int slotIndex = state.UnitCount++;

                ref var unit = ref state.Units[slotIndex];
                unit.CombatId = combatId;
                unit.SourceEntityId = -1; // idle에서는 원본 없음
                unit.ChampionSpecId = specId;
                unit.StarLevel = 1;
                unit.OwnerIndex = ownerIndex;
                unit.TeamIndex = teamIndex;
                unit.GridCol = (byte)gridCol;
                unit.GridRow = (byte)gridRow;
                unit.SizeW = 1;
                unit.SizeH = 1;
                unit.State = CombatState.Idle;
                unit.IsAlive = true;

                // 스탯 설정
                if (charInfo != null)
                {
                    unit.MaxHP = spec.stat_hp;
                    unit.CurrentHP = spec.stat_hp;
                    unit.Attack = spec.stat_atk;
                    unit.Armor = spec.stat_def;
                    unit.MagicResist = (int)spec.ap_reduce;
                    unit.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                    unit.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                    unit.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                    unit.MaxMana = 100;
                    unit.CurrentMana = 0;

                    // 관통/크리
                    unit.ArmorPenetration = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                    unit.MagicPenetration = Mathf.Clamp((int)(spec.stat_res_pierce * 100), 0, 100);
                    unit.CritChance = Mathf.Max(0, (int)(spec.crit_rate * 100));
                    if (unit.CritChance <= 0) unit.CritChance = 25;
                    unit.CritMultiplier = Mathf.Max(0, (int)(spec.crit_power * 100));
                    if (unit.CritMultiplier <= 0) unit.CritMultiplier = 150;
                    unit.HitChance = 100;

                    // 스킬 ID
                    unit.SkillSpecId = GetPrimarySkillId(spec);

                    // ATK 키프레임 지연
                    unit.AtkHitDelay = ExtractAtkHitDelay(spec.prefab_id, DefaultTickRate);

                    // 범위 기본공격
                    unit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(specId, out _);
                }
                else
                {
                    // 스펙 없으면 최소 기본값 (SpawnTutorialUnit 패턴)
                    unit.MaxHP = 100;
                    unit.CurrentHP = 100;
                    unit.Attack = 10;
                    unit.Armor = 0;
                    unit.MagicResist = 0;
                    unit.AttackSpeed = 100;
                    unit.AttackRange = 1;
                    unit.MoveSpeed = 100;
                    unit.MaxMana = 100;
                    unit.CurrentMana = 0;
                    unit.CritChance = 25;
                    unit.CritMultiplier = 150;
                    unit.HitChance = 100;
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
                state.SetGridMulti(gridCol, gridRow, unit.SizeW, unit.SizeH, combatId);
            }
        }

        /// <summary>
        /// GameWorld 없이 SkillFactory를 직접 사용하여 스킬 인스턴스 생성.
        /// SkillSystem.SetupSkills(state, world) 패턴을 재현하되, world 의존성 제거.
        /// </summary>
        private static void SetupSkillsWithoutWorld(CombatMatchState state)
        {
            // SkillFactory가 아직 초기화되지 않았으면 초기화
            SkillFactory.Initialize(DefaultTickRate);

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (unit.SkillSpecId <= 0) continue;

                var skill = SkillFactory.Create(unit.SkillSpecId);
                if (skill == null) continue;

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
                state.Skills[i] = skill;
            }
        }

        /// <summary>ISpecCharacterInfo에서 첫 번째 스킬 ID 추출 (AutoChessSpecAdapter.GetPrimarySkillId 복제)</summary>
        private static int GetPrimarySkillId(ISpecCharacterInfo c)
        {
            var ids = c.skill_ids;
            if (ids != null && ids.Length > 0 && ids[0] > 0)
                return ids[0];
            return 0;
        }

        /// <summary>ATK Execute 키프레임 지연 프레임 추출 (CombatSetupSystem.ExtractAtkHitDelay 복제)</summary>
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
