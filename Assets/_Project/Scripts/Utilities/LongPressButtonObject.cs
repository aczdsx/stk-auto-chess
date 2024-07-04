using UnityEngine;
using UnityEngine.EventSystems;

public class LongPressButtonObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float longPressDuration = 1.0f; // 롱탭으로 간주되는 시간 (초)
    private bool isPressing = false;
    private float pressTime;

    // 롱탭 동작 시 실행할 함수
    public void OnLongPress()
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        pressTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
    }

    void Update()
    {
        if (isPressing && (Time.time - pressTime) >= longPressDuration)
        {
            isPressing = false;
            OnLongPress();
        }
    }
}
