namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 시뮬레이션 디버그 로거.
    /// 프레임 단위로 유닛 행동을 텍스트 로그로 기록.
    /// 형식: {frame} {Action} unit={id} {info}
    /// </summary>
    public static class CombatLogger
    {
        public static bool Enabled { get; private set; }

        /// <summary>로그 출력 콜백. Adapter 레이어에서 설정 (예: Debug.Log).</summary>
        public static System.Action<string> LogOutput;

        private static int _frame;
        private static System.Text.StringBuilder _sb;

        public static void Begin()
        {
            _frame = 0;
            _enabled = true;
            Enabled = true;
            _sb = new System.Text.StringBuilder(8192);
            _sb.AppendLine("FRAME ACTION       UNIT  INFO");
            _sb.AppendLine("----- ------------ ----- ----------------------------------------");
        }

        public static void End()
        {
            _enabled = false;
            Enabled = false;
        }

        public static void NextFrame()
        {
            _frame++;
        }

        public static string GetLog() => _sb?.ToString() ?? "";

        /// <summary>로그를 LogOutput 콜백으로 출력</summary>
        public static void Flush(string prefix = null)
        {
            var log = GetLog();
            if (string.IsNullOrEmpty(log)) return;
            LogOutput?.Invoke(prefix != null ? $"{prefix}\n{log}" : log);
        }

        // ── 내부 ──

        private static bool _enabled;

        private static void Log(string action, int unitId, string info)
        {
            if (!_enabled) return;
            _sb.AppendLine($"{_frame,5} {action,-12} {unitId,5}  {info}");
        }

        // ── 액션별 로그 메서드 ──

        public static void LogSpawn(int combatId, byte team, int col, int row, int hp, int atk, int range)
        {
            Log("SPAWN", combatId,
                $"team={team} ({col},{row}) hp={hp} atk={atk} range={range}");
        }

        public static void LogMove(int combatId, int fromCol, int fromRow, int toCol, int toRow)
        {
            Log("MOVE", combatId, $"({fromCol},{fromRow})→({toCol},{toRow})");
        }

        public static void LogBacklineJump(int combatId, int fromCol, int fromRow, int toCol, int toRow)
        {
            Log("JUMP", combatId, $"({fromCol},{fromRow})→({toCol},{toRow}) backline");
        }

        public static void LogAttack(int attackerId, int targetId, int damage, bool isCrit, bool isProjectile)
        {
            string extra = "";
            if (isCrit) extra += " CRIT";
            if (isProjectile) extra += " proj";
            Log("ATTACK", attackerId, $"→unit={targetId} dmg={damage}{extra}");
        }

        public static void LogDamage(int targetId, int damage, int remainingHP, int maxHP)
        {
            Log("DAMAGE", targetId, $"-{damage} hp={remainingHP}/{maxHP}");
        }

        public static void LogDeath(int combatId, byte team)
        {
            Log("DEATH", combatId, $"team={team}");
        }

        public static void LogDodge(int attackerId, int targetId)
        {
            Log("DODGE", targetId, $"evaded unit={attackerId}");
        }

        public static void LogSkillCast(int casterId, int targetId, int skillId, bool instant)
        {
            Log("SKILL", casterId,
                $"→unit={targetId} id={skillId}{(instant ? " instant" : " casting")}");
        }

        public static void LogSkillExecute(int casterId, int targetId, int skillId)
        {
            Log("SKILL_HIT", casterId, $"→unit={targetId} id={skillId}");
        }

        public static void LogCC(int targetId, CrowdControlType type, int frames)
        {
            Log("CC", targetId, $"{type} frames={frames}");
        }

        public static void LogHeal(int targetId, int amount, int newHP, int maxHP)
        {
            Log("HEAL", targetId, $"+{amount} hp={newHP}/{maxHP}");
        }

        public static void LogShieldAdd(int unitId, int amount)
        {
            Log("SHIELD+", unitId, $"+{amount}");
        }

        public static void LogShieldAbsorb(int unitId, int absorbed, int remaining)
        {
            Log("SHIELD", unitId, $"absorbed={absorbed} remaining={remaining}");
        }

        public static void LogProjectileHit(int targetId, int sourceId, int damage, bool isCrit)
        {
            Log("PROJ_HIT", targetId,
                $"from unit={sourceId} dmg={damage}{(isCrit ? " CRIT" : "")}");
        }

        public static void LogTargetSelect(int unitId, int targetId)
        {
            Log("TARGET", unitId, $"→unit={targetId}");
        }

        public static void LogResult(byte winner, int aliveA, int aliveB)
        {
            string result = winner == 0xFF ? "DRAW" : $"team={winner} wins";
            Log("RESULT", -1, $"{result} alive={aliveA}v{aliveB}");
        }
    }
}
