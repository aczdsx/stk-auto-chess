using System.Collections.Generic;
using System.Text;
using CookApps.AutoChess;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 테스트 모드 전용 디버그 UI (IMGUI)
    /// </summary>
    public class InGameTestDebugUI : MonoBehaviour
    {
        private static InGameTestDebugUI _instance;
        public static InGameTestDebugUI Instance => _instance;

        private float _gameSpeed = 1f;
        private bool _showDebugUI = true;
        private bool _showBattleResult = false;
        private BattleResultData _lastBattleResult;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized;

        // ── 프레임 디버거 (Inspector에서만 표시) ──
        private LocalSimulationRunner _runner;
        private CombatFrameRecorder _recorder;

        public static void Create()
        {
            if (_instance != null) return;

            var go = new GameObject("[InGameTestDebugUI]");
            _instance = go.AddComponent<InGameTestDebugUI>();
            DontDestroyOnLoad(go);
        }

        public static void Destroy()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                richText = true
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!_showDebugUI) return;

            InitStyles();

            // 토글 버튼 (항상 표시)
            if (GUI.Button(new Rect(10, 10, 100, 25), _showDebugUI ? "UI 숨기기" : "UI 보이기"))
            {
                _showDebugUI = !_showDebugUI;
            }

            // 메인 디버그 패널
            GUILayout.BeginArea(new Rect(10, 45, 250, 400));
            GUILayout.BeginVertical(_boxStyle);

            // 헤더
            GUILayout.Label("<color=yellow>[TEST MODE]</color>", _headerStyle);
            GUILayout.Space(5);

            // 속도 조절
            DrawSpeedControl();

            GUILayout.Space(10);

            // 일시정지
            DrawPauseControl();

            GUILayout.Space(10);

            // 전투 결과 버튼
            if (_lastBattleResult != null)
            {
                if (GUILayout.Button(_showBattleResult ? "결과 숨기기" : "전투 결과 보기", GUILayout.Height(30)))
                {
                    _showBattleResult = !_showBattleResult;
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 전투 결과 패널
            if (_showBattleResult && _lastBattleResult != null)
            {
                DrawBattleResultPanel();
            }

            // 프레임 디버거는 Inspector에서만 표시
        }

        private void DrawSpeedControl()
        {
            GUILayout.Label("게임 속도", _labelStyle);

            GUILayout.BeginHorizontal();

            // 슬라이더
            float newSpeed = GUILayout.HorizontalSlider(_gameSpeed, 0.1f, 3f, GUILayout.Width(150));
            if (Mathf.Abs(newSpeed - _gameSpeed) > 0.01f)
            {
                _gameSpeed = newSpeed;
                ApplyGameSpeed();
            }

            // 현재 속도 표시
            GUILayout.Label($"{_gameSpeed:F1}x", _labelStyle, GUILayout.Width(40));

            GUILayout.EndHorizontal();

            // 프리셋 버튼
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x", GUILayout.Width(45))) SetSpeed(0.5f);
            if (GUILayout.Button("1x", GUILayout.Width(45))) SetSpeed(1f);
            if (GUILayout.Button("2x", GUILayout.Width(45))) SetSpeed(2f);
            if (GUILayout.Button("3x", GUILayout.Width(45))) SetSpeed(3f);
            GUILayout.EndHorizontal();
        }

        private void DrawPauseControl()
        {
            bool isPaused = InGameMainFlowManager.Instance?.IsPaused ?? false;

            if (GUILayout.Button(isPaused ? "▶ 재개" : "⏸ 일시정지", GUILayout.Height(30)))
            {
                if (isPaused)
                    InGameMainFlowManager.Instance?.Resume();
                else
                    InGameMainFlowManager.Instance?.Pause();
            }
        }

        private void SetSpeed(float speed)
        {
            _gameSpeed = speed;
            ApplyGameSpeed();
        }

        private void ApplyGameSpeed()
        {
            InGameMainFlowManager.Instance?.SetPlaySpeed(_gameSpeed);
        }

        private void DrawBattleResultPanel()
        {
            float panelWidth = 350;
            float panelHeight = 450;
            float x = Screen.width - panelWidth - 20;
            float y = 50;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
            GUILayout.BeginVertical(_boxStyle);

            GUILayout.Label("<color=cyan>[ 전투 결과 ]</color>", _headerStyle);
            GUILayout.Space(5);

            // 결과
            string resultColor = _lastBattleResult.IsWin ? "lime" : "red";
            string resultText = _lastBattleResult.IsWin ? "승리!" : "패배";
            GUILayout.Label($"<color={resultColor}><size=20>{resultText}</size></color>", _labelStyle);
            GUILayout.Label($"전투 시간: {_lastBattleResult.BattleTime:F1}초", _labelStyle);

            GUILayout.Space(10);

            // 플레이어 통계
            GUILayout.Label("<color=yellow>▶ 플레이어 팀</color>", _headerStyle);
            GUILayout.Label($"  총 데미지: {FormatNumber(_lastBattleResult.PlayerTotalDamage)}", _labelStyle);
            GUILayout.Label($"  총 힐량: {FormatNumber(_lastBattleResult.PlayerTotalHeal)}", _labelStyle);

            GUILayout.Space(5);

            // 개인별 통계
            if (_lastBattleResult.PlayerStats != null)
            {
                foreach (var stat in _lastBattleResult.PlayerStats)
                {
                    string mvpMark = stat.IsMvp ? " <color=yellow>★MVP</color>" : "";
                    GUILayout.Label($"  <color=white>{stat.CharacterName}</color>{mvpMark}", _labelStyle);
                    GUILayout.Label($"    DMG: {FormatNumber(stat.DamageDealt)} | HEAL: {FormatNumber(stat.HealGiven)}", _labelStyle);
                }
            }

            GUILayout.Space(10);

            // 적 통계
            GUILayout.Label("<color=red>▶ 적 팀</color>", _headerStyle);
            GUILayout.Label($"  총 데미지: {FormatNumber(_lastBattleResult.EnemyTotalDamage)}", _labelStyle);
            GUILayout.Label($"  총 힐량: {FormatNumber(_lastBattleResult.EnemyTotalHeal)}", _labelStyle);

            GUILayout.Space(5);

            if (_lastBattleResult.EnemyStats != null)
            {
                foreach (var stat in _lastBattleResult.EnemyStats)
                {
                    GUILayout.Label($"  <color=white>{stat.CharacterName}</color>", _labelStyle);
                    GUILayout.Label($"    DMG: {FormatNumber(stat.DamageDealt)} | HEAL: {FormatNumber(stat.HealGiven)}", _labelStyle);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private string FormatNumber(double value)
        {
            if (value >= 1000000)
                return $"{value / 1000000:F1}M";
            if (value >= 1000)
                return $"{value / 1000:F1}K";
            return $"{value:F0}";
        }

        /// <summary>
        /// 전투 결과 설정 (전투 종료 시 호출)
        /// </summary>
        public void SetBattleResult(bool isWin, float battleTime)
        {
            _lastBattleResult = new BattleResultData
            {
                IsWin = isWin,
                BattleTime = battleTime,
                PlayerTotalDamage = InGameStatistics.Instance.GetTotalAmount(ActionType.Damaged, true),
                PlayerTotalHeal = InGameStatistics.Instance.GetTotalAmount(ActionType.Healed, true),
                EnemyTotalDamage = InGameStatistics.Instance.GetTotalAmount(ActionType.Damaged, false),
                EnemyTotalHeal = InGameStatistics.Instance.GetTotalAmount(ActionType.Healed, false),
                PlayerStats = new List<CharacterStatResult>(),
                EnemyStats = new List<CharacterStatResult>()
            };

            // 플레이어 캐릭터 통계
            int mvpId = InGameStatistics.Instance.GetMvpID();
            var playerCharacters = InGameObjectManager.Instance.StartingPlayerCharacters;
            if (playerCharacters != null)
            {
                foreach (var character in playerCharacters)
                {
                    var stat = new CharacterStatResult
                    {
                        CharacterId = character.CharacterId,
                        CharacterName = GetCharacterName(character.CharacterId),
                        DamageDealt = InGameStatistics.Instance.GetAttackDamageAmount(character.CharacterUId),
                        HealGiven = InGameStatistics.Instance.GetGivenHealAmount(character.CharacterUId),
                        IsMvp = character.CharacterId == mvpId
                    };
                    _lastBattleResult.PlayerStats.Add(stat);
                }

                // DPS 내림차순 정렬
                _lastBattleResult.PlayerStats.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));
            }

            // 적 캐릭터 통계
            var enemyCharacters = InGameObjectManager.Instance.StartingEnemiesCharacters;
            if (enemyCharacters != null)
            {
                foreach (var character in enemyCharacters)
                {
                    var stat = new CharacterStatResult
                    {
                        CharacterId = character.CharacterId,
                        CharacterName = GetCharacterName(character.CharacterId),
                        DamageDealt = InGameStatistics.Instance.GetAttackDamageAmount(character.CharacterUId),
                        HealGiven = InGameStatistics.Instance.GetGivenHealAmount(character.CharacterUId),
                        IsMvp = false
                    };
                    _lastBattleResult.EnemyStats.Add(stat);
                }

                _lastBattleResult.EnemyStats.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));
            }

            _showBattleResult = true;

            // 콘솔에도 출력
            LogBattleResult();
        }

        private string GetCharacterName(int characterId)
        {
            var specCharacter = SpecDataManager.Instance.GetSpecCharacter(characterId);
            if (specCharacter != null)
            {
                return LanguageManager.Instance.GetDefaultText(specCharacter.name_token);
            }

            var specMonster = SpecDataManager.Instance.GetSpecCharacter(characterId);
            if (specMonster != null)
            {
                return LanguageManager.Instance.GetDefaultText(specMonster.name_token);
            }

            return $"ID:{characterId}";
        }

        private void LogBattleResult()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 전투 결과 ==========");
            sb.AppendLine($"결과: {(_lastBattleResult.IsWin ? "승리" : "패배")}");
            sb.AppendLine($"전투 시간: {_lastBattleResult.BattleTime:F1}초");
            sb.AppendLine();
            sb.AppendLine("[플레이어 팀]");
            sb.AppendLine($"  총 데미지: {_lastBattleResult.PlayerTotalDamage:N0}");
            sb.AppendLine($"  총 힐량: {_lastBattleResult.PlayerTotalHeal:N0}");

            foreach (var stat in _lastBattleResult.PlayerStats)
            {
                string mvp = stat.IsMvp ? " ★MVP" : "";
                sb.AppendLine($"  - {stat.CharacterName}{mvp}: DMG {stat.DamageDealt:N0} / HEAL {stat.HealGiven:N0}");
            }

            sb.AppendLine();
            sb.AppendLine("[적 팀]");
            sb.AppendLine($"  총 데미지: {_lastBattleResult.EnemyTotalDamage:N0}");
            sb.AppendLine($"  총 힐량: {_lastBattleResult.EnemyTotalHeal:N0}");

            foreach (var stat in _lastBattleResult.EnemyStats)
            {
                sb.AppendLine($"  - {stat.CharacterName}: DMG {stat.DamageDealt:N0} / HEAL {stat.HealGiven:N0}");
            }

            sb.AppendLine("================================");

            Debug.Log(sb.ToString());
        }

        // ── 프레임 디버거 ──

        public void SetFrameDebugger(LocalSimulationRunner runner, CombatFrameRecorder recorder)
        {
            _runner = runner;
            _recorder = recorder;
        }

        public void ClearFrameDebugger()
        {
            _runner = null;
            _recorder = null;
        }

        // Inspector(InGameTestConfigEditor)에서 접근용
        public LocalSimulationRunner Runner => _runner;
        public CombatFrameRecorder Recorder => _recorder;

        private class BattleResultData
        {
            public bool IsWin;
            public float BattleTime;
            public double PlayerTotalDamage;
            public double PlayerTotalHeal;
            public double EnemyTotalDamage;
            public double EnemyTotalHeal;
            public List<CharacterStatResult> PlayerStats;
            public List<CharacterStatResult> EnemyStats;
        }

        private class CharacterStatResult
        {
            public int CharacterId;
            public string CharacterName;
            public double DamageDealt;
            public double HealGiven;
            public bool IsMvp;
        }
    }
}
