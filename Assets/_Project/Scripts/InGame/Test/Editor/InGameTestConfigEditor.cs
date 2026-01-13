#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace CookApps.AutoBattler.Editor
{
    [CustomEditor(typeof(InGameTestConfig))]
    public class InGameTestConfigEditor : UnityEditor.Editor
    {
        private const int GridWidth = 5;  // 0~4
        private const int GridHeight = 7; // 0~5
        private const float CellSize = 50f;
        private const float GridPadding = 10f;

        private SerializedProperty _stageChapterIdProp;
        private SerializedProperty _playerCharactersProp;
        private SerializedProperty _enemyCharactersProp;
        private SerializedProperty _cameraSizeProp;
        private SerializedProperty _cameraPositionProp;
        private SerializedProperty _battleTimeLimitProp;
        private SerializedProperty _restartDelayProp;

        private bool _showGridVisualization = true;
        private bool _showPresetSection = true;
        private string _presetName = "NewPreset";
        private Vector2 _presetScrollPos;
        private List<string> _presetFiles;

        // 그리드 클릭 배치용
        private bool _isPlayerPlacementMode = true; // true=플레이어, false=적
        private int _selectedCharacterIdForPlacement = 0;

        private GUIStyle _cellStyle;
        private GUIStyle _playerCellStyle;
        private GUIStyle _enemyCellStyle;

        private void OnEnable()
        {
            _stageChapterIdProp = serializedObject.FindProperty("StageChapterId");
            _playerCharactersProp = serializedObject.FindProperty("PlayerCharacters");
            _enemyCharactersProp = serializedObject.FindProperty("EnemyCharacters");
            _cameraSizeProp = serializedObject.FindProperty("CameraSize");
            _cameraPositionProp = serializedObject.FindProperty("CameraPosition");
            _battleTimeLimitProp = serializedObject.FindProperty("BattleTimeLimit");
            _restartDelayProp = serializedObject.FindProperty("RestartDelay");

            RefreshPresetList();
        }

        private void InitStyles()
        {
            // 텍스처가 null이거나 GC로 파괴됐는지도 체크
            if (_cellStyle == null || _playerCellStyle?.normal?.background == null)
            {
                _cellStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    wordWrap = true
                };

                _playerCellStyle = new GUIStyle(_cellStyle);
                var playerTex = MakeTexture(2, 2, new Color(0.2f, 0.6f, 1f, 0.7f));
                playerTex.hideFlags = HideFlags.HideAndDontSave;
                _playerCellStyle.normal.background = playerTex;

                _enemyCellStyle = new GUIStyle(_cellStyle);
                var enemyTex = MakeTexture(2, 2, new Color(1f, 0.3f, 0.3f, 0.7f));
                enemyTex.hideFlags = HideFlags.HideAndDontSave;
                _enemyCellStyle.normal.background = enemyTex;
            }
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(10);

            // 헤더
            EditorGUILayout.LabelField("InGame Test Config", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 데이터 새로고침 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("SpecData 새로고침", GUILayout.Height(25)))
            {
                InGameTestSpecDataHelper.Reload();
                Debug.Log("[InGameTestConfig] SpecData 새로고침 완료");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 스테이지 설정
            EditorGUILayout.PropertyField(_stageChapterIdProp);

            EditorGUILayout.Space(10);

            // 그리드 시각화 섹션
            _showGridVisualization = EditorGUILayout.Foldout(_showGridVisualization, "그리드 시각화", true);
            if (_showGridVisualization)
            {
                DrawGridVisualization();
            }

            EditorGUILayout.Space(10);

            // 캐릭터 리스트 (플레이어)
            EditorGUILayout.LabelField("내 캐릭터", EditorStyles.boldLabel);
            DrawCharacterList(_playerCharactersProp, true);

            EditorGUILayout.Space(10);

            // 캐릭터 리스트 (적)
            EditorGUILayout.LabelField("적 캐릭터", EditorStyles.boldLabel);
            DrawCharacterList(_enemyCharactersProp, false);

            EditorGUILayout.Space(10);

            // 카메라 설정
            EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cameraSizeProp);
            EditorGUILayout.PropertyField(_cameraPositionProp);

            EditorGUILayout.Space(10);

            // 전투 설정
            EditorGUILayout.LabelField("전투 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_battleTimeLimitProp);
            EditorGUILayout.PropertyField(_restartDelayProp);

            EditorGUILayout.Space(10);

            // 프리셋 섹션
            _showPresetSection = EditorGUILayout.Foldout(_showPresetSection, "프리셋 관리", true);
            if (_showPresetSection)
            {
                DrawPresetSection();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCharacterList(SerializedProperty listProp, bool isPlayer)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var element = listProp.GetArrayElementAtIndex(i);
                DrawCharacterEntry(element, i, listProp, isPlayer);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 캐릭터 추가", GUILayout.Height(25)))
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                // 기본값 초기화
                newElement.FindPropertyRelative("CharacterId").intValue = 0;
                newElement.FindPropertyRelative("Level").intValue = 1;
                newElement.FindPropertyRelative("GridX").intValue = 0;
                newElement.FindPropertyRelative("GridY").intValue = 0;
                newElement.FindPropertyRelative("MultipleAtk").floatValue = 1f;
                newElement.FindPropertyRelative("MultipleHp").floatValue = 1f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCharacterEntry(SerializedProperty element, int index, SerializedProperty listProp, bool isPlayer)
        {
            var idProp = element.FindPropertyRelative("CharacterId");
            var levelProp = element.FindPropertyRelative("Level");
            var gridXProp = element.FindPropertyRelative("GridX");
            var gridYProp = element.FindPropertyRelative("GridY");
            var multiAtkProp = element.FindPropertyRelative("MultipleAtk");
            var multiHpProp = element.FindPropertyRelative("MultipleHp");

            Color bgColor = isPlayer ? new Color(0.7f, 0.85f, 1f) : new Color(1f, 0.75f, 0.75f);
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // 헤더 라인
            EditorGUILayout.BeginHorizontal();

            // 캐릭터 드롭다운
            var allEntries = InGameTestSpecDataHelper.GetAllEntries();
            int currentId = idProp.intValue;
            int selectedIndex = 0;

            var displayNames = new string[allEntries.Count + 1];
            displayNames[0] = "(없음)";

            for (int i = 0; i < allEntries.Count; i++)
            {
                displayNames[i + 1] = allEntries[i].DisplayName;
                if (allEntries[i].Id == currentId)
                {
                    selectedIndex = i + 1;
                }
            }

            EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(30));

            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.MinWidth(200));
            if (newSelectedIndex != selectedIndex)
            {
                if (newSelectedIndex == 0)
                {
                    idProp.intValue = 0;
                }
                else
                {
                    var selectedEntry = allEntries[newSelectedIndex - 1];
                    // 구분자가 아닌 경우에만 설정
                    if (selectedEntry.Id > 0)
                    {
                        idProp.intValue = selectedEntry.Id;
                    }
                }
            }

            GUILayout.FlexibleSpace();

            // 삭제 버튼
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                listProp.DeleteArrayElementAtIndex(index);
                return;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 현재 ID 표시 (직접 입력도 가능)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID (직접입력)", GUILayout.Width(80));
            idProp.intValue = EditorGUILayout.IntField(idProp.intValue, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // 두 번째 라인: 레벨, 좌표
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Lv", GUILayout.Width(20));
            levelProp.intValue = EditorGUILayout.IntField(levelProp.intValue, GUILayout.Width(40));

            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            gridXProp.intValue = EditorGUILayout.IntSlider(gridXProp.intValue, 0, GridWidth - 1, GUILayout.Width(100));

            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            gridYProp.intValue = EditorGUILayout.IntSlider(gridYProp.intValue, 0, GridHeight - 1, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // 세 번째 라인: 배수
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ATK배수", GUILayout.Width(55));
            multiAtkProp.floatValue = EditorGUILayout.FloatField(multiAtkProp.floatValue, GUILayout.Width(50));

            EditorGUILayout.LabelField("HP배수", GUILayout.Width(50));
            multiHpProp.floatValue = EditorGUILayout.FloatField(multiHpProp.floatValue, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawGridVisualization()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 배치 모드 선택
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("배치 모드:", GUILayout.Width(60));

            GUI.backgroundColor = _isPlayerPlacementMode ? new Color(0.2f, 0.6f, 1f) : Color.gray;
            if (GUILayout.Button("플레이어(P)", GUILayout.Width(80)))
            {
                _isPlayerPlacementMode = true;
            }

            GUI.backgroundColor = !_isPlayerPlacementMode ? new Color(1f, 0.3f, 0.3f) : Color.gray;
            if (GUILayout.Button("적(E)", GUILayout.Width(60)))
            {
                _isPlayerPlacementMode = false;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 캐릭터 선택 드롭다운
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("배치할 캐릭터:", GUILayout.Width(85));

            var allEntries = InGameTestSpecDataHelper.GetAllEntries();
            var displayNames = new string[allEntries.Count + 1];
            displayNames[0] = "(선택)";
            int selectedIndex = 0;

            for (int i = 0; i < allEntries.Count; i++)
            {
                displayNames[i + 1] = allEntries[i].DisplayName;
                if (allEntries[i].Id == _selectedCharacterIdForPlacement)
                {
                    selectedIndex = i + 1;
                }
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames);
            if (newIndex != selectedIndex)
            {
                _selectedCharacterIdForPlacement = newIndex == 0 ? 0 : allEntries[newIndex - 1].Id;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("좌클릭: 배치 / 우클릭: 삭제", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);

            // 그리드 그리기
            float totalWidth = GridWidth * CellSize + GridPadding * 2;
            float totalHeight = GridHeight * CellSize + GridPadding * 2;

            var gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            // 배경
            EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

            // 캐릭터 위치 수집
            var playerPositions = new Dictionary<Vector2Int, List<int>>();
            var enemyPositions = new Dictionary<Vector2Int, List<int>>();

            for (int i = 0; i < _playerCharactersProp.arraySize; i++)
            {
                var element = _playerCharactersProp.GetArrayElementAtIndex(i);
                int id = element.FindPropertyRelative("CharacterId").intValue;
                if (id <= 0) continue;

                int x = element.FindPropertyRelative("GridX").intValue;
                int y = element.FindPropertyRelative("GridY").intValue;
                var pos = new Vector2Int(x, y);

                if (!playerPositions.ContainsKey(pos))
                    playerPositions[pos] = new List<int>();
                playerPositions[pos].Add(id);
            }

            for (int i = 0; i < _enemyCharactersProp.arraySize; i++)
            {
                var element = _enemyCharactersProp.GetArrayElementAtIndex(i);
                int id = element.FindPropertyRelative("CharacterId").intValue;
                if (id <= 0) continue;

                int x = element.FindPropertyRelative("GridX").intValue;
                int y = element.FindPropertyRelative("GridY").intValue;
                var pos = new Vector2Int(x, y);

                if (!enemyPositions.ContainsKey(pos))
                    enemyPositions[pos] = new List<int>();
                enemyPositions[pos].Add(id);
            }

            // 셀 그리기 (Y축 반전: 게임에서 Y가 위로 갈수록 커짐)
            Event evt = Event.current;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    // Y축 반전하여 그리기
                    int drawY = GridHeight - 1 - y;
                    var cellRect = new Rect(
                        gridRect.x + GridPadding + x * CellSize,
                        gridRect.y + GridPadding + drawY * CellSize,
                        CellSize - 2,
                        CellSize - 2
                    );

                    var pos = new Vector2Int(x, y);
                    bool hasPlayer = playerPositions.ContainsKey(pos);
                    bool hasEnemy = enemyPositions.ContainsKey(pos);

                    string cellText = $"({x},{y})";

                    // 셀 스타일 결정
                    GUIStyle currentStyle = _cellStyle;
                    string label = cellText;

                    if (hasPlayer && hasEnemy)
                    {
                        currentStyle = _playerCellStyle;
                        label = $"P{playerPositions[pos].Count}/E{enemyPositions[pos].Count}";
                    }
                    else if (hasPlayer)
                    {
                        currentStyle = _playerCellStyle;
                        label = $"P\n{GetCharacterNamesForCell(playerPositions[pos])}";
                    }
                    else if (hasEnemy)
                    {
                        currentStyle = _enemyCellStyle;
                        label = $"E\n{GetCharacterNamesForCell(enemyPositions[pos])}";
                    }

                    // 셀 그리기
                    GUI.Box(cellRect, label, currentStyle);

                    // 클릭 처리
                    if (evt.type == UnityEngine.EventType.MouseDown && cellRect.Contains(evt.mousePosition))
                    {
                        if (evt.button == 0) // 좌클릭: 배치
                        {
                            if (_selectedCharacterIdForPlacement > 0)
                            {
                                AddCharacterAtPosition(x, y, _selectedCharacterIdForPlacement, _isPlayerPlacementMode);
                            }
                            evt.Use();
                        }
                        else if (evt.button == 1) // 우클릭: 삭제
                        {
                            ShowDeleteMenu(x, y, hasPlayer, hasEnemy);
                            evt.Use();
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void AddCharacterAtPosition(int gridX, int gridY, int characterId, bool isPlayer)
        {
            var listProp = isPlayer ? _playerCharactersProp : _enemyCharactersProp;

            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);

            newElement.FindPropertyRelative("CharacterId").intValue = characterId;
            newElement.FindPropertyRelative("Level").intValue = 1;
            newElement.FindPropertyRelative("GridX").intValue = gridX;
            newElement.FindPropertyRelative("GridY").intValue = gridY;
            newElement.FindPropertyRelative("MultipleAtk").floatValue = 1f;
            newElement.FindPropertyRelative("MultipleHp").floatValue = 1f;

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowDeleteMenu(int gridX, int gridY, bool hasPlayer, bool hasEnemy)
        {
            var menu = new GenericMenu();

            if (hasPlayer)
            {
                menu.AddItem(new GUIContent("플레이어 캐릭터 삭제"), false, () => RemoveCharactersAtPosition(gridX, gridY, true));
            }
            if (hasEnemy)
            {
                menu.AddItem(new GUIContent("적 캐릭터 삭제"), false, () => RemoveCharactersAtPosition(gridX, gridY, false));
            }
            if (hasPlayer && hasEnemy)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("모두 삭제"), false, () =>
                {
                    RemoveCharactersAtPosition(gridX, gridY, true);
                    RemoveCharactersAtPosition(gridX, gridY, false);
                });
            }

            if (!hasPlayer && !hasEnemy)
            {
                menu.AddDisabledItem(new GUIContent("캐릭터 없음"));
            }

            menu.ShowAsContext();
        }

        private void RemoveCharactersAtPosition(int gridX, int gridY, bool isPlayer)
        {
            var listProp = isPlayer ? _playerCharactersProp : _enemyCharactersProp;

            for (int i = listProp.arraySize - 1; i >= 0; i--)
            {
                var element = listProp.GetArrayElementAtIndex(i);
                int x = element.FindPropertyRelative("GridX").intValue;
                int y = element.FindPropertyRelative("GridY").intValue;

                if (x == gridX && y == gridY)
                {
                    listProp.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 저장
            EditorGUILayout.BeginHorizontal();
            _presetName = EditorGUILayout.TextField("프리셋 이름", _presetName);
            if (GUILayout.Button("저장", GUILayout.Width(60)))
            {
                SavePreset(_presetName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 불러오기 목록
            EditorGUILayout.LabelField("저장된 프리셋:");

            if (_presetFiles == null || _presetFiles.Count == 0)
            {
                EditorGUILayout.LabelField("(저장된 프리셋 없음)", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                _presetScrollPos = EditorGUILayout.BeginScrollView(_presetScrollPos, GUILayout.MaxHeight(150));

                foreach (var file in _presetFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    EditorGUILayout.LabelField(fileName);

                    if (GUILayout.Button("불러오기", GUILayout.Width(70)))
                    {
                        LoadPreset(file);
                    }
                    if (GUILayout.Button("삭제", GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("프리셋 삭제", $"'{fileName}' 프리셋을 삭제하시겠습니까?", "삭제", "취소"))
                        {
                            DeletePreset(file);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("새로고침"))
            {
                RefreshPresetList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #region Preset Management

        private string GetPresetDirectory()
        {
            string dir = "Assets/_Project/Scripts/InGame/Test/Editor/Presets";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private void RefreshPresetList()
        {
            _presetFiles = new List<string>();
            string dir = GetPresetDirectory();

            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.json");
                _presetFiles.AddRange(files);
            }
        }

        private void SavePreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                EditorUtility.DisplayDialog("오류", "프리셋 이름을 입력하세요.", "확인");
                return;
            }

            var config = (InGameTestConfig)target;
            string json = JsonUtility.ToJson(new PresetData(config), true);

            string path = Path.Combine(GetPresetDirectory(), $"{name}.json");
            File.WriteAllText(path, json);

            RefreshPresetList();
            AssetDatabase.Refresh();

            Debug.Log($"[InGameTestConfig] 프리셋 저장: {path}");
        }

        private void LoadPreset(string filePath)
        {
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("오류", "프리셋 파일을 찾을 수 없습니다.", "확인");
                return;
            }

            string json = File.ReadAllText(filePath);
            var presetData = JsonUtility.FromJson<PresetData>(json);

            var config = (InGameTestConfig)target;
            Undo.RecordObject(config, "Load Preset");

            presetData.ApplyTo(config);

            EditorUtility.SetDirty(config);
            serializedObject.Update();

            Debug.Log($"[InGameTestConfig] 프리셋 로드: {filePath}");
        }

        private void DeletePreset(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                string metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }

            RefreshPresetList();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Utility

        private string GetCharacterNamesForCell(List<int> characterIds)
        {
            var names = new List<string>();
            foreach (var id in characterIds)
            {
                var entry = InGameTestSpecDataHelper.FindById(id);
                if (entry.HasValue)
                {
                    // DisplayName에서 이름 부분만 추출 (예: "[3301] 이름" -> "이름")
                    string displayName = entry.Value.DisplayName;
                    int bracketEnd = displayName.IndexOf(']');
                    if (bracketEnd >= 0 && bracketEnd + 2 < displayName.Length)
                    {
                        names.Add(displayName.Substring(bracketEnd + 2));
                    }
                    else
                    {
                        names.Add(displayName);
                    }
                }
                else
                {
                    names.Add(id.ToString());
                }
            }
            return string.Join("\n", names);
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion

        #region Preset Data Class

        [System.Serializable]
        private class PresetData
        {
            public int StageChapterId;
            public List<TestCharacterData> PlayerCharacters;
            public List<TestCharacterData> EnemyCharacters;
            public float CameraSize;
            public Vector3 CameraPosition;
            public float BattleTimeLimit;
            public float RestartDelay;

            public PresetData() { }

            public PresetData(InGameTestConfig config)
            {
                StageChapterId = config.StageChapterId;
                PlayerCharacters = new List<TestCharacterData>(config.PlayerCharacters);
                EnemyCharacters = new List<TestCharacterData>(config.EnemyCharacters);
                CameraSize = config.CameraSize;
                CameraPosition = config.CameraPosition;
                BattleTimeLimit = config.BattleTimeLimit;
                RestartDelay = config.RestartDelay;
            }

            public void ApplyTo(InGameTestConfig config)
            {
                config.StageChapterId = StageChapterId;
                config.PlayerCharacters = new List<TestCharacterData>(PlayerCharacters);
                config.EnemyCharacters = new List<TestCharacterData>(EnemyCharacters);
                config.CameraSize = CameraSize;
                config.CameraPosition = CameraPosition;
                config.BattleTimeLimit = BattleTimeLimit;
                config.RestartDelay = RestartDelay;
            }
        }

        #endregion
    }
}
#endif
