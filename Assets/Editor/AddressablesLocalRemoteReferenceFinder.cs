#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace CookApps.AutoBattler.Editor
{
    public class AddressablesLocalRemoteReferenceFinder : EditorWindow
    {
        private enum GroupBuildType
        {
            Unknown,
            Local,
            Remote
        }

        private struct GroupInfo
        {
            public string GroupName;
            public GroupBuildType BuildType;
            public List<AddressableAssetEntry> Entries;
        }

        private struct CrossReference
        {
            public string SourceAssetPath;
            public string SourceGroupName;
            public string ReferencedAssetPath;
            public string ReferencedGroupName;
        }

        private Vector2 _scrollPosition;
        private Vector2 _groupScrollPosition;
        private List<CrossReference> _references = new List<CrossReference>();
        private List<GroupInfo> _groupInfos = new List<GroupInfo>();
        private Dictionary<string, GroupInfo> _assetToGroupMap = new Dictionary<string, GroupInfo>();
        private bool _isScanning;
        private int _scannedCount;
        private int _totalCount;
        private bool _showGroupInfo = true;

        // Profile Path IDs (from AddressableAssetSettings)
        private const string LocalBuildPathId = "e3886738edb4841efab4e6e501113a07";
        private const string RemoteBuildPathId = "93ddb3c7ece4b489db541c706b57288d";

        [MenuItem("Tools/Addressables Local→Remote Reference Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressablesLocalRemoteReferenceFinder>("Local→Remote Finder");
            window.minSize = new Vector2(700, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Addressables: Local → Remote 참조 찾기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Local 번들의 에셋이 Remote 번들의 에셋을 참조하면 문제가 발생합니다:\n" +
                "• Remote 에셋이 Local 번들에 중복 포함되어 번들 크기 증가\n" +
                "• 또는 빌드 시 의존성 오류 발생",
                MessageType.Warning);
            EditorGUILayout.Space(5);

            // Addressables 설정 확인
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Addressable Asset Settings를 찾을 수 없습니다.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField($"Active Profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}");
            EditorGUILayout.Space(5);

            // 스캔 버튼
            EditorGUI.BeginDisabledGroup(_isScanning);
            if (GUILayout.Button(_isScanning ? $"스캔 중... ({_scannedCount}/{_totalCount})" : "스캔 시작", GUILayout.Height(30)))
            {
                ScanAddressables(settings);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 그룹 정보 표시
            _showGroupInfo = EditorGUILayout.Foldout(_showGroupInfo, $"그룹 정보 ({_groupInfos.Count}개)");
            if (_showGroupInfo && _groupInfos.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                _groupScrollPosition = EditorGUILayout.BeginScrollView(_groupScrollPosition, GUILayout.MaxHeight(150));
                {
                    int localCount = 0;
                    int remoteCount = 0;

                    for (int i = 0; i < _groupInfos.Count; i++)
                    {
                        var info = _groupInfos[i];
                        string typeLabel = info.BuildType switch
                        {
                            GroupBuildType.Local => "[Local]",
                            GroupBuildType.Remote => "[Remote]",
                            _ => "[???]"
                        };

                        Color labelColor = info.BuildType switch
                        {
                            GroupBuildType.Local => new Color(0.4f, 0.8f, 0.4f),
                            GroupBuildType.Remote => new Color(0.4f, 0.6f, 1f),
                            _ => Color.gray
                        };

                        var style = new GUIStyle(EditorStyles.label) { normal = { textColor = labelColor } };
                        EditorGUILayout.LabelField($"{typeLabel} {info.GroupName} ({info.Entries.Count} entries)", style);

                        if (info.BuildType == GroupBuildType.Local) localCount++;
                        else if (info.BuildType == GroupBuildType.Remote) remoteCount++;
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Local: {localCount}개, Remote: {remoteCount}개", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // 결과
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"발견된 문제 참조: {_references.Count}개", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    for (int i = 0; i < _references.Count; i++)
                    {
                        DrawReferenceItem(_references[i]);
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
                    _groupInfos.Clear();
                    _assetToGroupMap.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReferenceItem(CrossReference reference)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Source (Local)
                EditorGUILayout.BeginHorizontal();
                {
                    var localStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.4f, 0.8f, 0.4f) } };
                    EditorGUILayout.LabelField("[Local]", localStyle, GUILayout.Width(50));
                    EditorGUILayout.LabelField(reference.SourceGroupName, GUILayout.Width(150));

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

                // Referenced (Remote)
                EditorGUILayout.BeginHorizontal();
                {
                    var remoteStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.4f, 0.6f, 1f) } };
                    EditorGUILayout.LabelField("  → [Remote]", remoteStyle, GUILayout.Width(75));
                    EditorGUILayout.LabelField(reference.ReferencedGroupName, GUILayout.Width(125));

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
            }
            EditorGUILayout.EndVertical();
        }

        private void ScanAddressables(AddressableAssetSettings settings)
        {
            _references.Clear();
            _groupInfos.Clear();
            _assetToGroupMap.Clear();
            _isScanning = true;

            try
            {
                // 1. 모든 그룹 분석하여 Local/Remote 분류
                AnalyzeGroups(settings);

                // 2. 에셋 → 그룹 매핑 구축
                BuildAssetToGroupMap();

                // 3. Local 그룹의 에셋들이 Remote 그룹의 에셋을 참조하는지 검사
                FindCrossReferences();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
            }

            Debug.Log($"[LocalRemoteFinder] 스캔 완료: {_references.Count}개 문제 참조 발견");
        }

        private void AnalyzeGroups(AddressableAssetSettings settings)
        {
            var groups = settings.groups;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null) continue;

                // ReadOnly 그룹(Built In Data 등) 스킵
                if (group.ReadOnly) continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null) continue;

                GroupBuildType buildType = GroupBuildType.Unknown;

                // BuildPath ID로 Local/Remote 구분
                var buildPathId = schema.BuildPath.Id;
                if (buildPathId == LocalBuildPathId)
                {
                    buildType = GroupBuildType.Local;
                }
                else if (buildPathId == RemoteBuildPathId)
                {
                    buildType = GroupBuildType.Remote;
                }
                else
                {
                    // ID로 판단이 안되면 경로 값으로 추측
                    string buildPathValue = settings.profileSettings.GetValueByName(settings.activeProfileId, "Local.BuildPath");
                    string currentBuildPath = schema.BuildPath.GetValue(settings);

                    if (currentBuildPath != null && buildPathValue != null)
                    {
                        if (currentBuildPath.Contains("Local") || currentBuildPath == buildPathValue)
                        {
                            buildType = GroupBuildType.Local;
                        }
                        else
                        {
                            buildType = GroupBuildType.Remote;
                        }
                    }
                }

                var entries = new List<AddressableAssetEntry>();
                group.GatherAllAssets(entries, true, true, true);

                _groupInfos.Add(new GroupInfo
                {
                    GroupName = group.Name,
                    BuildType = buildType,
                    Entries = entries
                });
            }
        }

        private void BuildAssetToGroupMap()
        {
            for (int i = 0; i < _groupInfos.Count; i++)
            {
                var groupInfo = _groupInfos[i];

                for (int j = 0; j < groupInfo.Entries.Count; j++)
                {
                    var entry = groupInfo.Entries[j];
                    if (entry == null) continue;

                    string assetPath = entry.AssetPath;
                    if (!string.IsNullOrEmpty(assetPath) && !_assetToGroupMap.ContainsKey(assetPath))
                    {
                        _assetToGroupMap[assetPath] = groupInfo;
                    }
                }
            }
        }

        private void FindCrossReferences()
        {
            // Local 그룹의 에셋들만 검사
            List<(string assetPath, GroupInfo groupInfo)> localAssets = new List<(string, GroupInfo)>();

            for (int i = 0; i < _groupInfos.Count; i++)
            {
                var groupInfo = _groupInfos[i];
                if (groupInfo.BuildType != GroupBuildType.Local) continue;

                for (int j = 0; j < groupInfo.Entries.Count; j++)
                {
                    var entry = groupInfo.Entries[j];
                    if (entry == null || entry.IsFolder) continue;

                    localAssets.Add((entry.AssetPath, groupInfo));
                }
            }

            _totalCount = localAssets.Count;
            _scannedCount = 0;

            for (int i = 0; i < localAssets.Count; i++)
            {
                _scannedCount = i + 1;
                var (assetPath, sourceGroupInfo) = localAssets[i];

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("참조 분석", $"분석 중: {assetPath}", (float)i / localAssets.Count);
                }

                // 이 에셋의 직접 의존성 가져오기
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

                for (int j = 0; j < dependencies.Length; j++)
                {
                    string depPath = dependencies[j];

                    // 자기 자신 제외
                    if (depPath == assetPath) continue;

                    // 의존성 에셋이 Remote 그룹에 있는지 확인
                    if (_assetToGroupMap.TryGetValue(depPath, out GroupInfo depGroupInfo))
                    {
                        if (depGroupInfo.BuildType == GroupBuildType.Remote)
                        {
                            _references.Add(new CrossReference
                            {
                                SourceAssetPath = assetPath,
                                SourceGroupName = sourceGroupInfo.GroupName,
                                ReferencedAssetPath = depPath,
                                ReferencedGroupName = depGroupInfo.GroupName
                            });
                        }
                    }
                }
            }
        }

        private void CopyResultsToClipboard()
        {
            if (_references.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "복사할 결과가 없습니다.", "확인");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Addressables Local → Remote 참조 리포트 ({_references.Count}개) ===");
            sb.AppendLine();

            // 그룹별로 정리
            var groupedRefs = new Dictionary<string, List<CrossReference>>();
            for (int i = 0; i < _references.Count; i++)
            {
                var reference = _references[i];
                if (!groupedRefs.ContainsKey(reference.SourceGroupName))
                {
                    groupedRefs[reference.SourceGroupName] = new List<CrossReference>();
                }
                groupedRefs[reference.SourceGroupName].Add(reference);
            }

            foreach (var kvp in groupedRefs)
            {
                sb.AppendLine($"## [Local] {kvp.Key} ({kvp.Value.Count}개 문제)");
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var reference = kvp.Value[i];
                    sb.AppendLine($"  {reference.SourceAssetPath}");
                    sb.AppendLine($"    → [Remote:{reference.ReferencedGroupName}] {reference.ReferencedAssetPath}");
                }
                sb.AppendLine();
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[LocalRemoteFinder] 결과가 클립보드에 복사되었습니다.");
        }
    }
}
#endif
