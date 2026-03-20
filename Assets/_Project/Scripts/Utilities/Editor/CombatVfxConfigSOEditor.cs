using CookApps.AutoChess.View;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoChess.Editor
{
    [CustomPropertyDrawer(typeof(CombatVfxConfigSO.VfxEntry))]
    public class VfxEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = property.FindPropertyRelative("Type");
            var statType = property.FindPropertyRelative("StatType");

            if (type != null && statType != null)
            {
                string typeName = type.enumDisplayNames[type.enumValueIndex];
                string statName = statType.enumDisplayNames[statType.enumValueIndex];

                if (statName == "None")
                    label = new GUIContent(typeName);
                else
                    label = new GUIContent($"{typeName} / {statName}");
            }

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
