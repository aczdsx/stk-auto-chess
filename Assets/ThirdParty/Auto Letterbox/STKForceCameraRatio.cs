using System.Collections;
using System.Collections.Generic;
using AutoLetterbox;
using Unity.VisualScripting;
using UnityEngine;

public class STKForceCameraRatio : MonoBehaviour
{
    void Awake()
    {
        // 화면 비율이 16:9보다 클 경우 화면비를 조정한다 (ex. 아이패드)
        float defRatio = 16f / 9f;
        float width = Screen.currentResolution.width;
        float height = Screen.currentResolution.height;
        float currentRatio = width / height;
        if (defRatio > currentRatio)
        {
            var item = gameObject.GetOrAddComponent<ForceCameraRatio>(); // 필요할 때만 추가
            item.ratio = new Vector2(16, 9);
        }

        // 화면 비율이 2:1보다 길쭉할 경우 (ex. 갤럭시 폴드 외부화면)
        defRatio = 2f / 1f;
        if (defRatio < currentRatio)
        {
            var item = gameObject.GetOrAddComponent<ForceCameraRatio>(); // 필요할 때만 추가
            item.ratio = new Vector2(2, 1);
        }
    }
}