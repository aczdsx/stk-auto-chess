using System;
using System.Collections;
using CookApps.AutoBattler;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class TargetLineRenderer : MonoBehaviour
{
    [SerializeField] private GameObject _arrowFx;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Color _ownColor;
    [SerializeField] private Color _otherColor;

    private TargetLineConfig _config;
    private Coroutine _currentCoroutine;
    private Material _cachedMaterial;

    private TargetLineConfig Config
    {
        get
        {
            if (_config == null)
                SoDataProvider.Instance.TryGet(out _config);
            return _config;
        }
    }

    public void DrawLine(CharacterController startCharacter, CharacterController targetCharacter, bool isOwn,
        Action onComplete = null)
    {
        var color = isOwn ? _ownColor : _otherColor;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;

        var yOffset = new Vector3(0, Config.CharacterYOffset, 0);
        Func<Vector3> getStart = () => startCharacter.Position3D + yOffset;
        Func<Vector3> getTarget = () => targetCharacter.Position3D + yOffset;

        if (gameObject.activeInHierarchy)
        {
            StartDrawCoroutine(DrawGuideLine(getStart, getTarget, onComplete));
        }
    }

    public void DrawLine(Vector3 startPos, Vector3 targetPos, Action onComplete = null)
    {
        _lineRenderer.startColor = _ownColor;
        _lineRenderer.endColor = _ownColor;

        StartDrawCoroutine(DrawGuideLine(() => startPos, () => targetPos, onComplete));
    }

    private void StartDrawCoroutine(IEnumerator routine)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(routine);
    }

    private Vector3 EvaluateArc(Vector3 start, Vector3 end, float t)
    {
        float arcHeight = Mathf.Sin(Mathf.PI * t) * Config.Height;
        return Vector3.Lerp(start, end, t) + new Vector3(0, arcHeight, 0);
    }

    private void UpdateLine(Vector3 start, Vector3 end, int count)
    {
        _lineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(count - 1, 1);
            _lineRenderer.SetPosition(i, EvaluateArc(start, end, t));
        }
    }

    private IEnumerator DrawGuideLine(Func<Vector3> getStart, Func<Vector3> getTarget, Action onComplete = null)
    {
        var config = Config;
        if (config == null)
        {
            Debug.LogWarning("[TargetLineRenderer] TargetLineConfig not loaded.");
            onComplete?.Invoke();
            _currentCoroutine = null;
            yield break;
        }

        _cachedMaterial ??= _lineRenderer.material;

        WaitForEndOfFrame waitTime = new WaitForEndOfFrame();

        _lineRenderer.positionCount = 0;
        _arrowFx.transform.position = EvaluateArc(getStart(), getTarget(), 0f);

        float time = 0f;

        while (time < config.LineDurationTime)
        {
            time += Time.unscaledDeltaTime;
            float value = time / config.LineDurationTime;

            float t = Mathf.Clamp01((value * config.PositionCount + config.Offset) / (config.PositionCount - 1f));

            var start = getStart();
            var end = getTarget();

            // 시작 ~ 현재 진행점까지만 라인 표시
            int visibleCount = Mathf.Max((int)(t * config.PositionCount), 2);
            UpdateLine(start, end, visibleCount);

            var currentPos = EvaluateArc(start, end, t);
            var prevPos = EvaluateArc(start, end, Mathf.Max(t - 0.01f, 0f));
            var dir = currentPos - prevPos;
            if (dir.sqrMagnitude > 0.0001f)
                _arrowFx.transform.rotation = Quaternion.LookRotation(dir);

            _arrowFx.transform.position = currentPos;

            // UV offset 스크롤
            _cachedMaterial.mainTextureOffset = new Vector2(time * config.ScrollSpeed, 0f);

            yield return waitTime;
        }

        _cachedMaterial.mainTextureOffset = Vector2.zero;
        _lineRenderer.positionCount = 0;
        _currentCoroutine = null;
        onComplete?.Invoke();
    }
}
