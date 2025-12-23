using UnityEngine;
using UnityEditor;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// ParachuteCurveTester의 커스텀 에디터
    /// 에디터에서 테스트 버튼을 제공
    /// </summary>
    [CustomEditor(typeof(ParachuteCurveTester))]
    public class ParachuteCurveTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 필드 노출 (CurveData, start/target 등)
            DrawDefaultInspector();

            ParachuteCurveTester tester = (ParachuteCurveTester)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !tester.isTesting && tester.curveData != null;
            if (GUILayout.Button("테스트 시작", GUILayout.Height(30)))
            {
                tester.StartTest();
                EditorUtility.SetDirty(tester);
                SceneView.RepaintAll();
            }

            GUI.enabled = tester.isTesting;
            if (GUILayout.Button("테스트 중지", GUILayout.Height(30)))
            {
                tester.StopTest();
                EditorUtility.SetDirty(tester);
                SceneView.RepaintAll();
            }

            GUI.enabled = true;
            if (GUILayout.Button("리셋", GUILayout.Height(30)))
            {
                tester.ResetTest();
                EditorUtility.SetDirty(tester);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            // Runtime Info & Scrub
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime / Scrub", EditorStyles.boldLabel);

            float duration = tester.curveData != null ? tester.curveData.duration : 0f;
            float progress = (duration > 0f) ? Mathf.Clamp01(tester._testDuration / duration) : 0f;

            if (tester.isTesting)
            {
                EditorGUILayout.LabelField("진행률", (progress * 100f).ToString("F1") + "%");
                EditorGUILayout.Slider(progress, 0f, 1f);
                EditorGUILayout.LabelField("경과 시간", tester._testDuration.ToString("F2") + " / " +
                    (tester.curveData != null ? tester.curveData.duration.ToString("F2") : "0.00") + " 초");
                EditorGUILayout.HelpBox("테스트 진행 중... 중지 후 타임라인을 스크럽할 수 있습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("테스트 중지 상태에서 타임라인을 스크럽해 미리보기 가능합니다.", MessageType.None);

                EditorGUI.BeginChangeCheck();
                float newScrub = EditorGUILayout.Slider("Timeline Scrub", tester.scrubT, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tester, "Scrub Timeline");
                    tester.scrubT = newScrub;
                    tester.ApplyScrub(newScrub);
                    EditorUtility.SetDirty(tester);
                    SceneView.RepaintAll();
                }

                EditorGUILayout.LabelField("미리보기 시간", (duration * tester.scrubT).ToString("F2") + " / " +
                    (duration > 0 ? duration.ToString("F2") : "0.00") + " 초");
            }

            // 씬 뷰 업데이트 강제
            if (tester.isTesting)
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            ParachuteCurveTester tester = (ParachuteCurveTester)target;
            
            // 씬 뷰에서 시작/목표 위치 표시
            Handles.color = Color.green;
            Handles.DrawWireCube(tester.startPosition, Vector3.one * 0.5f);
            Handles.Label(tester.startPosition + Vector3.up * 0.5f, "Start");

            Handles.color = Color.red;
            Handles.DrawWireCube(tester.targetPosition, Vector3.one * 0.5f);
            Handles.Label(tester.targetPosition + Vector3.up * 0.5f, "Target");

            // 시작/목표 위치를 씬 뷰에서 편집 가능하게
            EditorGUI.BeginChangeCheck();
            Vector3 newStartPos = Handles.PositionHandle(tester.startPosition, Quaternion.identity);
            Vector3 newTargetPos = Handles.PositionHandle(tester.targetPosition, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tester, "Move Test Positions");
                tester.startPosition = newStartPos;
                tester.targetPosition = newTargetPos;
                EditorUtility.SetDirty(tester);
            }
        }
    }
}

