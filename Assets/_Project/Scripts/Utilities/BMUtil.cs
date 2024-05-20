using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BMUtil
{
    // 해당 트랜스폼의 하위 개체 삭제
    public static void RemoveChildObjects(Transform targetTransform)
    {
        for (int i = 0; i < targetTransform.childCount; i++)
        {
            UnityEngine.Object.Destroy(targetTransform.GetChild(i).gameObject);
        }
    }
}
