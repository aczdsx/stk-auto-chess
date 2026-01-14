#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 임포트 모드
    /// </summary>
    public enum ImportMode
    {
        All,        // Default + Dialogue 모두
        Default,    // Language → Default 테이블만
        Dialogue    // DialogueLanguage → Dialogue 테이블만
    }

    /// <summary>
    /// Language JSON 데이터를 Unity Localization StringTable로 임포트하는 에디터 창
    /// </summary>
    public class LocalizationImportWindow : EditorWindow
    {
        private const string DefaultJsonPath = "Assets/OriginalSpecData.json";

        private string _jsonPath = DefaultJsonPath;
        private ImportMode _importMode = ImportMode.All;
        private string _resultMessage = "";
        private MessageType _resultMessageType = MessageType.None;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Localization/Import Language Data", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationImportWindow>("Language Import");
            window.minSize = new Vector2(450, 350);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawJsonPathSection();
            EditorGUILayout.Space(10);

            DrawTableSettings();
            EditorGUILayout.Space(10);

            DrawImportButton();
            EditorGUILayout.Space(10);

            DrawResultSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Unity Localization JSON Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "서버 JSON 데이터를 Unity Localization StringTable로 임포트합니다.\n\n" +
                "• Language → Default 테이블 (token_key 사용)\n" +
                "• DialogueLanguage → Dialogue 테이블 (text_desc_token 사용)",
                MessageType.Info
            );
        }

        private void DrawJsonPathSection()
        {
            EditorGUILayout.LabelField("JSON 소스", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _jsonPath = EditorGUILayout.TextField("JSON 파일 경로", _jsonPath);
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("JSON 파일 선택", "Assets", "json");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 상대 경로로 변환
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        _jsonPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        _jsonPath = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // 파일 존재 여부 표시
            if (File.Exists(_jsonPath))
            {
                var fileInfo = new FileInfo(_jsonPath);
                EditorGUILayout.HelpBox(
                    $"파일 크기: {fileInfo.Length / 1024:N0} KB\n수정 시간: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}",
                    MessageType.None
                );
            }
            else
            {
                EditorGUILayout.HelpBox("지정된 경로에 파일이 없습니다.", MessageType.Warning);
            }
        }

        private void DrawTableSettings()
        {
            EditorGUILayout.LabelField("임포트 설정", EditorStyles.boldLabel);

            _importMode = (ImportMode)EditorGUILayout.EnumPopup("임포트 대상", _importMode);

            string modeDescription = _importMode switch
            {
                ImportMode.All => "Language와 DialogueLanguage 모두 임포트합니다.\n→ Default, Dialogue 테이블 생성/업데이트",
                ImportMode.Default => "Language만 임포트합니다.\n→ Default 테이블 생성/업데이트",
                ImportMode.Dialogue => "DialogueLanguage만 임포트합니다.\n→ Dialogue 테이블 생성/업데이트",
                _ => ""
            };
            EditorGUILayout.HelpBox(modeDescription, MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "지원되는 언어 필드:\n" +
                "• language_kr → Korean (ko)\n" +
                "• language_en → English (en)\n" +
                "• language_ja → Japanese (ja)\n" +
                "• language_zh → Chinese Simplified (zh-Hans)\n" +
                "• language_tw → Chinese Traditional (zh-Hant)",
                MessageType.None
            );
        }

        private void DrawImportButton()
        {
            EditorGUI.BeginDisabledGroup(!File.Exists(_jsonPath));

            if (GUILayout.Button("Import", GUILayout.Height(40)))
            {
                ImportLanguageData();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawResultSection()
        {
            if (!string.IsNullOrEmpty(_resultMessage))
            {
                EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_resultMessage, _resultMessageType);

                if (GUILayout.Button("결과 지우기"))
                {
                    _resultMessage = "";
                }
            }
        }

        private void ImportLanguageData()
        {
            _resultMessage = "";
            _resultMessageType = MessageType.None;

            EditorUtility.DisplayProgressBar("Language Import", "JSON 파일을 읽는 중...", 0.1f);

            try
            {
                EditorUtility.DisplayProgressBar("Language Import", "데이터를 파싱하는 중...", 0.3f);

                LocalizationImporter.ImportResult result;

                switch (_importMode)
                {
                    case ImportMode.All:
                        result = LocalizationImporter.ImportAllFromJsonFile(_jsonPath);
                        break;
                    case ImportMode.Default:
                        result = LocalizationImporter.ImportFromJsonFile(_jsonPath, ImportTableType.Default);
                        break;
                    case ImportMode.Dialogue:
                        result = LocalizationImporter.ImportFromJsonFile(_jsonPath, ImportTableType.Dialogue);
                        break;
                    default:
                        result = LocalizationImporter.ImportAllFromJsonFile(_jsonPath);
                        break;
                }

                EditorUtility.DisplayProgressBar("Language Import", "완료", 1.0f);

                _resultMessage = result.GetSummary();
                _resultMessageType = result.Success ? MessageType.Info : MessageType.Error;

                if (result.Success)
                {
                    Debug.Log($"[LocalizationImportWindow] 임포트 성공\n{_resultMessage}");
                }
                else
                {
                    Debug.LogError($"[LocalizationImportWindow] 임포트 실패: {result.ErrorMessage}");
                }
            }
            catch (System.Exception e)
            {
                _resultMessage = $"예외 발생: {e.Message}";
                _resultMessageType = MessageType.Error;
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif
