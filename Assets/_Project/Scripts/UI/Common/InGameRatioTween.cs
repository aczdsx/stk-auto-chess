using DG.Tweening;
using TMPro;
using UnityEngine;

public class InGameRatioTween : MonoBehaviour
{
    public TMP_Text damageRatio;
    public Color defaultColor;
    public Color damageColor;

    public void DamageFXTween()
    {
            damageRatio.DOColor(damageColor, 0.1f).SetEase(Ease.OutQuad);
            damageRatio.DOColor(defaultColor, 0.1f).SetEase(Ease.InQuad).SetDelay(0.08f);
            damageRatio.transform.DOScale(1.3f, 0.08f).SetEase(Ease.OutQuad);
            damageRatio.transform.DOScale(1f, 0.08f).SetEase(Ease.InQuad).SetDelay(0.08f);
    }
    

}
