using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using PrimeTween;

public class InGameCamera : CachedMonoBehaviour, IRegistrable
{
    [SerializeField]
    private GameObject _rootObj;

    [SerializeField]
    private Camera _mainCamera;

    [SerializeField]
    private Camera _characterCamera;

    private CancellationTokenSource _cancellationTokenSource;

    public Camera MainCamera => _mainCamera;
    public Camera CharacterCamera => _characterCamera;
    
    private void Awake()
    {
        ObjectRegistry.Register(this);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        ObjectRegistry.Unregister(this);
    }

    public void ShakeCamera(float durationTime, float magnitude)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        Shake(durationTime, magnitude, _cancellationTokenSource.Token).Forget();
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
        float startSize = _mainCamera.orthographicSize;
        Vector3 startPos = _mainCamera.transform.position;

        var sizeTween = Tween.Custom(startSize, targetSize, duration,
            (float newSize) =>
            {
                _characterCamera.orthographicSize = newSize;
                _mainCamera.orthographicSize = newSize;
            },
            ease: Ease.OutQuad);

        var positionTween = Tween.Custom(startPos, targetPosition, duration,
            (Vector3 newPosition) =>
            {
                _mainCamera.transform.position = newPosition;
            },
            ease: Ease.OutQuad).OnComplete(this, _ =>
        {
            _characterCamera.orthographicSize = targetSize;
            _mainCamera.orthographicSize = targetSize;

            _mainCamera.transform.position = targetPosition;
        });

        await UniTask.WhenAll(sizeTween.ToUniTask(), positionTween.ToUniTask());

        await UniTask.WaitUntil(() =>
        {
            if (_mainCamera == null)
                return true;
            
            return Mathf.Approximately(_mainCamera.orthographicSize, targetSize) &&
                   Mathf.Approximately(_mainCamera.transform.position.y, targetPosition.y);
        });
    }
    public void SetForceCameraRotation(Vector3 targetRotation)
    {
        _mainCamera.transform.rotation = Quaternion.Euler(targetRotation);
    }

    private async UniTaskVoid Shake(float duration, float magnitude, CancellationToken cancellationToken)
    {
        Vector3 originalPos = _rootObj.transform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _rootObj.transform.localPosition = originalPos;
                return;
            }

            float x = originalPos.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * magnitude;

            _rootObj.transform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        _rootObj.transform.localPosition = originalPos;
    }

    public RegistryKey Key => RegistryKey.InGameCamera;
}
