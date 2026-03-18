using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 뉴 미니게임 — 크로스헤어 입력 수신 및 마크 범위 판정.
    /// Update()에서 직접 Input을 처리 (GraphicRaycaster의 Graphic.depth=-1 문제 우회).
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private RectTransform _crosshairVisual;
        [SerializeField] private Canvas _canvas;

        #endregion

        #region Private Fields

        private GachaNewController _controller;
        private RectTransform _parentRect;

        private bool _isEnabled;
        private bool _isDragging;
        private Vector2 _prevPosition;
        private bool _hasPrevPosition;

        private MotionHandle _showHandle;
        private MotionHandle _hideHandle;
        private RectTransform[] _uiButtonRects;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _parentRect = GetComponent<RectTransform>();

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            // 크로스헤어 비주얼 초기 숨김
            if (_crosshairVisual != null)
            {
                _crosshairVisual.localScale = Vector3.zero;
                _crosshairVisual.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_isEnabled) return;

            // 입력 감지 (Mouse + Touch 동시 지원)
            bool inputBegan = false;
            bool inputHeld = false;
            bool inputEnded = false;
            Vector2 inputPos = Vector2.zero;

            // Mouse
            if (Input.GetMouseButtonDown(0))
            {
                inputBegan = true;
                inputPos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                inputHeld = true;
                inputPos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                inputEnded = true;
                inputPos = Input.mousePosition;
            }

            // Touch fallback (Device Simulator / 모바일)
            if (!inputBegan && !inputHeld && !inputEnded && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                inputPos = touch.position;
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        inputBegan = true;
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        inputHeld = true;
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        inputEnded = true;
                        break;
                }
            }

            if (inputBegan)
            {
                // UI 버튼 영역 터치 시 크로스헤어 입력 무시
                if (IsOverUIButton(inputPos)) return;

                _isDragging = true;
                _hasPrevPosition = false;
                MoveToPosition(inputPos);
                _prevPosition = _crosshairVisual.anchoredPosition;
                _hasPrevPosition = true;
                ShowCrosshair();
                CheckOverlap();
            }
            else if (inputHeld && _isDragging)
            {
                MoveToPosition(inputPos);
                CheckOverlap();
                _prevPosition = _crosshairVisual.anchoredPosition;
            }
            else if (inputEnded && _isDragging)
            {
                _isDragging = false;
                _hasPrevPosition = false;
                HideCrosshair();
            }
        }

        #endregion

        #region Public Methods

        public bool IsEnabled => _isEnabled;

        public void Initialize(GachaNewController controller)
        {
            _controller = controller;
        }

        public void SetUIButtonRects(RectTransform[] rects)
        {
            _uiButtonRects = rects;
        }

        public void SetEnabled(bool enabled)
        {
            Debug.Log($"[CrosshairController] SetEnabled({enabled}) called. Previous: {_isEnabled}");
            _isEnabled = enabled;

            if (!enabled)
            {
                _isDragging = false;
                _showHandle.TryCancel();
                _hideHandle.TryCancel();
                if (_crosshairVisual != null)
                {
                    _crosshairVisual.localScale = Vector3.zero;
                    _crosshairVisual.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Private Methods

        private bool IsOverUIButton(Vector2 screenPos)
        {
            if (_uiButtonRects == null) return false;
            Camera cam = _canvas != null ? _canvas.worldCamera : null;
            for (int i = 0; i < _uiButtonRects.Length; i++)
            {
                if (_uiButtonRects[i] != null
                    && _uiButtonRects[i].gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(_uiButtonRects[i], screenPos, cam))
                    return true;
            }
            return false;
        }

        private void MoveToPosition(Vector2 screenPos)
        {
            if (_crosshairVisual == null) return;

            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect,
                screenPos,
                _canvas != null ? _canvas.worldCamera : null,
                out Vector2 localPoint);

            if (!success) return;

            _crosshairVisual.anchoredPosition = localPoint;
        }

        private void CheckOverlap()
        {
            if (_controller == null) return;

            List<MarkController> marks = _controller.GetUnfoundMarks();
            if (marks == null) return;

            Vector2 currentPos = _crosshairVisual.anchoredPosition;

            for (int i = 0; i < marks.Count; i++)
            {
                MarkController mark = marks[i];
                if (mark == null || mark.IsFound) continue;

                float dist;
                if (_hasPrevPosition)
                {
                    // Line-sweep: 이전→현재 선분 위 최근접점과 마크 거리 비교
                    Vector2 closest = ClosestPointOnSegment(_prevPosition, currentPos, mark.Position);
                    dist = Vector2.Distance(closest, mark.Position);
                }
                else
                {
                    dist = Vector2.Distance(currentPos, mark.Position);
                }

                if (dist <= mark.DetectionRadius)
                {
                    mark.Activate();
                }
            }
        }

        /// <summary>
        /// 선분(segA→segB) 위에서 point에 가장 가까운 점을 반환.
        /// </summary>
        private static Vector2 ClosestPointOnSegment(Vector2 segA, Vector2 segB, Vector2 point)
        {
            Vector2 ab = segB - segA;
            float sqrLen = ab.sqrMagnitude;

            // segA == segB (이동 없음)
            if (sqrLen < 0.0001f) return segA;

            float t = Vector2.Dot(point - segA, ab) / sqrLen;
            t = Mathf.Clamp01(t);
            return segA + ab * t;
        }

        private void ShowCrosshair()
        {
            if (_crosshairVisual == null) return;

            _hideHandle.TryCancel();

            _crosshairVisual.localScale = Vector3.zero;
            _crosshairVisual.gameObject.SetActive(true);

            _showHandle = LMotion.Create(
                    Vector3.zero,
                    Vector3.one,
                    0.15f)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(_crosshairVisual)
                .AddTo(gameObject);
        }

        private void HideCrosshair()
        {
            if (_crosshairVisual == null) return;

            _showHandle.TryCancel();

            _hideHandle = LMotion.Create(
                    _crosshairVisual.localScale,
                    Vector3.zero,
                    0.12f)
                .WithEase(Ease.InBack)
                .WithOnComplete(() =>
                {
                    if (_crosshairVisual != null)
                        _crosshairVisual.gameObject.SetActive(false);
                })
                .BindToLocalScale(_crosshairVisual)
                .AddTo(gameObject);
        }

        #endregion
    }
}
