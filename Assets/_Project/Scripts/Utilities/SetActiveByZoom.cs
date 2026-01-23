using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;

public class SetActiveByZoom : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float targetRatio;
    [SerializeField] private bool setOnOverRatio;

    private void LateUpdate()
    {
        var cameraController = MainCameraHolder.CameraGestureController;
        if (cameraController == null)
            return;

        var zoomRatio = cameraController.ZoomRatio;
        var isOverRatio = zoomRatio >= targetRatio;

        // setOnOverRatio가 false: 줌 값이 targetRatio를 넘으면 끄고, 낮으면 킴
        // setOnOverRatio가 true: 줌 값이 targetRatio를 넘으면 키고, 낮으면 끔
        var shouldBeActive = setOnOverRatio ? isOverRatio : !isOverRatio;

        if (target.activeSelf != shouldBeActive)
        {
            target.SetActive(shouldBeActive);
        }
    }
}