using CookApps.AutoBattler;
using UnityEngine;

public class SetActiveByZoom : MonoBehaviour
{
    [SerializeField] private ParticleSystemRenderer target;
    [SerializeField] private float targetRatio;
    [SerializeField] private bool setOnOverRatio;
    [SerializeField] private float fadeSpeed = 5f;

    private float currentAlpha = 1f;
    private Color originalColor;
    private Material material;

    private void Start()
    {
        if (target != null)
        {
            material = target.material;
            if (material != null)
            {
                originalColor = material.color;
                currentAlpha = originalColor.a;
            }
        }
    }

    private void LateUpdate()
    {
        var cameraController = MainCameraHolder.CameraGestureController;
        if (cameraController == null || material == null)
            return;

        var zoomRatio = cameraController.ZoomRatio;
        var isOverRatio = zoomRatio >= targetRatio;

        // setOnOverRatio가 false: 줌 값이 targetRatio를 넘으면 끄고(0), 낮으면 킴(1)
        // setOnOverRatio가 true: 줌 값이 targetRatio를 넘으면 키고(1), 낮으면 끔(0)
        var targetAlpha = setOnOverRatio ? (isOverRatio ? 1f : 0f) : (isOverRatio ? 0f : 1f);

        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            var newColor = originalColor;
            newColor.a = currentAlpha;
            material.color = newColor;
        }
    }
}