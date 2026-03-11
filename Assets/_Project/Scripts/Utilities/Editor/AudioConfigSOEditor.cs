using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioConfigSO 커스텀 인스펙터.
/// AudioMixer 설정, 스캔 폴더 관리, BGM 폴더 스캔, 검증 경고, 초기화 기능을 제공한다.
/// </summary>
[CustomEditor(typeof(AudioConfigSO))]
public class AudioConfigSOEditor : Editor
{
    /// <summary>
    /// 인스펙터 레이아웃을 그린다.
    /// 순서: 스캔 폴더 설정 → 스캔 버튼 → 검증 경고 → 매핑 딕셔너리 → 초기화 버튼
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var so = (AudioConfigSO)target;

        // ── AudioMixer ──
        var mixerProp = serializedObject.FindProperty("_audioMixerRef");
        if (mixerProp != null)
            EditorGUILayout.PropertyField(mixerProp, new GUIContent("Audio Mixer"));

        GUILayout.Space(10);

        // ── 스캔 폴더 설정 ──
        EditorGUILayout.LabelField("스캔 폴더", EditorStyles.boldLabel);

        if (so.scanFolders != null)
        {
            for (int i = 0; i < so.scanFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(so.scanFolders[i], EditorStyles.miniLabel);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    Undo.RecordObject(so, "스캔 폴더 제거");
                    so.scanFolders.RemoveAt(i);
                    EditorUtility.SetDirty(so);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (GUILayout.Button("+ 폴더 추가"))
            AddScanFolder(so);

        GUILayout.Space(6);

        // ── 스캔 버튼 ──
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("BGM 폴더 스캔", GUILayout.Height(28)))
            ScanBGMFolders();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // ── 검증 경고 ──
        DrawValidation(so);

        // ── 매핑 딕셔너리 ──
        EditorGUILayout.LabelField($"매핑 데이터 ({(so.bgmDisplayNames != null ? so.bgmDisplayNames.Count : 0)}개)", EditorStyles.boldLabel);
        var prop = serializedObject.FindProperty("bgmDisplayNames");
        if (prop != null)
            EditorGUILayout.PropertyField(prop, GUIContent.none);

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        // ── 초기화 버튼 ──
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("전체 초기화"))
        {
            if (EditorUtility.DisplayDialog("BGM Display Name 초기화",
                "모든 매핑 데이터가 삭제됩니다. 계속하시겠습니까?", "초기화", "취소"))
            {
                Undo.RecordObject(so, "BGM 매핑 초기화");
                so.bgmDisplayNames.Clear();
                EditorUtility.SetDirty(so);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    /// <summary>
    /// bgmDisplayNames에서 null 항목과 중복 AudioClip을 검사하여 HelpBox로 표시한다.
    /// </summary>
    private void DrawValidation(AudioConfigSO so)
    {
        if (so.bgmDisplayNames == null)
            return;

        int nullCount = 0;
        var nameCount = new Dictionary<string, int>();

        foreach (var kvp in so.bgmDisplayNames)
        {
            if (kvp.Key == null)
            {
                nullCount++;
                continue;
            }

            var clipName = kvp.Key.name;
            if (nameCount.ContainsKey(clipName))
                nameCount[clipName]++;
            else
                nameCount[clipName] = 1;
        }

        if (nullCount > 0)
            EditorGUILayout.HelpBox($"비어있는 AudioClip 항목이 {nullCount}개 있습니다.", MessageType.Warning);

        var duplicateNames = new List<string>();
        foreach (var kvp in nameCount)
        {
            if (kvp.Value > 1)
                duplicateNames.Add(kvp.Key);
        }

        if (duplicateNames.Count > 0)
            EditorGUILayout.HelpBox($"중복 AudioClip: {string.Join(", ", duplicateNames)}", MessageType.Error);
    }

    /// <summary>
    /// 폴더 선택 다이얼로그를 열어 스캔 폴더 목록에 추가한다.
    /// 프로젝트 외부 경로는 무시한다.
    /// </summary>
    private void AddScanFolder(AudioConfigSO so)
    {
        var selected = EditorUtility.OpenFolderPanel("BGM 폴더 선택", "Assets/_Project", "");
        if (string.IsNullOrEmpty(selected))
            return;

        var dataPath = Application.dataPath;
        if (!selected.StartsWith(dataPath))
        {
            Debug.LogWarning("[AudioConfig] 프로젝트 내부 폴더만 선택할 수 있습니다.");
            return;
        }

        var relativePath = "Assets" + selected.Substring(dataPath.Length);
        so.scanFolders ??= new List<string>();

        if (so.scanFolders.Contains(relativePath))
            return;

        Undo.RecordObject(so, "스캔 폴더 추가");
        so.scanFolders.Add(relativePath);
        EditorUtility.SetDirty(so);
    }

    /// <summary>
    /// scanFolders의 모든 폴더에서 AudioClip을 검색하여 bgmDisplayNames에 추가한다.
    /// null/중복 항목 정리, 기본 표시명 생성, 이름순 정렬을 수행한다.
    /// </summary>
    private void ScanBGMFolders()
    {
        var so = (AudioConfigSO)target;
        Undo.RecordObject(so, "BGM 폴더 스캔");

        so.bgmDisplayNames ??= new();

        // 기존 항목에서 null/중복 정리
        var existingClipNames = new HashSet<string>();
        var validEntries = new List<KeyValuePair<AudioClip, string>>();
        int nullCount = 0;

        foreach (var kvp in so.bgmDisplayNames)
        {
            if (kvp.Key == null)
            {
                nullCount++;
                continue;
            }

            if (!existingClipNames.Contains(kvp.Key.name))
            {
                existingClipNames.Add(kvp.Key.name);
                validEntries.Add(kvp);
            }
        }

        if (nullCount > 0 || validEntries.Count != so.bgmDisplayNames.Count)
        {
            so.bgmDisplayNames.Clear();
            foreach (var kvp in validEntries)
                so.bgmDisplayNames.Add(kvp.Key, kvp.Value);
        }

        // 폴더 스캔
        int addedCount = 0;

        if (so.scanFolders == null || so.scanFolders.Count == 0)
        {
            Debug.LogWarning("[AudioConfig] 스캔 대상 폴더가 없습니다.");
            return;
        }

        foreach (var folder in so.scanFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[AudioConfig] 폴더를 찾을 수 없습니다: {folder}");
                continue;
            }

            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                if (clip == null || existingClipNames.Contains(clip.name))
                    continue;

                var defaultName = AudioConfigSO.FormatFallback(clip.name);
                so.bgmDisplayNames.Add(clip, defaultName);
                existingClipNames.Add(clip.name);
                addedCount++;
            }
        }

        // 이름순 정렬
        var sorted = new List<KeyValuePair<AudioClip, string>>();
        foreach (var kvp in so.bgmDisplayNames)
        {
            if (kvp.Key != null)
                sorted.Add(kvp);
        }
        sorted.Sort((a, b) => string.Compare(a.Key.name, b.Key.name, System.StringComparison.Ordinal));

        so.bgmDisplayNames.Clear();
        foreach (var kvp in sorted)
            so.bgmDisplayNames.Add(kvp.Key, kvp.Value);

        EditorUtility.SetDirty(so);
        Debug.Log($"[AudioConfig] 스캔 완료. 추가: {addedCount}개, null 제거: {nullCount}개, 총: {so.bgmDisplayNames.Count}개");
    }
}
