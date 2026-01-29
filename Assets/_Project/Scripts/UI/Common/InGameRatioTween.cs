using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

public class InGameRatioTween : MonoBehaviour
{
    public TMP_Text damageRatio;
    public Color defaultColor;
    public Color damageColor;

    public void DamageFXTween()
    {
            LMotion.Create(damageRatio.color, damageColor, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToColor(damageRatio)
                .AddTo(this);
            LMotion.Create(damageRatio.color, defaultColor, 0.1f)
                .WithEase(Ease.InQuad)
                .WithDelay(0.08f)
                .BindToColor(damageRatio)
                .AddTo(this);
            LMotion.Create(damageRatio.transform.localScale, Vector3.one * 1.3f, 0.08f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(damageRatio.transform)
                .AddTo(this);
            LMotion.Create(damageRatio.transform.localScale, Vector3.one, 0.08f)
                .WithEase(Ease.InQuad)
                .WithDelay(0.08f)
                .BindToLocalScale(damageRatio.transform)
                .AddTo(this);
    }


}
