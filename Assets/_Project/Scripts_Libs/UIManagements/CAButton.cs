using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;


namespace CookApps.TeamBattle.UIManagements
{
    public enum DefaultClickSoundType
    {
        None = -1,
        Basic,
        Custom_0,
        Custom_1,
        Custom_2,
    }

    public enum ReactionType
    {
        None = 0,
        Jelly = 1,
        Punch = 11,
        Punch_Small = 12,
        Custom = 13,
    }
    
    [AddComponentMenu("UI/CA Button")]
    public class CAButton : Button
    {
        [SerializeField] private bool isBlockDrag = false;
        [SerializeField] private bool useDefaultClickSound = true;
        [SerializeField] private DefaultClickSoundType defaultClickSoundType;
        [SerializeField] private ReactionType reactionType = ReactionType.None;
        public static event Action<DefaultClickSoundType> OnPlayDefaultClickSound;

        private Vector3 initialScale;
        private SimpleTweener tweenScaleX = new SimpleTweener();
        private TweenLerp<float> tweenScaleXValue;
        private SimpleTweener tweenScaleY = new SimpleTweener();
        private TweenLerp<float> tweenScaleYValue;
        private bool isPressed = false;

        protected override void Start()
        {
            base.Start();
            initialScale = this.transform.localScale;
        }
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
                return;

            // 버튼을 누르고 있는 동안 축소
            if (reactionType == ReactionType.Punch || reactionType == ReactionType.Punch_Small)
            {
                StopAllCoroutines(); // 기존 애니메이션 중단
                isPressed = true;
                StartCoroutine(TweenScale(new Vector3(0.95f, 0.95f, 1f), 0.1f));  // 버튼을 눌렀을 때 크기를 축소
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
                return;

            if (reactionType == ReactionType.Punch || reactionType == ReactionType.Punch_Small)
            {
                // 버튼을 뗄 때 Punch 효과
                isPressed = false;
                StartCoroutine(PunchEffect());
            }
        }

        private IEnumerator PunchEffect()
        {
            // 눌렀을 때 축소되었던 것을 되돌린 후 Punch 효과 적용
            yield return TweenScale(new Vector3(1.05f, 1.05f, 1f), 0.1f);  // 팡팡 튀는 효과
            yield return TweenScale(initialScale, 0.1f);  // 원래 크기로 복귀
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
            {
                return;
            }

            SelectableBlockerManager.Instance.OnClicked(gameObject.name);
            if (useDefaultClickSound)
            {
                OnPlayDefaultClickSound?.Invoke(defaultClickSoundType);
            }

            // ReactionType에 따른 애니메이션 실행
            if (reactionType != ReactionType.None)
            {
                switch (reactionType)
                {
                    case ReactionType.Jelly:
                        StartCoroutine(JellyTween());
                        break;
                    case ReactionType.Punch:
                        StartCoroutine(PunchTween());
                        break;
                    case ReactionType.Punch_Small:
                        StartCoroutine(PunchSmallTween());
                        break;
                }
            }

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
            {
                return;
            }

            SelectableBlockerManager.Instance.OnClicked(gameObject.name);
            if (useDefaultClickSound)
            {
                OnPlayDefaultClickSound?.Invoke(defaultClickSoundType);
            }

            base.OnSubmit(eventData);
        }
        
        private IEnumerator JellyTween()
        {
            yield return TweenScale(new Vector3(1.248f, 0.904f, 1f), 0.15f);
            yield return TweenScale(new Vector3(0.92f, 1.12f, 1f), 0.15f);
            BackToNormal();
        }

        private IEnumerator PunchTween()
        {
            yield return TweenScale(new Vector3(1.08f, 1.08f, 1f), 0.15f);
            BackToNormal();
        }

        private IEnumerator PunchSmallTween()
        {
            yield return TweenScale(new Vector3(1.05f, 1.05f, 1f), 0.15f);
            BackToNormal();
        }

        private IEnumerator TweenScale(Vector3 targetScale, float duration)
        {
            Vector3 startScale = this.transform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                this.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            this.transform.localScale = targetScale;
        }

        private void BackToNormal()
        {
            StartCoroutine(TweenScale(initialScale, 0.2f));
        }
    }

    public class SimpleTweener
    {
        public enum TweenLoopType
        {
            Default,
            Yoyo // Tweens to the end values then back to the original ones and so on (X to Y, Y to X, repeat).
        }

        public float duration { get; private set; }
        public float elapsed { get; private set; }

        public SimpleTweener()
        {
            this.duration = 0.0f;

            this.LastLerp = 0.0f;
            this.elapsed = 0.0f;
            this.delay = 0.0f;

            this.Reverse = false;
            this.SwapStartEnd = false;
            this.tweenCount = 1;
            this.loopType = TweenLoopType.Default;

            this.easing = null;
        }

