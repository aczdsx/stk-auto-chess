using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

            // _levelUpAnimations 바인딩
            var animations = gameObject.GetComponents<DOTweenAnimation>();
            var animationsProperty = so.FindProperty("_levelUpAnimations");
            animationsProperty.arraySize = animations.Length;
            for (int i = 0; i < animations.Length; i++)
            {
                animationsProperty.GetArrayElementAtIndex(i).objectReferenceValue = animations[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(transcendStar);

            Debug.Log($"[TranscendStar] Auto Bind 완료 - DOTweenAnimation {animations.Length}개 바인딩됨");
        }
    }
}
