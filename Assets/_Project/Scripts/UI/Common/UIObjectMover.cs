using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Serialization;

public class UIObjectMover : MonoBehaviour
{
    private Canvas _canvas;
    private InGameTile _startTile;
    private InGameTile _endTile;

    private float timeElapsed = 0.0f;
    private bool movingForward = true;

    [SerializeField] private AnimationCurve forwardCurve;
    [SerializeField] private AnimationCurve backwardCurve;
    [SerializeField] private float forwardDuration = 1.0f;
    [SerializeField] private float backwardDuration = 0.4f;

    public void SetMover(InGameTile startTile, InGameTile endTile)
    {
        _startTile = startTile;
        _endTile = endTile;
        _canvas = SceneUILayerManager.Instance.MainCanvas;
    }

    void Update()
    {
        if (_startTile != null && _endTile != null)
        {
            if(_startTile.OccupiedCharacter == null)
            {
                this.gameObject.SetActive(false);
                return;
            }
            
            timeElapsed += Time.deltaTime;

            float currentDuration = movingForward ? forwardDuration : backwardDuration;

            if (timeElapsed > currentDuration)
            {
                timeElapsed = 0.0f;
                movingForward = !movingForward;
            }

            float t = timeElapsed / currentDuration;
            float curvedT = movingForward ? forwardCurve.Evaluate(t) : backwardCurve.Evaluate(t);

            Vector3 targetPosition = Vector3.Lerp(_startTile.View.Position, _endTile.View.Position, curvedT);

            Vector2 screenPoint = Camera.main.WorldToScreenPoint(targetPosition);

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                this.gameObject.transform.position = screenPoint;
            }
            else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera || _canvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, screenPoint,
                    _canvas.worldCamera, out Vector2 localPoint);
                this.gameObject.transform.localPosition = localPoint;
            }
        }
    }
}