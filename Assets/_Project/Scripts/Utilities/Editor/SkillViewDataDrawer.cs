using CookApps.AutoBattler;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoChess.Editor
{
    [CustomPropertyDrawer(typeof(SkillViewData))]
    public class SkillViewDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 기본 foldout 그리기
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var useCustomRotation = property.FindPropertyRelative("useCustomRotation");

            var iter = property.Copy();
            var endProp = property.Copy();
            endProp.Next(false); // 다음 sibling으로 이동 (범위 종료 지점)
            iter.Next(true); // 첫 번째 자식으로 이동

            do
            {
                string propName = iter.name;
                float h = EditorGUI.GetPropertyHeight(iter, true);
                var rect = new Rect(position.x, y, position.width, h);

                bool isRotationField = propName == "rotationOffset" || propName == "flipScale";

                if (isRotationField && useCustomRotation != null && !useCustomRotation.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(rect, iter, true);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.PropertyField(rect, iter, true);
                }

                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            while (iter.Next(false) && !SerializedProperty.EqualContents(iter, endProp));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
