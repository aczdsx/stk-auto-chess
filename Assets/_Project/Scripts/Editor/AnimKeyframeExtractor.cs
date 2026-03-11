using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CookApps.AutoChess;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using CookApps.BattleSystem;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 여러 폴더를 지정 → 폴더별 partial class 파일로 분리 생성.
    /// 각 클립의 AnimationEventKey enum과 시간을 모두 기록.
    /// Key = MakeKey(characterId, isFront, clipType) — 순수 int 연산.
    /// </summary>
    public class AnimKeyframeExtractor : EditorWindow
    {
        private List<DefaultAsset> _targetFolders = new() { null };
        private bool _includeSkill = true;
        private string _lastLog = "";
        private Vector2 _scrollPos;

        private const string OutputDir = "Assets/_Project/Scripts/InGame_New/View/Unit/AnimKeyframeData";
        private const int ExecuteStart = (int)AnimationEventKey.ExecuteStart;
        private const int ExecuteEnd = (int)AnimationEventKey.ExecuteEnd;
        private const int VFXStart = (int)AnimationEventKey.VFXStart;
        private const int VFXEnd = (int)AnimationEventKey.VFXEnd;

        private static readonly Regex NumericPrefix = new(@"^(\d+)", RegexOptions.Compiled);

        [MenuItem("Tools/Anim Keyframe Extractor")]
        private static void ShowWindow()
        {
            var wnd = GetWindow<AnimKeyframeExtractor>("Anim Keyframe Extractor");
            wnd.minSize = new Vector2(450, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animation Keyframe Extractor", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Target Folders", EditorStyles.miniBoldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(150));
            for (int i = 0; i < _targetFolders.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetFolders[i] = (DefaultAsset)EditorGUILayout.ObjectField(
                        $"[{i}]", _targetFolders[i], typeof(DefaultAsset), false);

                    if (_targetFolders.Count > 1 && GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        _targetFolders.RemoveAt(i);
                        i--;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Add Folder", GUILayout.Height(20)))
            {
                _targetFolders.Add(null);
            }

            EditorGUILayout.Space(4);
            _includeSkill = EditorGUILayout.Toggle("Include SKL Clips", _includeSkill);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Extract & Save", GUILayout.Height(30)))
            {
                ExtractAndSave();
            }

            if (!string.IsNullOrEmpty(_lastLog))
            {
                EditorGUILayout.HelpBox(_lastLog, MessageType.Info);
            }
        }

        private struct ClipEntry
        {
            public string DisplayKey;       // "컨트롤러명/클립명" (코멘트용)
            public int CharacterId;         // 컨트롤러명에서 파싱한 숫자
            public bool IsFront;
            public AnimClipType ClipType;
            public float ClipLength;
            public float FrameRate;
            public List<EventEntry> Events;
        }

        private struct EventEntry
        {
            public AnimationEventKey EventKey;
            public float Time;
        }

        private void ExtractAndSave()
        {
            var validFolders = new List<(string path, string name)>();
            foreach (var folder in _targetFolders)
            {
                if (folder == null) continue;
                string path = AssetDatabase.GetAssetPath(folder);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;
                string name = folder.name;
                validFolders.Add((path, name));
            }

            if (validFolders.Count == 0)
            {
                _lastLog = "ERROR: 유효한 폴더가 하나도 없습니다.";
                Debug.LogWarning("[AnimKeyframeExtractor] 유효한 폴더 없음");
                Repaint();
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outDir = Path.Combine(projectRoot, OutputDir.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            int totalClips = 0;
            int totalFiles = 0;

            foreach (var (folderPath, folderName) in validFolders)
            {
                Debug.Log($"[AnimKeyframeExtractor] 스캔: {folderPath}");

                var clips = new List<ClipEntry>();
                ScanFolder(folderPath, clips);

                if (clips.Count == 0)
                {
                    Debug.LogWarning($"[AnimKeyframeExtractor] {folderPath}: 클립 없음, 스킵");
                    continue;
                }

                string code = GeneratePartialFile(folderName, clips);
                string fileName = $"AnimKeyframeData.{folderName}.cs";
                string fullPath = Path.Combine(outDir, fileName);

                File.WriteAllText(fullPath, code, Encoding.UTF8);
                Debug.Log($"[AnimKeyframeExtractor] 저장: {fileName} ({clips.Count}개 클립)");

                totalClips += clips.Count;
                totalFiles++;
            }

            AssetDatabase.Refresh();

            _lastLog = totalFiles > 0
                ? $"완료! {totalFiles}개 파일, {totalClips}개 클립 추출\n저장: {OutputDir}/"
                : "WARNING: 추출된 클립이 없습니다.";
            Debug.Log($"[AnimKeyframeExtractor] {_lastLog}");
            Repaint();
        }

        private void ScanFolder(string folderPath, List<ClipEntry> results)
        {
            // AnimatorController
            var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { folderPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if (controller == null) continue;

                string ctrlName = Path.GetFileNameWithoutExtension(path);
                foreach (var clip in controller.animationClips)
                    TryAddClip(clip, ctrlName, results);
            }

            // AnimatorOverrideController
            var overrideGuids = AssetDatabase.FindAssets("t:AnimatorOverrideController", new[] { folderPath });
            foreach (var guid in overrideGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
                if (controller == null) continue;

                string ctrlName = Path.GetFileNameWithoutExtension(path);
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                controller.GetOverrides(overrides);

                foreach (var pair in overrides)
                {
                    var clip = pair.Value ?? pair.Key;
                    if (clip != null) TryAddClip(clip, ctrlName, results);
                }
            }
        }

        private void TryAddClip(AnimationClip clip, string ctrlName, List<ClipEntry> results)
        {
            // 클립 종류 판별
            if (!TryParseClipType(clip.name, out bool isFront, out AnimClipType clipType))
                return;
            if (clipType is AnimClipType.SKL or AnimClipType.SKL2 && !_includeSkill)
                return;

            // 컨트롤러명에서 캐릭터 ID 파싱
            if (!TryParseCharacterId(ctrlName, out int characterId))
            {
                Debug.LogWarning($"[AnimKeyframeExtractor] 컨트롤러명에서 숫자를 찾을 수 없음: {ctrlName}");
                return;
            }

            string displayKey = $"{ctrlName}/{clip.name}";

            // 중복 방지
            if (results.Any(e => e.DisplayKey == displayKey)) return;

            var eventEntries = new List<EventEntry>();
            var animEvents = AnimationUtility.GetAnimationEvents(clip);

            foreach (var evt in animEvents)
            {
                int k = evt.intParameter;
                if ((k > ExecuteStart && k < ExecuteEnd) ||
                    (k >= VFXStart && k <= VFXEnd) ||
                    k == (int)AnimationEventKey.Start ||
                    k == (int)AnimationEventKey.End)
                {
                    eventEntries.Add(new EventEntry
                    {
                        EventKey = (AnimationEventKey)k,
                        Time = evt.time,
                    });
                }
            }

            results.Add(new ClipEntry
            {
                DisplayKey = displayKey,
                CharacterId = characterId,
                IsFront = isFront,
                ClipType = clipType,
                ClipLength = clip.length,
                FrameRate = clip.frameRate,
                Events = eventEntries,
            });
        }

        /// <summary>클립명에서 방향(Front/Back)과 타입(ATK/ATK2/SKL/SKL2) 파싱</summary>
        private static bool TryParseClipType(string clipName, out bool isFront, out AnimClipType clipType)
        {
            isFront = false;
            clipType = AnimClipType.ATK;

            if (clipName.StartsWith("Front_")) isFront = true;
            else if (clipName.StartsWith("Back_")) isFront = false;
            else return false;

            // 접두어 제거 후 타입 판별
            string suffix = clipName.Substring(clipName.IndexOf('_') + 1);

            if (suffix == "ATK") clipType = AnimClipType.ATK;
            else if (suffix == "ATK2") clipType = AnimClipType.ATK2;
            else if (suffix == "CRIT") clipType = AnimClipType.CRIT;
            else if (suffix == "SKL") clipType = AnimClipType.SKL;
            else if (suffix == "SKL2") clipType = AnimClipType.SKL2;
            else return false;

            return true;
        }

        /// <summary>컨트롤러명 앞부분에서 숫자 ID 파싱 (예: "15232101_AnimationController" → 15232101)</summary>
        private static bool TryParseCharacterId(string ctrlName, out int characterId)
        {
            characterId = 0;
            var match = NumericPrefix.Match(ctrlName);
            if (!match.Success) return false;
            return int.TryParse(match.Groups[1].Value, out characterId);
        }

        private static string GeneratePartialFile(string folderName, List<ClipEntry> clips)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated by AnimKeyframeExtractor — DO NOT EDIT");
            sb.AppendLine($"// Source folder: {folderName}");
            sb.AppendLine("using CookApps.BattleSystem;");
            sb.AppendLine();
            sb.AppendLine("namespace CookApps.AutoChess");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class AnimKeyframeData");
            sb.AppendLine("    {");

            // 폴더별 초기화 메서드
            string methodName = $"Register_{SanitizeIdentifier(folderName)}";
            sb.AppendLine($"        /// <summary>{folderName} 폴더의 키프레임 데이터 등록</summary>");
            sb.AppendLine($"        public static void {methodName}()");
            sb.AppendLine("        {");

            clips.Sort((a, b) => string.Compare(a.DisplayKey, b.DisplayKey, System.StringComparison.Ordinal));

            foreach (var clip in clips)
            {
                var execEvents = clip.Events
                    .Where(e => (int)e.EventKey > ExecuteStart && (int)e.EventKey < ExecuteEnd)
                    .ToList();

                float execTime = execEvents.Count > 0
                    ? execEvents[0].Time
                    : clip.ClipLength * 0.4f;

                // MakeKey를 에디터에서 계산해서 int 리터럴로 출력
                int key = AnimKeyframeData.MakeKey(clip.CharacterId, clip.IsFront, clip.ClipType);
                string dir = clip.IsFront ? "Front" : "Back";
                string comment = $" // {clip.CharacterId} {dir}_{clip.ClipType}";
                string fallback = execEvents.Count == 0 ? " (fallback)" : "";

                sb.AppendLine($"            ExecuteTimes[{key}] = {execTime:F4}f;{comment}{fallback}");
                sb.AppendLine($"            ClipLengths[{key}] = {clip.ClipLength:F4}f;");

                // 모든 이벤트 키 기록
                if (clip.Events.Count > 0)
                {
                    var evtList = string.Join(", ",
                        clip.Events.Select(e =>
                            $"(AnimationEventKey.{e.EventKey}, {e.Time:F4}f)"));
                    sb.AppendLine($"            ClipEvents[{key}] = new[] {{ {evtList} }};");
                }

                // 다타 공격: ATK 클립에서 Execute 이벤트가 2개 이상이면 히트 타이밍 배열 생성
                if ((clip.ClipType == AnimClipType.ATK || clip.ClipType == AnimClipType.ATK2 || clip.ClipType == AnimClipType.CRIT) && execEvents.Count >= 2)
                {
                    var hitTimeList = string.Join(", ", execEvents.Select(e => $"{e.Time:F4}f"));
                    sb.AppendLine($"            AttackHitTimes[{key}] = new[] {{ {hitTimeList} }};{comment} ({execEvents.Count}hits)");
                }

                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string SanitizeIdentifier(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
