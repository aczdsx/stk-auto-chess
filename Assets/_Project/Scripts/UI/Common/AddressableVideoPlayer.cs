using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// AssetReference로 VideoClip을 로드하여 루프 재생하는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class AddressableVideoPlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private RawImage _targetRawImage;
        [SerializeField] private RawImage _backgroundRawImage;  // 블러 배경용 (전체 화면)
        [SerializeField] private bool _playOnLoad = true;
        [SerializeField] private bool _loop = true;

        private AsyncOperationHandle<VideoClip> _videoClipHandle;
        private RenderTexture _renderTexture;
        private bool _isLoaded;

        private void Awake()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = _loop;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        /// <summary>
        /// AssetReference를 통해 비디오를 로드하고 재생합니다
        /// </summary>
        public async UniTask LoadAndPlayAsync(AssetReference videoAssetReference)
        {
            if (videoAssetReference == null || !videoAssetReference.RuntimeKeyIsValid())
            {
                Debug.LogWarning("[AddressableVideoPlayer] Invalid AssetReference");
                return;
            }

            await LoadVideoClipAsync(videoAssetReference);

            if (_playOnLoad && _isLoaded)
            {
                Play();
            }
        }

        /// <summary>
        /// Addressable 키를 통해 비디오를 로드하고 재생합니다
        /// </summary>
        public async UniTask LoadAndPlayAsync(string addressableKey)
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogWarning("[AddressableVideoPlayer] Invalid addressable key");
                return;
            }

            await LoadVideoClipByKeyAsync(addressableKey);

            if (_playOnLoad && _isLoaded)
            {
                Play();
            }
        }

        private async UniTask LoadVideoClipAsync(AssetReference videoAssetReference)
        {
            Release();

            _videoClipHandle = videoAssetReference.LoadAssetAsync<VideoClip>();
            await _videoClipHandle.WaitUntilDone();

            if (_videoClipHandle.Status == AsyncOperationStatus.Succeeded)
            {
                SetupVideoPlayer(_videoClipHandle.Result);
                _isLoaded = true;
            }
            else
            {
                Debug.LogError($"[AddressableVideoPlayer] Failed to load video: {videoAssetReference.RuntimeKey}");
            }
        }

        private async UniTask LoadVideoClipByKeyAsync(string key)
        {
            Release();

            _videoClipHandle = Addressables.LoadAssetAsync<VideoClip>(key);
            await _videoClipHandle.ToUniTask();

            if (_videoClipHandle.Status == AsyncOperationStatus.Succeeded)
            {
                SetupVideoPlayer(_videoClipHandle.Result);
                _isLoaded = true;
            }
            else
            {
                Debug.LogError($"[AddressableVideoPlayer] Failed to load video: {key}");
            }
        }

        private void SetupVideoPlayer(VideoClip clip)
        {
            // RenderTexture 생성
            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }

            _renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            _renderTexture.Create();

            _videoPlayer.clip = clip;
            _videoPlayer.targetTexture = _renderTexture;

            // 블러 배경 RawImage에 RenderTexture 연결 (전체 화면)
            if (_backgroundRawImage != null)
            {
                _backgroundRawImage.texture = _renderTexture;

                // AspectRatioFitter 설정 (전체 화면 채움)
                var bgAspectFitter = _backgroundRawImage.GetComponent<AspectRatioFitter>();
                if (bgAspectFitter == null)
                {
                    bgAspectFitter = _backgroundRawImage.gameObject.AddComponent<AspectRatioFitter>();
                }
                bgAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                bgAspectFitter.aspectRatio = (float)clip.width / clip.height;
            }

            // 메인 RawImage에 RenderTexture 연결
            if (_targetRawImage != null)
            {
                _targetRawImage.texture = _renderTexture;

                // AspectRatioFitter 설정 (비디오 비율 유지)
                var aspectFitter = _targetRawImage.GetComponent<AspectRatioFitter>();
                if (aspectFitter == null)
                {
                    aspectFitter = _targetRawImage.gameObject.AddComponent<AspectRatioFitter>();
                }
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspectFitter.aspectRatio = (float)clip.width / clip.height;
            }
        }

        public void Play()
        {
            if (_isLoaded && _videoPlayer != null && _videoPlayer.enabled && gameObject.activeInHierarchy)
            {
                _videoPlayer.Play();
            }
        }

        public void Pause()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Pause();
            }
        }

        public void Stop()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        public void SetLoop(bool loop)
        {
            _loop = loop;
            if (_videoPlayer != null)
            {
                _videoPlayer.isLooping = loop;
            }
        }

        public bool IsPlaying => _videoPlayer != null && _videoPlayer.isPlaying;

        public bool IsLoaded => _isLoaded;

        private void Release()
        {
            _isLoaded = false;

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.clip = null;
                _videoPlayer.targetTexture = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_videoClipHandle.IsValid())
            {
                Addressables.Release(_videoClipHandle);
            }
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}
