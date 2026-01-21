#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    public class DuplicateComponentCleaner : EditorWindow
    {
        private string componentTypeName = "LocalizeStringEvent";
        private Vector2 scrollPosition;
        private List<DuplicateInfo> duplicates = new List<DuplicateInfo>();

        private struct DuplicateInfo
        {
            public string prefabPath;
            public string objectPath;
            public string objectName;
            public int count;
            public bool isNestedPrefab;
        }

        [MenuItem("Tools/Duplicate Component Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<DuplicateComponentCleaner>("Duplicate Cleaner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Duplicate Component Cleaner", EditorStyles.boldLabel);
            GUILayout.Space(10);

            componentTypeName = EditorGUILayout.TextField("Component Type Name", componentTypeName);

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find in Scene", GUILayout.Height(30)))
            {
                FindDuplicatesInScene();
            }
            if (GUILayout.Button("Find in Prefabs", GUILayout.Height(30)))
            {
                FindDuplicatesInPrefabs();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (duplicates.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {duplicates.Count} objects with duplicates:", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                for (int i = 0; i < duplicates.Count; i++)
                {
                    var dup = duplicates[i];
                    EditorGUILayout.BeginHorizontal();

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dup.prefabPath);
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false, GUILayout.Width(180));

                    string label = dup.isNestedPrefab ? $"[Nested] {dup.objectName}" : dup.objectName;
                    EditorGUILayout.LabelField(label, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"x{dup.count}", GUILayout.Width(30));

                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        SelectObjectInPrefab(dup);
                    }
                    if (GUILayout.Button("Fix", GUILayout.Width(40)))
                    {
                        FixDuplicate(dup);
                        duplicates.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);

                // 통계 표시
                int nestedCount = duplicates.Count(d => d.isNestedPrefab);
                int normalCount = duplicates.Count - nestedCount;
                EditorGUILayout.LabelField($"Normal: {normalCount}, Nested Prefab: {nestedCount}");

                GUILayout.Space(5);

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Fix Normal Only (Safer)", GUILayout.Height(30)))
                {
                    FixNormalOnly();
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Fix All (Including Nested)", GUILayout.Height(30)))
                {
                    FixAllDuplicates();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("No duplicates found. Click 'Find' to search.", MessageType.Info);
            }
        }

        private void FindDuplicatesInScene()
        {
            duplicates.Clear();

            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                var components = FindComponentsByName(go);
                if (components.Length > 1)
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        prefabPath = "",
                        objectPath = GetGameObjectPath(go),
                        objectName = go.name,
                        count = components.Length,
                        isNestedPrefab = false
                    });
                }
            }

            Debug.Log($"[DuplicateComponentCleaner] Scene scan complete. Found {duplicates.Count} objects with duplicate {componentTypeName}");
        }

        private void FindDuplicatesInPrefabs()
        {
            duplicates.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            int processed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    CheckPrefabForDuplicates(prefab, path);
                }

                processed++;
                if (processed % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar("Scanning Prefabs",
                        $"Processing {processed}/{guids.Length}",
                        (float)processed / guids.Length);
                }
            }

            EditorUtility.ClearProgressBar();

            int nestedCount = duplicates.Count(d => d.isNestedPrefab);
            Debug.Log($"[DuplicateComponentCleaner] Prefab scan complete. Found {duplicates.Count} objects ({nestedCount} nested) with duplicate {componentTypeName}");
        }

        private void CheckPrefabForDuplicates(GameObject prefab, string prefabPath)
        {
            var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                var components = FindComponentsByName(t.gameObject);
                if (components.Length > 1)
                {
                    // 네스티드 프리팹인지 확인
                    bool isNested = false;

                    if (t.gameObject != prefab)
                    {
                        var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
                        if (nearestRoot != null && nearestRoot != prefab)
                        {
                            isNested = true;
                        }
                    }

                    duplicates.Add(new DuplicateInfo
                    {
                        prefabPath = prefabPath,
                        objectPath = GetRelativePath(t.gameObject, prefab),
                        objectName = t.gameObject.name,
                        count = components.Length,
                        isNestedPrefab = isNested
                    });
                }
            }
        }

        private Component[] FindComponentsByName(GameObject go)
        {
            var allComponents = go.GetComponents<Component>();
            var matched = new List<Component>();

            foreach (var comp in allComponents)
            {
                if (comp != null && comp.GetType().Name.Contains(componentTypeName))
                {
                    matched.Add(comp);
                }
            }

            return matched.ToArray();
        }

        private void SelectObjectInPrefab(DuplicateInfo info)
        {
            if (string.IsNullOrEmpty(info.prefabPath))
            {
                // 씬 오브젝트인 경우
                var go = GameObject.Find(info.objectPath);
                if (go != null)
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                return;
            }

            // 프리팹 에셋 로드
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.prefabPath);
            if (prefab == null) return;

            // 프리팹 모드로 열기
            AssetDatabase.OpenAsset(prefab);

            // 프리팹 스테이지가 열릴 때까지 대기 후 오브젝트 선택
            EditorApplication.delayCall += () =>
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    GameObject targetObject = null;

                    // 경로로 먼저 찾기
                    if (!string.IsNullOrEmpty(info.objectPath))
                    {
                        var found = stage.prefabContentsRoot.transform.Find(info.objectPath);
                        if (found != null)
                        {
                            targetObject = found.gameObject;
                        }
                    }

                    // 경로로 못 찾으면 이름으로 검색
                    if (targetObject == null)
                    {
                        var allTransforms = stage.prefabContentsRoot.GetComponentsInChildren<Transform>(true);
                        foreach (var t in allTransforms)
                        {
                            if (t.gameObject.name == info.objectName)
                            {
                                var comps = FindComponentsByName(t.gameObject);
                                if (comps.Length > 1)
                                {
                                    targetObject = t.gameObject;
                                    break;
                                }
                            }
                        }
                    }

                    if (targetObject != null)
                    {
                        Selection.activeGameObject = targetObject;
                        EditorGUIUtility.PingObject(targetObject);

                        // 모든 컴포넌트 접고, 타겟 컴포넌트만 펼치기
                        CollapseAllExpandTarget(targetObject);
                    }
                }
            };
        }

        private void CollapseAllExpandTarget(GameObject go)
        {
            var allComponents = go.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                // 타겟 컴포넌트 타입이면 펼치기, 아니면 접기
                bool isTarget = comp.GetType().Name.Contains(componentTypeName);
                InternalEditorUtility.SetIsInspectorExpanded(comp, isTarget);
            }
        }

        private void FixDuplicate(DuplicateInfo info)
        {
            if (string.IsNullOrEmpty(info.prefabPath))
            {
                var go = GameObject.Find(info.objectPath);
                if (go != null)
                {
                    RemoveDuplicateComponents(go);
                }
                return;
            }

            // 항상 부모 프리팹을 수정 (네스티드 프리팹의 경우에도 부모에서 오버라이드 제거)
            string targetPrefabPath = info.prefabPath;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(targetPrefabPath);
            try
            {
                GameObject targetObject = FindObjectByPath(prefabRoot, info.objectPath);

                // 경로로 못 찾으면 이름으로 재검색
                if (targetObject == null)
                {
                    targetObject = FindObjectByName(prefabRoot, info.objectName);
                }

                if (targetObject != null)
                {
                    int removed = RemoveDuplicateComponents(targetObject);
                    if (removed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, targetPrefabPath);
                        Debug.Log($"[DuplicateComponentCleaner] Fixed: {targetPrefabPath} / {info.objectName} (removed {removed})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DuplicateComponentCleaner] Object not found: {info.objectName} in {targetPrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private GameObject FindObjectByName(GameObject root, string name)
        {
            if (root.name == name)
            {
                var comps = FindComponentsByName(root);
                if (comps.Length > 1) return root;
            }

            var allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.gameObject.name == name)
                {
                    var comps = FindComponentsByName(t.gameObject);
                    if (comps.Length > 1) return t.gameObject;
                }
            }
            return null;
        }

        private int RemoveDuplicateComponents(GameObject go)
        {
            var components = FindComponentsByName(go);
            if (components.Length <= 1) return 0;

            int removed = 0;
            for (int i = components.Length - 1; i >= 1; i--)
            {
                Object.DestroyImmediate(components[i], true);
                removed++;
            }
            return removed;
        }

        private void FixNormalOnly()
        {
            var normalDuplicates = duplicates.Where(d => !d.isNestedPrefab).ToList();

            if (!EditorUtility.DisplayDialog("Confirm",
                $"Remove duplicate {componentTypeName} from {normalDuplicates.Count} normal objects?\n(Nested prefabs will be skipped)",
                "Yes", "Cancel"))
            {
                return;
            }

            FixDuplicatesList(normalDuplicates);

            // 남은 네스티드만 유지
            duplicates = duplicates.Where(d => d.isNestedPrefab).ToList();
        }

        private void FixAllDuplicates()
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                $"Remove duplicate {componentTypeName} from {duplicates.Count} objects?\n(Including nested prefabs - may modify multiple prefab assets)",
                "Yes", "Cancel"))
            {
                return;
            }

            FixDuplicatesList(duplicates.ToList());
            duplicates.Clear();
        }

        private void FixDuplicatesList(List<DuplicateInfo> list)
        {
            // 프리팹별로 그룹화 (항상 부모 프리팹 기준 - 네스티드도 부모에서 수정)
            var groupedByPrefab = list
                .Where(d => !string.IsNullOrEmpty(d.prefabPath))
                .GroupBy(d => d.prefabPath)
                .ToList();

            int fixedCount = 0;
            int totalGroups = groupedByPrefab.Count;
            int currentGroup = 0;

            foreach (var group in groupedByPrefab)
            {
                currentGroup++;
                EditorUtility.DisplayProgressBar("Fixing Duplicates",
                    $"Processing {currentGroup}/{totalGroups}: {System.IO.Path.GetFileName(group.Key)}",
                    (float)currentGroup / totalGroups);

                string prefabPath = group.Key;
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    bool modified = false;
                    foreach (var info in group)
                    {
                        // 경로로 먼저 찾고, 못 찾으면 이름으로 재검색
                        GameObject targetObject = FindObjectByPath(prefabRoot, info.objectPath);
                        if (targetObject == null)
                        {
                            targetObject = FindObjectByName(prefabRoot, info.objectName);
                        }

                        if (targetObject != null)
                        {
                            int removed = RemoveDuplicateComponents(targetObject);
                            if (removed > 0)
                            {
                                fixedCount++;
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                        Debug.Log($"[DuplicateComponentCleaner] Fixed prefab: {prefabPath}");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            EditorUtility.ClearProgressBar();

            // 씬 오브젝트 처리
            var sceneObjects = list.Where(d => string.IsNullOrEmpty(d.prefabPath)).ToList();
            foreach (var info in sceneObjects)
            {
                var go = GameObject.Find(info.objectPath);
                if (go != null)
                {
                    Undo.RecordObject(go, "Remove Duplicate Components");
                    int removed = RemoveDuplicateComponents(go);
                    if (removed > 0) fixedCount++;
                }
            }

            Debug.Log($"[DuplicateComponentCleaner] Fixed {fixedCount} objects");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private GameObject FindObjectByPath(GameObject root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            Transform found = root.transform.Find(path);
            return found != null ? found.gameObject : null;
        }

        private string GetRelativePath(GameObject go, GameObject root)
        {
            if (go == root)
                return "";

            var path = new List<string>();
            Transform current = go.transform;
            Transform rootTransform = root.transform;

            while (current != null && current != rootTransform)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
#endif