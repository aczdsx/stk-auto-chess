
using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;
using UnityEngine.Audio;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Naninovel의 AudioManager를 대체하여 SoundManager를 통해 오디오를 재생합니다.
    /// @bgm "snd_bgm_lobby", @sfx "snd_sfx_ui_btn_confirm", @voice "voice_001" 등의 명령어가
    /// SoundManager를 통해 재생됩니다.
    /// </summary>
    [InitializeAtRuntime(@override: typeof(AudioManager))]
    public class NaninovelCustomAudioManager : IStatefulService<SettingsStateMap>, IStatefulService<GameStateMap>, IAudioManager
    {
        [Serializable]
        public class Settings
        {
            public float MasterVolume = 1f;
            public float BgmVolume = 1f;
            public float SfxVolume = 1f;
            public float VoiceVolume = 1f;
            public string VoiceLocale;
            public List<NamedFloat> AuthorVolume = new();
        }

        [Serializable]
        public class GameState
        {
            public List<AudioClipState> BgmClips = new();
            public List<AudioClipState> SfxClips = new();
        }

        public Naninovel.AudioConfiguration Configuration { get; }
        public AudioMixer AudioMixer => null; // SoundManager가 AudioMixer를 관리

        public float MasterVolume { get; set; } = 1f;

        public float BgmVolume
        {
            get => SoundManager.Instance?.BGMVolume ?? 1f;
            set => SoundManager.Instance?.SetBGMVolume(value);
        }

        public float SfxVolume
        {
            get => SoundManager.Instance?.SFXVolume ?? 1f;
            set => SoundManager.Instance?.SetSFXVolume(value);
        }

        public float VoiceVolume
        {
            get => SoundManager.Instance?.VOXVolume ?? _voiceVolume;
            set
            {
                _voiceVolume = value;
                if (SoundManager.Instance != null && SoundManager.Instance.IsReady)
                    SoundManager.Instance.SetVOXVolume(value);
            }
        }

        public string VoiceLocale { get; set; }
        public IResourceLoader AudioLoader => _audioLoader;
        public IResourceLoader VoiceLoader => _voiceLoader;

        private float _voiceVolume = 1f;
        private readonly Dictionary<string, float> _authorVolume = new();
        private readonly HashSet<string> _playingBgm = new();
        private readonly HashSet<string> _playingSfx = new();
        private string _playingVoice;

        private readonly IResourceProviderManager _resources;
        private readonly ILocalizationManager _l10n;
        private LocalizableResourceLoader<AudioClip> _audioLoader;
        private LocalizableResourceLoader<AudioClip> _voiceLoader;

        public NaninovelCustomAudioManager(Naninovel.AudioConfiguration config, IResourceProviderManager resources, ILocalizationManager l10n)
        {
            Configuration = config;
            _resources = resources;
            _l10n = l10n;
        }

        public UniTask InitializeService()
        {
            _audioLoader = Configuration.AudioLoader.CreateLocalizableFor<AudioClip>(_resources, _l10n);
            _voiceLoader = Configuration.VoiceLoader.CreateLocalizableFor<AudioClip>(_resources, _l10n);

            Debug.Log("[NaninovelCustomAudioManager] Initialized - Using SoundManager for audio playback");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            SoundManager.Instance?.StopBGM();
            SoundManager.Instance?.StopAllSound();

            _playingBgm.Clear();
            _playingSfx.Clear();
            _playingVoice = null;

            _audioLoader?.ReleaseAll(this);
            _voiceLoader?.ReleaseAll(this);
        }

        public void DestroyService()
        {
            _audioLoader?.ReleaseAll(this);
            _voiceLoader?.ReleaseAll(this);
        }

        /// <summary>
        /// SoundManager가 준비되지 않았으면 강제로 초기화합니다.
        /// Naninovel이 SoundManager보다 먼저 초기화될 수 있어 이 체크가 필요합니다.
        /// </summary>
        private void EnsureSoundManagerReady()
        {
            if (SoundManager.Instance != null && !SoundManager.Instance.IsReady)
            {
                SoundManager.Instance.Initialize();
                Debug.Log("[NaninovelCustomAudioManager] SoundManager was not ready, forced initialization");
            }
        }

        #region State Management

        public void SaveServiceState(SettingsStateMap stateMap)
        {
            var settings = new Settings
            {
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                VoiceVolume = VoiceVolume,
                VoiceLocale = VoiceLocale,
                AuthorVolume = new List<NamedFloat>()
            };

            foreach (var kv in _authorVolume)
                settings.AuthorVolume.Add(new NamedFloat(kv.Key, kv.Value));

            stateMap.SetState(settings);
        }

        public UniTask LoadServiceState(SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>();

            _authorVolume.Clear();

            if (settings == null)
            {
                MasterVolume = Configuration.DefaultMasterVolume;
                BgmVolume = Configuration.DefaultBgmVolume;
                SfxVolume = Configuration.DefaultSfxVolume;
                VoiceVolume = Configuration.DefaultVoiceVolume;
                VoiceLocale = Configuration.VoiceLocales?.Count > 0 ? Configuration.VoiceLocales[0] : null;
                return UniTask.CompletedTask;
            }

            MasterVolume = settings.MasterVolume;
            BgmVolume = settings.BgmVolume;
            SfxVolume = settings.SfxVolume;
            VoiceVolume = settings.VoiceVolume;
            VoiceLocale = settings.VoiceLocale;

            foreach (var item in settings.AuthorVolume)
                _authorVolume[item.Name] = item.Value;

            return UniTask.CompletedTask;
        }

        public void SaveServiceState(GameStateMap stateMap)
        {
            var state = new GameState
            {
                BgmClips = new List<AudioClipState>(),
                SfxClips = new List<AudioClipState>()
            };

            foreach (var bgm in _playingBgm)
                state.BgmClips.Add(new AudioClipState(bgm, 1f, true));

            foreach (var sfx in _playingSfx)
                state.SfxClips.Add(new AudioClipState(sfx, 1f, false));

            stateMap.SetState(state);
        }

        public async UniTask LoadServiceState(GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>() ?? new GameState();

            StopVoice();
            await StopAllBgm();
            await StopAllSfx();

            if (state.BgmClips != null)
            {
                foreach (var clip in state.BgmClips)
                    await PlayBgm(clip.Path, clip.Volume, 0, clip.Looped);
            }

            if (state.SfxClips != null)
            {
                foreach (var clip in state.SfxClips)
                    await PlaySfx(clip.Path, clip.Volume, 0, clip.Looped);
            }
        }

        #endregion

        #region BGM

        public UniTask PlayBgm(string path, float volume = 1f, float fadeTime = 0f, bool loop = true,
            string introPath = null, string group = default, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // SoundManager 준비 상태 확인 및 초기화
            EnsureSoundManagerReady();

            // SoundManager를 통해 BGM 재생
            var result = SoundManager.Instance?.PlayBGM(path);
            if (result == null)
            {
                Debug.LogWarning($"[NaninovelCustomAudioManager] PlayBgm failed: {path} - SoundManager may not be ready or audioID not found");
            }
            _playingBgm.Add(path);

            Debug.Log($"[NaninovelCustomAudioManager] PlayBgm: {path}");
            return UniTask.CompletedTask;
        }

        public UniTask StopBgm(string path, float fadeTime = 0f, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // fadeTime이 있으면 페이드아웃, 없으면 즉시 정지
            if (fadeTime > 0)
                SoundManager.Instance?.StopBGM(fadeTime);
            else
                SoundManager.Instance?.StopBGM();

            _playingBgm.Remove(path);

            Debug.Log($"[NaninovelCustomAudioManager] StopBgm: {path}");
            return UniTask.CompletedTask;
        }

        public UniTask StopAllBgm(float fadeTime = 0f, AsyncToken token = default)
        {
            if (fadeTime > 0)
                SoundManager.Instance?.StopBGM(fadeTime);
            else
                SoundManager.Instance?.StopBGM();

            _playingBgm.Clear();

            Debug.Log("[NaninovelCustomAudioManager] StopAllBgm");
            return UniTask.CompletedTask;
        }

        public UniTask ModifyBgm(string path, float volume, bool loop, float time, AsyncToken token = default)
        {
            // SoundManager에서는 개별 트랙 볼륨 조절이 제한적
            // 필요시 확장 가능
            return UniTask.CompletedTask;
        }

        public bool IsBgmPlaying(string path)
        {
            return !string.IsNullOrEmpty(path) && _playingBgm.Contains(path);
        }

        public void GetPlayedBgm(ICollection<string> paths)
        {
            foreach (var bgm in _playingBgm)
                paths.Add(bgm);
        }

        #endregion

        #region SFX

        public UniTask PlaySfx(string path, float volume = 1f, float fadeTime = 0f, bool loop = false,
            string group = default, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // SoundManager 준비 상태 확인 및 초기화
            EnsureSoundManagerReady();

            // SoundManager를 통해 SFX 재생
            var result = SoundManager.Instance?.PlaySFX(path);
            if (result == null)
            {
                Debug.LogWarning($"[NaninovelCustomAudioManager] PlaySfx failed: {path} - SoundManager may not be ready or audioID not found");
            }

            if (loop)
                _playingSfx.Add(path);

            Debug.Log($"[NaninovelCustomAudioManager] PlaySfx: {path}");
            return UniTask.CompletedTask;
        }

        public UniTask PlaySfxFast(string path, float volume = 1f, string group = default,
            bool restart = true, bool additive = true)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // SoundManager 준비 상태 확인 및 초기화
            EnsureSoundManagerReady();

            SoundManager.Instance?.PlaySFX(path);

            Debug.Log($"[NaninovelCustomAudioManager] PlaySfxFast: {path}");
            return UniTask.CompletedTask;
        }

        public UniTask StopSfx(string path, float fadeTime = 0f, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // SoundFX enum으로 파싱 시도
            if (Enum.TryParse<SoundFX>(path, out var sfx))
                SoundManager.Instance?.StopSFX(sfx);

            _playingSfx.Remove(path);

            Debug.Log($"[NaninovelCustomAudioManager] StopSfx: {path}");
            return UniTask.CompletedTask;
        }

        public UniTask StopAllSfx(float fadeTime = 0f, AsyncToken token = default)
        {
            _playingSfx.Clear();

            Debug.Log("[NaninovelCustomAudioManager] StopAllSfx");
            return UniTask.CompletedTask;
        }

        public UniTask ModifySfx(string path, float volume, bool loop, float time, AsyncToken token = default)
        {
            return UniTask.CompletedTask;
        }

        public bool IsSfxPlaying(string path)
        {
            return !string.IsNullOrEmpty(path) && _playingSfx.Contains(path);
        }

        public void GetPlayedSfx(ICollection<string> paths)
        {
            foreach (var sfx in _playingSfx)
                paths.Add(sfx);
        }

        #endregion

        #region Voice

        public UniTask PlayVoice(string path, float volume = 1f, string group = default,
            string authorId = default, AsyncToken token = default)
        {
            if (string.IsNullOrEmpty(path)) return UniTask.CompletedTask;

            // SoundManager 준비 상태 확인 및 초기화
            EnsureSoundManagerReady();

            // 기존 음성 정지
            StopVoice();

            // Author별 볼륨 적용
            if (!string.IsNullOrEmpty(authorId))
            {
                var authorVolume = GetAuthorVolume(authorId);
                if (authorVolume >= 0)
                    volume *= authorVolume;
            }

            // SoundManager를 통해 Voice 재생
            var result = SoundManager.Instance?.PlayVOX(path);
            if (result == null)
            {
                Debug.LogWarning($"[NaninovelCustomAudioManager] PlayVoice failed: {path} - SoundManager may not be ready or audioID not found");
            }
            _playingVoice = path;

            Debug.Log($"[NaninovelCustomAudioManager] PlayVoice: {path}, Author: {authorId}");
            return UniTask.CompletedTask;
        }

        public void StopVoice()
        {
            if (!string.IsNullOrEmpty(_playingVoice))
            {
                SoundManager.Instance?.StopVOX(_playingVoice);
                _playingVoice = null;

                Debug.Log("[NaninovelCustomAudioManager] StopVoice");
            }
        }

        public bool IsVoicePlaying(string path)
        {
            return !string.IsNullOrEmpty(path) && _playingVoice == path;
        }

        public string GetPlayedVoice()
        {
            return _playingVoice;
        }

        #endregion

        #region Audio Track (Limited Support)

        public IAudioTrack GetAudioTrack(string path)
        {
            // SoundManager는 IAudioTrack을 지원하지 않음
            return null;
        }

        public IAudioTrack GetVoiceTrack(string path)
        {
            // SoundManager는 IAudioTrack을 지원하지 않음
            return null;
        }

        #endregion

        #region Author Volume

        public float GetAuthorVolume(string authorId)
        {
            if (string.IsNullOrEmpty(authorId)) return -1;
            return _authorVolume.TryGetValue(authorId, out var volume) ? volume : -1;
        }

        public void SetAuthorVolume(string authorId, float volume)
        {
            if (string.IsNullOrEmpty(authorId)) return;
            _authorVolume[authorId] = volume;
        }

        #endregion
    }
}
