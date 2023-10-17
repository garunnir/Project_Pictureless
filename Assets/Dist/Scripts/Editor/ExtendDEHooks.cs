using UnityEngine;
using UnityEditor;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.DialogueEditor;

[InitializeOnLoad]
public static class ExtendDEHooks
{
    static ExtendDEHooks()
    {
        DialogueEditorWindow.customNodeMenuSetup += AddItemsToNodeMenu;
        DialogueEditorWindow.customDrawDialogueEntryNode += AddToNodeDisplay;
        DialogueEditorWindow.customDrawAssetInspector += AddToAssetInspector;
        DialogueEditorWindow.customDrawDialogueEntryInspector += AddToEntryInspector;
        SequenceEditorTools.customSequenceMenuSetup += AddItemsToSequenceMenu;
        SequenceEditorTools.tryDragAndDrop += TryDragAndDrop;
    }

    private static void AddToAssetInspector(DialogueDatabase database, Asset asset)
    {
        GUILayout.Label("Extra Info in " + asset.GetType().Name);
    }

    private static void AddToEntryInspector(DialogueDatabase database, DialogueEntry entry)
    {
        GUILayout.Label("Extra Info in entry " + entry.id);
    }

    private static void AddItemsToNodeMenu(DialogueDatabase database, GenericMenu menu)
    {
        menu.AddDisabledItem(new GUIContent("Test"));
    }

    private static void AddToNodeDisplay(DialogueDatabase database, DialogueEntry entry, Rect boxRect)
    {
        //GUI.Label(boxRect, "Test");
    }

    private static void AddItemsToSequenceMenu(GenericMenu menu)
    {
        menu.AddDisabledItem(new GUIContent("Test"));
        // Use SequenceEditorTools.AddText("command") to add sequencer commands.
    }

    private static bool TryDragAndDrop(UnityEngine.Object obj, ref string sequence)
    {
        Debug.Log("Try drag: " + obj);
        return false;
    }
}