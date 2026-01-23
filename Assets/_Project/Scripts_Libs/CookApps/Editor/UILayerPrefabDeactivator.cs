#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.Editor
{
    /// <summary>
    /// UILayer 프리팹 저장 시 자동으로 비활성화하는 프로세서
    /// </summary>
    [InitializeOnLoad]
    public static class UILayerPrefabSaveProcessor
    {
        static UILayerPrefabSaveProcessor()
        {
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private static void OnPrefabSaving(GameObject prefabRoot)
        {
            var uiLayer = prefabRoot.GetComponent<UILayer>();
            if (uiLayer == null)
                return;

            if (prefabRoot.activeSelf)
            {
                prefabRoot.SetActive(false);
                Debug.Log($"[UILayerPrefabSaveProcessor] 자동 비활성화: {prefabRoot.name}");
            }
        }
    }

    /// <summary>
    /// UILayer를 상속받은 컴포넌트가 최상위에 있는 프리팹들을 비활성화 상태로 저장하는 에디터 툴
    /// </summary>
    public class UILayerPrefabDeactivator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameObject> activePrefabs = new List<GameObject>();
        private string searchFolder = "Assets/_Project";
        private bool searchCompleted = false;

        [MenuItem("Tools/UILayer Prefab Deactivator")]
        private static void Open()
        {
            GetWindow<UILayerPrefabDeactivator>("UILayer Prefab Deactivator");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UILayer 프리팹 비활성화 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "UILayer를 상속받은 컴포넌트가 최상위에 있는 프리팹 중 활성화된 프리팹을 찾아 비활성화합니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            searchFolder = EditorGUILayout.TextField("검색 폴더", searchFolder);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("활성화된 UILayer 프리팹 검색", GUILayout.Height(30)))
            {
                SearchActivePrefabs();
            }

            GUI.enabled = activePrefabs.Count > 0;
            if (GUILayout.Button("모두 비활성화 후 저장", GUILayout.Height(30)))
            {
                DeactivateAllPrefabs();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (searchCompleted)
            {
                if (activePrefabs.Count == 0)
                {
                    EditorGUILayout.HelpBox("활성화된 UILayer 프리팹이 없습니다.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField($"활성화된 UILayer 프리팹: {activePrefabs.Count}개", EditorStyles.boldLabel);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                    for (int i = 0; i < activePrefabs.Count; i++)
                    {
                        var prefab = activePrefabs[i];
                        if (prefab == null)
                            continue;

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

                        if (GUILayout.Button("비활성화", GUILayout.Width(80)))
                        {
                            DeactivatePrefab(prefab);
                            activePrefabs.RemoveAt(i);
                            i--;
                        }

                        if (GUILayout.Button("선택", GUILayout.Width(50)))
                        {
                            Selection.activeObject = prefab;
                            EditorGUIUtility.PingObject(prefab);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void SearchActivePrefabs()
        {
            activePrefabs.Clear();
            searchCompleted = false;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });
            int total = guids.Length;

            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (EditorUtility.DisplayCancelableProgressBar(
                    "UILayer 프리팹 검색 중...",
                    $"({i + 1}/{total}) {path}",
                    (float)(i + 1) / total))
                {
                    break;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                // 최상위 오브젝트에 UILayer 컴포넌트가 있고 활성화 상태인지 확인
                var uiLayer = prefab.GetComponent<UILayer>();
                if (uiLayer != null && prefab.activeSelf)
                {
                    activePrefabs.Add(prefab);
                }
            }

            EditorUtility.ClearProgressBar();
            searchCompleted = true;

            Debug.Log($"[UILayerPrefabDeactivator] 검색 완료. 활성화된 UILayer 프리팹: {activePrefabs.Count}개");
        }

        private void DeactivateAllPrefabs()
        {
            int count = activePrefabs.Count;

            for (int i = 0; i < count; i++)
            {
                var prefab = activePrefabs[i];
                if (prefab == null)
                    continue;

                EditorUtility.DisplayProgressBar(
                    "프리팹 비활성화 중...",
                    $"({i + 1}/{count}) {prefab.name}",
                    (float)(i + 1) / count);

                DeactivatePrefab(prefab);
            }

            EditorUtility.ClearProgressBar();
            activePrefabs.Clear();

            AssetDatabase.SaveAssets();
            Debug.Log($"[UILayerPrefabDeactivator] {count}개 프리팹 비활성화 완료");
        }

        private void DeactivatePrefab(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);

            // 프리팹 열어서 수정
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                editScope.prefabContentsRoot.SetActive(false);
            }

            Debug.Log($"[UILayerPrefabDeactivator] 비활성화: {path}");
        }
    }
}
#endif
