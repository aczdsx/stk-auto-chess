using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler.Editor
{
    [CustomEditor(typeof(SpriteLoader))]
    public class SpriteLoaderEditor : UnityEditor.Editor
    {
        private SerializedProperty isSpriteRendererProp;
        private SerializedProperty targetRendererProp;
        private SerializedProperty targetImageProp;

        private void OnEnable()
        {
            isSpriteRendererProp = serializedObject.FindProperty("isSpriteRenderer");
            targetRendererProp = serializedObject.FindProperty("targetRenderer");
            targetImageProp = serializedObject.FindProperty("targetImage");

            // 자동으로 필드 채우기
            AutoFillIfEmpty();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // isSpriteRenderer에 따라 적절한 필드만 표시
            bool isSpriteRenderer = isSpriteRendererProp.boolValue;

            EditorGUILayout.PropertyField(isSpriteRendererProp, new GUIContent("Is Sprite Renderer"));

            if (isSpriteRenderer)
            {
                EditorGUILayout.PropertyField(targetRendererProp, new GUIContent("Target Renderer"));
            }
            else
            {
                EditorGUILayout.PropertyField(targetImageProp, new GUIContent("Target Image"));
            }

            // 경고 메시지
            EditorGUILayout.Space(5);
            ShowValidationMessages();

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoFillIfEmpty()
        {
            SpriteLoader loader = (SpriteLoader)target;

            bool isSpriteRenderer = isSpriteRendererProp.boolValue;
            SpriteRenderer currentRenderer = targetRendererProp.objectReferenceValue as SpriteRenderer;
            Image currentImage = targetImageProp.objectReferenceValue as Image;

            // 이미 올바르게 설정되어 있으면 건너뛰기
            if ((isSpriteRenderer && currentRenderer != null) || (!isSpriteRenderer && currentImage != null))
                return;

            // 자동으로 채우기
            SpriteRenderer spriteRenderer = loader.GetComponent<SpriteRenderer>();
            Image image = loader.GetComponent<Image>();

            if (spriteRenderer != null || image != null)
            {
                serializedObject.Update();

                if (spriteRenderer != null)
                {
                    isSpriteRendererProp.boolValue = true;
                    targetRendererProp.objectReferenceValue = spriteRenderer;
                    targetImageProp.objectReferenceValue = null;
                }
                else if (image != null)
                {
                    isSpriteRendererProp.boolValue = false;
                    targetRendererProp.objectReferenceValue = null;
                    targetImageProp.objectReferenceValue = image;
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(loader);
            }
        }

        private void ShowValidationMessages()
        {
            bool isSpriteRenderer = isSpriteRendererProp.boolValue;
            SpriteRenderer renderer = targetRendererProp.objectReferenceValue as SpriteRenderer;
            Image image = targetImageProp.objectReferenceValue as Image;

            if (isSpriteRenderer)
            {
                if (renderer == null)
                {
                    EditorGUILayout.HelpBox("SpriteRenderer가 설정되지 않았습니다!", MessageType.Error);
                }
            }
            else
            {
                if (image == null)
                {
                    EditorGUILayout.HelpBox("Image가 설정되지 않았습니다!", MessageType.Error);
                }
            }
        }
    }
}