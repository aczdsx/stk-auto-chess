using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEditor;

namespace CookApps.AutoChess.Editor
{
    /// <summary>
    /// CharacterVfxConfig의 directional VFX 설정을 실시간으로 편집/미리보기하는 에디터 윈도우.
    /// 방향 버튼 클릭 즉시 해당 방향의 VFX가 씬에 표시된다.
    /// </summary>
    public class VfxDirectionPreviewWindow : EditorWindow
    {
        [MenuItem("Tools/VFX Direction Preview")]
        public static void ShowWindow()
        {
            GetWindow<VfxDirectionPreviewWindow>("VFX Direction Preview");
        }

        // 입력
        private CharacterVfxConfigSO _config;
        private GameObject _characterPrefab;
        private int _selectedVfxIndex;
        private Transform _parentTransform;

        // 편집용 값 (Config에서 로드, 수정 후 Save)
        private Vector3 _editRotOffset;
        private Vector3 _editFlipScale;
        private bool _editUseCustom;
        private bool _valuesLoaded;

        // 타일 방향
        private Vector3 _colDirection = new Vector3(1.2f, 0, 0);
        private Vector3 _rowDirection = new Vector3(0, 0, 1.2f);
        private bool _autoDetected;

        // 미리보기
        private readonly List<GameObject> _previewObjects = new();
        private int _activeDir = -1;
        private Vector2 _scrollPos;

        // 방향별 상태 추적
        private enum DirResult { NotTested, OK, Wrong }
        private readonly DirResult[] _dirResults = new DirResult[4];

        private struct DirInfo
        {
            public string Label;
            public int DirCol, DirRow;
            public bool FlipX, IsFront;
            public DirInfo(string l, int dc, int dr, bool fx, bool f) { Label=l; DirCol=dc; DirRow=dr; FlipX=fx; IsFront=f; }
        }

        private static readonly DirInfo[] Directions =
        {
            new("Back (col+1)",  1,  0, false, false),  // 0: 화면 우상
            new("Front (col-1)", -1, 0, true,  true),   // 1: 화면 좌하
            new("Left (row+1)",  0,  1, true,  false),  // 2: 화면 좌상
            new("Right (row-1)", 0, -1, false, true),   // 3: 화면 우하
        };

        private static readonly int IsFrontHash = Animator.StringToHash("IsFront");

        private void OnEnable() => TryAutoDetectTileDirections();
        private void OnDisable() => ClearPreview();

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ── 입력 ──
            EditorGUILayout.LabelField("VFX Direction Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            var newConfig = (CharacterVfxConfigSO)EditorGUILayout.ObjectField(
                "VfxConfig SO", _config, typeof(CharacterVfxConfigSO), false);
            _characterPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Character Prefab", _characterPrefab, typeof(GameObject), false);
            _parentTransform = (Transform)EditorGUILayout.ObjectField(
                "Parent (Scene)", _parentTransform, typeof(Transform), true);

            if (newConfig != _config)
            {
                _config = newConfig;
                _valuesLoaded = false;
                _selectedVfxIndex = 0;
                ClearPreview();
            }

