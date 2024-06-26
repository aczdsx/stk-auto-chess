using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using PrimeTween;

public class InGameCamera : MonoBehaviour
{
    [SerializeField]
    private GameObject _rootObj;

    [SerializeField]
    private Camera _mainCamera;

    [SerializeField]
    private Camera _characterCamera;

    private CancellationTokenSource _cancellationTokenSource;

    public void ShakeCamera(float durationTime, float magnitude)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        Shake(durationTime, magnitude, _cancellationTokenSource.Token).Forget();
    }

    public async UniTask SetCameraSize(float targetSize, float duration)
    {
        float startSize = _mainCamera.orthographicSize;

        Tween.Custom(startSize, targetSize, duration,
            (float newSize) =>
            {
                _characterCamera.orthographicSize = newSize;
                _mainCamera.orthographicSize = newSize;
            },
            ease: Ease.OutQuad).OnComplete(this, _ =>
        {
            _characterCamera.orthographicSize = targetSize;
            _mainCamera.orthographicSize = targetSize;
        });

        await UniTask.WaitUntil(() => Mathf.Approximately(_mainCamera.orthographicSize, targetSize));
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

            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            _rootObj.transform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        _rootObj.transform.localPosition = originalPos;
    }
}
