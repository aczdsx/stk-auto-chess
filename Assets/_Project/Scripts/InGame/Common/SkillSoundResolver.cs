using System.Collections.Generic;
using System.Text;
using ClockStone;
using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 스킬 사운드 이름을 해석하고 여러 버전의 사운드를 찾는 클래스 (최적화됨)
    /// 
    /// 사운드 네이밍 규칙:
    /// - 기본 형식: snd_sfx_skill_a_{characterId}_{version}
    /// - 예시: snd_sfx_skill_a_3401_01, snd_sfx_skill_a_3401_02
    /// - version은 01부터 시작하며, 최대 99까지 지원
    /// 
    /// 사용 예시:
    /// var resolver = new SkillSoundResolver(characterId);
    /// var soundNames = resolver.GetAvailableSoundNames();
    /// SoundManager.Instance.PlaySkillSounds(soundNames);
    /// </summary>
    public class SkillSoundResolver
    {
        private readonly int _characterId;
        private const int MAX_VERSION = 99;
        private const string SOUND_PREFIX = "snd_sfx_skill_a_";

        // 정적 캐시: 캐릭터 ID -> 사운드 이름 배열
        private static readonly Dictionary<int, string[]> _soundCache = new Dictionary<int, string[]>(64);
        private static readonly object _cacheLock = new object();

        // 미리 계산된 버전 문자열 (01~99)
        private static readonly string[] _versionStrings = new string[MAX_VERSION + 1];
        
        static SkillSoundResolver()
        {
            // 버전 문자열 미리 계산 (01, 02, ..., 99)
            for (int i = 0; i <= MAX_VERSION; i++)
            {
                _versionStrings[i] = i.ToString("D2");
            }
        }

        /// <summary>
        /// 스킬 사운드 리졸버 생성
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        public SkillSoundResolver(int characterId)
        {
            _characterId = characterId;
        }

        /// <summary>
        /// 사용 가능한 모든 스킬 사운드 이름을 반환합니다.
        /// 버전이 없는 기본 사운드와 버전이 있는 사운드(01, 02, ...)를 모두 찾습니다.
        /// 결과는 캐싱되어 재사용됩니다.
        /// </summary>
        /// <returns>사용 가능한 사운드 이름 리스트 (버전 순서대로 정렬됨)</returns>
        public List<string> GetAvailableSoundNames()
        {
            var cachedSounds = GetCachedSoundNames();
            if (cachedSounds == null || cachedSounds.Length == 0)
            {
                return new List<string>();
            }

            // 캐시된 배열을 리스트로 변환 (최소 할당)
            var result = new List<string>(cachedSounds.Length);
            for (int i = 0; i < cachedSounds.Length; i++)
            {
                result.Add(cachedSounds[i]);
            }
            return result;
        }

        /// <summary>
        /// 캐시된 사운드 이름 배열을 반환합니다. (최적화된 버전)
        /// </summary>
        /// <returns>사운드 이름 배열, 없으면 null</returns>
        public string[] GetCachedSoundNames()
        {
            // 캐시 확인
            lock (_cacheLock)
            {
                if (_soundCache.TryGetValue(_characterId, out var cached))
                {
                    return cached;
                }
            }

            // 캐시 미스: 새로 계산
            var soundNames = ComputeSoundNames();
            
            // 캐시에 저장
            lock (_cacheLock)
            {
                _soundCache[_characterId] = soundNames;
            }

            return soundNames;
        }

        /// <summary>
        /// 사운드 이름을 계산합니다. (string 할당 최소화)
        /// </summary>
        private string[] ComputeSoundNames()
        {
            // StringBuilder 재사용으로 string 할당 최소화
            var sb = new StringBuilder(SOUND_PREFIX.Length + 10); // 예상 크기
            sb.Append(SOUND_PREFIX);
            sb.Append(_characterId);
            string baseSoundName = sb.ToString();

            // 임시 리스트 (최대 크기 예상)
            var tempList = new List<string>(MAX_VERSION + 1);

            // 1. 기본 사운드 체크 (버전 없음)
            if (IsValidAudioID(baseSoundName))
            {
                tempList.Add(baseSoundName);
            }

            // 2. 버전이 있는 사운드 체크 (01, 02, ...)
            sb.Clear();
            sb.Append(baseSoundName);
            sb.Append('_');
            int underscoreIndex = sb.Length;

            for (int version = 1; version <= MAX_VERSION; version++)
            {
                // StringBuilder 재사용
                sb.Length = underscoreIndex;
                sb.Append(_versionStrings[version]);
                string soundName = sb.ToString();

                if (IsValidAudioID(soundName))
                {
                    tempList.Add(soundName);
                }
                else
                {
                    // 연속된 버전이 없으면 중단
                    // 단, 01이 없어도 02가 있을 수 있으므로 최소 3개까지 체크
                    if (version >= 3 && tempList.Count == 0)
                    {
                        break;
                    }
                }
            }

            // 배열로 변환 (불변성 보장)
            return tempList.Count > 0 ? tempList.ToArray() : null;
        }

        /// <summary>
        /// 첫 번째로 사용 가능한 사운드 이름을 반환합니다.
        /// 여러 버전이 있어도 첫 번째만 반환합니다.
        /// </summary>
        /// <returns>사운드 이름, 없으면 null</returns>
        public string GetFirstAvailableSoundName()
        {
            var cachedSounds = GetCachedSoundNames();
            return cachedSounds != null && cachedSounds.Length > 0 ? cachedSounds[0] : null;
        }

        /// <summary>
        /// AudioController를 통해 사운드 ID가 유효한지 확인합니다.
        /// </summary>
        private bool IsValidAudioID(string audioID)
        {
            return AudioController.IsValidAudioID(audioID);
        }

        /// <summary>
        /// 캐릭터 ID로부터 스킬 사운드 리졸버를 생성하는 헬퍼 메서드
        /// </summary>
        public static SkillSoundResolver Create(int characterId)
        {
            return new SkillSoundResolver(characterId);
        }

        /// <summary>
        /// 캐시를 초기화합니다. (테스트 또는 메모리 관리용)
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _soundCache.Clear();
            }
        }

        /// <summary>
        /// 특정 캐릭터의 캐시를 제거합니다.
        /// </summary>
        public static void RemoveCache(int characterId)
        {
            lock (_cacheLock)
            {
                _soundCache.Remove(characterId);
            }
        }
    }
}
