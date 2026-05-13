using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // Disable the GUI to make it read-only
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true; // Re-enable the GUI for other properties
    }
}
