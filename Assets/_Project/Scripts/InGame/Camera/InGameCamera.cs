using System.Threading;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class InGameCamera : CachedMonoBehaviour, IRegistrable
{
    [SerializeField]
    private GameObject _rootObj;

    [SerializeField]
    private Camera _mainCamera;

    [SerializeField]
    private Camera _characterCamera;

    private CancellationTokenSource _cancellationTokenSource;

    private MotionHandle _sizeHandle;
    private MotionHandle _positionHandle;

    private bool _isCameraShaking = false;

    private Vector3 _originalLocalPos;

    public Camera MainCamera => _mainCamera;
    public Camera CharacterCamera => _characterCamera;

    public enum CameraPositionMode
    {
        Default = 0,
        LobbyCombat = 1,
        LargeSize = 2,
        DefaultCombat = 3,
        LargeSizeCombat = 4,
    }

    private void Awake()
    {
        ObjectRegistry.Register(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _sizeHandle.TryCancel();
        _sizeHandle = default;
        _positionHandle.TryCancel();
        _positionHandle = default;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        ObjectRegistry.Unregister(this);
    }

    public bool CheckCameraShaking() => _isCameraShaking;

    public void ShakeCamera(float durationTime, float magnitude)
    {
        if (_isCameraShaking) return;

        _originalLocalPos = _rootObj.transform.localPosition;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _isCameraShaking = true;
        Shake(durationTime, magnitude, _cancellationTokenSource.Token).Forget();
    }

    public void StopShakingCamera()
    {
        if (!_isCameraShaking) return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _rootObj.transform.localPosition = _originalLocalPos;
        _isCameraShaking = false;
    }

    public float GetCameraSize()
    {
        return _mainCamera.orthographicSize;
    }

    public void SetCameraSize(float size)
    {
        _mainCamera.orthographicSize = size;
        _characterCamera.orthographicSize = size;
    }

    public Transform GetCameraTransform()
    {
        return _mainCamera.transform;
    }

    public void SetCameraPosition(Vector3 position)
    {
        _mainCamera.transform.position = position;
    }

    public async UniTask SetCameraSize(float targetSize, Vector3 targetPosition, float duration)
    {
        // 기존 Tween 정리
        _sizeHandle.TryCancel();
        _positionHandle.TryCancel();

        float startSize = _mainCamera.orthographicSize;
        Vector3 startPos = _mainCamera.transform.position;

        _sizeHandle = LMotion.Create(startSize, targetSize, duration)
            .WithEase(Ease.OutQuad)
            .Bind(newSize =>
            {
                _characterCamera.orthographicSize = newSize;
                _mainCamera.orthographicSize = newSize;
            })
            .AddTo(this);

        _positionHandle = LMotion.Create(startPos, targetPosition, duration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() =>
            {
                _characterCamera.orthographicSize = targetSize;
                _mainCamera.orthographicSize = targetSize;

                _mainCamera.transform.position = targetPosition;
            })
            .Bind(newPosition =>
            {
                _mainCamera.transform.position = newPosition;
            })
            .AddTo(this);

        await UniTask.WhenAll(_sizeHandle.ToUniTask(), _positionHandle.ToUniTask());

        await UniTask.WaitUntil(() =>
        {
            if (_mainCamera == null)
                return true;

            return Mathf.Approximately(_mainCamera.orthographicSize, targetSize) &&
                   Mathf.Approximately(_mainCamera.transform.position.y, targetPosition.y);
        });
    }

    public void SetCameraPositionMode(CameraPositionMode mode)
    {
        switch (mode)
        {
            case CameraPositionMode.Default:
                SetCameraSize(8.0f, new Vector3(-15.0f, 9.5f, -12f), 1.0f).Forget();
                break;
            case CameraPositionMode.LobbyCombat:
                SetCameraSize(7.5f, new Vector3(0, 2.0f, -10), 1.0f).Forget();
                break;
            case CameraPositionMode.LargeSize:
                SetCameraSize(7.5f, new Vector3(-17.0f, 11.0f, -14f), 1.0f).Forget();
                break;
            case CameraPositionMode.DefaultCombat:
                SetCameraSize(5.0f, new Vector3(-15.0f, 10.8f, -12f), 1.5f).Forget();
                break;
            case CameraPositionMode.LargeSizeCombat:
                SetCameraSize(7.0f, new Vector3(-17.0f, 12.8f, -14f), 1.5f).Forget();
                break;
        }
    }

    public void SetForceCameraRotation(Vector3 targetRotation)
    {
        _mainCamera.transform.rotation = Quaternion.Euler(targetRotation);
    }

    private async UniTaskVoid Shake(float duration, float magnitude, CancellationToken cancellationToken)
    {

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _rootObj.transform.localPosition = _originalLocalPos;
                return;
            }

            float x = _originalLocalPos.x + Random.Range(-1f, 1f) * magnitude;
            float y = _originalLocalPos.y + Random.Range(-1f, 1f) * magnitude;

            _rootObj.transform.localPosition = new Vector3(x, y, _originalLocalPos.z);

            elapsed += Time.deltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        _rootObj.transform.localPosition = _originalLocalPos;
        _isCameraShaking = false;
    }

    public RegistryKey Key => RegistryKey.InGameCamera;
}
