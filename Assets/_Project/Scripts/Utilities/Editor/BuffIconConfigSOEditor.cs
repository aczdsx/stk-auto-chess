using CookApps.AutoChess.View;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoChess.Editor
{
    [CustomPropertyDrawer(typeof(BuffIconConfigSO.EffectIconEntry))]
    public class EffectIconEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var effectType = property.FindPropertyRelative("EffectType");
            var statType = property.FindPropertyRelative("StatType");

            if (effectType != null && statType != null)
            {
                string effectName = effectType.enumDisplayNames[effectType.enumValueIndex];
                string statName = statType.enumDisplayNames[statType.enumValueIndex];

                if (statName == "None")
                    label = new GUIContent(effectName);
                else
                    label = new GUIContent($"{effectName} / {statName}");
            }

            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(BuffIconConfigSO.SkillMarkerIconEntry))]
    public class SkillMarkerIconEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var markerType = property.FindPropertyRelative("MarkerType");

            if (markerType != null)
            {
                string markerName = markerType.enumDisplayNames[markerType.enumValueIndex];
                label = new GUIContent(markerName);
            }

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
