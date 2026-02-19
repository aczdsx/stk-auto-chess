using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// BGM AudioClip별 표시명을 관리하는 ScriptableObject.
    /// SoDataProvider를 통해 로드되며, LobbyBGMPlayer에서 참조한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BGMDisplayNameData", menuName = "ScriptableObjects/BGMDisplayNameData")]
    public class BGMDisplayNameSO : ScriptableObject
    {
        [HideInInspector]
        public List<string> scanFolders = new()
        {
            "Assets/_Project/Addressables/BuiltIn/Sounds/BGM",
            "Assets/_Project/Addressables/Remote/Sounds/BGM",
        };

        [SerializedDictionary("Audio Clip", "Display Name")]
        public SerializedDictionary<AudioClip, string> bgmDisplayNames;

        private Dictionary<string, string> _runtimeCache;

        /// <summary>
        /// audioID에 대응하는 표시명을 반환한다. 매핑이 없으면 접두사를 제거한 폴백 이름을 반환한다.
        /// </summary>
        public string GetDisplayName(string audioID)
        {
            if (string.IsNullOrEmpty(audioID))
                return string.Empty;

            _runtimeCache ??= BuildRuntimeCache();

            if (_runtimeCache.TryGetValue(audioID, out var displayName)
                && !string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }

            return FormatFallback(audioID);
        }

        /// <summary>
        /// bgmDisplayNames 딕셔너리로부터 clip.name → displayName 런타임 캐시를 생성한다.
        /// </summary>
        private Dictionary<string, string> BuildRuntimeCache()
        {
            var cache = new Dictionary<string, string>();
            foreach (var kvp in bgmDisplayNames)
            {
                if (kvp.Key != null)
                    cache[kvp.Key.name] = kvp.Value;
            }
            return cache;
        }

        /// <summary>
        /// audioID에서 "snd_bgm_" 또는 "bgm_" 접두사를 제거하고 언더스코어를 공백으로 치환한 폴백 이름을 반환한다.
        /// </summary>
        public static string FormatFallback(string audioID)
        {
            var formatted = audioID;
            if (formatted.StartsWith("snd_bgm_", System.StringComparison.OrdinalIgnoreCase))
                formatted = formatted.Substring(8);
            else if (formatted.StartsWith("bgm_", System.StringComparison.OrdinalIgnoreCase))
                formatted = formatted.Substring(4);

            return formatted.Replace('_', ' ');
        }
    }
}
