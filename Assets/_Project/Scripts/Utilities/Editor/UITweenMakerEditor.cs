using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenMaker))]
public class UITweenMakerEditor : Editor
{
    private static bool isPreviewing;
    private static List<Tween> activeTweens;
    private static double lastTime;
    private static UITweenMaker currentTarget;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var tweenMaker = (UITweenMaker)target;

        EditorGUI.BeginDisabledGroup(Application.isPlaying);

        if (!isPreviewing)
        {
            if (GUILayout.Button("Play Preview", GUILayout.Height(30)))
            {
                StartPreview(tweenMaker);
            }
        }
        else
        {
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Stop Preview", GUILayout.Height(30)))
            {
                StopPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUI.EndDisabledGroup();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Edit Mode에서만 미리보기가 가능합니다.", MessageType.Info);
        }
    }

    private void StartPreview(UITweenMaker tweenMaker)
    {
        StopPreview();

        currentTarget = tweenMaker;
        currentTarget.SaveAllOriginals();

        activeTweens = tweenMaker.CreateAllTweens();
        if (activeTweens.Count == 0)
        {
            currentTarget.RestoreAllOriginals();
            currentTarget = null;
            return;
        }

        isPreviewing = true;
        lastTime = EditorApplication.timeSinceStartup;

        foreach (var tween in activeTweens)
        {
            tween.SetAutoKill(false);
            tween.SetUpdate(UpdateType.Manual);
            tween.OnComplete(() => CheckAllComplete());
        }

        EditorApplication.update += EditorUpdate;
    }

    private static void EditorUpdate()
    {
        if (!isPreviewing || activeTweens == null) return;

        var currentTime = EditorApplication.timeSinceStartup;
        var deltaTime = (float)(currentTime - lastTime);
        lastTime = currentTime;

        DOTween.ManualUpdate(deltaTime, deltaTime);

        SceneView.RepaintAll();
    }

    private static void CheckAllComplete()
    {
        if (activeTweens == null) return;

        var allComplete = true;
        foreach (var tween in activeTweens)
        {
            if (tween != null && tween.IsActive() && !tween.IsComplete())
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete)
        {
            StopPreview();
        }
    }

    private static void StopPreview()
    {
        EditorApplication.update -= EditorUpdate;

        if (activeTweens != null)
        {
            foreach (var tween in activeTweens)
            {
                tween?.Kill();
            }
            activeTweens.Clear();
            activeTweens = null;
        }

        if (currentTarget != null)
        {
            currentTarget.RestoreAllOriginals();
            currentTarget = null;
        }

        isPreviewing = false;
    }

    private void OnDisable()
    {
        StopPreview();
    }
}
