using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class InGameCamera : MonoBehaviour
{
    [SerializeField]
    private GameObject _rootObj;

    private CancellationTokenSource _cancellationTokenSource;

    public void ShakeCamera(float durationTime, float magnitude)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        Shake(durationTime, magnitude, _cancellationTokenSource.Token).Forget();
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
