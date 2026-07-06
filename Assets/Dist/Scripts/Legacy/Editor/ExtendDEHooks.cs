//using UnityEngine;
//using UnityEditor;
//using PixelCrushers.DialogueSystem;
//using PixelCrushers.DialogueSystem.DialogueEditor;
//using System.Collections.Generic;
//using PixelCrushers;
//using UnityEditorInternal;
//using static UnityEngine.GUI;
//using Garunnir;

//[InitializeOnLoad]
//public static partial class ExtendDEHooks
//{
//    static ExtendDEHooks()
//    {
//        //DialogueEditorWindow.MultinodeMapSelection
//        //DialogueEditorWindow.customNodeMenuSetup += AddItemsToNodeMenu;
//        //DialogueEditorWindow.customDrawDialogueEntryNode += AddToNodeDisplay;
//        //DialogueEditorWindow.customDrawMapEntryInspector += AddToMapNodeDisplay;
//        //DialogueEditorWindow.customDrawAssetInspector += AddToAssetInspector;
//        //DialogueEditorWindow.customDrawDialogueEntryInspector += AddToEntryInspector;
//        SequenceEditorTools.customSequenceMenuSetup += AddItemsToSequenceMenu;
//        SequenceEditorTools.tryDragAndDrop += TryDragAndDrop;
//    }
//    static Texture cachedAlignment;
//    static Texture cachedAlignmentCursor;
//    static Rect alignRect;


//    private static void AddToMapNodeDisplay(DialogueDatabase database, MapEntry entry)
//    {
//        GUILayout.Label("Extra Info in entry " + entry.id);
//    }

//    private static void AddToAssetInspector(DialogueDatabase database, Asset asset)
//    {
//        GUILayout.Label("Extra Info in " + asset.GetType().Name);
//        if (asset is Actor)
//        {
//            m_serializedObject ??= new SerializedObject(database.CharDialogueTable);
//            GUILayout.BeginHorizontal();

//            Actor target = (Actor)asset;
//            bool isTargetDataChanged = actorID != target.id;
//            GUILayout.BeginVertical();
//            GUILayout.Label("MapPosID:");
//            Field.SetValue(target.fields,ConstDataTable.Map.ID, EditorGUILayout.Popup(Field.LookupInt(target.fields, ConstDataTable.Map.ID), database.maps.ConvertAll(x => x.Title).ToArray(), GUILayout.Height(EditorGUIUtility.singleLineHeight)));
//            Field.SetValue(target.fields, ConstDataTable.Map.Pos, EditorGUILayout.IntField(Field.LookupInt(target.fields, ConstDataTable.Map.Pos)));
//            GUILayout.EndVertical();
//            GUILayout.BeginVertical();
//            GUILayout.Label("OwnDialogue:");
//            target.conversationIdx = EditorGUILayout.Popup(target.conversationIdx, database.conversations.ConvertAll(x=>x.Title).ToArray(), GUILayout.Height(EditorGUIUtility.singleLineHeight));
//            GUILayout.EndVertical();
//            //target.conversation = database.conversations[int.Parse(str)-1];
//            GUILayout.BeginVertical();
//            GUILayout.Label("Alignment");
//            GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.Height(100), GUILayout.Width(100) });
//            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), cachedAlignment ??= EditorGUIUtility.Load("Custom/Alignment.png") as Texture2D);
//            alignRect = GUILayoutUtility.GetLastRect();
//            var rect = alignRect;
//            if (Event.current.type == EventType.MouseDown && Event.current.button == 0&&Event.current.mousePosition.x>alignRect.x&&
//                Event.current.mousePosition.x < alignRect.x+alignRect.width&&Event.current.mousePosition.y > alignRect.y && Event.current.mousePosition.y < alignRect.y + alignRect.height)
//            {
//                var calPos = Event.current.mousePosition - new Vector2(alignRect.x+50,alignRect.y+50);
//                calPos = -calPos/50;
//                if (calPos.magnitude>1)
//                calPos= calPos.normalized;
//                target.alignment = calPos;
//            }
//            GUI.DrawTexture(new Rect(alignRect .x- target.alignment.x*50+50-10, alignRect.y- target.alignment.y* 50 + 50-10,20,20), cachedAlignmentCursor ??= EditorGUIUtility.Load("Dialogue System/Event.png") as Texture2D);
//            GUILayout.EndVertical();
 

//            GUILayout.EndHorizontal();
//            Rect windowRect = new Rect(10, 30, 200, 100);
//            foldLocDialogue =EditorGUILayout.Foldout(foldLocDialogue, new GUIContent("CharBarkDialouge", "Portrait images using texture assets."));
//                        m_serializedObject ??= new SerializedObject(database.CharDialogueTable);
//            if (foldLocDialogue)
//            {
//                var newToolbarSelection = GUILayout.Toolbar(m_toolbarSelection, ToolbarLabels);
//                if (newToolbarSelection != m_toolbarSelection)
//                {
//                    m_toolbarSelection = newToolbarSelection;
//                    m_textTable = database.CharDialogueTable;
//                    m_needRefreshLists = true;
//                    actorID = target.id;
//                }
//                else if(isTargetDataChanged)
//                {
//                    m_toolbarSelection = 0;
//                    m_textTable = database.CharDialogueTable;
//                    m_needRefreshLists = true;
//                    actorID = target.id;
//                }
//                switch (m_toolbarSelection)
//                {
//                    case 0:
//                        DrawLanguagesTab(database, target);
//                        break;
//                    case 1:
//                        //m_fieldListScrollPosition = GUILayout.BeginScrollView(m_fieldListScrollPosition, GUILayout.ExpandWidth(true));
//                        //GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.Height(100), GUILayout.ExpandWidth(true)});
//                        DrawFieldsTab(database, target);

//                        //GUILayout.EndScrollView();

//                        break;
//                }
//            }
//            ShowDNDStatus(target,isTargetDataChanged);
//        }

//    }

//    private static void AddToEntryInspector(DialogueDatabase database, DialogueEntry entry)
//    {
//        GUILayout.Label("Extra Info in entry " + entry.id);
//    }

//    private static void AddItemsToNodeMenu(DialogueDatabase database, GenericMenu menu)
//    {
//        menu.AddDisabledItem(new GUIContent("Test"));
//    }

//    private static void AddToNodeDisplay(DialogueDatabase database, DialogueEntry entry, Rect boxRect)
//    {
//        //GUI.Label(boxRect, "Test");
//    }

//    private static void AddItemsToSequenceMenu(GenericMenu menu)
//    {
//        menu.AddDisabledItem(new GUIContent("Test"));
//        // Use SequenceEditorTools.AddText("command") to add sequencer commands.
//    }

//    private static bool TryDragAndDrop(UnityEngine.Object obj, ref string sequence)
//    {
//        Debug.Log("Try drag: " + obj);
//        return false;
//    }
   
//}