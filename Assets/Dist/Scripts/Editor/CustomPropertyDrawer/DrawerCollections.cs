using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem;

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
public class CharacterAttribute : PropertyAttribute
{
}
[CustomPropertyDrawer(typeof(CharacterAttribute))]
public class CharacterEditor : PropertyDrawer
{
    SerializedObject serializedObject;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        serializedObject ??= property.serializedObject;
        serializedObject.Update();
        
        GUI.Label(position, label);
        property.intValue = int.Parse(GUI.TextField(position, property.stringValue));
        
        base.OnGUI(position, property, label);
        return;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fields"), true);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("persistentDataName"), true);
        if (GUILayout.Button(new GUIContent("Unique", "Assign a unique persistent data name."), GUILayout.Width(60)))
        {
            AssignUniquePersistentDataName();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("spritePortrait"), new GUIContent("Portrait (Sprite)"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portrait"), new GUIContent("Portrait (Texture2D)"), true);

        var barkUISettingsProperty = serializedObject.FindProperty("barkUISettings");
        barkUISettingsProperty.isExpanded = EditorGUILayout.Foldout(barkUISettingsProperty.isExpanded, "Bark UI Settings");
        if (barkUISettingsProperty.isExpanded)
        {
            var barkUIProperty = barkUISettingsProperty.FindPropertyRelative("barkUI");
            EditorGUILayout.PropertyField(barkUIProperty, true);
            if (barkUIProperty.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(barkUISettingsProperty.FindPropertyRelative("barkUIOffset"), true);
            }
        }

        var standardUISettingsProperty = serializedObject.FindProperty("standardDialogueUISettings");
        standardUISettingsProperty.isExpanded = EditorGUILayout.Foldout(standardUISettingsProperty.isExpanded, "Dialogue UI Settings");
        if (standardUISettingsProperty.isExpanded)
        {
            var subtitlePanelNumberProperty = standardUISettingsProperty.FindPropertyRelative("subtitlePanelNumber");
            EditorGUILayout.PropertyField(subtitlePanelNumberProperty, true);
            if (subtitlePanelNumberProperty.enumValueIndex == (int)SubtitlePanelNumber.Custom)
            {
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("customSubtitlePanel"), true);
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("customSubtitlePanelOffset"), true);
            }
            var menuPanelNumberProperty = standardUISettingsProperty.FindPropertyRelative("menuPanelNumber");
            EditorGUILayout.PropertyField(menuPanelNumberProperty, true);
            if (menuPanelNumberProperty.enumValueIndex == (int)MenuPanelNumber.Custom)
            {
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("customMenuPanel"), true);
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("customMenuPanelOffset"), true);
            }
            if (menuPanelNumberProperty.enumValueIndex != (int)MenuPanelNumber.Default)
            {
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("useMenuPanelFor"), true);
            }
            EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("portraitAnimatorController"));
            var setSubtitleColorProperty = standardUISettingsProperty.FindPropertyRelative("setSubtitleColor");
            EditorGUILayout.PropertyField(setSubtitleColorProperty, true);
            if (setSubtitleColorProperty.boolValue)
            {
                var applyColorToPrependedNameProperty = standardUISettingsProperty.FindPropertyRelative("applyColorToPrependedName");
                EditorGUILayout.PropertyField(applyColorToPrependedNameProperty, true);
                if (applyColorToPrependedNameProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("prependActorNameSeparator"), true);
                    EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("prependActorNameFormat"), true);
                }
                EditorGUILayout.PropertyField(standardUISettingsProperty.FindPropertyRelative("subtitleColor"), true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    private void AssignUniquePersistentDataName()
    {
        //serializedObject.ApplyModifiedProperties();
        //foreach (var t in targets)
        //{
        //    var dialogueActor = t as DialogueActor;
        //    if (dialogueActor == null) continue;
        //    Undo.RecordObject(dialogueActor, "Unique ID");
        //    dialogueActor.persistentDataName = DialogueLua.StringToTableIndex(DialogueActor.GetActorName(dialogueActor.transform) + "_" + dialogueActor.GetInstanceID());
        //    EditorUtility.SetDirty(dialogueActor);
        //    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dialogueActor.gameObject.scene);
        //}
        //serializedObject.Update();
    }
}
