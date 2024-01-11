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
public class DNDStatusAttribute : PropertyAttribute
{
}
[CustomPropertyDrawer(typeof(DNDStatusAttribute))]
public class DNDStatusEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.Toggle(false);
        AnimationCurve curve = EditorGUILayout.CurveField(property.FindPropertyRelative("RangeCorrection").animationCurveValue);
    }
}
