using CookApps.TeamBattle.Utility;
using UnityEngine;

/// <summary>
/// [Camera] 카메라 드래그 이동 · 핀치 줌
/// </summary>
public partial class InGameTouchManager
{
    private void CameraMove(bool isPointerOverUI)
    {
        Vector3 initialPosition = Vector3.zero;
        Vector3 currentPosition = Vector3.zero;
        bool isInputBegan = false;
        bool isInputMoved = false;
        float distanceFactor = -0.01f;
        // 줌 후 쿨다운 타이머 감소
        if (_zoomCooldownTimer > 0)
        {
            _zoomCooldownTimer -= Time.deltaTime;
        }

        if (!Application.isEditor)
        {
            if (Input.touchCount == 2)
            {
                HandleZoom(Input.GetTouch(0), Input.GetTouch(1));
                _zoomCooldownTimer = _zoomCooldown;
                return;
            }
            else if (Input.touchCount == 1 && _zoomCooldownTimer <= 0)
            {
                Touch touch = Input.GetTouch(0);
                initialPosition = touch.position;
                currentPosition = touch.position;
                isInputBegan = touch.phase == TouchPhase.Began;
                isInputMoved = touch.phase == TouchPhase.Moved;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                initialPosition = Input.mousePosition;
                isInputBegan = true;
            }

            if (Input.GetMouseButton(0))
            {
                currentPosition = Input.mousePosition;
                isInputMoved = true;
            }
        }

        if (_zoomCooldownTimer <= 0)
        {
            if (isInputBegan)
            {
                _initialFingersPosition = initialPosition;
                _initialCameraPosition = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraTransform().position;
            }
            else if (isInputMoved && !isPointerOverUI)
            {
                Vector2 direction = (currentPosition - _initialFingersPosition).normalized;
                float cameraSize = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraSize();
                float normalizedSize = (2.0f - (cameraSize - _cameraMinSize) / (_cameraMaxSize - _cameraMinSize)) * 0.3f;
                float distance = Vector2.Distance(currentPosition, _initialFingersPosition) * distanceFactor *
                                 normalizedSize;

                Vector2 distancePosition;
                distancePosition.x = direction.x * distance;
                distancePosition.y = direction.y * distance;

                Vector3 newCameraPosition = new Vector3(
                    Mathf.Clamp(_initialCameraPosition.x + distancePosition.x, -2, 2),
                    Mathf.Clamp(_initialCameraPosition.y + distancePosition.y, -2, 4),
                    Mathf.Clamp(_initialCameraPosition.z - distancePosition.x, -12, -8)
                );

                ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPosition(newCameraPosition);
            }
        }
    }

    private void HandleZoom(Touch touch1, Touch touch2)
    {
        if (touch2.phase == TouchPhase.Began)
        {
            _initialFingersDistance = Vector2.Distance(touch1.position, touch2.position);
            _initialCameraSize = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).GetCameraSize();
        }
        else if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
        {
            var currentFingersDistance = Vector2.Distance(touch1.position, touch2.position);
            var scaleFactor = _initialFingersDistance / currentFingersDistance;

            float size = _initialCameraSize * scaleFactor;
            size = Mathf.Clamp(size, _cameraMinSize, _cameraMaxSize);
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(size);
        }
    }
}
