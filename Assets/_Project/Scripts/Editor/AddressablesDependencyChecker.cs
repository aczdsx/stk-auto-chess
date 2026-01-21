#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// BuiltIn 그룹에서 Remote 그룹의 에셋을 참조하는지 체크하는 에디터 도구
    /// </summary>
    public class AddressablesDependencyChecker : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<DependencyIssue> _issues = new List<DependencyIssue>();
        private bool _isChecking;

        private class DependencyIssue
        {
            public string SourceAssetPath;
            public string SourceGroupName;
            public string ReferencedAssetPath;
            public string ReferencedGroupName;
        }

        [MenuItem("Tools/Addressables/Check BuiltIn → Remote Dependencies")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressablesDependencyChecker>("Addressables Dependency Checker");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("BuiltIn → Remote 의존성 체크", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("BuiltIn(Local) 그룹의 에셋이 Remote 그룹의 에셋을 참조하면 문제가 발생할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledGroupScope(_isChecking))
            {
                if (GUILayout.Button("의존성 체크 실행", GUILayout.Height(30)))
                {
                    CheckDependencies();
                }
            }

            EditorGUILayout.Space(10);

            if (_issues.Count > 0)
            {
                EditorGUILayout.LabelField($"발견된 문제: {_issues.Count}개", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                for (int i = 0; i < _issues.Count; i++)
                {
                    var issue = _issues[i];
                    DrawIssueItem(issue, i);
                }

                EditorGUILayout.EndScrollView();
            }
            else if (!_isChecking && _issues.Count == 0)
            {
                EditorGUILayout.HelpBox("문제가 발견되지 않았습니다.", MessageType.None);
            }
        }

        private void DrawIssueItem(DependencyIssue issue, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"#{index + 1}", EditorStyles.miniBoldLabel);

            // Source (BuiltIn)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source (BuiltIn):", GUILayout.Width(120));
            EditorGUILayout.LabelField($"[{issue.SourceGroupName}]", GUILayout.Width(150));
            if (GUILayout.Button(issue.SourceAssetPath, EditorStyles.linkLabel))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(issue.SourceAssetPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Referenced (Remote)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("→ References (Remote):", GUILayout.Width(120));
            EditorGUILayout.LabelField($"[{issue.ReferencedGroupName}]", GUILayout.Width(150));
            if (GUILayout.Button(issue.ReferencedAssetPath, EditorStyles.linkLabel))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(issue.ReferencedAssetPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void CheckDependencies()
        {
            _isChecking = true;
            _issues.Clear();

            try
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    Debug.LogError("[DependencyChecker] Addressables Settings not found");
                    return;
                }

                // 그룹별 에셋 경로 수집
                var localAssets = new Dictionary<string, string>(); // path -> groupName
                var remoteAssets = new Dictionary<string, string>(); // path -> groupName

                foreach (var group in settings.groups)
                {
                    if (group == null || group.ReadOnly)
                        continue;

                    bool isRemote = IsRemoteGroup(group);

                    foreach (var entry in group.entries)
                    {
                        string assetPath = entry.AssetPath;

                        if (isRemote)
                        {
                            if (!remoteAssets.ContainsKey(assetPath))
                                remoteAssets[assetPath] = group.Name;
                        }
                        else
                        {
                            if (!localAssets.ContainsKey(assetPath))
                                localAssets[assetPath] = group.Name;
                        }
                    }
                }

                Debug.Log($"[DependencyChecker] Local assets: {localAssets.Count}, Remote assets: {remoteAssets.Count}");

                // Local 에셋의 의존성 체크
                int processed = 0;
                int total = localAssets.Count;

                foreach (var kvp in localAssets)
                {
                    string localPath = kvp.Key;
                    string localGroupName = kvp.Value;

                    EditorUtility.DisplayProgressBar(
                        "의존성 체크 중...",
                        $"{localPath}",
                        (float)processed / total);

                    // 해당 에셋의 모든 의존성 가져오기
                    string[] dependencies = AssetDatabase.GetDependencies(localPath, true);

                    foreach (string depPath in dependencies)
                    {
                        // 자기 자신은 스킵
                        if (depPath == localPath)
                            continue;

                        // 스크립트, 셰이더 등 제외
                        if (depPath.EndsWith(".cs") || depPath.EndsWith(".shader") || depPath.EndsWith(".cginc"))
                            continue;

                        // Remote 에셋을 참조하는지 체크
                        if (remoteAssets.TryGetValue(depPath, out string remoteGroupName))
                        {
                            _issues.Add(new DependencyIssue
                            {
                                SourceAssetPath = localPath,
                                SourceGroupName = localGroupName,
                                ReferencedAssetPath = depPath,
                                ReferencedGroupName = remoteGroupName
                            });
                        }
                    }

                    processed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isChecking = false;
                Repaint();
            }

            Debug.Log($"[DependencyChecker] Check complete. Found {_issues.Count} issues.");
        }

        /// <summary>
        /// 그룹이 Remote인지 판단
        /// </summary>
        private bool IsRemoteGroup(AddressableAssetGroup group)
        {
            // 그룹 이름으로 판단 (프로젝트 컨벤션에 맞게 수정)
            string groupName = group.Name.ToLower();

            if (groupName.Contains("remote"))
                return true;

            if (groupName.Contains("builtin") || groupName.Contains("local") || groupName.Contains("built-in"))
                return false;

            // BundledAssetGroupSchema의 BuildPath로 판단
            var schema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
            if (schema != null)
            {
                string buildPath = schema.BuildPath.GetValue(group.Settings);
                string loadPath = schema.LoadPath.GetValue(group.Settings);

                // Remote 경로 패턴 체크
                if (loadPath.Contains("http") || loadPath.Contains("[RemoteLoadPath]") ||
                    buildPath.Contains("[RemoteBuildPath]") || buildPath.Contains("Remote"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif