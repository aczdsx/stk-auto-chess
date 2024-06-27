using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaView : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _lastSafeAreaSize = Vector2.zero;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // If the safe area hasn't changed, no need to reapply
        if (_lastSafeAreaSize == new Vector2(safeArea.width, safeArea.height))
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Set the anchorMin and anchorMax to RectTransform directly to avoid parent influence
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        _lastSafeAreaSize = new Vector2(safeArea.width, safeArea.height);
    }

    void Update()
    {
        // Check if the safe area has changed (e.g., device rotation)
        if (_lastSafeAreaSize != new Vector2(Screen.safeArea.width, Screen.safeArea.height))
        {
            ApplySafeArea();
        }
    }
}
