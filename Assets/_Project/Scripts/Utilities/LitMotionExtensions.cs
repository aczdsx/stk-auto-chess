using System;
using LitMotion.Adapters;
using UnityEngine;

namespace LitMotion.Animation.Components
{
    public abstract class RectTransformAnchoredPositionAnimationBase<TOptions, TAdapter>
        : PropertyAnimationComponent<RectTransform, Vector2, TOptions, TAdapter>
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Vector2, TOptions>
    {
        protected override Vector2 GetValue(RectTransform target)
        {
            return target.anchoredPosition;
        }

        protected override void SetValue(RectTransform target, in Vector2 value)
        {
            target.anchoredPosition = value;
        }

        protected override Vector2 GetRelativeValue(in Vector2 startValue, in Vector2 relativeValue)
        {
            return startValue + relativeValue;
        }
    }

    [Serializable]
    [LitMotionAnimationComponentMenu("UI/Rect Transform/Anchored Position")]
    public sealed class RectTransformAnchoredPositionAnimation
        : RectTransformAnchoredPositionAnimationBase<NoOptions, Vector2MotionAdapter> { }

    [Serializable]
    [LitMotionAnimationComponentMenu("UI/Rect Transform/Anchored Position (Punch)")]
    public sealed class RectTransformAnchoredPositionPunchAnimation
        : RectTransformAnchoredPositionAnimationBase<PunchOptions, Vector2PunchMotionAdapter> { }

    [Serializable]
    [LitMotionAnimationComponentMenu("UI/Rect Transform/Anchored Position (Shake)")]
    public sealed class RectTransformAnchoredPositionShakeAnimation
        : RectTransformAnchoredPositionAnimationBase<ShakeOptions, Vector2ShakeMotionAdapter> { }
}
