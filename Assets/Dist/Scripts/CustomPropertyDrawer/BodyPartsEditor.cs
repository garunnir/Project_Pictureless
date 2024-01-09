using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class BodyPartsAttribute : PropertyAttribute
{
}
[CustomPropertyDrawer(typeof(BodyPartsAttribute))]
public class BodyPartsEditor :PropertyDrawer 
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.Toggle(false);
    }
}
