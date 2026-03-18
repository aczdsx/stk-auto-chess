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

        /// <summary>로그 파일 저장 디렉터리. null이면 파일 저장 안 함.</summary>
        public static string LogDirectory;

        private static int _frame;
        private static System.Text.StringBuilder _sb;
        private static string _sessionTimestamp;

        public static void Begin()
        {
            _frame = 0;
            _enabled = true;
            Enabled = true;
            _sb = new System.Text.StringBuilder(8192);
            _sb.AppendLine("FRAME ACTION       UNIT  INFO");
            _sb.AppendLine("----- ------------ ----- ----------------------------------------");
            _sessionTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
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

        /// <summary>로그를 LogOutput 콜백으로 출력하고, LogDirectory가 설정되어 있으면 파일로도 저장</summary>
        public static void Flush(string prefix = null)
        {
            var log = GetLog();
            if (string.IsNullOrEmpty(log)) return;
            LogOutput?.Invoke(prefix != null ? $"{prefix}\n{log}" : log);
            SaveToFile(log, prefix);
        }

        private static void SaveToFile(string log, string prefix)
        {
            if (string.IsNullOrEmpty(LogDirectory)) return;

            if (!System.IO.Directory.Exists(LogDirectory))
                System.IO.Directory.CreateDirectory(LogDirectory);

            var tag = string.IsNullOrEmpty(prefix) ? "combat" : prefix.Trim('[', ']', ' ');
            var fileName = $"{tag}_{_sessionTimestamp}.log";
            var filePath = System.IO.Path.Combine(LogDirectory, fileName);
            System.IO.File.WriteAllText(filePath, log);
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

        public static void LogMove(int combatId, int fromCol, int fromRow, int toCol, int toRow,
            int targetId = -1, int bfsDist = -1)
        {
            string extra = "";
            if (targetId >= 0) extra += $" tgt={targetId}";
            if (bfsDist >= 0) extra += $" bfs={bfsDist}";
            Log("MOVE", combatId, $"({fromCol},{fromRow})→({toCol},{toRow}){extra}");
        }

        public static void LogMoveWait(int combatId, int targetId, int dist, string reason)
        {
            Log("MOVE_WAIT", combatId, $"tgt={targetId} dist={dist} {reason}");
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

        public static void LogFacing(int combatId, string mode, int fromCol, int fromRow, int toCol, int toRow,
            float myX, float myZ, float tgtX, float tgtZ, bool flipX, bool front)
        {
            Log("FACING", combatId, $"{mode} ({fromCol},{fromRow})→({toCol},{toRow}) " +
                $"my=({myX:F2},{myZ:F2}) tgt=({tgtX:F2},{tgtZ:F2}) flip={flipX} front={front}");
        }

        public static void LogResult(byte winner, int aliveA, int aliveB)
        {
            string result = winner == 0xFF ? "DRAW" : $"team={winner} wins";
            Log("RESULT", -1, $"{result} alive={aliveA}v{aliveB}");
        }
    }
}
