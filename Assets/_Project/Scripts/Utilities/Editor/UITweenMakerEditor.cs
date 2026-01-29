using System.Collections.Generic;
using LitMotion;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenMaker))]
public class UITweenMakerEditor : Editor
{
    private static bool isPreviewing;
    private static List<MotionHandle> activeHandles;
    private static double lastTime;
    private static UITweenMaker currentTarget;
    private static ManualMotionDispatcher dispatcher;

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

        dispatcher = new ManualMotionDispatcher();
        activeHandles = tweenMaker.CreateAllTweens(dispatcher.Scheduler);
        if (activeHandles.Count == 0)
        {
            currentTarget.RestoreAllOriginals();
            currentTarget = null;
            dispatcher = null;
            return;
        }

        isPreviewing = true;
        lastTime = EditorApplication.timeSinceStartup;

        EditorApplication.update += EditorUpdate;
    }

    private static void EditorUpdate()
    {
        if (!isPreviewing || activeHandles == null) return;

        var currentTime = EditorApplication.timeSinceStartup;
        var deltaTime = (float)(currentTime - lastTime);
        lastTime = currentTime;

        dispatcher.Update(deltaTime);

        SceneView.RepaintAll();

        // Check if all motions completed
        bool allComplete = true;
        for (int i = 0; i < activeHandles.Count; i++)
        {
            if (activeHandles[i].IsActive())
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

        if (activeHandles != null)
        {
            for (int i = 0; i < activeHandles.Count; i++)
            {
                if (activeHandles[i].IsActive())
                    activeHandles[i].Cancel();
            }
            activeHandles.Clear();
            activeHandles = null;
        }

        if (dispatcher != null)
        {
            dispatcher.Reset();
            dispatcher = null;
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
