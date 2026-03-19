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

        [Header("Arrow Direction")]
        [SerializeField] private RectTransform _arrowPivot;
        [SerializeField] private float _arrowMoveSpeed = 720f;

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

        private float _currentArrowAngle;
        private bool _hasArrowAngle;

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

            // Arrow 초기 숨김
            if (_arrowPivot != null)
                _arrowPivot.gameObject.SetActive(false);
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
                UpdateArrowAndSubMarks();
            }
            else if (inputHeld && _isDragging)
            {
                MoveToPosition(inputPos);
                CheckOverlap();
                UpdateArrowAndSubMarks();
                _prevPosition = _crosshairVisual.anchoredPosition;
            }
            else if (inputEnded && _isDragging)
            {
                _isDragging = false;
                _hasPrevPosition = false;
                // 크로스헤어는 마지막 위치에 유지 (HideCrosshair 제거)
            }

            // 드래그 중이 아닐 때도 인디케이터는 계속 갱신
            if (!_isDragging && _crosshairVisual != null && _crosshairVisual.gameObject.activeSelf)
            {
                UpdateArrowAndSubMarks();
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
                HideArrowAndSubMarks();
            }
            else
            {
                // 활성화 시 화면 중앙에 즉시 표시
                if (_crosshairVisual != null)
                {
                    _crosshairVisual.anchoredPosition = Vector2.zero;
                    ShowCrosshair();
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

            _hasArrowAngle = false;
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

        #region Arrow Direction & SubMark

        private void UpdateArrowAndSubMarks()
        {
            if (_controller == null) return;

            List<MarkController> marks = _controller.GetUnfoundMarks();
            if (marks == null) return;

            Vector2 crosshairPos = _crosshairVisual.anchoredPosition;

            // 가장 가까운 미발견 마크 찾기 + SubMark 범위 체크
            float closestSqrDist = float.MaxValue;
            MarkController closestMark = null;

            for (int i = 0; i < marks.Count; i++)
            {
                MarkController mark = marks[i];
                if (mark == null || mark.IsFound) continue;

                float sqrDist = Vector2.SqrMagnitude(mark.Position - crosshairPos);

                // 가장 가까운 마크 추적
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closestMark = mark;
                }

                // SubMark 범위 체크
                float dist = Mathf.Sqrt(sqrDist);
                float subRadius = mark.SubDetectionRadius;
                float mainRadius = mark.DetectionRadius;

                if (dist <= subRadius && dist > mainRadius)
                {
                    mark.ShowSubMark();
                }
                else
                {
                    mark.HideSubMark();
                }
            }

            // Arrow 회전
            if (_arrowPivot != null)
            {
                if (closestMark != null)
                {
                    Vector2 dir = closestMark.Position - crosshairPos;
                    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    if (!_hasArrowAngle)
                    {
                        _currentArrowAngle = targetAngle;
                        _hasArrowAngle = true;
                    }
                    else
                    {
                        _currentArrowAngle = Mathf.MoveTowardsAngle(
                            _currentArrowAngle, targetAngle, _arrowMoveSpeed * Time.deltaTime);
                    }

                    _arrowPivot.localRotation = Quaternion.Euler(0f, 0f, _currentArrowAngle - 90f);

                    if (!_arrowPivot.gameObject.activeSelf)
                        _arrowPivot.gameObject.SetActive(true);
                }
                else
                {
                    if (_arrowPivot.gameObject.activeSelf)
                        _arrowPivot.gameObject.SetActive(false);
                }
            }
        }

        private void HideArrowAndSubMarks()
        {
            if (_arrowPivot != null)
                _arrowPivot.gameObject.SetActive(false);

            if (_controller == null) return;
            List<MarkController> marks = _controller.GetUnfoundMarks();
            if (marks == null) return;

            for (int i = 0; i < marks.Count; i++)
            {
                if (marks[i] != null)
                    marks[i].HideSubMark();
            }
        }

        #endregion
    }
}
