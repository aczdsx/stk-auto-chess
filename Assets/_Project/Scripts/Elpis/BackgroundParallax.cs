using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;

public class BackgroundParallax : CachedMonoBehaviour
{
    private Transform targetCamera;
    private Camera _camera;

    [Range(0.5f, 1.0f)]
    public float followSpeed = 1.0f;

    [Tooltip("카메라 줌에 따른 스케일 조절 배율 (0 = 스케일 고정, 1 = 줌과 동일하게 스케일)")]
    [Range(0f, 1.0f)]
    public float scaleSpeed = 1.0f;

    private Vector3 offset;
    private Vector3 initialScale;
    private float initialOrthoSize;

    private void Awake()
    {
        _camera = MainCameraHolder.MainCamera;
        targetCamera = _camera.transform;
        offset = CachedTr.position - targetCamera.position;
        initialScale = CachedTr.localScale;
        initialOrthoSize = _camera.orthographicSize;
    }

    private void LateUpdate()
    {
        float zoomRatio = _camera.orthographicSize / initialOrthoSize;
        float scaleRatio = Mathf.Lerp(1f, zoomRatio, scaleSpeed);

        // 스케일 배율에 따라 위치 오프셋도 조절
        CachedTr.position = targetCamera.position * followSpeed + offset * scaleRatio;

        // 카메라 줌에 따른 스케일 조절
        if (scaleSpeed > 0f)
        {
            CachedTr.localScale = initialScale * scaleRatio;
        }
    }
}