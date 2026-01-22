using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CookApps.AutoBattler.Editor
{
    public class BrokenPrefabFinder : EditorWindow
    {
        private enum IssueType
        {
            MissingScript,
            MissingReference,
            MissingPrefab
        }

        private struct PrefabIssue
        {
            public string PrefabPath;
            public string GameObjectPath;
            public IssueType Type;
            public string Details;
        }

        private Vector2 _scrollPosition;
        private List<PrefabIssue> _issues = new List<PrefabIssue>();
        private bool _isScanning;
        private string _searchFolder = "Assets";
        private bool _checkMissingScripts = true;
        private bool _checkMissingReferences = true;
        private bool _checkMissingPrefabs = true;
        private int _scannedCount;
        private int _totalCount;

        [MenuItem("Tools/Broken Prefab Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<BrokenPrefabFinder>("Broken Prefab Finder");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("맛간 프리팹 찾기", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 설정
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("검색 설정", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                _searchFolder = EditorGUILayout.TextField("검색 폴더", _searchFolder);
                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    string folder = EditorUtility.OpenFolderPanel("검색 폴더 선택", _searchFolder, "");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        if (folder.StartsWith(Application.dataPath))
                        {
                            _searchFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                _checkMissingScripts = EditorGUILayout.Toggle("Missing Script 검사", _checkMissingScripts);
                _checkMissingReferences = EditorGUILayout.Toggle("Missing Reference 검사", _checkMissingReferences);
                _checkMissingPrefabs = EditorGUILayout.Toggle("Missing Prefab 검사", _checkMissingPrefabs);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 스캔 버튼
            EditorGUI.BeginDisabledGroup(_isScanning);
            if (GUILayout.Button(_isScanning ? $"스캔 중... ({_scannedCount}/{_totalCount})" : "프리팹 스캔", GUILayout.Height(30)))
            {
                ScanPrefabs();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 결과
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"발견된 문제: {_issues.Count}개", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    for (int i = 0; i < _issues.Count; i++)
                    {
                        var issue = _issues[i];
                        DrawIssueItem(issue, i);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            // 하단 버튼
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("결과 복사"))
                {
                    CopyResultsToClipboard();
                }
                if (GUILayout.Button("결과 초기화"))
                {
                    _issues.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIssueItem(PrefabIssue issue, int index)
        {
            Color bgColor = issue.Type switch
            {
                IssueType.MissingScript => new Color(1f, 0.3f, 0.3f, 0.3f),
                IssueType.MissingReference => new Color(1f, 0.6f, 0.2f, 0.3f),
                IssueType.MissingPrefab => new Color(0.8f, 0.2f, 0.8f, 0.3f),
                _ => Color.gray
            };

            var oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUI.backgroundColor = oldBgColor;

                EditorGUILayout.BeginHorizontal();
                {
                    string typeLabel = issue.Type switch
                    {
                        IssueType.MissingScript => "[Script]",
                        IssueType.MissingReference => "[Ref]",
                        IssueType.MissingPrefab => "[Prefab]",
                        _ => "[?]"
                    };

                    EditorGUILayout.LabelField(typeLabel, GUILayout.Width(60));

                    if (GUILayout.Button(issue.PrefabPath, EditorStyles.linkLabel))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(issue.PrefabPath);
                        if (prefab != null)
                        {
                            EditorGUIUtility.PingObject(prefab);
                            Selection.activeObject = prefab;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"위치: {issue.GameObjectPath}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"상세: {issue.Details}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void ScanPrefabs()
        {
            _issues.Clear();
            _isScanning = true;

            try
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _searchFolder });
                _totalCount = prefabGuids.Length;
                _scannedCount = 0;

                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    _scannedCount = i + 1;
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar("프리팹 스캔", $"스캔 중: {path}", (float)i / prefabGuids.Length);
                    }

                    ScanPrefabAtPath(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
            }

            Debug.Log($"[BrokenPrefabFinder] 스캔 완료: {_totalCount}개 프리팹 중 {_issues.Count}개 문제 발견");
        }

        private void ScanPrefabAtPath(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            // Missing Script 검사
            if (_checkMissingScripts)
            {
                CheckMissingScripts(prefab, path, "");
            }

            // Missing Reference 검사
            if (_checkMissingReferences)
            {
                CheckMissingReferences(prefab, path, "");
            }

            // Missing Prefab 검사
            if (_checkMissingPrefabs)
            {
                CheckMissingPrefabs(prefab, path, "");
            }
        }

        private void CheckMissingScripts(GameObject go, string prefabPath, string objectPath)
        {
            string currentPath = string.IsNullOrEmpty(objectPath) ? go.name : $"{objectPath}/{go.name}";

            Component[] components = go.GetComponents<Component>();
            int missingCount = 0;

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                }
            }

            if (missingCount > 0)
            {
                _issues.Add(new PrefabIssue
                {
                    PrefabPath = prefabPath,
                    GameObjectPath = currentPath,
                    Type = IssueType.MissingScript,
                    Details = $"{missingCount}개의 Missing Script"
                });
            }

            // 자식 검사
            for (int i = 0; i < go.transform.childCount; i++)
            {
                CheckMissingScripts(go.transform.GetChild(i).gameObject, prefabPath, currentPath);
            }
        }

        private void CheckMissingReferences(GameObject go, string prefabPath, string objectPath)
        {
            string currentPath = string.IsNullOrEmpty(objectPath) ? go.name : $"{objectPath}/{go.name}";

            Component[] components = go.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;

                SerializedObject so = new SerializedObject(components[i]);
                SerializedProperty sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // objectReferenceValue가 null이지만 instanceID가 0이 아니면 깨진 참조
                        if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                        {
                            _issues.Add(new PrefabIssue
                            {
                                PrefabPath = prefabPath,
                                GameObjectPath = currentPath,
                                Type = IssueType.MissingReference,
                                Details = $"{components[i].GetType().Name}.{sp.propertyPath}"
                            });
                        }
                    }
                }
            }

            // 자식 검사
            for (int i = 0; i < go.transform.childCount; i++)
            {
                CheckMissingReferences(go.transform.GetChild(i).gameObject, prefabPath, currentPath);
            }
        }

        private void CheckMissingPrefabs(GameObject go, string prefabPath, string objectPath)
        {
            string currentPath = string.IsNullOrEmpty(objectPath) ? go.name : $"{objectPath}/{go.name}";

            // 중첩 프리팹이 깨졌는지 확인
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                var status = PrefabUtility.GetPrefabInstanceStatus(go);
                if (status == PrefabInstanceStatus.MissingAsset)
                {
                    _issues.Add(new PrefabIssue
                    {
                        PrefabPath = prefabPath,
                        GameObjectPath = currentPath,
                        Type = IssueType.MissingPrefab,
                        Details = "중첩 프리팹 에셋 누락"
                    });
                }
            }

            // 자식 검사
            for (int i = 0; i < go.transform.childCount; i++)
            {
                CheckMissingPrefabs(go.transform.GetChild(i).gameObject, prefabPath, currentPath);
            }
        }

        private void CopyResultsToClipboard()
        {
            if (_issues.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "복사할 결과가 없습니다.", "확인");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Broken Prefab Report ({_issues.Count}개) ===");
            sb.AppendLine();

            for (int i = 0; i < _issues.Count; i++)
            {
                var issue = _issues[i];
                sb.AppendLine($"[{issue.Type}] {issue.PrefabPath}");
                sb.AppendLine($"  위치: {issue.GameObjectPath}");
                sb.AppendLine($"  상세: {issue.Details}");
                sb.AppendLine();
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[BrokenPrefabFinder] 결과가 클립보드에 복사되었습니다.");
        }
    }
}
