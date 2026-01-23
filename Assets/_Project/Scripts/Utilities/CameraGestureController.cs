using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

// ReSharper disable All

public class CameraGestureController : CachedMonoBehaviour
{
    private const float PositionThreshold = 0.001f;
    private const float ZoomThreshold = 0.001f;

    [Header("카메라 이동 설정")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float panSmoothing = 10f;

    [Header("카메라 줌 설정")]
    [SerializeField] private float zoomSpeed = 0.01f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomSmoothing = 10f;

    [Header("마우스 설정 (에디터용)")]
    [SerializeField] private float mouseScrollZoomSpeed = 2f;

    [Header("카메라 범위 설정 (카메라 뷰 평면 기준)")]
    [SerializeField] private Vector2 boundaryMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 boundaryMax = new Vector2(10f, 10f);

    private Camera mainCamera;
    private Transform mainCameraTransform;
    private float mainCameraAspect;

    /// <summary>
    /// 현재 줌 비율 (0 = minZoom, 1 = 실제 허용 최대 줌)
    /// </summary>
    public float ZoomRatio
    {
        get
        {
            if (mainCamera == null) return 0f;
            var currentZoom = mainCamera.orthographicSize;
            return Mathf.InverseLerp(minZoom, cachedMaxAllowedZoom, currentZoom);
        }
    }

    private Vector3 targetPosition;
    private float targetZoom;
    private float cachedMaxAllowedZoom;

    private Vector2 lastTouchPosition;
    private Vector2 lastMousePosition;
    private float lastPinchDistance;
    private bool isDragging;
    private bool isMouseDragging;

    private EventSystem cachedEventSystem;
    private bool needsPositionSmoothing;
    private bool needsZoomSmoothing;
    private bool isAutoMoving; // MoveAsync 실행 중 입력 차단용
    
    private Transform followTarget;
    private float followSpeed;

    private bool canInteractCamera;

    private void Awake()
    {
        InitializeCamera();
        InitializeVariables();
        InitializeMaxAllowedZoom();
    }
    
    private void Update()
    {
        CheckGesture();
        ApplySmoothing();
    }
    
    private void CheckGesture()
    {
        cachedEventSystem = EventSystem.current;
        
#if UNITY_EDITOR
        
        isDragging = false;
        if (!isAutoMoving)
        {
            if (followTarget)
            {
                UpdateFollowTarget();
            }
            else
            {
                if (!canInteractCamera)
                    return;
                
                HandleMouseInput();
            }
        }
        
#else
        
        var touchCount = Input.touchCount;
        if (touchCount > 0 && !isAutoMoving && !canInteractCamera)
            HandleTouchInput(touchCount);
        
#endif
    }

    #region Initialize

    private void InitializeCamera()
    {
        mainCamera = MainCameraHolder.MainCamera;
        if (mainCamera == null)
            mainCamera = Camera.main;

        canInteractCamera = true;
    }

    private void InitializeVariables()
    {
        mainCameraAspect = mainCamera.aspect;
        mainCameraTransform = mainCamera.transform;
        
        targetPosition = mainCameraTransform.position;
        targetZoom = mainCamera.orthographicSize;
    }
    
    private void InitializeMaxAllowedZoom()
    {
        var boundaryWidth = boundaryMax.x - boundaryMin.x;
        var boundaryHeight = boundaryMax.y - boundaryMin.y;

        var maxZoomByWidth = boundaryWidth / (2f * mainCameraAspect);
        var maxZoomByHeight = boundaryHeight / 2f;

        cachedMaxAllowedZoom = Mathf.Min(maxZoom, maxZoomByWidth, maxZoomByHeight);
    }

    #endregion

    public void SetCanInteractCamera(bool canInteract)
    {
        canInteractCamera = canInteract;
    }

    #region Mouse (Editor)

    private void HandleMouseInput()
    {
        HandleMouseDrag();
        HandleMouseScroll();
    }

    private void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsMouseOverUI())
            {
                lastMousePosition = Input.mousePosition;
                isMouseDragging = true;
            }
        }
        else if (Input.GetMouseButton(0) && isMouseDragging)
        {
            // 드래그 중 UI 위로 가면 드래그 중단
            if (IsMouseOverUI())
            {
                isMouseDragging = false;
                return;
            }

            var currentMousePosition = (Vector2)Input.mousePosition;
            var delta = currentMousePosition - lastMousePosition;
            ApplyPanDelta(delta);
            lastMousePosition = currentMousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDragging = false;
            // 다음 클릭 시 이전 위치와의 큰 delta 방지
            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleMouseScroll()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta == 0f || IsMouseOverUI())
        {
            return;
        }

        ApplyZoomDelta(-scrollDelta * mouseScrollZoomSpeed);
    }

    private bool IsMouseOverUI()
    {
        return cachedEventSystem != null && cachedEventSystem.IsPointerOverGameObject();
    }

    #endregion

    #region Touch Handler
    
    private void HandleTouchInput(int touchCount)
    {
        if (touchCount == 1)
        {
            HandlePanning();
        }
        else if (touchCount == 2)
        {
            HandlePinchZoom();
        }
        else
        {
            isDragging = false;
        }
    }

    /// <summary>
    /// 카메라 드래그 이동을 위한 함수
    /// </summary>
    private void HandlePanning()
    {
        var currentTouch = Input.GetTouch(0);

        if (IsTouchOverUI(currentTouch))
        {
            isDragging = false;
            return;
        }

        switch (currentTouch.phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = currentTouch.position;
                isDragging = true;
                break;

            case TouchPhase.Moved:
                if (isDragging)
                {
                    Vector2 delta = currentTouch.position - lastTouchPosition;
                    ApplyPanDelta(delta);
                    lastTouchPosition = currentTouch.position;
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                isDragging = false;
                break;
        }
    }

    /// <summary>
    /// 카메라 줌 기능을 위한 함수
    /// </summary>
    private void HandlePinchZoom()
    {
        var touch0 = Input.GetTouch(0);
        var touch1 = Input.GetTouch(1);

        if (IsTouchOverUI(touch0) || IsTouchOverUI(touch1))
        {
            return;
        }

        var currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            lastPinchDistance = currentPinchDistance;
            isDragging = false;
            return;
        }

        if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            var pinchDelta = lastPinchDistance - currentPinchDistance;
            ApplyZoomDelta(pinchDelta * zoomSpeed);
            lastPinchDistance = currentPinchDistance;
        }
    }

    private bool IsTouchOverUI(Touch touch)
    {
        return cachedEventSystem && cachedEventSystem.IsPointerOverGameObject(touch.fingerId);
    }

    #endregion

    #region Main

    private void ApplyPanDelta(Vector2 delta)
    {
        // 카메라 로컬 좌표계에서 타겟까지의 거리 (회전 고려)
        var localTarget = mainCameraTransform.InverseTransformPoint(targetPosition);
        var distanceToTarget = localTarget.z;

        // 화면 중심의 월드 좌표
        var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget);
        var centerWorld = mainCamera.ScreenToWorldPoint(screenCenter);

        // 드래그 후 위치의 월드 좌표
        var scaledDelta = delta * panSpeed * Time.deltaTime;
        var screenOffset = new Vector3(screenCenter.x - scaledDelta.x, screenCenter.y - scaledDelta.y, distanceToTarget);
        var offsetWorld = mainCamera.ScreenToWorldPoint(screenOffset);

        // 이동량 계산 (화면 기준 정확한 이동)
        var movementDelta = offsetWorld - centerWorld;
        
        // 각 축을 독립적으로 체크하여 부분 이동 허용
        var clampedDelta = ClampMovementToBoundary(movementDelta);
        
        if (clampedDelta.sqrMagnitude < 0.0001f)
        {
            // 이동량이 거의 없으면 무시
            return;
        }
        
        targetPosition += clampedDelta;
        needsPositionSmoothing = true;
    }
    
    /// <summary>
    /// 이동량을 바운더리 내로 제한 (각 축 독립적으로 처리)
    /// </summary>
    private Vector3 ClampMovementToBoundary(Vector3 movementDelta)
    {
        var verticalExtent = mainCamera.orthographicSize;
        var horizontalExtent = verticalExtent * mainCameraAspect;
        
        // 현재 뷰 중심의 바운더리 평면 좌표
        var currentViewCenter = GetViewCenterWorldPosition(targetPosition);
        var currentBoundaryPoint = WorldToBoundaryPlane(currentViewCenter);
        
        // 이동 후 뷰 중심의 바운더리 평면 좌표
        var newPosition = targetPosition + movementDelta;
        var newViewCenter = GetViewCenterWorldPosition(newPosition);
        var newBoundaryPoint = WorldToBoundaryPlane(newViewCenter);
        
        // boundary 범위 계산
        var centerX = (boundaryMin.x + boundaryMax.x) * 0.5f;
        var centerY = (boundaryMin.y + boundaryMax.y) * 0.5f;
        var halfWidth = Mathf.Max(0, (boundaryMax.x - boundaryMin.x) * 0.5f - horizontalExtent);
        var halfHeight = Mathf.Max(0, (boundaryMax.y - boundaryMin.y) * 0.5f - verticalExtent);

        var minX = centerX - halfWidth;
        var maxX = centerX + halfWidth;
        var minY = centerY - halfHeight;
        var maxY = centerY + halfHeight;
        
        // 각 축별로 clamp
        var clampedX = Mathf.Clamp(newBoundaryPoint.x, minX, maxX);
        var clampedY = Mathf.Clamp(newBoundaryPoint.y, minY, maxY);
        
        // 실제 적용할 바운더리 평면 좌표의 델타
        var allowedDeltaX = clampedX - currentBoundaryPoint.x;
        var allowedDeltaY = clampedY - currentBoundaryPoint.y;
        
        // 바운더리 평면 델타를 월드 좌표로 변환
        var worldDelta = mainCameraTransform.rotation * new Vector3(allowedDeltaX, allowedDeltaY, 0f);
        
        return worldDelta;
    }
    
    /// <summary>
    /// 주어진 카메라 위치에서 화면 중심이 바라보는 월드 좌표
    /// </summary>
    private Vector3 GetViewCenterWorldPosition(Vector3 cameraPosition)
    {
        var originalPosition = mainCameraTransform.position;
        mainCameraTransform.position = cameraPosition;
        
        var localTarget = mainCameraTransform.InverseTransformPoint(cameraPosition);
        var distanceToTarget = localTarget.z;
        var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget);
        var viewCenter = mainCamera.ScreenToWorldPoint(screenCenter);
        
        mainCameraTransform.position = originalPosition;
        return viewCenter;
    }
    
    /// <summary>
    /// 월드 좌표를 바운더리 평면 좌표(2D)로 변환 (카메라 회전 고려)
    /// </summary>
    private Vector2 WorldToBoundaryPlane(Vector3 worldPoint)
    {
        // 카메라의 로컬 좌표계를 기준으로 변환
        // 바운더리는 카메라가 바라보는 평면에 정의됨
        var cameraRotation = mainCameraTransform.rotation;
        var boundaryCenter3D = new Vector3(
            (boundaryMin.x + boundaryMax.x) * 0.5f,
            (boundaryMin.y + boundaryMax.y) * 0.5f,
            0f
        );
        
        // 회전된 바운더리 중심을 월드로
        var rotatedCenter = cameraRotation * boundaryCenter3D;
        
        // 월드 좌표를 카메라의 역회전으로 변환하여 바운더리 평면 좌표 획득
        var inverseRotation = Quaternion.Inverse(cameraRotation);
        var localPoint = inverseRotation * worldPoint;
        
        return new Vector2(localPoint.x, localPoint.y);
    }

    private void ApplyZoomDelta(float delta)
    {
        targetZoom += delta;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, cachedMaxAllowedZoom);
        needsZoomSmoothing = true;
    }

    private void ApplySmoothing()
    {
        if(isAutoMoving)
            return;
        
        if (needsZoomSmoothing)
        {
            var currentZoomSize = mainCamera.orthographicSize;
            var newZoomSize = Mathf.Lerp(currentZoomSize, targetZoom, zoomSmoothing * Time.deltaTime);
            mainCamera.orthographicSize = newZoomSize;

            var zoomDifference = Mathf.Abs(targetZoom - newZoomSize);
            if (zoomDifference < ZoomThreshold)
            {
                mainCamera.orthographicSize = targetZoom;
                needsZoomSmoothing = false;
            }
        }

        if (needsPositionSmoothing || needsZoomSmoothing)
            ClampTargetPositionToBoundary();

        if (needsPositionSmoothing)
        {
            var currentPosition = mainCameraTransform.position;
            var newPosition = Vector3.Lerp(currentPosition, targetPosition, panSmoothing * Time.deltaTime);
            mainCameraTransform.position = newPosition;

            var sqrDistance = (targetPosition - newPosition).sqrMagnitude;
            if (sqrDistance < PositionThreshold * PositionThreshold)
            {
                mainCameraTransform.position = targetPosition;
                needsPositionSmoothing = false;
            }
        }
    }

    #endregion

    #region Boundary
    
    /// <summary>
    /// 카메라 범위내로 카메라 포지션 값 세팅해주는 함수. (카메라 회전 고려)
    /// </summary>
    private void ClampTargetPositionToBoundary()
    {
        var verticalExtent = mainCamera.orthographicSize;
        var horizontalExtent = verticalExtent * mainCameraAspect;

        // 현재 화면 중심이 보는 월드 좌표 계산
        var localTarget = mainCameraTransform.InverseTransformPoint(targetPosition);
        var distanceToTarget = localTarget.z;
        var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget);
        var viewCenter = mainCamera.ScreenToWorldPoint(screenCenter);

        // 뷰 중심을 바운더리 평면 좌표로 변환
        var boundaryPoint = WorldToBoundaryPlane(viewCenter);

        // boundary 범위
        var centerX = (boundaryMin.x + boundaryMax.x) * 0.5f;
        var centerY = (boundaryMin.y + boundaryMax.y) * 0.5f;
        var halfWidth = Mathf.Max(0, (boundaryMax.x - boundaryMin.x) * 0.5f - horizontalExtent);
        var halfHeight = Mathf.Max(0, (boundaryMax.y - boundaryMin.y) * 0.5f - verticalExtent);

        // 바운더리 평면 좌표를 clamp
        var clampedX = Mathf.Clamp(boundaryPoint.x, centerX - halfWidth, centerX + halfWidth);
        var clampedY = Mathf.Clamp(boundaryPoint.y, centerY - halfHeight, centerY + halfHeight);

        // clamp된 만큼 카메라 위치 조정 (회전 고려)
        var deltaX = clampedX - boundaryPoint.x;
        var deltaY = clampedY - boundaryPoint.y;

        if (Mathf.Abs(deltaX) > 0.001f || Mathf.Abs(deltaY) > 0.001f)
        {
            // 바운더리 평면의 델타를 월드 좌표로 변환
            var worldDelta = mainCameraTransform.rotation * new Vector3(deltaX, deltaY, 0f);
            targetPosition += worldDelta;

            // 바운더리 클램프 시에는 카메라 위치도 즉시 업데이트하여 튕김 방지
            // (스무딩으로 인한 핑퐁 현상 방지)
            mainCameraTransform.position = targetPosition;
            needsPositionSmoothing = false;
        }
    }

    #endregion

    #region Follow Target

    public void SetFollowTarget(Transform target, float followSpeed)
    {
        followTarget = target;
        this.followSpeed = followSpeed;
    }

    public void SetAutoMoving(bool value)
    {
        isAutoMoving = value;
    }

    public void SetCameraPositionAndZoom(Vector3 position, float zoom)
    {
        mainCameraTransform.position = position;
        targetPosition = position;

        var clampedZoom = Mathf.Clamp(zoom, minZoom, cachedMaxAllowedZoom);
        mainCamera.orthographicSize = clampedZoom;
        targetZoom = clampedZoom;

        needsPositionSmoothing = false;
        needsZoomSmoothing = false;
    }

    private void UpdateFollowTarget()
    {
        // 카메라 회전을 고려하여 타겟이 화면 중앙에 오도록 카메라 위치 계산
        var localOffset = mainCameraTransform.InverseTransformPoint(followTarget.position);
        var cameraTargetPos = mainCameraTransform.position + mainCameraTransform.rotation * new Vector3(localOffset.x, localOffset.y, 0f);

        var newPos = Vector3.Lerp(mainCameraTransform.position, cameraTargetPos, followSpeed * Time.deltaTime);
        mainCameraTransform.position = newPos;
        targetPosition = newPos;
    }

    #endregion

    #region Move Task

    public async UniTask ZoomAsync(float targetZoomValue, float duration)
    {
        if (isAutoMoving) return;

        isAutoMoving = true;

        var startZoom = mainCamera.orthographicSize;
        var clampedTargetZoom = Mathf.Clamp(targetZoomValue, minZoom, cachedMaxAllowedZoom);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);

            // EaseOutCubic for smooth deceleration
            var easeT = 1f - Mathf.Pow(1f - t, 3f);

            mainCamera.orthographicSize = Mathf.Lerp(startZoom, clampedTargetZoom, easeT);
            targetZoom = mainCamera.orthographicSize;

            await UniTask.Yield();
        }

        mainCamera.orthographicSize = clampedTargetZoom;
        targetZoom = clampedTargetZoom;

        isAutoMoving = false;
    }

    public async UniTask MoveAsync(Vector3 targetPos, float duration)
    {
        if (isAutoMoving) return;
        
        isAutoMoving = true;
        
        var startPosition = mainCameraTransform.position;
        var elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            
            // EaseOutCubic for smooth deceleration
            var easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            mainCameraTransform.position = Vector3.Lerp(startPosition, targetPos, easeT);
            targetPosition = mainCameraTransform.position;
            
            await UniTask.Yield();
        }
        
        mainCameraTransform.position = targetPos;
        targetPosition = targetPos;

        isAutoMoving = false;
    }

    public async UniTask ZoomAndMoveAsync(Vector3 worldTargetPos, float targetZoomValue, float duration)
    {
        isAutoMoving = true;

        var startZoom = mainCamera.orthographicSize;
        var startPosition = mainCameraTransform.position;
        var clampedTargetZoom = Mathf.Clamp(targetZoomValue, minZoom, cachedMaxAllowedZoom);

        // Calculate camera position to center the target (like followTarget)
        var localOffset = mainCameraTransform.InverseTransformPoint(worldTargetPos);
        var cameraTargetPos = mainCameraTransform.position + mainCameraTransform.rotation * new Vector3(localOffset.x, localOffset.y, 0f);

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var easeT = 1f - Mathf.Pow(1f - t, 3f);

            mainCamera.orthographicSize = Mathf.Lerp(startZoom, clampedTargetZoom, easeT);
            targetZoom = mainCamera.orthographicSize;

            mainCameraTransform.position = Vector3.Lerp(startPosition, cameraTargetPos, easeT);
            targetPosition = mainCameraTransform.position;

            await UniTask.Yield();
        }

        mainCamera.orthographicSize = clampedTargetZoom;
        targetZoom = clampedTargetZoom;
        mainCameraTransform.position = cameraTargetPos;
        targetPosition = cameraTargetPos;

        // 스무딩 플래그 초기화 (이전 드래그 상태가 남아있으면 카메라가 튕기는 문제 방지)
        needsPositionSmoothing = false;
        needsZoomSmoothing = false;

        isAutoMoving = false;
    }

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (mainCamera == null) return;

        InitializeMaxAllowedZoom();
        targetZoom = Mathf.Clamp(targetZoom, minZoom, cachedMaxAllowedZoom);
        needsZoomSmoothing = true;
        needsPositionSmoothing = true;
    }

    private void OnDrawGizmosSelected()
    {
        // 카메라 회전을 적용하여 바운더리 그리기 (고정된 월드 위치)
        var cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        var rotation = cameraTransform.rotation;
        
        // 바운더리 중심을 원점 기준으로 계산
        var boundaryCenterLocal = new Vector3(
            (boundaryMin.x + boundaryMax.x) * 0.5f,
            (boundaryMin.y + boundaryMax.y) * 0.5f,
            0f
        );
        
        // 바운더리 네 모서리 (로컬 좌표, 중심 기준)
        var halfWidth = (boundaryMax.x - boundaryMin.x) * 0.5f;
        var halfHeight = (boundaryMax.y - boundaryMin.y) * 0.5f;
        
        var corner1Local = new Vector3(-halfWidth, -halfHeight, 0f);
        var corner2Local = new Vector3(halfWidth, -halfHeight, 0f);
        var corner3Local = new Vector3(halfWidth, halfHeight, 0f);
        var corner4Local = new Vector3(-halfWidth, halfHeight, 0f);
        
        // 회전을 적용한 바운더리 중심 위치
        var boundaryWorldCenter = rotation * boundaryCenterLocal;
        
        // 회전을 적용한 월드 좌표
        var corner1World = boundaryWorldCenter + rotation * corner1Local;
        var corner2World = boundaryWorldCenter + rotation * corner2Local;
        var corner3World = boundaryWorldCenter + rotation * corner3Local;
        var corner4World = boundaryWorldCenter + rotation * corner4Local;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(corner1World, corner2World);
        Gizmos.DrawLine(corner2World, corner3World);
        Gizmos.DrawLine(corner3World, corner4World);
        Gizmos.DrawLine(corner4World, corner1World);
    }
#endif
}