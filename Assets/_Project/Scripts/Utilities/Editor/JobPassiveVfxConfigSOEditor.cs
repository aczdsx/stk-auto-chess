using CookApps.AutoChess.View;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoChess.Editor
{
    [CustomPropertyDrawer(typeof(JobPassiveVfxConfigSO.UnitVfxEntry))]
    public class UnitVfxEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = property.FindPropertyRelative("Type");
            if (type != null)
                label = new GUIContent(type.enumDisplayNames[type.enumValueIndex]);

            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(JobPassiveVfxConfigSO.AreaVfxEntry))]
    public class AreaVfxEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = property.FindPropertyRelative("Type");
            if (type != null)
                label = new GUIContent(type.enumDisplayNames[type.enumValueIndex]);

            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(JobPassiveVfxConfigSO.ProjectileOverrideEntry))]
    public class ProjectileOverrideEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelProp = property.FindPropertyRelative("Label");
            var idProp = property.FindPropertyRelative("Id");
            if (labelProp != null && !string.IsNullOrEmpty(labelProp.stringValue))
                label = new GUIContent(labelProp.stringValue);
            else if (idProp != null)
                label = new GUIContent($"Id {idProp.intValue}");

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
