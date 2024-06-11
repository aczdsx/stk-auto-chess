using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class InGameRatioTween : MonoBehaviour
{
    public TMP_Text damageRatio;
    public Color defaultColor;
    public Color damageColor;

    public void DamageFXTween()
    {
            damageRatio.DOColor(damageColor, 0.2f).SetEase(Ease.OutQuad);
            damageRatio.DOColor(defaultColor, 0.2f).SetEase(Ease.OutQuad).SetDelay(0.2f);
            damageRatio.transform.DOScale(1.4f, 0.2f).SetEase(Ease.OutQuad);
            damageRatio.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad).SetDelay(0.2f);
    }
    

}