        public void Reset(float d, EasingObject.EasingPosition easingfn = null, float overshoot_amplitude = 0.0f,
            float period = 0.0f)
        {
            this.duration = d;

            this.LastLerp = 0.0f;
            this.elapsed = 0.0f;
            this.delay = 0.0f;

            this.Reverse = false;
            this.SwapStartEnd = false;
            this.tweenCount = 1;
            this.loopType = TweenLoopType.Default;

            this.easing = (easingfn == null) ? EasingObject.LinearEasing : easingfn;
            this.easing_overshoot_amplitude = overshoot_amplitude;
            this.easing_period = period;
        }

        public void Shake(float d, float amplitude = 0.1f, float period = 0.12f)
        {
            this.Reset(d, EasingObject.ElasticEasingOut, amplitude, period);
            this.SwapStartEnd = true;
        }

        public void LoopType(TweenLoopType type, int count = 0)
        {
            this.loopType = type;
            if (count <= 0)
            {
//            if (this.loopType == TweenLoopType.Yoyo)
                count = int.MaxValue;
//            else
//                count = 1;
            }

            this.tweenCount = count;
        }

        public void Delay(float delay)
        {
            this.delay = delay;
        }

        public bool Reverse { get; set; }
        public bool SwapStartEnd { get; set; }

        public float Normalized
        {
            get { return (this.duration == 0.0f) ? 1.0f : this.elapsed / this.duration; }
        }

        public virtual bool Completed
        {
            get
            {
                if (this.duration > 0.0f)
                {
                    if (this.elapsed > this.duration)
                        this.Kill();
                    else
                        return false;
                }

                return this.duration == 0.0f;
            }
        }

        public bool Playing
        {
            get { return this.duration > 0.0f; }
        }

        public float LastLerp { get; private set; }

        public virtual void Update(float deltaTime)
        {
            if (this.Completed)
                return;

            if (this.delay > 0.0f)
            {
                if (this.delay > deltaTime)
                {
                    this.delay -= deltaTime;
                    return;
                }

                deltaTime -= this.delay;
                this.delay = 0.0f;
            }

            this.elapsed += deltaTime;
            if (this.elapsed > this.duration)
                this.elapsed = this.duration;

            this.LastLerp = this.easing(0.0f, 1.0f, this.Reverse ? (this.duration - this.elapsed) : this.elapsed,
                this.duration, this.easing_overshoot_amplitude, this.easing_period);

            if (this.elapsed >= this.duration)
            {
                this.tweenCount--;
                if (this.tweenCount == 0)
                    this.elapsed = float.MaxValue;
                else
                {
                    this.elapsed = 0.0f;
                    if (this.loopType == TweenLoopType.Yoyo)
                        this.Reverse = !this.Reverse;
                }
            }
        }

        public void Kill()
        {
            this.duration = 0.0f;
        }

        public virtual void ForceComplete()
        {
            this.duration = 0.0f;
        }

        public TweenLerp<int> CreateTween(int start, int end)
        {
            return new TweenLerp<int>(start, end, SimpleTweener.IntLerp, this);
        }

        public TweenLerp<float> CreateTween(float start, float end)
        {
            return new TweenLerp<float>(start, end, SimpleTweener.FloatLerp, this);
        }

        public TweenLerp<Vector3> CreateTween(Vector3 start, Vector3 end)
        {
            return new TweenLerp<Vector3>(start, end, SimpleTweener.Vector3Lerp, this);
        }

        public TweenLerp<Vector2> CreateTween(Vector2 start, Vector2 end)
        {
            return new TweenLerp<Vector2>(start, end, SimpleTweener.Vector2Lerp, this);
        }

        public TweenLerp<Quaternion> CreateTween(Quaternion start, Quaternion end)
        {
            return new TweenLerp<Quaternion>(start, end, Quaternion.Lerp, this);
        }

        public TweenLerp<Color> CreateTween(Color start, Color end)
        {
            return new TweenLerp<Color>(start, end, SimpleTweener.ColorLerp, this);
        }

        public TweenLerp<TYPE> CreateTween<TYPE>(TYPE start, TYPE end, TweenLerp<TYPE>.Lerp lerpfn)
        {
            return new TweenLerp<TYPE>(start, end, lerpfn, this);
        }

        public static float FloatLerp(float start, float end, float t)
        {
            return start + (end - start) * t;
        }

        public static int IntLerp(int start, int end, float t)
        {
            return start + (int) ((float) (end - start) * t);
        }

        public static Vector3 Vector3Lerp(Vector3 start, Vector3 end, float t)
        {
            return start + (end - start) * t;
        }

        public static Vector2 Vector2Lerp(Vector2 start, Vector2 end, float t)
        {
            return start + (end - start) * t;
        }

