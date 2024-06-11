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
    public Sequence DamageTween()
    {
        return DOTween.Sequence()
            .Join(damageRatio.DOColor(damageColor, 0.2f).SetEase(Ease.OutQuad))
            .Join(damageRatio.transform.DOScale(1.14f, 0.2f).SetEase(Ease.OutQuad))
            .Append(damageRatio.DOColor(defaultColor, 0.2f).SetEase(Ease.InQuad))
            .Append(damageRatio.transform.DOScale(1f, 0.2f).SetEase(Ease.InQuad));     
    }


}