            if (_config == null || _characterPrefab == null)
            {
                EditorGUILayout.HelpBox("VfxConfig SO와 캐릭터 InGame 프리팹을 넣어주세요.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            // 타일 방향
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            _colDirection = EditorGUILayout.Vector3Field("Col+1", _colDirection, GUILayout.Width(250));
            _rowDirection = EditorGUILayout.Vector3Field("Row+1", _rowDirection, GUILayout.Width(250));
            if (GUILayout.Button("Auto", GUILayout.Width(45)))
                TryAutoDetectTileDirections();
            EditorGUILayout.EndHorizontal();

            if (!_autoDetected)
            {
                EditorGUILayout.HelpBox(
                    "타일(Tile_0_0 등)을 씬에서 찾지 못했습니다. 방향 벡터가 부정확할 수 있습니다.\n" +
                    "InGame 씬을 열거나 Col+1/Row+1 값을 수동 입력하세요.",
                    MessageType.Warning);
            }

            // VFX 선택
            var skillVfxList = _config.SkillEffectPrefabs;
            if (skillVfxList == null || skillVfxList.Length == 0)
            {
                EditorGUILayout.HelpBox("SkillEffectPrefabs가 비어있습니다.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(8);
            string[] vfxNames = new string[skillVfxList.Length];
            for (int i = 0; i < skillVfxList.Length; i++)
            {
                var d = skillVfxList[i];
                vfxNames[i] = d?.Prefab != null ? $"[{i}] {d.Prefab.name}" : $"[{i}] (null)";
            }

            int prevIdx = _selectedVfxIndex;
            _selectedVfxIndex = Mathf.Clamp(_selectedVfxIndex, 0, skillVfxList.Length - 1);
            _selectedVfxIndex = EditorGUILayout.Popup("VFX", _selectedVfxIndex, vfxNames);
            if (prevIdx != _selectedVfxIndex) _valuesLoaded = false;

            var selectedData = skillVfxList[_selectedVfxIndex];
            if (selectedData == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            // Config에서 값 로드
            if (!_valuesLoaded)
            {
                _editRotOffset = selectedData.UseCustomRotation ? selectedData.RotationOffset : new Vector3(0, -90, 0);
                _editFlipScale = selectedData.UseCustomRotation ? selectedData.FlipScale : new Vector3(1, 1, -1);
                _editUseCustom = selectedData.UseCustomRotation;
                _valuesLoaded = true;
            }

            EditorGUILayout.LabelField($"Position: {selectedData.Position}  |  " +
                                       $"Followable: {selectedData.Followable}  |  " +
                                       $"Persistent: {selectedData.Persistent}",
                                       EditorStyles.miniLabel);

            // ── 편집 영역 ──
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Edit Values", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _editUseCustom = EditorGUILayout.Toggle("Use Custom Rotation", _editUseCustom);

            // Rotation Offset + ±90° 버튼
            EditorGUILayout.BeginHorizontal();
            _editRotOffset = EditorGUILayout.Vector3Field("Rotation Offset", _editRotOffset);
            if (GUILayout.Button("X+90", GUILayout.Width(42))) { _editRotOffset.x = NormalizeAngle(_editRotOffset.x + 90); GUI.changed = true; }
            if (GUILayout.Button("X-90", GUILayout.Width(42))) { _editRotOffset.x = NormalizeAngle(_editRotOffset.x - 90); GUI.changed = true; }
            if (GUILayout.Button("Y+90", GUILayout.Width(42))) { _editRotOffset.y = NormalizeAngle(_editRotOffset.y + 90); GUI.changed = true; }
            if (GUILayout.Button("Y-90", GUILayout.Width(42))) { _editRotOffset.y = NormalizeAngle(_editRotOffset.y - 90); GUI.changed = true; }
            if (GUILayout.Button("Z+90", GUILayout.Width(42))) { _editRotOffset.z = NormalizeAngle(_editRotOffset.z + 90); GUI.changed = true; }
            if (GUILayout.Button("Z-90", GUILayout.Width(42))) { _editRotOffset.z = NormalizeAngle(_editRotOffset.z - 90); GUI.changed = true; }
            EditorGUILayout.EndHorizontal();

            // Flip Scale + 반전 버튼
            EditorGUILayout.BeginHorizontal();
            _editFlipScale = EditorGUILayout.Vector3Field("Flip Scale", _editFlipScale);
            if (GUILayout.Button("X flip", GUILayout.Width(48))) { _editFlipScale.x *= -1; GUI.changed = true; }
            if (GUILayout.Button("Y flip", GUILayout.Width(48))) { _editFlipScale.y *= -1; GUI.changed = true; }
            if (GUILayout.Button("Z flip", GUILayout.Width(48))) { _editFlipScale.z *= -1; GUI.changed = true; }
            EditorGUILayout.EndHorizontal();

            // 프리셋
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Presets:", GUILayout.Width(55));
            if (GUILayout.Button("Default (0,-90,0)", GUILayout.Width(130)))
            {
                _editRotOffset = new Vector3(0, -90, 0);
                _editFlipScale = new Vector3(1, 1, -1);
                GUI.changed = true;
            }
            if (GUILayout.Button("Tetora (-90,-90,0)", GUILayout.Width(140)))
            {
                _editRotOffset = new Vector3(-90, -90, 0);
                _editFlipScale = new Vector3(1, -1, 1);
                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();

            bool valuesChanged = EditorGUI.EndChangeCheck();

            // Config 차이 표시
            bool isDirty = _editUseCustom != selectedData.UseCustomRotation ||
                           _editRotOffset != (selectedData.UseCustomRotation ? selectedData.RotationOffset : new Vector3(0,-90,0)) ||
                           _editFlipScale != (selectedData.UseCustomRotation ? selectedData.FlipScale : new Vector3(1,1,-1));

            if (isDirty)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Config와 다른 값입니다.", MessageType.Warning);
                if (GUILayout.Button("Save to\nConfig", GUILayout.Width(70), GUILayout.Height(38)))
                    SaveToConfig(selectedData);
                if (GUILayout.Button("Revert", GUILayout.Width(55), GUILayout.Height(38)))
                    _valuesLoaded = false;
                EditorGUILayout.EndHorizontal();
            }

            // 값 변경 시 미리보기 자동 갱신
            if (valuesChanged && _activeDir >= 0)
                ShowDirection(_activeDir);

            // ── 4방향 결과 테이블 ──
            EditorGUILayout.Space(8);
            DrawDirectionTable(_editRotOffset, _editFlipScale);

            // ── 방향 버튼 (누르면 바로 이펙트 표시) ──
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Preview Direction", EditorStyles.boldLabel);

            var prevBg = GUI.backgroundColor;

            // Isometric 배치
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = _activeDir == 2 ? Color.cyan : Color.white;
            if (GUILayout.Button("Left\n(좌상)", GUILayout.Width(85), GUILayout.Height(38)))
                ShowDirection(2);
            GUILayout.Space(8);
            GUI.backgroundColor = _activeDir == 0 ? Color.cyan : Color.white;
            if (GUILayout.Button("Back\n(우상)", GUILayout.Width(85), GUILayout.Height(38)))
                ShowDirection(0);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = _activeDir == 1 ? Color.cyan : Color.white;
            if (GUILayout.Button("Front\n(좌하)", GUILayout.Width(85), GUILayout.Height(38)))
                ShowDirection(1);
            GUILayout.Space(8);
            GUI.backgroundColor = _activeDir == 3 ? Color.cyan : Color.white;
            if (GUILayout.Button("Right\n(우하)", GUILayout.Width(85), GUILayout.Height(38)))
                ShowDirection(3);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = prevBg;

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All 4 Directions", GUILayout.Height(25)))
                ShowAllDirections();
            if (GUILayout.Button("Clear", GUILayout.Height(25), GUILayout.Width(60)))
            { _activeDir = -1; ClearPreview(); }
            EditorGUILayout.EndHorizontal();

            // ── 활성 방향 진단 ──
            if (_activeDir >= 0)
            {
                EditorGUILayout.Space(8);
                DrawActiveDirectionPanel();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDirectionTable(Vector3 rotOffset, Vector3 flipScale)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Direction", EditorStyles.miniLabel, GUILayout.Width(110));
            GUILayout.Label("Char", EditorStyles.miniLabel, GUILayout.Width(50));
            GUILayout.Label("Final Euler", EditorStyles.miniLabel, GUILayout.Width(140));
            GUILayout.Label("VFX Scale", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label("Flip", EditorStyles.miniLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < Directions.Length; i++)
            {
                var dir = Directions[i];
                Vector3 worldDir = (_colDirection * dir.DirCol + _rowDirection * dir.DirRow).normalized;
                Quaternion lookRot = worldDir != Vector3.zero ? Quaternion.LookRotation(worldDir) : Quaternion.identity;
                Quaternion finalRot = lookRot * Quaternion.Euler(rotOffset);
                Vector3 euler = finalRot.eulerAngles;
                bool flipped = dir.DirCol > 0 || dir.DirRow < 0;
                Vector3 scale = flipped ? flipScale : Vector3.one;

                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = flipped ? new Color(1f, 0.6f, 0.3f) : new Color(0.5f, 0.9f, 1f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(dir.Label, GUILayout.Width(110));
                GUILayout.Label(dir.IsFront ? "Front" : "Back", GUILayout.Width(50));
                GUILayout.Label($"({euler.x:F0}, {euler.y:F0}, {euler.z:F0})", style, GUILayout.Width(140));
                GUILayout.Label($"({scale.x:F0}, {scale.y:F0}, {scale.z:F0})", style, GUILayout.Width(100));
                GUILayout.Label(flipped ? "Y" : "", GUILayout.Width(30));
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── Active Direction Panel ──

        private void DrawActiveDirectionPanel()
        {
            var dir = Directions[_activeDir];
            bool isFlipped = dir.DirCol > 0 || dir.DirRow < 0;
            string groupName = isFlipped ? "Flip 그룹 (Back/Right)" : "Non-Flip 그룹 (Front/Left)";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"현재: {dir.Label}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{groupName}  |  flipScale {(isFlipped ? "적용됨" : "미적용 (Vector3.one)")}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // OK / NG 판정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("이 방향 결과:", GUILayout.Width(85));

            var prevBg = GUI.backgroundColor;

            GUI.backgroundColor = _dirResults[_activeDir] == DirResult.OK ? Color.green : Color.white;
            if (GUILayout.Button("OK", GUILayout.Width(50), GUILayout.Height(24)))
            {
                _dirResults[_activeDir] = DirResult.OK;
                AdvanceToNextDirection();
            }

            GUI.backgroundColor = _dirResults[_activeDir] == DirResult.Wrong ? new Color(1f, 0.4f, 0.4f) : Color.white;
            if (GUILayout.Button("NG", GUILayout.Width(50), GUILayout.Height(24)))
                _dirResults[_activeDir] = DirResult.Wrong;

            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();

            // NG일 때 맥락적 가이드
            if (_dirResults[_activeDir] == DirResult.Wrong)
            {
                EditorGUILayout.Space(4);
                if (!isFlipped)
                {
                    EditorGUILayout.HelpBox(
                        "Non-Flip 방향이 틀림 → rotationOffset을 조정하세요.\n" +
                        "위의 X±90 / Y±90 / Z±90 버튼으로 빠르게 시도할 수 있습니다.\n" +
                        "맞는 값을 찾으면 OK를 누르세요.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Flip 방향이 틀림 → flipScale을 조정하세요.\n" +
                        "위의 X flip / Y flip / Z flip 버튼으로 축을 반전해보세요.\n" +
                        "맞는 값을 찾으면 OK를 누르세요.",
                        MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();

            // 전체 진단 요약
            DrawDiagnosisSummary();
        }

        private void AdvanceToNextDirection()
        {
            // OK 누르면 다음 미확인 방향으로 자동 이동
            int[] order = { 1, 0, 3, 2 }; // Front → Back → Right → Left (non-flip 먼저)
            foreach (int idx in order)
            {
                if (_dirResults[idx] == DirResult.NotTested)
                {
                    ShowDirection(idx);
                    return;
                }
            }
        }

        private void DrawDiagnosisSummary()
        {
            // 테스트된 방향이 있을 때만 요약 표시
            bool anyTested = false;
            for (int i = 0; i < 4; i++)
                if (_dirResults[i] != DirResult.NotTested) anyTested = true;
            if (!anyTested) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("진단 요약", EditorStyles.boldLabel);

            for (int i = 0; i < 4; i++)
            {
                var d = Directions[i];
                string status = _dirResults[i] switch
                {
                    DirResult.OK => "<color=green>OK</color>",
                    DirResult.Wrong => "<color=red>NG</color>",
                    _ => "<color=grey>-</color>"
                };
                var style = new GUIStyle(EditorStyles.label) { richText = true };
                EditorGUILayout.LabelField($"  {d.Label}: {status}", style);
            }

            // 전부 OK면 저장 안내
            bool allTestedOk = true;
            bool anyWrong = false;
            for (int i = 0; i < 4; i++)
            {
                if (_dirResults[i] == DirResult.Wrong) anyWrong = true;
                if (_dirResults[i] != DirResult.OK) allTestedOk = false;
            }

            EditorGUILayout.Space(2);
            if (allTestedOk)
            {
                EditorGUILayout.HelpBox(
                    $"전 방향 정상! 현재 값:\n" +
                    $"rotationOffset: ({_editRotOffset.x:F0}, {_editRotOffset.y:F0}, {_editRotOffset.z:F0})\n" +
                    $"flipScale: ({_editFlipScale.x:F0}, {_editFlipScale.y:F0}, {_editFlipScale.z:F0})",
                    MessageType.Info);

                if (GUILayout.Button("Save to Config", GUILayout.Height(28)))
                {
                    var skillVfxList = _config.SkillEffectPrefabs;
                    if (skillVfxList != null && _selectedVfxIndex < skillVfxList.Length)
                        SaveToConfig(skillVfxList[_selectedVfxIndex]);
                }
            }
            else if (anyWrong)
            {
                // 패턴 분석
                bool flipOk = (_dirResults[0] == DirResult.OK || _dirResults[0] == DirResult.NotTested) &&
                              (_dirResults[3] == DirResult.OK || _dirResults[3] == DirResult.NotTested);
                bool nonFlipOk = (_dirResults[1] == DirResult.OK || _dirResults[1] == DirResult.NotTested) &&
                                 (_dirResults[2] == DirResult.OK || _dirResults[2] == DirResult.NotTested);
                bool flipWrong = _dirResults[0] == DirResult.Wrong || _dirResults[3] == DirResult.Wrong;
                bool nonFlipWrong = _dirResults[1] == DirResult.Wrong || _dirResults[2] == DirResult.Wrong;

                if (nonFlipWrong && !flipWrong)
                    EditorGUILayout.HelpBox("Front/Left(non-flip)가 틀림 → rotationOffset ±90° 조정 필요", MessageType.Warning);
                else if (flipWrong && !nonFlipWrong)
                    EditorGUILayout.HelpBox("Back/Right(flip)가 틀림 → flipScale 축 반전 필요", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("양쪽 그룹 모두 틀림 → Front부터 rotationOffset 조정 후 flipScale 보정", MessageType.Warning);
            }

            // 리셋
            if (GUILayout.Button("Reset Results", GUILayout.Width(100)))
            {
                for (int i = 0; i < 4; i++) _dirResults[i] = DirResult.NotTested;
            }

            EditorGUILayout.EndVertical();
        }

        // ── Save ──

        private void SaveToConfig(SkillViewData data)
        {
            var so = new SerializedObject(_config);
            var arr = so.FindProperty("_skillEffectPrefabs");
            var elem = arr.GetArrayElementAtIndex(_selectedVfxIndex);

            elem.FindPropertyRelative("useCustomRotation").boolValue = _editUseCustom;
            elem.FindPropertyRelative("rotationOffset").vector3Value = _editRotOffset;
            elem.FindPropertyRelative("flipScale").vector3Value = _editFlipScale;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[VFX Preview] Saved to {_config.name}: " +
                      $"rotOffset=({_editRotOffset.x},{_editRotOffset.y},{_editRotOffset.z}), " +
                      $"flipScale=({_editFlipScale.x},{_editFlipScale.y},{_editFlipScale.z})");
        }

        // ── Preview ──

        private void ShowDirection(int dirIndex)
        {
            ClearPreview();
            _activeDir = dirIndex;
            var parent = GetOrCreateRoot(Directions[dirIndex].Label);
            SpawnDirectionPreview(dirIndex, Vector3.zero, parent);
            SceneView.RepaintAll();
        }

        private void ShowAllDirections()
        {
            ClearPreview();
            _activeDir = -1;
            float spacing = 4f;
            var parent = GetOrCreateRoot("All");
            for (int d = 0; d < Directions.Length; d++)
                SpawnDirectionPreview(d, new Vector3((d - 1.5f) * spacing, 0, 0), parent);
            SceneView.RepaintAll();
        }

        private Transform GetOrCreateRoot(string suffix)
        {
            if (_parentTransform != null)
            {
                // 기존 미리보기 자식 정리
                var existing = _parentTransform.Find("[VFX Preview]");
                if (existing != null) DestroyImmediate(existing.gameObject);

                var container = new GameObject("[VFX Preview]");
                container.transform.SetParent(_parentTransform, false);
                container.hideFlags = HideFlags.DontSave;
                _previewObjects.Add(container);
                Selection.activeGameObject = container;
                return container.transform;
            }

            var root = new GameObject($"[VFX Preview] {_config.name} - {suffix}");
            root.hideFlags = HideFlags.DontSave;
            _previewObjects.Add(root);
            Selection.activeGameObject = root;
            return root.transform;
        }

        private void SpawnDirectionPreview(int dirIndex, Vector3 offset, Transform root)
        {
            if (_config == null || _characterPrefab == null) return;
            var skillVfxList = _config.SkillEffectPrefabs;
            if (skillVfxList == null || _selectedVfxIndex >= skillVfxList.Length) return;
            var data = skillVfxList[_selectedVfxIndex];
            if (data?.Prefab == null) return;

            var dir = Directions[dirIndex];

            Vector3 pos;
            if (_parentTransform != null)
                pos = _parentTransform.position + offset;
            else if (SceneView.lastActiveSceneView != null)
                pos = SceneView.lastActiveSceneView.pivot + offset;
            else
                pos = offset;

            // 라벨
            var labelGo = new GameObject($"[{dir.Label}] {(dir.IsFront ? "Front" : "Back")} flip={dir.FlipX}");
            labelGo.transform.SetParent(root);
            labelGo.transform.position = pos + Vector3.up * 2.5f;
            labelGo.hideFlags = HideFlags.DontSave;

            // 캐릭터
            var charGo = (GameObject)PrefabUtility.InstantiatePrefab(_characterPrefab);
            charGo.name = $"[Char] {dir.Label}";
            charGo.transform.SetParent(root);
            charGo.transform.position = pos;
            charGo.transform.rotation = Quaternion.identity;
            charGo.hideFlags = HideFlags.DontSave;

            Transform skillPosTransform = null;
            var charView = charGo.GetComponentInChildren<SpriteCharacterView>();
            if (charView != null)
            {
                ApplyCharacterDirection(charView, dir.FlipX, dir.IsFront);
                skillPosTransform = GetSkillPositionTransform(charView, data.Position);
            }

            // VFX rotation 계산 (런타임과 동일: BoardWorldHelper 기반 방향 벡터)
            Vector3 worldDir = GetDirectionVector(pos, dir.DirCol, dir.DirRow);
            Quaternion lookRot = worldDir != Vector3.zero ? Quaternion.LookRotation(worldDir) : Quaternion.identity;
            Quaternion finalRot = lookRot * Quaternion.Euler(_editRotOffset);
            bool flipped = dir.DirCol > 0 || dir.DirRow < 0;
            Vector3 vfxScale = flipped ? _editFlipScale : Vector3.one;

            // VFX 스폰 (런타임 SpawnSkillVfxDirectional과 동일한 순서)
            Vector3 spawnPos = skillPosTransform != null ? skillPosTransform.position : pos;
            var vfxGo = (GameObject)PrefabUtility.InstantiatePrefab(data.Prefab);
            vfxGo.name = $"[VFX {_selectedVfxIndex}] {dir.Label}";
            vfxGo.hideFlags = HideFlags.DontSave;
            vfxGo.transform.position = spawnPos;
            vfxGo.transform.rotation = Quaternion.identity;

            // parent 설정 (런타임과 동일)
            if (skillPosTransform != null && data.Followable)
                vfxGo.transform.SetParent(skillPosTransform);
            else
                vfxGo.transform.SetParent(root);

            // rotation/scale 설정 (parent 후에 적용 — 런타임과 동일 순서)
            if (worldDir != Vector3.zero)
                vfxGo.transform.rotation = finalRot;

            // followable일 때 부모(SkillRoot)의 FlipX를 상쇄
            if (data.Followable && skillPosTransform != null && skillPosTransform.lossyScale.x < 0)
                vfxScale.x *= -1;

            vfxGo.transform.localScale = vfxScale;
        }

        // ── Character Direction ──

        private static void ApplyCharacterDirection(SpriteCharacterView charView, bool flipX, bool isFront)
        {
            var so = new SerializedObject(charView);
            Transform rootTr = so.FindProperty("_rootTransform")?.objectReferenceValue as Transform;
            Transform skillRootTr = so.FindProperty("_skillRootTransform")?.objectReferenceValue as Transform;

            if (rootTr != null)
            {
                Vector3 s = rootTr.localScale;
                s.x = flipX ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
                rootTr.localScale = s;
            }
            if (skillRootTr != null)
            {
                Vector3 s = skillRootTr.localScale;
                s.x = flipX ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
                skillRootTr.localScale = s;
            }

            var animator = charView.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool(IsFrontHash, isFront);
                animator.Update(0);
            }
        }

        private static Transform GetSkillPositionTransform(SpriteCharacterView charView, SkillPosition pos)
        {
            switch (pos)
            {
                case SkillPosition.SKILL_ROOT: return charView.SkillRootTransform;
                case SkillPosition.SKILL_TOP: return charView.SkillTopFXTransform;
                case SkillPosition.SKILL_MIDDLE: return charView.SkillMiddleFXTransform;
                case SkillPosition.SKILL_BOTTOM: return charView.SkillBottomFXTransform;
                default: return charView.transform;
            }
        }

        // ── Cleanup ──

        private void ClearPreview()
        {
            foreach (var go in _previewObjects)
                if (go != null) DestroyImmediate(go);
            _previewObjects.Clear();

            // parentTransform 하위 잔여물 정리
            if (_parentTransform != null)
            {
                var existing = _parentTransform.Find("[VFX Preview]");
                if (existing != null) DestroyImmediate(existing.gameObject);
            }

            while (true)
            {
                var leftover = GameObject.Find("[VFX Preview]");
                if (leftover != null) DestroyImmediate(leftover);
                else break;
            }
        }

        // ── Util ──

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle <= -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// 실제 씬의 타일 위치로 방향 벡터를 계산 (런타임 BoardWorldHelper와 동일 원리).
        /// casterPos 근처 타일을 찾아 인접 타일과의 차이로 방향 산출.
        /// fallback: 저장된 _colDirection/_rowDirection 사용.
        /// </summary>
        private Vector3 GetDirectionVector(Vector3 casterPos, int dirCol, int dirRow)
        {
            // 씬에 타일이 있으면 실제 좌표로 계산
            if (_autoDetected || TryAutoDetectTileDirections())
            {
                return (_colDirection * dirCol + _rowDirection * dirRow).normalized;
            }

            // fallback: 저장된 방향 벡터
            return (_colDirection * dirCol + _rowDirection * dirRow).normalized;
        }

        private bool TryAutoDetectTileDirections()
        {
            _autoDetected = false;
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform t00 = null, t10 = null, t01 = null;
            foreach (var t in all)
            {
                if (t.name == "Tile_0_0") t00 = t;
                else if (t.name == "Tile_1_0") t10 = t;
                else if (t.name == "Tile_0_1") t01 = t;
            }
            if (t00 != null && t10 != null) { _colDirection = (t10.position - t00.position).normalized; _autoDetected = true; }
            if (t00 != null && t01 != null) { _rowDirection = (t01.position - t00.position).normalized; _autoDetected = true; }
            Repaint();
            return _autoDetected;
        }
    }
}
