using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 뉴 미니게임 — 마크 상태 관리 및 활성화 연출.
    /// Mark_R / Mark_SR / Mark_SSR 프리팹에 부착.
    /// </summary>
    public class MarkController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Detection")]
        [SerializeField] private float _detectionRadius = 70f;

        [Header("References")]
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private Transform _visualRoot;

        #endregion

        #region Private Fields

        private GachaNewController _controller;
        private bool _isFound;

        #endregion

        #region Properties

        public RectTransform RectTransform { get; private set; }
        public bool IsFound => _isFound;
        public float DetectionRadius => _detectionRadius;
        public Vector2 Position => RectTransform.anchoredPosition;
        public int ResultIndex { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();

            // _visualRoot 미지정 시 자신 Transform 사용
            if (_visualRoot == null)
                _visualRoot = transform;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 마크 초기화. GachaNewController에서 마크 생성 후 호출.
        /// </summary>
        /// <param name="controller">이 마크를 소유하는 GachaNewController</param>
        /// <param name="resultIndex">대응하는 가챠 결과 인덱스</param>
        public void Initialize(GachaNewController controller, int resultIndex)
        {
            _controller = controller;
            ResultIndex = resultIndex;
            _isFound = false;

            // 초기 비활성 상태: scale 0 으로 리셋
            _visualRoot.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 마크 활성화 — 중복 호출 무시, scale 0→1 애니메이션 + 파티클 + 사운드 재생.
        /// </summary>
        public void Activate()
        {
            if (_isFound) return;

            _isFound = true;

            gameObject.SetActive(true);
            _visualRoot.localScale = Vector3.zero;

            PlayActivateAnimationAsync().Forget();
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid PlayActivateAnimationAsync()
        {
            var token = destroyCancellationToken;

            // Scale 0 → 1 (0.3초, EaseOutBack)
            await LMotion.Create(Vector3.zero, Vector3.one, 0.3f)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(_visualRoot)
                .AddTo(gameObject)
                .ToUniTask(token);

            if (token.IsCancellationRequested) return;

            // 파티클 재생
            if (_particleSystem != null)
                _particleSystem.Play();

            // 사운드 재생
            SoundManager.Instance?.PlaySFX(SoundFX.snd_sfx_gacha_open_whoosh01);

            // 컨트롤러에 발견 통보
            _controller?.OnMarkFound(this);
        }

        #endregion
    }
}
