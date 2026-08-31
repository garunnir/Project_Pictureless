#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterDefinition))]
public sealed class CharacterDefinitionEditor : OdinEditor
{
    SerializedProperty _alignmentProp;

    protected override void OnEnable()
    {
        base.OnEnable();
        _alignmentProp = serializedObject.FindProperty("_alignment");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (_alignmentProp == null)
            return;

        serializedObject.Update();
        CharacterAlignmentDrawer.Draw(_alignmentProp);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
