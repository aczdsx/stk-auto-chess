using UnityEngine;

public class SimpleBackground : MonoBehaviour
{
    public Transform targetCamera;

    // 1.0이면 화면에 완전 고정 (스타2 방식)
    // 0.99이면 아주 미세하게 카메라보다 늦게 따라옴 (거리감 극대화)
    [Range(0.9f, 1.0f)]
    public float followSpeed = 1.0f;

    private Vector3 offset;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main.transform;

        // 시작 시점의 카메라와 행성 사이의 거리를 딱 한 번 저장
        offset = transform.position - targetCamera.position;
    }

    void LateUpdate()
    {
        // 카메라 위치에 오프셋을 더해 위치를 잡되, 
        // followSpeed를 곱해 아주 미세한 차이를 만듭니다.
        transform.position = (targetCamera.position * followSpeed) + offset;
    }
}