#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace CookApps.AutoBattler.Editor
{
    [CustomEditor(typeof(InGameTestConfig))]
    public class InGameTestConfigEditor : UnityEditor.Editor
    {
        private const float CellSize = 50f;
        private const float GridPadding = 10f;

        private SerializedProperty _modeProp;
        private SerializedProperty _stageIdProp;
        private SerializedProperty _stageChapterIdProp;
        private SerializedProperty _gridWidthProp;
        private SerializedProperty _gridHeightProp;
        private SerializedProperty _playerCharactersProp;
        private SerializedProperty _enemyCharactersProp;
        private SerializedProperty _cameraSizeProp;
        private SerializedProperty _cameraPositionProp;
        private SerializedProperty _battleTimeLimitProp;
        private SerializedProperty _restartDelayProp;
        private SerializedProperty _playerInvincibleProp;
        private SerializedProperty _enemyInvincibleProp;
        private SerializedProperty _enableFrameRecorderProp;
        private SerializedProperty _recordStartFrameProp;
        private SerializedProperty _recordEndFrameProp;
        private SerializedProperty _showVfxSyncOverlayProp;

        private bool _showGridVisualization = true;
        private bool _showPresetSection = true;
        private string _presetName = "NewPreset";
        private Vector2 _presetScrollPos;
        private List<string> _presetFiles;

        // 그리드 클릭 배치용
        private bool _isPlayerPlacementMode = true; // true=플레이어, false=적
        private int _selectedCharacterIdForPlacement = 0;

        // 현재 그리드 크기 (모드에 따라 동적 변경)
        private int CurrentGridWidth => GetCurrentGridWidth();
        private int CurrentGridHeight => GetCurrentGridHeight();

        private GUIStyle _cellStyle;
        private GUIStyle _playerCellStyle;
        private GUIStyle _enemyCellStyle;

        private void OnEnable()
        {
            _modeProp = serializedObject.FindProperty("Mode");
            _stageIdProp = serializedObject.FindProperty("StageId");
            _stageChapterIdProp = serializedObject.FindProperty("StageChapterId");
            _gridWidthProp = serializedObject.FindProperty("GridWidth");
            _gridHeightProp = serializedObject.FindProperty("GridHeight");
            _playerCharactersProp = serializedObject.FindProperty("PlayerCharacters");
            _enemyCharactersProp = serializedObject.FindProperty("EnemyCharacters");
            _cameraSizeProp = serializedObject.FindProperty("CameraSize");
            _cameraPositionProp = serializedObject.FindProperty("CameraPosition");
            _battleTimeLimitProp = serializedObject.FindProperty("BattleTimeLimit");
            _restartDelayProp = serializedObject.FindProperty("RestartDelay");
            _playerInvincibleProp = serializedObject.FindProperty("PlayerInvincible");
            _enemyInvincibleProp = serializedObject.FindProperty("EnemyInvincible");
            _enableFrameRecorderProp = serializedObject.FindProperty("EnableFrameRecorder");
            _recordStartFrameProp = serializedObject.FindProperty("RecordStartFrame");
            _recordEndFrameProp = serializedObject.FindProperty("RecordEndFrame");
            _showVfxSyncOverlayProp = serializedObject.FindProperty("ShowVfxSyncOverlay");

            RefreshPresetList();
        }

        private int GetCurrentGridWidth()
        {
            if (_modeProp.enumValueIndex == (int)TestMode.Stage && _stageIdProp.intValue > 0)
            {
                var stage = InGameTestSpecDataHelper.FindStageById(_stageIdProp.intValue);
                if (stage.HasValue) return stage.Value.MapWidth;
            }
            return _gridWidthProp.intValue > 0 ? _gridWidthProp.intValue : 5;
        }

        private int GetCurrentGridHeight()
        {
            if (_modeProp.enumValueIndex == (int)TestMode.Stage && _stageIdProp.intValue > 0)
            {
                var stage = InGameTestSpecDataHelper.FindStageById(_stageIdProp.intValue);
                if (stage.HasValue) return stage.Value.MapHeight;
            }
            return _gridHeightProp.intValue > 0 ? _gridHeightProp.intValue : 7;
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

            // 모드 선택
            DrawModeSelection();

            EditorGUILayout.Space(10);

            bool isStageMode = _modeProp.enumValueIndex == (int)TestMode.Stage;

            // 스테이지 설정 (Stage 모드일 때만)
            if (isStageMode)
            {
                DrawStageSelection();
                EditorGUILayout.Space(10);
            }
            else
            {
                // Custom 모드: 맵 설정
                DrawCustomModeSettings();
                EditorGUILayout.Space(10);
            }

            // 그리드 시각화 섹션
            string gridTitle = isStageMode
                ? $"그리드 시각화 ({CurrentGridWidth}x{CurrentGridHeight}) - 스테이지 몬스터 표시"
                : $"그리드 시각화 ({CurrentGridWidth}x{CurrentGridHeight})";
            _showGridVisualization = EditorGUILayout.Foldout(_showGridVisualization, gridTitle, true);
            if (_showGridVisualization)
            {
                DrawGridVisualization();
            }

            EditorGUILayout.Space(10);

            // 캐릭터 리스트 (플레이어)
            EditorGUILayout.LabelField("내 캐릭터", EditorStyles.boldLabel);
            DrawCharacterList(_playerCharactersProp, true);

            EditorGUILayout.Space(10);

            // 캐릭터 리스트 (적) - Custom 모드일 때만 편집 가능
            if (!isStageMode)
            {
                EditorGUILayout.LabelField("적 캐릭터", EditorStyles.boldLabel);
                DrawCharacterList(_enemyCharactersProp, false);
            }
            else
            {
                EditorGUILayout.LabelField("적 캐릭터 (스테이지 데이터 사용)", EditorStyles.boldLabel);
                DrawStageMonsterInfo();
            }

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

            // 디버그 설정
            EditorGUILayout.LabelField("디버그 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_playerInvincibleProp, new GUIContent("플레이어 무적"));
            EditorGUILayout.PropertyField(_enemyInvincibleProp, new GUIContent("적 무적"));

            EditorGUILayout.Space(10);

            // 시뮬레이션 디버거
            EditorGUILayout.LabelField("시뮬레이션 디버거", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_enableFrameRecorderProp, new GUIContent("프레임 레코더 활성화"));
            if (_enableFrameRecorderProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_recordStartFrameProp, new GUIContent("녹화 시작 프레임 (0=전투 시작)"));
                EditorGUILayout.PropertyField(_recordEndFrameProp, new GUIContent("녹화 종료 프레임 (0=전투 끝)"));
                EditorGUILayout.PropertyField(_showVfxSyncOverlayProp, new GUIContent("VFX 동기화 오버레이"));
                EditorGUI.indentLevel--;
            }

            // Play 모드: 프레임 조작 컨트롤
            if (Application.isPlaying && _enableFrameRecorderProp.boolValue)
            {
                DrawPlayModeFrameControls();
            }

            EditorGUILayout.EndVertical();

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
            gridXProp.intValue = EditorGUILayout.IntSlider(gridXProp.intValue, 0, CurrentGridWidth - 1, GUILayout.Width(100));

            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            gridYProp.intValue = EditorGUILayout.IntSlider(gridYProp.intValue, 0, CurrentGridHeight - 1, GUILayout.Width(100));
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
            bool isStageMode = _modeProp.enumValueIndex == (int)TestMode.Stage;
            int gridWidth = CurrentGridWidth;
            int gridHeight = CurrentGridHeight;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 배치 모드 선택 (Stage 모드에서는 플레이어만 배치 가능)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("배치 모드:", GUILayout.Width(60));

            GUI.backgroundColor = _isPlayerPlacementMode ? new Color(0.2f, 0.6f, 1f) : Color.gray;
            if (GUILayout.Button("플레이어(P)", GUILayout.Width(80)))
            {
                _isPlayerPlacementMode = true;
            }

            if (!isStageMode)
            {
                GUI.backgroundColor = !_isPlayerPlacementMode ? new Color(1f, 0.3f, 0.3f) : Color.gray;
                if (GUILayout.Button("적(E)", GUILayout.Width(60)))
                {
                    _isPlayerPlacementMode = false;
                }
            }
            else
            {
                _isPlayerPlacementMode = true; // Stage 모드에서는 항상 플레이어 배치
                GUI.enabled = false;
                GUI.backgroundColor = Color.gray;
                GUILayout.Button("적(E) 🔒", GUILayout.Width(70));
                GUI.enabled = true;
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
            string hint = isStageMode
                ? "좌클릭: 플레이어 배치 / 우클릭: 삭제 (적은 스테이지 데이터)"
                : "좌클릭: 배치 / 우클릭: 삭제";
            EditorGUILayout.LabelField(hint, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);

            // 그리드 그리기
            float totalWidth = gridWidth * CellSize + GridPadding * 2;
            float totalHeight = gridHeight * CellSize + GridPadding * 2;

            var gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            // 배경
            EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

            // 캐릭터 위치 수집
            var playerPositions = new Dictionary<Vector2Int, List<int>>();
            var enemyPositions = new Dictionary<Vector2Int, List<int>>();

            // 플레이어 캐릭터
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

            // 적 캐릭터 (Stage 모드에서는 StageMonster 데이터 사용)
            if (isStageMode && _stageIdProp.intValue > 0)
            {
                var stageMonsters = InGameTestSpecDataHelper.GetStageMonstersByStageId(_stageIdProp.intValue);
                foreach (var monster in stageMonsters)
                {
                    var pos = new Vector2Int(monster.CoordX, monster.CoordY);
                    if (!enemyPositions.ContainsKey(pos))
                        enemyPositions[pos] = new List<int>();
                    enemyPositions[pos].Add(monster.MonsterId);
                }
            }
            else if (!isStageMode)
            {
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
            }

            // 셀 그리기 (Y축 반전: 게임에서 Y가 위로 갈수록 커짐)
            Event evt = Event.current;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    // Y축 반전하여 그리기
                    int drawY = gridHeight - 1 - y;
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
                                // Stage 모드에서는 플레이어만 배치 가능
                                if (isStageMode || _isPlayerPlacementMode)
                                {
                                    AddCharacterAtPosition(x, y, _selectedCharacterIdForPlacement, true);
                                }
                                else
                                {
                                    AddCharacterAtPosition(x, y, _selectedCharacterIdForPlacement, _isPlayerPlacementMode);
                                }
                            }
                            evt.Use();
                        }
                        else if (evt.button == 1) // 우클릭: 삭제
                        {
                            // Stage 모드에서는 플레이어만 삭제 가능
                            if (isStageMode)
                            {
                                if (hasPlayer)
                                {
                                    RemoveCharactersAtPosition(x, y, true);
                                }
                            }
                            else
                            {
                                ShowDeleteMenu(x, y, hasPlayer, hasEnemy);
                            }
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

        private void DrawModeSelection()
        {
            EditorGUILayout.LabelField("테스트 모드", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Custom 버튼
            bool isCustom = _modeProp.enumValueIndex == (int)TestMode.Custom;
            GUI.backgroundColor = isCustom ? new Color(0.4f, 0.8f, 0.4f) : Color.gray;
            if (GUILayout.Button("Custom\n(직접 배치)", GUILayout.Height(50)))
            {
                _modeProp.enumValueIndex = (int)TestMode.Custom;
            }

            // Stage 버튼
            bool isStage = _modeProp.enumValueIndex == (int)TestMode.Stage;
            GUI.backgroundColor = isStage ? new Color(0.4f, 0.6f, 1f) : Color.gray;
            if (GUILayout.Button("Stage\n(스테이지 불러오기)", GUILayout.Height(50)))
            {
                _modeProp.enumValueIndex = (int)TestMode.Stage;
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // 모드 설명
            if (isCustom)
            {
                EditorGUILayout.HelpBox("Custom 모드: 캐릭터와 적을 직접 배치합니다.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Stage 모드: SpecData 스테이지를 불러와 적/맵을 자동 설정합니다.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCustomModeSettings()
        {
            EditorGUILayout.LabelField("맵 설정 (Custom)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(_stageChapterIdProp, new GUIContent("맵 챕터 ID"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("그리드 크기", GUILayout.Width(80));
            _gridWidthProp.intValue = EditorGUILayout.IntField(_gridWidthProp.intValue, GUILayout.Width(40));
            EditorGUILayout.LabelField("x", GUILayout.Width(15));
            _gridHeightProp.intValue = EditorGUILayout.IntField(_gridHeightProp.intValue, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStageSelection()
        {
            EditorGUILayout.LabelField("스테이지 선택", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 스테이지 드롭다운
            var stages = InGameTestSpecDataHelper.Stages;
            int currentStageId = _stageIdProp.intValue;
            int selectedIndex = 0;

            // 드롭다운 옵션 생성
            var displayNames = new string[stages.Count + 1];
            displayNames[0] = "(스테이지 선택)";

            for (int i = 0; i < stages.Count; i++)
            {
                displayNames[i + 1] = $"{stages[i].DisplayName} ({stages[i].MapWidth}x{stages[i].MapHeight})";
                if (stages[i].StageId == currentStageId)
                {
                    selectedIndex = i + 1;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("스테이지", GUILayout.Width(60));
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, displayNames);
            if (newSelectedIndex != selectedIndex)
            {
                if (newSelectedIndex == 0)
                {
                    _stageIdProp.intValue = 0;
                }
                else
                {
                    _stageIdProp.intValue = stages[newSelectedIndex - 1].StageId;
                    _stageChapterIdProp.intValue = stages[newSelectedIndex - 1].ChapterId;
                }
            }
            EditorGUILayout.EndHorizontal();

            // 현재 선택된 스테이지 정보
            if (_stageIdProp.intValue > 0)
            {
                var stageEntry = InGameTestSpecDataHelper.FindStageById(_stageIdProp.intValue);
                if (stageEntry.HasValue)
                {
                    var monsters = InGameTestSpecDataHelper.GetStageMonstersByStageId(_stageIdProp.intValue);
                    EditorGUILayout.HelpBox(
                        $"챕터 {stageEntry.Value.ChapterId} - 스테이지 {stageEntry.Value.StageNumber}\n" +
                        $"맵 크기: {stageEntry.Value.MapWidth} x {stageEntry.Value.MapHeight}\n" +
                        $"몬스터 수: {monsters.Count}",
                        MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("스테이지를 선택하세요.", MessageType.Warning);
            }

            // 직접 입력
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stage ID", GUILayout.Width(60));
            _stageIdProp.intValue = EditorGUILayout.IntField(_stageIdProp.intValue, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStageMonsterInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_stageIdProp.intValue <= 0)
            {
                EditorGUILayout.LabelField("(스테이지를 선택하세요)", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                var monsters = InGameTestSpecDataHelper.GetStageMonstersByStageId(_stageIdProp.intValue);
                if (monsters.Count == 0)
                {
                    EditorGUILayout.LabelField("(몬스터 데이터 없음)", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"총 {monsters.Count}마리", EditorStyles.miniLabel);
                    foreach (var monster in monsters)
                    {
                        var monsterEntry = InGameTestSpecDataHelper.FindById(monster.MonsterId);
                        string name = monsterEntry.HasValue ? monsterEntry.Value.DisplayName : $"ID:{monster.MonsterId}";
                        EditorGUILayout.LabelField($"  • {name} Lv.{monster.MonsterLevel} @ ({monster.CoordX},{monster.CoordY})", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndVertical();
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

        #region Play Mode Frame Controls

        private int _editorSnapshotIndex;
        private bool _editorPaused;
        private bool _foldUnits;
        private bool _foldProjectiles;
        private bool _foldEvents = true;
        private Vector2 _detailScrollPos;

        /// <summary>
        /// 프레임 디버거 컨트롤.
        /// Layout/Repaint 불일치 방지를 위해 고정 컨트롤 수 유지.
        /// 스냅샷 상세는 GUILayout 대신 고정 Rect로 직접 그림.
        /// </summary>
        private void DrawPlayModeFrameControls()
        {
            var recorder = GetRecorderFromRunner();
            bool hasData = recorder != null && recorder.SnapshotCount > 0;
            int total = hasData ? recorder.SnapshotCount : 1;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("프레임 컨트롤 (Play 모드)", EditorStyles.boldLabel);

            // 프레임 정보
            CookApps.AutoChess.CombatFrameSnapshot snapshot = null;
            if (hasData)
            {
                _editorSnapshotIndex = Mathf.Clamp(_editorSnapshotIndex, 0, total - 1);
                snapshot = recorder.GetSnapshot(_editorSnapshotIndex);
            }
            int frameNum = snapshot?.FrameIndex ?? 0;

            EditorGUILayout.LabelField(hasData
                ? $"Frame: {frameNum}  ({_editorSnapshotIndex + 1} / {total})"
                : "녹화 대기 중...",
                _editorPaused ? EditorStyles.boldLabel : EditorStyles.label);

            // 슬라이더 (고정 — 항상 1개)
            int newIndex = EditorGUILayout.IntSlider(_editorSnapshotIndex, 0, Mathf.Max(0, total - 1));
            if (hasData && newIndex != _editorSnapshotIndex)
            {
                _editorSnapshotIndex = newIndex;
                EnsureEditorPaused();
                SeekToCurrentFrame();
            }

            // Pause / Resume (고정 — 항상 1개 버튼)
            GUI.backgroundColor = _editorPaused ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button(_editorPaused ? "▶ Resume" : "⏸ Pause", GUILayout.Height(28)))
            {
                if (hasData)
                {
                    _editorPaused = !_editorPaused;
                    var runner = GetLocalRunner();
                    if (_editorPaused)
                    {
                        runner?.PauseTickByDebugger();
                    }
                    else
                    {
                        _editorSnapshotIndex = total - 1;
                        SeekToCurrentFrame();
                        runner?.ResumeTickByDebugger();
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            // 프레임 이동 (고정 — 항상 4개 버튼)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀◀ -10", GUILayout.Height(25)) && hasData)
            {
                EnsureEditorPaused();
                _editorSnapshotIndex = Mathf.Max(0, _editorSnapshotIndex - 10);
                SeekToCurrentFrame();
            }
            if (GUILayout.Button("◀ -1", GUILayout.Height(25)) && hasData)
            {
                EnsureEditorPaused();
                _editorSnapshotIndex = Mathf.Max(0, _editorSnapshotIndex - 1);
                SeekToCurrentFrame();
            }
            if (GUILayout.Button("▶ +1", GUILayout.Height(25)) && hasData)
            {
                EnsureEditorPaused();
                _editorSnapshotIndex = Mathf.Min(total - 1, _editorSnapshotIndex + 1);
                SeekToCurrentFrame();
            }
            if (GUILayout.Button("▶▶ +10", GUILayout.Height(25)) && hasData)
            {
                EnsureEditorPaused();
                _editorSnapshotIndex = Mathf.Min(total - 1, _editorSnapshotIndex + 10);
                SeekToCurrentFrame();
            }
            EditorGUILayout.EndHorizontal();

            // 라이브 모드: 자동으로 최신 인덱스 추적
            if (!_editorPaused && hasData)
                _editorSnapshotIndex = total - 1;

            // ── 스냅샷 상세: 고정 높이 영역에 직접 렌더링 (Repaint 불일치 방지) ──
            DrawSnapshotDetails(snapshot);
        }

        /// <summary>
        /// 스냅샷 상세를 고정 높이 GUILayout.GetRect 안에서 GUI.Label로 직접 렌더링.
        /// EditorGUILayout 컨트롤을 사용하지 않으므로 Layout/Repaint 불일치 없음.
        /// </summary>
        private void DrawSnapshotDetails(CookApps.AutoChess.CombatFrameSnapshot snapshot)
        {
            int unitCount = snapshot?.UnitCount ?? 0;
            int projCount = snapshot?.ProjectileCount ?? 0;
            int evtCount = snapshot?.EventCount ?? 0;

            EditorGUILayout.LabelField($"유닛: {unitCount}  투사체: {projCount}  이벤트: {evtCount}");

            // 고정 높이 영역 확보 (내부는 GUI.Label로 직접 렌더링)
            const float detailHeight = 300f;
            const float lineHeight = 16f;
            Rect areaRect = GUILayoutUtility.GetRect(0, detailHeight, GUILayout.ExpandWidth(true));

            // 배경
            EditorGUI.DrawRect(areaRect, new Color(0.15f, 0.15f, 0.15f, 0.3f));

            if (snapshot == null) return;

            // 스크롤 가능한 텍스트 빌드
            if (_snapshotLines == null || _snapshotCacheIndex != _editorSnapshotIndex)
            {
                RebuildSnapshotText(snapshot);
                _snapshotCacheIndex = _editorSnapshotIndex;
            }

            // 텍스트 영역 (내부 스크롤)
            float contentHeight = _snapshotLineCount * lineHeight;
            Rect viewRect = new Rect(0, 0, areaRect.width - 16, contentHeight);

            _detailScrollPos = GUI.BeginScrollView(areaRect, _detailScrollPos, viewRect);

            var style = EditorStyles.miniLabel;
            float y = 0f;
            for (int i = 0; i < _snapshotLineCount; i++)
            {
                GUI.Label(new Rect(4, y, viewRect.width, lineHeight), _snapshotLines[i], style);
                y += lineHeight;
            }

            GUI.EndScrollView();
        }

        // 스냅샷 텍스트 캐시 (매 프레임 문자열 재생성 방지)
        private string[] _snapshotLines;
        private int _snapshotLineCount;
        private int _snapshotCacheIndex = -1;

        private void RebuildSnapshotText(CookApps.AutoChess.CombatFrameSnapshot snapshot)
        {
            var lines = new List<string>();

            // 유닛
            lines.Add($"── 유닛 ({snapshot.UnitCount}) ──");
            for (int i = 0; i < snapshot.UnitCount; i++)
            {
                ref var u = ref snapshot.Units[i];
                if (!u.IsAlive && u.State == CookApps.AutoChess.CombatState.Dead) continue;
                string team = u.TeamIndex == 0 ? "A" : "B";
                string cc = u.ActiveCC != CookApps.AutoChess.CrowdControlType.None
                    ? $" CC:{u.ActiveCC}({u.CCRemainingFrames}f)" : "";
                string shield = u.ShieldAmount > 0 ? $" S:{u.ShieldAmount}" : "";
                lines.Add($"  #{u.CombatId} [{team}] ({u.GridCol},{u.GridRow}) HP:{u.CurrentHp}/{u.MaxHp} MP:{u.CurrentMana}/{u.MaxMana} {u.State}{cc}{shield}");
            }

            // 투사체
            lines.Add($"── 투사체 ({snapshot.ProjectileCount}) ──");
            for (int i = 0; i < snapshot.ProjectileCount; i++)
            {
                ref var p = ref snapshot.Projectiles[i];
                lines.Add($"  P#{p.ProjectileId} ({p.Col},{p.Row}) src:#{p.SourceCombatId} {p.HitBehavior} {p.Type}");
            }

            // 이벤트
            lines.Add($"── 이벤트 ({snapshot.EventCount}) ──");
            for (int i = 0; i < snapshot.EventCount; i++)
            {
                ref var e = ref snapshot.Events[i];
                lines.Add($"  {e.Description}");
            }

            _snapshotLines = lines.ToArray();
            _snapshotLineCount = _snapshotLines.Length;
        }

        private void EnsureEditorPaused()
        {
            if (!_editorPaused)
            {
                _editorPaused = true;
                GetLocalRunner()?.PauseTickByDebugger();
            }
        }

        /// <summary>현재 _editorSnapshotIndex에 해당하는 프레임으로 리플레이 되감기</summary>
        private void SeekToCurrentFrame()
        {
            var runner = GetLocalRunner();
            if (runner?.ReplayController == null) return;

            var recorder = GetRecorderFromRunner();
            if (recorder == null || _editorSnapshotIndex < 0) return;

            // 스냅샷 인덱스 → 해당 프레임까지의 틱 수
            runner.ReplayController.SeekToFrame(_editorSnapshotIndex, recorder);
        }

        private CookApps.AutoChess.LocalSimulationRunner GetLocalRunner()
        {
            return InGameTestDebugUI.Instance?.Runner;
        }

        private CookApps.AutoChess.CombatFrameRecorder GetRecorderFromRunner()
        {
            return InGameTestDebugUI.Instance?.Recorder;
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
            public TestMode Mode;
            public int StageId;
            public int StageChapterId;
            public int GridWidth;
            public int GridHeight;
            public List<TestCharacterData> PlayerCharacters;
            public List<TestCharacterData> EnemyCharacters;
            public float CameraSize;
            public Vector3 CameraPosition;
            public float BattleTimeLimit;
            public float RestartDelay;
            public bool PlayerInvincible;
            public bool EnemyInvincible;
            public bool EnableFrameRecorder;
            public int RecordStartFrame;
            public int RecordEndFrame;
            public bool ShowVfxSyncOverlay;

            public PresetData() { }

            public PresetData(InGameTestConfig config)
            {
                Mode = config.Mode;
                StageId = config.StageId;
                StageChapterId = config.StageChapterId;
                GridWidth = config.GridWidth;
                GridHeight = config.GridHeight;
                PlayerCharacters = new List<TestCharacterData>(config.PlayerCharacters);
                EnemyCharacters = new List<TestCharacterData>(config.EnemyCharacters);
                CameraSize = config.CameraSize;
                CameraPosition = config.CameraPosition;
                BattleTimeLimit = config.BattleTimeLimit;
                RestartDelay = config.RestartDelay;
                PlayerInvincible = config.PlayerInvincible;
                EnemyInvincible = config.EnemyInvincible;
                EnableFrameRecorder = config.EnableFrameRecorder;
                RecordStartFrame = config.RecordStartFrame;
                RecordEndFrame = config.RecordEndFrame;
                ShowVfxSyncOverlay = config.ShowVfxSyncOverlay;
            }

            public void ApplyTo(InGameTestConfig config)
            {
                config.Mode = Mode;
                config.StageId = StageId;
                config.StageChapterId = StageChapterId;
                config.GridWidth = GridWidth;
                config.GridHeight = GridHeight;
                config.PlayerCharacters = new List<TestCharacterData>(PlayerCharacters);
                config.EnemyCharacters = new List<TestCharacterData>(EnemyCharacters);
                config.CameraSize = CameraSize;
                config.CameraPosition = CameraPosition;
                config.BattleTimeLimit = BattleTimeLimit;
                config.RestartDelay = RestartDelay;
                config.PlayerInvincible = PlayerInvincible;
                config.EnemyInvincible = EnemyInvincible;
                config.EnableFrameRecorder = EnableFrameRecorder;
                config.RecordStartFrame = RecordStartFrame;
                config.RecordEndFrame = RecordEndFrame;
                config.ShowVfxSyncOverlay = ShowVfxSyncOverlay;
            }
        }

        #endregion
    }
}
#endif
