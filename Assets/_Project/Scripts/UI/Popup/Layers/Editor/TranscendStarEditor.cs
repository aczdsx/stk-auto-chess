using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler.Editor
{
    [CustomEditor(typeof(TranscendStar))]
    public class TranscendStarEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Auto Bind"))
            {
                AutoBind();
            }
        }

        private void AutoBind()
        {
            var transcendStar = (TranscendStar)target;
            var gameObject = transcendStar.gameObject;

            SerializedObject so = new SerializedObject(transcendStar);

            // _star 바인딩 (Image 컴포넌트)
            var starProperty = so.FindProperty("_star");
            starProperty.objectReferenceValue = gameObject.GetComponent<Image>();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(transcendStar);

            Debug.Log("[TranscendStar] Auto Bind 완료 - _star 바인딩됨");
        }
    }
}
