using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class UITweenMaker : MonoBehaviour
{
    [SerializeField] private ImageColorTween[] imageColorTweens;
    [SerializeField] private RectMoveTween[] rectMoveTweens;

    public async UniTask PlayAllTweens()
    {
        var totalCount = imageColorTweens.Length + rectMoveTweens.Length;
        if (totalCount == 0) return;

        var tasks = new UniTask[totalCount];
        var index = 0;

        for (var i = 0; i < imageColorTweens.Length; i++)
        {
            tasks[index++] = imageColorTweens[i].ExecuteTweenAsync();
        }

        for (var i = 0; i < rectMoveTweens.Length; i++)
        {
            tasks[index++] = rectMoveTweens[i].ExecuteTweenAsync();
        }

        await UniTask.WhenAll(tasks);
    }

    public List<MotionHandle> CreateAllTweens(IMotionScheduler scheduler = null)
    {
        var handles = new List<MotionHandle>();

        for (var i = 0; i < imageColorTweens.Length; i++)
        {
            var handle = imageColorTweens[i].CreateTween(scheduler);
            if (handle.IsActive()) handles.Add(handle);
        }

        for (var i = 0; i < rectMoveTweens.Length; i++)
        {
            var handle = rectMoveTweens[i].CreateTween(scheduler);
            if (handle.IsActive()) handles.Add(handle);
        }

        return handles;
    }

    public void SaveAllOriginals()
    {
        for (var i = 0; i < imageColorTweens.Length; i++)
            imageColorTweens[i].SaveOriginal();

        for (var i = 0; i < rectMoveTweens.Length; i++)
            rectMoveTweens[i].SaveOriginal();
    }

    public void RestoreAllOriginals()
    {
        for (var i = 0; i < imageColorTweens.Length; i++)
            imageColorTweens[i].RestoreOriginal();

        for (var i = 0; i < rectMoveTweens.Length; i++)
            rectMoveTweens[i].RestoreOriginal();
    }

    [Serializable]
    private class ImageColorTween
    {
        [SerializeField] private Image image;
        [SerializeField] private Color targetColor;
        [SerializeField] private float duration;
        [SerializeField] private Ease ease = Ease.OutQuad;

        [NonSerialized] private Color originalColor;
        [NonSerialized] private bool hasSavedOriginal;

        public void SaveOriginal()
        {
            if (image == null) return;
            originalColor = image.color;
            hasSavedOriginal = true;
        }

        public void RestoreOriginal()
        {
            if (image == null || !hasSavedOriginal) return;
            image.color = originalColor;
            hasSavedOriginal = false;
        }

        public MotionHandle CreateTween(IMotionScheduler scheduler = null)
        {
            if (image == null || duration <= 0f) return default;
            var builder = LMotion.Create(image.color, targetColor, duration).WithEase(ease);
            if (scheduler != null) builder = builder.WithScheduler(scheduler);
            return builder.BindToColor(image);
        }

        public async UniTask ExecuteTweenAsync()
        {
            await CreateTween().ToUniTask();
        }
    }

    [Serializable]
    private class RectMoveTween
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Vector2 targetPosition;
        [SerializeField] private float duration;
        [SerializeField] private Ease ease = Ease.OutQuad;

        [NonSerialized] private Vector2 originalPosition;
        [NonSerialized] private bool hasSavedOriginal;

        public void SaveOriginal()
        {
            if (rectTransform == null) return;
            originalPosition = rectTransform.anchoredPosition;
            hasSavedOriginal = true;
        }

        public void RestoreOriginal()
        {
            if (rectTransform == null || !hasSavedOriginal) return;
            rectTransform.anchoredPosition = originalPosition;
            hasSavedOriginal = false;
        }

        public MotionHandle CreateTween(IMotionScheduler scheduler = null)
        {
            if (rectTransform == null || duration <= 0f) return default;
            var builder = LMotion.Create(rectTransform.anchoredPosition, targetPosition, duration).WithEase(ease);
            if (scheduler != null) builder = builder.WithScheduler(scheduler);
            return builder.Bind(v => rectTransform.anchoredPosition = v);
        }

        public async UniTask ExecuteTweenAsync()
        {
            await CreateTween().ToUniTask();
        }
    }
}