        public static Color ColorLerp(Color start, Color end, float t)
        {
            return start + (end - start) * t;
        }

        /////////////////////////////////////////////////////////////////
        // private
        private float delay;

        private int tweenCount;
        private TweenLoopType loopType;

        private EasingObject.EasingPosition easing;
        private float easing_overshoot_amplitude;
        private float easing_period;
    }


    public struct TweenLerp<T>
    {
        public delegate T Lerp(T start, T end, float t);

        public bool enabled;
        public T start;
        public T end;

        private Lerp lerp;
        private SimpleTweener tweener;

        public TweenLerp(T s, T e, Lerp l, SimpleTweener tweener)
        {
            this.enabled = true;
            this.start = s;
            this.end = e;

            this.tweener = tweener;
            this.lerp = l;
        }

        public T Start
        {
            get { return this.start; }
        }

        public T End
        {
            get { return this.end; }
        }

        public T Value
        {
            get
            {
                return this.tweener.SwapStartEnd
                    ? this.lerp(this.end, this.start, this.tweener.LastLerp)
                    : this.lerp(this.start, this.end, this.tweener.LastLerp);
            }
        }

        public SimpleTweener Tweener
        {
            get { return this.tweener; }
        }
    }

    public class EasingObject
    {
        private const float TWO_PI = Mathf.PI * 2.0f;

        public static Dictionary<EasingPosition, float[]> DEF_PARAM = new Dictionary<EasingPosition, float[]>()
        {
            {EasingObject.ElasticEasingIn, new float[2] {0.1f, 0.12f}},
            {EasingObject.ElasticEasingOut, new float[2] {0.1f, 0.12f}},
            {EasingObject.ElasticEasingInOut, new float[2] {0.1f, 0.12f}},
            {EasingObject.BackEasingIn, new float[2] {1.70158f, 0.0f}},
            {EasingObject.BackEasingOut, new float[2] {1.70158f, 0.0f}},
            {EasingObject.BackEasingInOut, new float[2] {1.70158f, 0.0f}}
        };

        public delegate float EasingPosition(float s, float e, float deltaTime, float duration,
            float overshoot_amplitude, float period);


        public static float LinearEasing(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            return (e - s) * deltaTime / duration + s;
        }

        public static float LinearEasingInOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration / 2.0f;

            if (deltaTime < 1.0f)
                return (e - s) * deltaTime / duration + s;

            return (e - s) * (2.0f - deltaTime) / duration + s;
        }

        public static float QuadEasingIn(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration;
            return (e - s) * deltaTime * deltaTime + s;
        }

        public static float QuadEasingOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration;
            return (s - e) * deltaTime * (deltaTime - 2.0f) + s;
        }

        public static float QuadEasingInOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration / 2.0f;

            if (deltaTime < 1.0f)
                return (e - s) / 2.0f * deltaTime * deltaTime + s;

            deltaTime--;
            return (s - e) / 2.0f * (deltaTime * (deltaTime - 2.0f) - 1) + s;
        }

        public static float CircEasingIn(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration;
            return (s - e) * (Mathf.Sqrt(1.0f - deltaTime * deltaTime) - 1.0f) + s;
        }

        public static float CircEasingOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration;
            deltaTime--;
            return (e - s) * Mathf.Sqrt(1 - deltaTime * deltaTime) + s;
        }

        public static float CircEasingInOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration / 2.0f;
            if (deltaTime < 1.0f)
                return (s - e) / 2.0f * (Mathf.Sqrt(1.0f - deltaTime * deltaTime) - 1.0f) + s;

            deltaTime -= 2.0f;
            return (e - s) / 2.0f * (Mathf.Sqrt(1.0f - deltaTime * deltaTime) + 1.0f) + s;
        }

        public static float ExpoEasingIn(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            return (e - s) * Mathf.Pow(2.0f, 10.0f * (deltaTime / duration - 1.0f)) + s;
        }

        public static float ExpoEasingOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            return (e - s) * (-Mathf.Pow(2.0f, -10.0f * deltaTime / duration) + 1.0f) + s;
        }

        public static float ExpoEasingInOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            deltaTime /= duration / 2.0f;
            if (deltaTime < 1.0f)
                return (e - s) / 2.0f * Mathf.Pow(2.0f, 10.0f * (deltaTime - 1.0f)) + s;

            deltaTime--;
            return (e - s) / 2.0f * (-Mathf.Pow(2.0f, -10.0f * deltaTime) + 2.0f) + s;
        }

        public static float ElasticEasingIn(float s, float e, float deltaTime, float duration, float amplitude,
            float period)
        {
            float ss, d;

            d = e - s;

            if (deltaTime == 0.0f)
                return s;

            if ((deltaTime /= duration) == 1.0f)
                return e;

            if (period == 0)
                period = duration * 0.3f;

            if (amplitude == 0 || (d > 0 && amplitude < d) || (d < 0 && amplitude < -d))
            {
                amplitude = d;
                ss = period / 4;
            }
            else
                ss = period / TWO_PI * Mathf.Asin(d / amplitude);

            return -(amplitude * Mathf.Pow(2, 10 * (deltaTime -= 1.0f)) *
                     Mathf.Sin((deltaTime * duration - ss) * TWO_PI / period)) + s;
        }

        public static float ElasticEasingOut(float s, float e, float deltaTime, float duration, float amplitude,
            float period)
        {
            float ss, d;

            d = e - s;

            if (deltaTime == 0.0f)
                return s;

            if ((deltaTime /= duration) == 1.0f)
                return e;

            if (period == 0)
                period = duration * 0.3f;

            if (amplitude == 0 || (d > 0 && amplitude < d) || (d < 0 && amplitude < -d))
            {
                amplitude = d;
                ss = period / 4;
            }
            else
                ss = period / TWO_PI * Mathf.Asin(d / amplitude);

            return (amplitude * Mathf.Pow(2, -10 * deltaTime) *
                Mathf.Sin((deltaTime * duration - ss) * TWO_PI / period) + e);
        }

        public static float ElasticEasingInOut(float s, float e, float deltaTime, float duration, float amplitude,
            float period)
        {
            float ss, d;

            d = e - s;

            if (deltaTime == 0)
                return s;

            if ((deltaTime /= duration * 0.5f) == 2)
                return e;

            if (period == 0)
                period = duration * (0.3f * 1.5f);

            if (amplitude == 0 || (d > 0 && amplitude < d) || (d < 0 && amplitude < -d))
            {
                amplitude = d;
                ss = period / 4;
            }
            else
                ss = period / TWO_PI * Mathf.Asin(d / amplitude);

            if (deltaTime < 1.0f)
                return -0.5f * (amplitude * Mathf.Pow(2, 10 * (deltaTime -= 1)) *
                                Mathf.Sin((deltaTime * duration - ss) * TWO_PI / period)) + s;

            return amplitude * Mathf.Pow(2, -10 * (deltaTime -= 1)) *
                Mathf.Sin((deltaTime * duration - ss) * TWO_PI / period) * 0.5f + e;
        }

        public static float BackEasingIn(float s, float e, float deltaTime, float duration, float overshoot,
            float unused2)
        {
            return (e - s) * (deltaTime /= duration) * deltaTime * ((overshoot + 1) * deltaTime - overshoot) + s;
        }

        public static float BackEasingOut(float s, float e, float deltaTime, float duration, float overshoot,
            float unused2)
        {
            return (e - s) * ((deltaTime = deltaTime / duration - 1) * deltaTime *
                ((overshoot + 1) * deltaTime + overshoot) + 1) + s;
        }

        public static float BackEasingInOut(float s, float e, float deltaTime, float duration, float overshoot,
            float unused2)
        {
            if ((deltaTime /= duration * 0.5f) < 1)
            {
                return (e - s) * 0.5f *
                    (deltaTime * deltaTime * (((overshoot *= 1.525f) + 1) * deltaTime - overshoot)) + s;
            }

            return (e - s) / 2 * ((deltaTime -= 2) * deltaTime * (((overshoot *= 1.525f) + 1) * deltaTime + overshoot) +
                                  2) + s;
        }

        public static float BounceEasingIn(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            return (e - s) - BounceEasingOut(0.0f, e - s, duration - deltaTime, duration, unused1, unused2) + s;
        }

        public static float BounceEasingOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            if ((deltaTime /= duration) < (1.0f / 2.75f))
                return (e - s) * (7.5625f * deltaTime * deltaTime) + s;

            if (deltaTime < (2.0f / 2.75f))
                return (e - s) * (7.5625f * (deltaTime -= (1.5f / 2.75f)) * deltaTime + 0.75f) + s;

            if (deltaTime < (2.5f / 2.75f))
                return (e - s) * (7.5625f * (deltaTime -= (2.25f / 2.75f)) * deltaTime + 0.9375f) + s;

            return (e - s) * (7.5625f * (deltaTime -= (2.625f / 2.75f)) * deltaTime + 0.984375f) + s;
        }

        public static float BounceEasingInOut(float s, float e, float deltaTime, float duration, float unused1,
            float unused2)
        {
            if (deltaTime < duration * 0.5f)
                return BounceEasingIn(0.0f, e - s, deltaTime * 2.0f, duration, unused1, unused2) * 0.5f + s;

            return BounceEasingOut(0.0f, e - s, deltaTime * 2.0f - duration, duration, unused1, unused2) * 0.5f +
                   (e - s) * 0.5f + s;
        }
    }
}