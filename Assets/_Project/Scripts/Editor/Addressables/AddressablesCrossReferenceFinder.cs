using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CookApps.AutoBattler.Editor
{
    public class AddressablesCrossReferenceFinder : EditorWindow
    {
        private struct CrossReference
        {
            public string SourceAssetPath;
            public string ReferencedAssetPath;
            public string SourceAssetType;
        }

        private const string DefaultBuiltInPath = "Assets/_Project/Addressables/BuiltIn";
        private const string DefaultRemotePath = "Assets/_Project/Addressables/Remote";

        private Vector2 _scrollPosition;
        private List<CrossReference> _references = new List<CrossReference>();
        private bool _isScanning;
        private string _builtInFolder = DefaultBuiltInPath;
        private string _remoteFolder = DefaultRemotePath;
        private int _scannedCount;
        private int _totalCount;

        // 필터링
        private bool _showPrefabs = true;
        private bool _showMaterials = true;
        private bool _showScriptableObjects = true;
        private bool _showScenes = true;
        private bool _showOthers = true;

        [MenuItem("Tools/Addressbles/Addressables Cross Reference Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressablesCrossReferenceFinder>("Cross Reference Finder");
            window.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("BuiltIn → Remote 참조 찾기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("BuiltIn 폴더의 에셋이 Remote 폴더의 에셋을 참조하면 Addressables 빌드 시 문제가 발생할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space(5);

            // 폴더 설정
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("폴더 설정", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                _builtInFolder = EditorGUILayout.TextField("BuiltIn 폴더", _builtInFolder);
                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    string folder = EditorUtility.OpenFolderPanel("BuiltIn 폴더 선택", _builtInFolder, "");
                    if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
                    {
                        _builtInFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _remoteFolder = EditorGUILayout.TextField("Remote 폴더", _remoteFolder);
                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    string folder = EditorUtility.OpenFolderPanel("Remote 폴더 선택", _remoteFolder, "");
                    if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
                    {
                        _remoteFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 필터 설정
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("표시 필터", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                _showPrefabs = GUILayout.Toggle(_showPrefabs, "Prefab", EditorStyles.miniButtonLeft);
                _showMaterials = GUILayout.Toggle(_showMaterials, "Material", EditorStyles.miniButtonMid);
                _showScriptableObjects = GUILayout.Toggle(_showScriptableObjects, "ScriptableObject", EditorStyles.miniButtonMid);
                _showScenes = GUILayout.Toggle(_showScenes, "Scene", EditorStyles.miniButtonMid);
                _showOthers = GUILayout.Toggle(_showOthers, "기타", EditorStyles.miniButtonRight);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 스캔 버튼
            EditorGUI.BeginDisabledGroup(_isScanning);
            if (GUILayout.Button(_isScanning ? $"스캔 중... ({_scannedCount}/{_totalCount})" : "스캔 시작", GUILayout.Height(30)))
            {
                ScanCrossReferences();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 결과
            int filteredCount = GetFilteredCount();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"발견된 참조: {filteredCount}개 (전체 {_references.Count}개)", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    for (int i = 0; i < _references.Count; i++)
                    {
                        var reference = _references[i];
                        if (!ShouldShowReference(reference)) continue;
                        DrawReferenceItem(reference);
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
                    _references.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool ShouldShowReference(CrossReference reference)
        {
            string type = reference.SourceAssetType;
            if (type == "Prefab") return _showPrefabs;
            if (type == "Material") return _showMaterials;
            if (type == "ScriptableObject") return _showScriptableObjects;
            if (type == "Scene") return _showScenes;
            return _showOthers;
        }

        private int GetFilteredCount()
        {
            int count = 0;
            for (int i = 0; i < _references.Count; i++)
            {
                if (ShouldShowReference(_references[i])) count++;
            }
            return count;
        }

        private void DrawReferenceItem(CrossReference reference)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Source 에셋
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"[{reference.SourceAssetType}]", GUILayout.Width(120));

                    var sourceAsset = AssetDatabase.LoadAssetAtPath<Object>(reference.SourceAssetPath);
                    if (GUILayout.Button(reference.SourceAssetPath, EditorStyles.linkLabel))
                    {
                        if (sourceAsset != null)
                        {
                            EditorGUIUtility.PingObject(sourceAsset);
                            Selection.activeObject = sourceAsset;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 참조하는 Remote 에셋
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("→ 참조:", GUILayout.Width(50));

                    var refAsset = AssetDatabase.LoadAssetAtPath<Object>(reference.ReferencedAssetPath);
                    if (GUILayout.Button(reference.ReferencedAssetPath, EditorStyles.linkLabel))
                    {
                        if (refAsset != null)
                        {
                            EditorGUIUtility.PingObject(refAsset);
                            Selection.activeObject = refAsset;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void ScanCrossReferences()
        {
            _references.Clear();
            _isScanning = true;

            try
            {
                // BuiltIn 폴더의 모든 에셋 찾기
                string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { _builtInFolder });
                _totalCount = allAssetGuids.Length;
                _scannedCount = 0;

                // Remote 폴더 경로 정규화
                string remotePathNormalized = _remoteFolder.Replace("\\", "/");
                if (!remotePathNormalized.EndsWith("/"))
                {
                    remotePathNormalized += "/";
                }

                for (int i = 0; i < allAssetGuids.Length; i++)
                {
                    _scannedCount = i + 1;
                    string assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[i]);

                    // 폴더는 스킵
                    if (AssetDatabase.IsValidFolder(assetPath)) continue;

                    if (i % 100 == 0)
                    {
                        EditorUtility.DisplayProgressBar("참조 스캔", $"스캔 중: {assetPath}", (float)i / allAssetGuids.Length);
                    }

                    // 에셋의 모든 의존성 가져오기
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

                    for (int j = 0; j < dependencies.Length; j++)
                    {
                        string depPath = dependencies[j];

                        // 자기 자신 제외
                        if (depPath == assetPath) continue;

                        // Remote 폴더에 있는 에셋인지 확인
                        if (depPath.StartsWith(remotePathNormalized) || depPath.StartsWith(_remoteFolder))
                        {
                            _references.Add(new CrossReference
                            {
                                SourceAssetPath = assetPath,
                                ReferencedAssetPath = depPath,
                                SourceAssetType = GetAssetType(assetPath)
                            });
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
            }

            Debug.Log($"[CrossReferenceFinder] 스캔 완료: {_totalCount}개 에셋 중 {_references.Count}개 참조 발견");
        }

        private string GetAssetType(string path)
        {
            if (path.EndsWith(".prefab")) return "Prefab";
            if (path.EndsWith(".mat")) return "Material";
            if (path.EndsWith(".unity")) return "Scene";
            if (path.EndsWith(".asset")) return "ScriptableObject";
            if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".tga")) return "Texture";
            if (path.EndsWith(".fbx") || path.EndsWith(".obj")) return "Model";
            if (path.EndsWith(".anim")) return "Animation";
            if (path.EndsWith(".controller")) return "Animator";
            if (path.EndsWith(".shader")) return "Shader";
            return "Other";
        }

        private void CopyResultsToClipboard()
        {
            if (_references.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "복사할 결과가 없습니다.", "확인");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== BuiltIn → Remote 참조 리포트 ({_references.Count}개) ===");
            sb.AppendLine($"BuiltIn: {_builtInFolder}");
            sb.AppendLine($"Remote: {_remoteFolder}");
            sb.AppendLine();

            for (int i = 0; i < _references.Count; i++)
            {
                var reference = _references[i];
                sb.AppendLine($"[{reference.SourceAssetType}] {reference.SourceAssetPath}");
                sb.AppendLine($"  → {reference.ReferencedAssetPath}");
                sb.AppendLine();
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[CrossReferenceFinder] 결과가 클립보드에 복사되었습니다.");
        }
    }
}
