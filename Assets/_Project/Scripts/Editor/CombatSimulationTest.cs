using UnityEngine;
using UnityEditor;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 시뮬레이션 테스트 러너.
    /// 하드코딩된 유닛으로 3v3 전투를 실행하고 프레임별 로그를 출력.
    /// </summary>
    public static class CombatSimulationTest
    {
        private const int TickRate = 30;
        private const int MaxTicks = 600; // 20초 (30fps × 20)

        [MenuItem("Tools/AutoChess/Run Combat Simulation Test")]
        public static void RunTest()
        {
            CombatLogger.Begin();

            var state = CombatMatchState.Create(0, 0, 1);

            // ── Team A (하단, row 0~3) ──
            // 탱커: 고HP, 고방어, 저공, 근접
            AddUnit(state, team: 0, col: 3, row: 1,
                hp: 800, atk: 40, armor: 40, mr: 30, atkSpd: 80,
                range: 1, moveSpd: 100, mana: 100);
            // 딜러: 중HP, 고공, 저방어, 원거리
            AddUnit(state, team: 0, col: 1, row: 0,
                hp: 400, atk: 90, armor: 10, mr: 10, atkSpd: 120,
                range: 3, moveSpd: 80, mana: 80);
            // 서포터: 중HP, 중공, 중방어, 근접
            AddUnit(state, team: 0, col: 5, row: 0,
                hp: 600, atk: 50, armor: 25, mr: 20, atkSpd: 100,
                range: 1, moveSpd: 100, mana: 120);

            // ── Team B (상단, row 4~7) ──
            // 탱커
            AddUnit(state, team: 1, col: 3, row: 6,
                hp: 750, atk: 45, armor: 35, mr: 35, atkSpd: 85,
                range: 1, moveSpd: 100, mana: 100);
            // 딜러
            AddUnit(state, team: 1, col: 1, row: 7,
                hp: 350, atk: 100, armor: 10, mr: 10, atkSpd: 110,
                range: 4, moveSpd: 80, mana: 90);
            // 어쌔신: 백라인 점프
            AddUnit(state, team: 1, col: 5, row: 7,
                hp: 500, atk: 70, armor: 15, mr: 15, atkSpd: 130,
                range: 1, moveSpd: 120, mana: 100,
                backlineJump: true);

            state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
            state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);

            // 초기 유닛 로그
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                CombatLogger.LogSpawn(u.CombatId, u.TeamIndex, u.GridCol, u.GridRow,
                    u.MaxHP, u.Attack, u.AttackRange);
            }

            // ── 시뮬레이션 실행 ──
            var rng = new DeterministicRNG(12345);

            for (int tick = 0; tick < MaxTicks; tick++)
            {
                bool finished = CombatAISystem.Tick(state, ref rng, TickRate);
                if (finished)
                {
                    CombatLogger.LogResult(state.Winner, state.AliveCountA, state.AliveCountB);
                    break;
                }
            }

            if (!state.IsFinished)
            {
                CombatAISystem.DetermineWinner(state);
                CombatLogger.LogResult(state.Winner, state.AliveCountA, state.AliveCountB);
            }

            CombatLogger.End();

            // 콘솔 출력
            string log = CombatLogger.GetLog();
            Debug.Log($"=== Combat Simulation ({state.UnitCount} units, TickRate={TickRate}) ===\n{log}");
        }

        private static void AddUnit(CombatMatchState state, byte team, int col, int row,
            int hp, int atk, int armor, int mr, int atkSpd,
            int range, int moveSpd, int mana, bool backlineJump = false)
        {
            int idx = state.UnitCount++;
            int id = state.NextCombatId++;

            ref var unit = ref state.Units[idx];
            unit.CombatId = id;
            unit.SourceEntityId = id; // 테스트에선 동일
            unit.ChampionSpecId = id + 1;
            unit.StarLevel = 1;
            unit.TeamIndex = team;
            unit.OwnerIndex = team;
            unit.GridCol = (byte)col;
            unit.GridRow = (byte)row;
            unit.State = CombatState.Idle;
            unit.IsAlive = true;

            unit.MaxHP = hp;
            unit.CurrentHP = hp;
            unit.Attack = atk;
            unit.Armor = armor;
            unit.MagicResist = mr;
            unit.AttackSpeed = atkSpd;
            unit.AttackRange = range;
            unit.MoveSpeed = moveSpd;
            unit.MaxMana = mana;
            unit.CurrentMana = 0;
            unit.CritChance = 25;
            unit.CritMultiplier = 150;

            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;

            unit.HasBacklineJump = backlineJump;
            unit.BacklineJumpDone = false;

            state.SetGrid(col, row, id);
        }
    }
}
