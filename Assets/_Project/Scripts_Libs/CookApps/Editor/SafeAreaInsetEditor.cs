using UnityEditor;
using UnityEngine.UI;
using CookApps.TeamBattle.Utility;

namespace CookApps.AutoBattler.Editor
{
    [CustomEditor(typeof(SafeAreaInset))]
    public class SafeAreaInsetEditor : UnityEditor.Editor
    {
        private SerializedProperty canvasScalerProp;
        private SerializedProperty canvasScalerRectTrProp;

        private void OnEnable()
        {
            canvasScalerProp = serializedObject.FindProperty("canvasScaler");
            canvasScalerRectTrProp = serializedObject.FindProperty("canvasScalerRectTr");

            AutoFillIfEmpty();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (canvasScalerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CanvasScaler가 설정되지 않았습니다. 부모 계층에 CanvasScaler가 있는지 확인하세요.", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoFillIfEmpty()
        {
            if (canvasScalerProp.objectReferenceValue != null && canvasScalerRectTrProp.objectReferenceValue != null)
                return;

            var inset = (SafeAreaInset)target;
            var scaler = inset.GetComponentInParent<CanvasScaler>();
            if (scaler == null)
                return;

            serializedObject.Update();
            canvasScalerProp.objectReferenceValue = scaler;
            canvasScalerRectTrProp.objectReferenceValue = scaler.GetComponent<UnityEngine.RectTransform>();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(inset);
        }
    }
}