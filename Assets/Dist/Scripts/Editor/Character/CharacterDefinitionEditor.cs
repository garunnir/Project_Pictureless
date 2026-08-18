#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterDefinition))]
public sealed class CharacterDefinitionEditor : Editor
{
    SerializedProperty _alignmentProp;

    void OnEnable()
    {
        _alignmentProp = serializedObject.FindProperty("_alignment");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "_alignment");
        CharacterAlignmentDrawer.Draw(_alignmentProp);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
