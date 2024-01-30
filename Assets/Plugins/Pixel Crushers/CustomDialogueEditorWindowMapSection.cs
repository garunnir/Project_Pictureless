// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.Overlays;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the Conversations tab. If the user
    /// has selected the node editor (default), it uses the node editor part. Otherwise
    /// it uses the outline-style dialogue tree part.
    /// </summary>
    public partial class DialogueEditorWindow
    {
        bool isMakingCLink;
        MapContainer _currentMap;
        private MapEntry _currentMapEntry = null;
        [SerializeField]
        private int currentMapEntryID = -1;
        private bool mapTreeFoldout = false;
        private MapNode mapTree = null;
        private List<MapNode> mapOrphans = new List<MapNode>();
        private Dictionary<int, bool> mapEntryFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> mapEntryNodeHasSequence = new Dictionary<int, bool>();
        private Dictionary<int, GUIContent> mapEntryNodeDescription = new Dictionary<int, GUIContent>();
        private MapEntry serializedObjectCurrentMapEntry = null;

        private HashSet<int> mapOrphanIDs = new HashSet<int>();
        private int mapOrphanIDsMapContainerID = -1;

        private Field currentMapEntryActor = null;
        private Field currentMapEntryConversant = null;

        private GUIContent[] maplinkToDestinations = new GUIContent[0];
        private MapEntry maplinkToDestinationsFromEntry = null;
        private bool nodeEditorDeleteCurrentMapContainer = false;

        private MapEntry currentRuntimeMapEntry = null;

        private MapEntry currentHoveredMapEntry = null;

        private MapEntry mapNodeToDrag = null;

        private MapEntry maplinkSourceEntry = null;
        private MapEntry maplinkTargetEntry = null;

        private MapEntry currentMapHoveredEntry = null;
        private List<MapEntry> nodesInMapEntryGroup = null;
        private List<EntryGroup> subgroupsInMapEntryGroup = null;

        private bool entryPositionFoldout = false;

        private float canvasMapRectWidth { get { return canvasRectWidthMultiplier * MapEntry.CanvasRectWidth; } }
        private float canvasMapRectHeight { get { return MapEntry.CanvasRectHeight; } }
        private class MapNode
        {
            public MapEntry entry;
            public Link originLink;
            public GUIStyle guiStyle;
            public float indent;
            public bool isEditable;
            public bool hasFoldout;
            public List<MapNode> children;

            public MapNode(MapEntry entry, Link originLink, GUIStyle guiStyle, float indent, bool isEditable, bool hasFoldout)
            {
                this.entry = entry;
                this.originLink = originLink;
                this.guiStyle = guiStyle;
                this.indent = indent;
                this.isEditable = isEditable;
                this.hasFoldout = hasFoldout;
                this.children = new List<MapNode>();
            }
        }
        private MapEntry currentMapEntry
        {
            get
            {
                return _currentMapEntry;
            }
            set
            {
                _currentMapEntry = value;
                sequenceSyntaxState = SequenceSyntaxState.Unchecked;
                if (value != null)
                {
                    currentMapEntryID = value.id;
                    if (value.fields != null) BuildLanguageListFromFields(value.fields);
                }
                else
                {
                    CloseQuickDialogueTextEntry();
                }
                if (verboseDebug && value != null) Debug.Log("<color=magenta>Set current entry ID to " + currentMapEntryID + "</color>");
            }
        }
        private void ResetCurrentMapEntryID()
        {
            if (verboseDebug) Debug.Log("<color=magenta>Reset current entry ID</color>");
            currentMapEntryID = -1;

        }
        private void ApplyMapEntryTemplate(List<Field> fields)
        {
            if (template == null || template.mapEntryFields == null || fields == null) return;
            ApplyTemplate(fields, template.mapEntryFields);
        }
        public void DrawMapOutline()
        {
            if (currentMapContainer == null) return;
            EditorWindowTools.StartIndentedSection();
            EditorGUILayout.BeginVertical("HelpBox");
            DrawMapProperties();
            DrawMapFieldsFoldout();
            DrawMapTreeFoldout();
            EditorGUILayout.EndVertical();
            EditorWindowTools.EndIndentedSection();
        }
        private void CheckClickOnMapEntryGroup()
        {
            if (currentMapContainer == null) return;
            var clickPos = Event.current.mousePosition + canvasScrollPosition;
            for (int i = 0; i < currentMapContainer.entryGroups.Count; i++)
            {
                var group = currentMapContainer.entryGroups[i];
                var groupHeadingRect = new Rect(group.rect.x, group.rect.y, group.rect.width, entryGroupHeadingHeight);
                var resizeRect = new Rect(group.rect.x + group.rect.width - 20, group.rect.y + group.rect.height - 20, 20, 20);
                if (groupHeadingRect.Contains(clickPos) || resizeRect.Contains(clickPos))
                {
                    if (selectedEntryGroup != group || nodesInMapEntryGroup == null || subgroupsInMapEntryGroup == null)
                    {
                        selectedEntryGroup = group;
                        inspectorSelection = group;
                        nodesInMapEntryGroup = new List<MapEntry>();
                        isResizingEntryGroup = resizeRect.Contains(clickPos);
                        foreach (var entry in currentMapContainer.mapEntries)
                        {
                            if (group.rect.Contains(entry.canvasRect.TopLeft()) &&
                                group.rect.Contains(entry.canvasRect.BottomRight()))
                            {
                                nodesInMapEntryGroup.Add(entry);
                            }
                        }
                        subgroupsInMapEntryGroup = new List<EntryGroup>();
                        foreach (var otherGroup in currentMapContainer.entryGroups)
                        {
                            if (otherGroup == group) continue;
                            if (group.rect.Contains(otherGroup.rect.TopLeft()) &&
                                group.rect.Contains(otherGroup.rect.BottomRight()))
                            {
                                subgroupsInMapEntryGroup.Add(otherGroup);
                            }
                        }
                        return;
                    }
                }
            }
            ClearSelectedEntryGroup();
        }
        public bool DrawMapProperties()
        {
            if (currentMapContainer == null) return false;
            EditorGUI.BeginDisabledGroup(true); // Don't let user modify ID. Breaks things way more often than not.
            int newID = EditorGUILayout.IntField(new GUIContent("ID", "Internal ID. Change at your own risk."), currentMapContainer.id);
            EditorGUI.EndDisabledGroup();
            if (newID != currentMapContainer.id) SetNewConversationID(newID);

            bool changed = false;

            string newTitle = EditorGUILayout.TextField(new GUIContent("Title", "Conversation triggers reference conversations by this."), currentMapContainer.Title);
            if (!string.Equals(newTitle, currentMapContainer.Title))
            {
                currentMapContainer.Title = RemoveLeadingSlashes(newTitle); ;
                changed = true;
                SetDatabaseDirty("Change Conversation Title");
            }

            EditorGUILayout.HelpBox("Tip: You can organize conversations into submenus by using forward slashes ( / ) in mapContainer titles.", MessageType.Info);

            var description = Field.Lookup(currentMapContainer.fields, "Description");
            if (description != null)
            {
                EditorGUILayout.LabelField("Description");
                description.value = EditorGUILayout.TextArea(description.value);
            }

            actorField = DrawConversationParticipant(new GUIContent("Actor", "Primary actor, usually the PC."), actorField);
            conversantField = DrawConversationParticipant(new GUIContent("Conversant", "Other actor, usually an NPC."), conversantField);

            DrawOverrideSettings(currentMapContainer.overrideSettings);

            DrawOtherConversationPrimaryFields();

            if (customDrawAssetInspector != null)
            {
                customDrawAssetInspector(database, currentMapContainer);
            }

            return changed;
        }

        public bool DrawMapEntryInspector()
        {
            // Draw field contents:
            bool changedFieldContents = DrawMapEntryFieldContents();

            // Draw links:
            Link linkToDelete = null;
            MapEntry entryToLinkFrom = null;
            MapEntry entryToLinkTo = null;
            bool linkToAnotherConversation = false;
            bool changedLinks = DrawMapEntryLinks(currentMapEntry, ref linkToDelete, ref entryToLinkFrom, ref entryToLinkTo, ref linkToAnotherConversation);
            // Handle deletion:
            if (linkToDelete != null)
            {
                changedLinks = true;
                DeleteLink(linkToDelete);
                InitializeMapTree();
            }
            // Handle linking:
            if (entryToLinkFrom != null)
            {
                changedLinks = true;
                if (entryToLinkTo == null)
                {
                    if (linkToAnotherConversation)
                    {
                        LinkToAnotherMap(entryToLinkFrom);
                    }
                    else
                    {
                        LinkToNewMapEntry(entryToLinkFrom);
                    }
                }
                else
                {
                    CreateLink(entryToLinkFrom, entryToLinkTo);
                }
                InitializeMapTree();
            }

            return changedFieldContents || changedLinks;
        }
        private void DrawMapTreeFoldout()
        {
            CheckDialogueTreeGUIStyles();
            if (AreMapParticipantsValid())
            {
                bool isDialogueTreeOpen = EditorGUILayout.Foldout(mapTreeFoldout, "MapContainer Tree");
                if (isDialogueTreeOpen && !mapTreeFoldout) InitializeMapTree();
                mapTreeFoldout = isDialogueTreeOpen;
                if (mapTreeFoldout) DrawMapTree();
            }
            else
            {
                EditorGUILayout.LabelField("Dialogue Tree: Assign Actor and Mapcontainer first.");
            }
        }
        private string BuildMapEntryText(MapEntry entry)//text
        {
            string text = entry.currentMenuText;
            if (string.IsNullOrEmpty(text)) text = entry.currentDialogueText;
            if (string.IsNullOrEmpty(text)) text = "<" + entry.Title + ">";
            if (entry.isGroup) text = "{group} " + text;
            if (text.Contains("\n")) text = text.Replace("\n", string.Empty);
            string speaker = GetActorNameByID(entry.ActorID);
            text = string.Format("[{0}] {1}: {2}", entry.id, speaker, text);
            if (!showNodeEditor)
            { // Only show for Outline editor. For node editor, we draw small icons on node.
                if (!string.IsNullOrEmpty(entry.conditionsString)) text += " [condition]";
                if (!string.IsNullOrEmpty(entry.userScript)) text += " {script}";
            }
            //if ((entry.outgoingLinks == null) || (entry.outgoingLinks.Count == 0)) text += " [END]";
            return text;
        }
        private string GetMapEntryText(MapEntry entry)
        {
            if (entry == null) return string.Empty;
            if (!dialogueEntryText.ContainsKey(entry.id) || (dialogueEntryText[entry.id] == null))
            {
                dialogueEntryText[entry.id] = BuildMapEntryText(entry);
            }
            return dialogueEntryText[entry.id];
        }
        private void ResetMapTreecurrentMapEntryParticipants()
        {
            currentMapEntryActor = null;
            currentMapEntryConversant = null;
        }
        private void DrawMapNode(
           MapNode node,
           Link originLink,
           ref Link linkToDelete,
           ref MapEntry entryToLinkFrom,
           ref MapEntry entryToLinkTo,
           ref bool linkToAnotherConversation)
        {

            if (node == null) return;

            // Setup:
            bool deleted = false;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, GUILayout.Width(node.indent));
            if (node.isEditable)
            {

                // Draw foldout if applicable:
                if (node.hasFoldout && node.entry != null)
                {
                    Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(FoldoutRectWidth));
                    mapEntryFoldouts[node.entry.id] = EditorGUI.Foldout(rect, mapEntryFoldouts[node.entry.id], string.Empty);
                }

                // Draw label/button to edit:
                if (GUILayout.Button(GetMapEntryText(node.entry), node.guiStyle))
                {
                    GUIUtility.keyboardControl = 0;
                    currentMapEntry = (currentMapEntry != node.entry) ? node.entry : null;
                    ResetLuaWizards();
                    ResetMapTreecurrentMapEntryParticipants();
                }

                // Draw delete-node button:
                GUI.enabled = (originLink != null);
                deleted = GUILayout.Button(new GUIContent(" ", "Delete entry."), "OL Minus", GUILayout.Width(16));
                if (deleted) linkToDelete = originLink;
                GUI.enabled = true;
            }
            else
            {

                // Draw uneditable node:
                EditorGUILayout.LabelField(GetMapEntryText(node.entry), node.guiStyle);
                GUI.enabled = false;
                GUILayout.Button(" ", "OL Minus", GUILayout.Width(16));
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            // Draw contents if this is the currently-selected entry:
            if (!deleted && (node.entry == currentMapEntry) && node.isEditable)
            {
                DrawMapEntryContents(currentMapEntry, ref linkToDelete, ref entryToLinkFrom, ref entryToLinkTo, ref linkToAnotherConversation);
            }

            // Draw children:
            if (!deleted && node.hasFoldout && (node.entry != null) && mapEntryFoldouts[node.entry.id])
            {
                for (int i = 0; i < node.children.Count; i++)
                {
                    var child = node.children[i];
                    if (child != null)
                    {
                        DrawMapNode(child, child.originLink, ref linkToDelete, ref entryToLinkFrom, ref entryToLinkTo, ref linkToAnotherConversation);
                    }
                }
            }
        }
        private void ResetMapEntryNodeDescription(MapEntry entry)
        {
            dialogueEntryNodeDescription[entry.id] = null;
        }
        private void VerifyParticipantMapField(MapEntry entry, string fieldTitle, ref Field participantField)
        {
            if (participantField == null) participantField = Field.Lookup(entry.fields, fieldTitle);
            if (participantField == null)
            {
                participantField = new Field(fieldTitle, string.Empty, FieldType.Actor);
                entry.fields.Add(participantField);
                SetDatabaseDirty("Add Participant Field");
            }
        }
        private void DrawMapEntryParticipants(MapEntry entry)
        {
            // Make sure we have references to the actor and conversant fields:
            VerifyParticipantMapField(entry, "Actor", ref currentMapEntryActor);
            VerifyParticipantMapField(entry, "Conversant", ref currentMapEntryConversant);

            // If actor is unassigned, use conversation's values: (conversant may be set to None)
            if (IsActorIDUnassigned(currentMapEntryActor)) currentMapEntryActor.value = currentMapContainer.ActorID.ToString();
            //if (IsActorIDUnassigned(currentMapEntryConversant)) currentMapEntryConversant.value = currentMapContainer.ConversantID.ToString(); ;

            // Participant IDs:
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            DrawParticipantField(currentMapEntryActor, "Speaker of this entry.");
            DrawParticipantField(currentMapEntryConversant, "Listener.");
            EditorGUILayout.EndVertical();
            var swap = GUILayout.Button(new GUIContent(" ", "Swap participants."), "Popup", GUILayout.Width(24));
            EditorGUILayout.EndHorizontal();

            if (swap) SwapParticipants(ref currentMapEntryActor, ref currentMapEntryConversant);
        }
        //private void DrawLocalizedVersions(MapEntry entry, List<Field> fields, string titleFormat, bool alwaysAdd, FieldType fieldType, bool useSequenceEditor = false)
        //{
        //    DrawLocalizedVersions(null, entry, fields, titleFormat, alwaysAdd, fieldType, null, useSequenceEditor);
        //}
        //private void DrawLocalizedVersions(Asset asset, MapEntry entry, List<Field> fields, string titleFormat, bool alwaysAdd, FieldType fieldType, bool useSequenceEditor = false)
        //{
        //    DrawLocalizedVersions(null, entry, fields, titleFormat, alwaysAdd, fieldType, null, useSequenceEditor);
        //}
        //private void DrawLocalizedVersions(Asset asset, MapEntry entry, List<Field> fields, string titleFormat, bool alwaysAdd, FieldType fieldType, List<Field> alreadyDrawn, bool useSequenceEditor = false)
        //{
        //    bool indented = false;
        //    foreach (var language in languages)
        //    {
        //        string localizedTitle = string.Format(titleFormat, language);
        //        Field field = Field.Lookup(fields, localizedTitle);
        //        if ((field == null) && (alwaysAdd || (Field.FieldExists(template.dialogueEntryFields, localizedTitle))))
        //        {
        //            field = new Field(localizedTitle, string.Empty, fieldType);
        //            fields.Add(field);
        //        }
        //        if (field != null)
        //        {
        //            if (!indented)
        //            {
        //                indented = true;
        //                EditorWindowTools.StartIndentedSection();
        //            }
        //            if (useSequenceEditor)
        //            {
        //                EditorGUILayout.BeginHorizontal();
        //                EditorGUILayout.LabelField(localizedTitle);
        //                GUILayout.FlexibleSpace();
        //                if (entry != null) DrawAISequence(entry, field);
        //                EditorGUILayout.EndHorizontal();
        //                field.value = EditorGUILayout.TextArea(field.value);
        //            }
        //            else
        //            {
        //                //[AI] EditorGUILayout.LabelField(localizedTitle);
        //                //field.value = EditorGUILayout.TextArea(field.value);
        //                DrawLocalizableTextAreaMapField(new GUIContent(localizedTitle), asset, entry, field);
        //            }
        //            if (alreadyDrawn != null) alreadyDrawn.Add(field);
        //        }
        //    }
        //    if (indented) EditorWindowTools.EndIndentedSection();
        //}
        private void DrawLocalizableTextAreaMapField(GUIContent label, Asset asset, MapEntry entry, Field field)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            DrawAILocalizeTextButton(asset, entry, field);
            EditorGUILayout.EndHorizontal();
            field.value = EditorGUILayout.TextArea(field.value);
        }
        private void DrawOtherMapEntryPrimaryFields(MapEntry entry)
        {
            if (entry == null || entry.fields == null || template.dialogueEntryPrimaryFieldTitles == null) return;
            foreach (var field in entry.fields)
            {
                var fieldTitle = field.title;
                if (string.IsNullOrEmpty(fieldTitle)) continue;
                if (!template.dialogueEntryPrimaryFieldTitles.Contains(field.title)) continue;
                if (dialogueEntryBuiltInFieldTitles.Contains(fieldTitle)) continue;
                if (fieldTitle.StartsWith("Menu Text") || fieldTitle.StartsWith("Sequence") || fieldTitle.StartsWith("Response Menu Sequence")) continue;
                DrawMainSectionField(field);
            }
        }
        //private void DrawRevisableTextAreaField(GUIContent label, Asset asset, MapEntry entry, List<Field> fields, string fieldTitle)
        //{
        //    Field field = Field.Lookup(fields, fieldTitle);
        //    if (field == null)
        //    {
        //        field = new Field(fieldTitle, string.Empty, FieldType.Text);
        //        fields.Add(field);
        //        SetDatabaseDirty("Create Field " + fieldTitle);
        //    }
        //    DrawRevisableTextAreaField(label, asset, entry, field);
        //}
        private void DrawRevisableTextAreaField(GUIContent label, Asset asset, MapEntry entry, Field field)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            DrawAIReviseTextButton(asset, entry, field);
            EditorGUILayout.EndHorizontal();
            field.value = EditorGUILayout.TextArea(field.value);
        }
        public bool DrawMapEntryFieldContents()
        {
            if (currentMapEntry == null) return false;

            bool changed = false;
            EditorGUI.BeginChangeCheck();

            MapEntry entry = currentMapEntry;
            bool isstartMapEntry = (entry == startMapEntry) || (entry.id == 0);

            EditorGUI.BeginDisabledGroup(true); // Don't let user modify ID. Breaks things way more often than not.
            entry.id = StringToInt(EditorGUILayout.TextField(new GUIContent("ID", "Internal ID. Change at your own risk."), entry.id.ToString()), entry.id);
            EditorGUI.EndDisabledGroup();

            // Title:
            EditorGUI.BeginDisabledGroup(isstartMapEntry);
            entry.Title = EditorGUILayout.TextField(new GUIContent("Title", "Optional title for your reference only."), entry.Title);
            EditorGUI.EndDisabledGroup();

            if (isstartMapEntry)
            {
                EditorGUILayout.HelpBox("This is the START entry. In most cases, you should leave this entry alone and begin your mapContainer with its child entries.", MessageType.Warning);
                if (!allowEditStartEntry)
                {
                    if (GUILayout.Button("I understand. Edit anyway."))
                    {
                        allowEditStartEntry = true;
                    }
                }
            }

            // Description:
            var description = Field.Lookup(entry.fields, "Description");
            if (description != null)
            {
                EditorGUILayout.LabelField(new GUIContent("Description", "Description of this entry; notes for the author"));
                EditorGUI.BeginChangeCheck();
                description.value = EditorGUILayout.TextArea(description.value);
                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                    ResetMapEntryNodeDescription(entry);
                }
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("");
            var newPortrait = EditorGUILayout.ObjectField(new GUIContent("Background", "Map Background Img"),
                                         entry.backGroundTexture, typeof(Texture2D), false, GUILayout.Height(64)) as Texture2D;
            if (newPortrait != entry.backGroundTexture)
            {
                entry.backGroundTexture = newPortrait;
                SetCurrentMapEntry(entry);
                ClearActorInfoCaches();
                SetDatabaseDirty("MapEntry Portrait");
            }
            EditorGUILayout.EndHorizontal();
            var allacters = database.actors;
            var acters=allacters.FindAll(x =>Field.LookupInt(x.fields,ConstDataTable.Map.ID)==entry.MapID&& Field.LookupInt(x.fields, ConstDataTable.Map.Pos) == entry.id);
            if (acters != null)
            {
                actorFoldout= EditorGUILayout.Foldout(actorFoldout, "Actors");
                if (actorFoldout)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();
                    EditorGUI.indentLevel++;
                    foreach (var item in acters)
                    {
                        EditorGUILayout.BeginHorizontal();
                        item.Name = EditorGUILayout.TextField(item.Name);
                        EditorGUILayout.LabelField(new GUIContent("mapID"));
                        EditorGUI.BeginDisabledGroup(true);
                        //item.mapPosID.Item1 = EditorGUILayout.IntField(item.mapPosID.Item1);
                        ;
                        Field.SetValue(item.fields,ConstDataTable.Map.Pos,EditorGUILayout.IntField(Field.LookupInt(item.fields, ConstDataTable.Map.Pos)));
                        EditorGUI.EndDisabledGroup();
                        if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(21)))
                        {
                            Field.SetValue(item.fields, ConstDataTable.Map.Pos, -1);
                            Field.SetValue(item.fields, ConstDataTable.Map.ID, -1);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    selectedPlusActor = DrawAssetPopup<Actor>(selectedPlusActor, allacters, null);
                    if(GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(21)))
                    {
                        Actor actor =allacters[int.Parse(selectedPlusActor) - 1];
                        Field.SetValue(actor.fields, ConstDataTable.Map.Pos,entry.id);
                        Field.SetValue(actor.fields, ConstDataTable.Map.ID,entry.MapID);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        changed = true;
                        ResetMapEntryNodeDescription(entry);
                    }
                }
            }
            #region Unused
            //// Actor & conversant:
            ////DrawMapEntryParticipants(entry);

            //EditorGUI.BeginDisabledGroup(isstartMapEntry && !allowEditStartEntry);

            //// Is this a group or regular entry:
            //entry.isGroup = EditorGUILayout.Toggle(new GUIContent("Group", "Tick to organize children as a group."), entry.isGroup);

            //if (!entry.isGroup)
            //{
            //    EditorWindowTools.EditorGUILayoutBeginGroup();

            //    EditorGUI.BeginChangeCheck();

            //    // Menu text (including localized if defined in template):
            //    var menuTextField = Field.Lookup(entry.fields, "Menu Text");
            //    if (menuTextField == null)
            //    {
            //        menuTextField = new Field("Menu Text", "", FieldType.Text);
            //        entry.fields.Add(menuTextField);
            //    }
            //    var menuText = menuTextField.value;
            //    var menuTextLabel = string.IsNullOrEmpty(menuText) ? "Menu Text" : ("Menu Text (" + menuText.Length + " chars)");
            //    DrawRevisableTextAreaField(new GUIContent(menuTextLabel, "Response menu text (e.g., short paraphrase). If blank, uses Dialogue Text."), null, currentMapEntry, menuTextField);
            //    DrawLocalizedVersions(entry, entry.fields, "Menu Text {0}", false, FieldType.Text);

            //    // Dialogue text (including localized):
            //    var dialogueTextField = Field.Lookup(entry.fields, "Dialogue Text");
            //    if (dialogueTextField == null)
            //    {
            //        dialogueTextField = new Field("Dialogue Text", "", FieldType.Text);
            //        entry.fields.Add(dialogueTextField);
            //    }
            //    var dialogueText = dialogueTextField.value;
            //    var dialogueTextLabel = string.IsNullOrEmpty(dialogueText) ? "Dialogue Text" : ("Dialogue Text (" + dialogueText.Length + " chars)");
            //    DrawRevisableTextAreaField(new GUIContent(dialogueTextLabel, "Line spoken by actor. If blank, uses Menu Text."), null, currentMapEntry, dialogueTextField);
            //    DrawLocalizedVersions(entry, entry.fields, "{0}", true, FieldType.Localization);

            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        changed = true;
            //        if (string.Equals(entry.Title, "New Dialogue Entry")) entry.Title = string.Empty;
            //    }

            //    EditorWindowTools.EditorGUILayoutEndGroup();

            //    // Sequence (including localized if defined):
            //    EditorWindowTools.EditorGUILayoutBeginGroup();

            //    var sequenceField = Field.Lookup(entry.fields, "Sequence");
            //    EditorGUI.BeginChangeCheck();
            //    sequenceField.value = SequenceEditorTools.DrawLayout(new GUIContent("Sequence", "Cutscene played when speaking this entry. If set, overrides Dialogue Manager's Default Sequence. Drag audio clips to add AudioWait() commands."), sequenceField.value, ref sequenceRect, ref sequenceSyntaxState, entry, sequenceField);
            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        changed = true;
            //        dialogueEntryNodeHasSequence[entry.id] = !string.IsNullOrEmpty(sequenceField.value);
            //    }
            //    DrawLocalizedVersions(entry, entry.fields, "Sequence {0}", false, FieldType.Text, true);

            //    // Response Menu Sequence:
            //    bool hasResponseMenuSequence = entry.HasResponseMenuSequence();
            //    if (hasResponseMenuSequence)
            //    {
            //        EditorGUILayout.LabelField(new GUIContent("Response Menu Sequence", "Cutscene played during response menu following this entry."));
            //        entry.ResponseMenuSequence = EditorGUILayout.TextArea(entry.ResponseMenuSequence);
            //        DrawLocalizedVersions(entry, entry.fields, "Response Menu Sequence {0}", false, FieldType.Text);
            //    }
            //    else
            //    {
            //        hasResponseMenuSequence = EditorGUILayout.ToggleLeft(new GUIContent("Add Response Menu Sequence", "Tick to add a cutscene that plays during the response menu that follows this entry."), false);
            //        if (hasResponseMenuSequence) entry.ResponseMenuSequence = string.Empty;
            //    }

            //    EditorWindowTools.EditorGUILayoutEndGroup();
            //}

            //// Conditions:
            //EditorWindowTools.EditorGUILayoutBeginGroup();
            //luaConditionWizard.database = database;
            //entry.conditionsString = luaConditionWizard.Draw(new GUIContent("Conditions", "Optional Lua statement that must be true to use this entry."), entry.conditionsString);
            //int falseConditionIndex = EditorGUILayout.Popup("False Condition Action", GetFalseConditionIndex(entry.falseConditionAction), falseConditionActionStrings);
            //entry.falseConditionAction = falseConditionActionStrings[falseConditionIndex];
            //EditorWindowTools.EditorGUILayoutEndGroup();

            //// Script:
            //EditorWindowTools.EditorGUILayoutBeginGroup();
            //luaScriptWizard.database = database;
            //entry.userScript = luaScriptWizard.Draw(new GUIContent("Script", "Optional Lua code to run when entry is spoken."), entry.userScript);
            //EditorWindowTools.EditorGUILayoutEndGroup();

            //// Other primary fields defined in template:
            //DrawOtherMapEntryPrimaryFields(entry);
            #endregion
            // Events:
            entryEventFoldout = EditorGUILayout.Foldout(entryEventFoldout, "Events");
            if (entryEventFoldout) DrawUnityMapEvents();

            // Notes: (special handling to use TextArea)
            Field notes = Field.Lookup(entry.fields, "Notes");
            if (notes != null)
            {
                EditorGUILayout.LabelField("Notes");
                notes.value = EditorGUILayout.TextArea(notes.value);
            }

            // Custom inspector code hook:
            if (customDrawMapEntryInspector != null)
            {
                customDrawMapEntryInspector(database, entry);
            }

            //entryPositionFoldout = EditorGUILayout.Foldout(entryPositionFoldout, "Position");
            //if (entryPositionFoldout)
            //{
            //}
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();

            if (currentMapEntry.postion.upID != -1)
            {
                if (GUILayout.Button(new GUIContent("North", "")))
                {
                    AlignNearCardinalPosition(currentMapEntry);
                    SetCurrentMapEntry(currentMapContainer.GetMapEntry(currentMapEntry.postion.upID));
                }
                if (GUILayout.Button(new GUIContent("-"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
                {
                    var target = currentMapContainer.GetMapEntry(currentMapEntry.postion.upID);
                    DeleteCardinalConnect(entry, target, CardinalPoint.North);
                }
            }
            if (currentMapEntry.postion.downID != -1)
            {
                if (GUILayout.Button(new GUIContent("South", "")))
                {
                    AlignNearCardinalPosition(currentMapEntry);
                    SetCurrentMapEntry(currentMapContainer.GetMapEntry(currentMapEntry.postion.downID));
                }
                if (GUILayout.Button(new GUIContent("-"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
                {
                    var target = currentMapContainer.GetMapEntry(currentMapEntry.postion.downID);
                    DeleteCardinalConnect(entry, target, CardinalPoint.South);
                }
            }
            if (currentMapEntry.postion.leftID != -1)
            {
                if (GUILayout.Button(new GUIContent("West", "")))
                {
                    AlignNearCardinalPosition(currentMapEntry);
                    SetCurrentMapEntry(currentMapContainer.GetMapEntry(currentMapEntry.postion.leftID));
                }
                if (GUILayout.Button(new GUIContent("-"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
                {
                    var target = currentMapContainer.GetMapEntry(currentMapEntry.postion.leftID);
                    DeleteCardinalConnect(entry, target, CardinalPoint.West);
                }

            }
            if (currentMapEntry.postion.rightID != -1)
            {
                if (GUILayout.Button(new GUIContent("East", "")))
                {
                    AlignNearCardinalPosition(currentMapEntry);
                    SetCurrentMapEntry(currentMapContainer.GetMapEntry(currentMapEntry.postion.rightID));
                }
                if (GUILayout.Button(new GUIContent("-"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
                {
                    var target = currentMapContainer.GetMapEntry(currentMapEntry.postion.rightID);
                    DeleteCardinalConnect(entry, target, CardinalPoint.East);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndChangeCheck();
   

            // All Fields foldout:
            changed = EditorGUI.EndChangeCheck() || changed;
            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                entryFieldsFoldout = EditorGUILayout.Foldout(entryFieldsFoldout, "All Fields");
                if (entryFieldsFoldout)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Template", "Add any missing fields from the template."), EditorStyles.miniButton, GUILayout.Width(68)))
                    {
                        ApplyDialogueEntryTemplate(entry.fields);
                    }
                    if (GUILayout.Button(new GUIContent("Copy", "Copy these fields to the clipboard."), EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        CopyFields(entry.fields);
                    }
                    EditorGUI.BeginDisabledGroup(clipboardFields == null);
                    if (GUILayout.Button(new GUIContent("Paste", "Paste the clipboard into these fields."), EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        PasteFields(entry.fields);
                    }
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent(" ", "Add new m_field."), "OL Plus", GUILayout.Width(16))) entry.fields.Add(new Field());
                }
                EditorGUILayout.EndHorizontal();
                if (entryFieldsFoldout)
                {
                    DrawFieldsSection(entry.fields);
                }
            }
            finally
            {
                changed = EditorGUI.EndChangeCheck() || changed;
            }

            EditorGUI.EndDisabledGroup();

            if (changed)
            {
                BuildLanguageListFromFields(entry.fields);
                SetDatabaseDirty("Dialogue Entry Fields Changed");
            }
            return changed;
        }
        private void AlignAllCardinalPosition()
        {
            //스타트 엔트리를 먼저 기준으로 잡는다
            //스타트 엔트리에 연결점이 없다면 나올때 까지 다른 엔트리를 검색한다.
            //없다면 종료
            //있다면 그 엔트리부터 시작하면서 저장해간다.
            //주변이 전부 전기록에 저장되어있는 게 존재한다면 (없을거같음) 무한반복되면 이부분 수정
            
            List<int> prevEnt = new List<int>();
            prevEnt.Add(-1);
            MapEntry entry =currentMapContainer.GetFirstMapEntry();
            if (entry != null&&entry.IsExistCardinalPoints())
            {
                AlignNearCardinalPosition(entry, ref prevEnt);
            }
            else
            {
                var item =currentMapContainer.mapEntries.Where(x =>!prevEnt.Contains(x.postion.upID)|| !prevEnt.Contains(x.postion.downID) || !prevEnt.Contains(x.postion.leftID)|| !prevEnt.Contains(x.postion.rightID)).ToList<MapEntry>();
                if (item != null && item.Count != 0)
                {
                    AlignNearCardinalPosition(item[0], ref prevEnt);
                }
            }

            if (!prevEnt.Contains(entry.id))
            {
                prevEnt.Add(entry.id);
            }

        }
        private void AlignNearCardinalPosition(MapEntry entry, ref List<int> prevEnt)
        {
            Debug.LogWarning(entry.id+"/"+prevEnt.Count);
            MapEntry target;
            if (entry.postion.upID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.upID);
                if (!prevEnt.Contains(target.postion.upID))
                {
                    target.canvasRect = new Rect(entry.canvasRect.x, entry.canvasRect.y - entry.canvasRect.height - AutoHeightBetweenNodes, entry.canvasRect.width, entry.canvasRect.height);
                    prevEnt.Add(target.id);
                    AlignNearCardinalPosition(target, ref prevEnt);
                }
            }
            if (entry.postion.downID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.downID);
                if (!prevEnt.Contains(target.postion.downID))
                {
                    target.canvasRect = new Rect(entry.canvasRect.x, entry.canvasRect.y + entry.canvasRect.height + AutoHeightBetweenNodes, entry.canvasRect.width, entry.canvasRect.height);
                    prevEnt.Add(target.id);
                    AlignNearCardinalPosition(target, ref prevEnt);
                }
            }
            if (entry.postion.leftID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.leftID);
                if (!prevEnt.Contains(target.postion.leftID))
                {
                    target.canvasRect = new Rect(entry.canvasRect.x - entry.canvasRect.width - AutoWidthBetweenNodes, entry.canvasRect.y, entry.canvasRect.width, entry.canvasRect.height);
                    prevEnt.Add(target.id);
                    AlignNearCardinalPosition(target, ref prevEnt);
                }
            }
            if (entry.postion.rightID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.rightID);
                if (!prevEnt.Contains(target.postion.rightID))
                {
                    target.canvasRect = new Rect(entry.canvasRect.x + entry.canvasRect.width + AutoWidthBetweenNodes, entry.canvasRect.y, entry.canvasRect.width, entry.canvasRect.height);
                    prevEnt.Add(target.id);
                    AlignNearCardinalPosition(target, ref prevEnt);
                }
            }
        }
        private void AlignNearCardinalPosition(MapEntry entry)
        {
            MapEntry target;

            if (entry.postion.upID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.upID);
                target.canvasRect = new Rect(entry.canvasRect.x, entry.canvasRect.y - entry.canvasRect.height - 20 , entry.canvasRect.width, entry.canvasRect.height);
            }
            if (entry.postion.downID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.downID);
                target.canvasRect = new Rect(entry.canvasRect.x, entry.canvasRect.y + entry.canvasRect.height + 20 , entry.canvasRect.width, entry.canvasRect.height);
            }
            if (entry.postion.leftID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.leftID);
                target.canvasRect = new Rect(entry.canvasRect.x - entry.canvasRect.width - 20, entry.canvasRect.y, entry.canvasRect.width, entry.canvasRect.height);
            }
            if (entry.postion.rightID != -1)
            {
                target = currentMapContainer.GetMapEntry(entry.postion.rightID);
                target.canvasRect = new Rect(entry.canvasRect.x + entry.canvasRect.width + 20, entry.canvasRect.y, entry.canvasRect.width, entry.canvasRect.height);
            }
        }
        private void PrepareLinkToDestinations(MapEntry entry)
        {
            List<GUIContent> destinationList = new List<GUIContent>();
            destinationList.Add(new GUIContent("(Link To)", string.Empty));
            destinationList.Add(new GUIContent("(Another Conversation)", string.Empty));
            destinationList.Add(new GUIContent("(New Entry)", string.Empty));
            for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
            {
                var destinationEntry = currentMapContainer.mapEntries[i];
                if (destinationEntry != entry)
                {
                    if (linkToDebug)
                    {
                        destinationList.Add(new GUIContent("[" + destinationEntry.id + "]"));
                    }
                    else
                    {
                        var text = (prefs.preferTitlesForLinksTo && !string.IsNullOrEmpty(destinationEntry.Title))
                            ? ("<" + destinationEntry.Title + ">")
                            : GetMapEntryText(destinationEntry);
                        destinationList.Add(new GUIContent(Tools.StripRichTextCodes(text)));
                    }
                }
            }
            maplinkToDestinations = destinationList.ToArray();
            maplinkToDestinationsFromEntry = entry;
        }
        private GUIStyle GetLinkButtonStyle(MapEntry entry)
        {
            return ((database != null) && (entry != null) && database.IsPlayerID(entry.ActorID)) ? pcLinkButtonGUIStyle : npcLinkButtonGUIStyle;
        }
        private void MoveToEntry(MapEntry entry)
        {
            var x = entry.canvasRect.x + (entry.canvasRect.width / 2) - ((position.width / _zoom) / 2);
            var y = entry.canvasRect.y - (entry.canvasRect.height / _zoom);
            canvasScrollPosition = new Vector2(Mathf.Max(0, x), Mathf.Max(0, y));
            Repaint();
        }
        private int GetcurrentMapContainerIndex()
        {
            if (currentConversation != null)
            {
                if (conversationTitles == null) RecordConversationTitles();
                for (int i = 0; i < conversationTitles.Length; i++)
                {
                    if (string.Equals(currentConversation.Title, conversationTitles[i])) return i;
                }
            }
            return -1;
        }
        private void SetCurrentMapEntry(MapEntry entry)//현재 맵 엔트리 선택.
        {
            if (entry != null && currentMapContainer != null && entry.MapID != currentMapContainer.id)
            {
                var mapContainer = database.GetMapContainer(entry.MapID);
                OpenMapContainer(mapContainer);
                SetMapDropdownIndex(GetcurrentMapContainerIndex());
                InitializeMapTree();
                inspectorSelection = entry;
            }
            newSelectedLink = null;
            if (entry != currentMapEntry) ResetLuaWizards();
            currentMapEntry = entry;
            multinodeMapSelection.nodes.Clear();
            multinodeMapSelection.nodes.Add(entry);
            UpdateMapEntrySelection();
        }
        private void UpdateMapEntrySelection()
        {
            ResetMapTreecurrentMapEntryParticipants();
            if (multinodeMapSelection.nodes.Count == 0)
            {
                inspectorSelection = currentMapContainer;
                selectedLink = null;
            }
            else if (multinodeMapSelection.nodes.Count == 1)
            {
                inspectorSelection = currentMapEntry;
            }
            else
            {
                inspectorSelection = multinodeMapSelection;
            }
        }
        private void MoveLink(MapEntry entry, int linkIndex, int direction)
        {
            if ((entry != null) && (0 <= linkIndex && linkIndex < entry.outgoingLinks.Count))
            {
                int newIndex = Mathf.Clamp(linkIndex + direction, 0, entry.outgoingLinks.Count - 1);
                Link link = entry.outgoingLinks[linkIndex];
                entry.outgoingLinks.RemoveAt(linkIndex);
                entry.outgoingLinks.Insert(newIndex, link);
                SetDatabaseDirty("Move Link");
            }
        }
        private Dictionary<int, string> mapEntryText = new Dictionary<int, string>();
        private Dictionary<int, string> mapEntryNodeText = new Dictionary<int, string>();
        private Dictionary<int, GUIContent> mapNodeDescription = new Dictionary<int, GUIContent>();
        private Dictionary<int, bool> mapNodeHasSequence = new Dictionary<int, bool>();
        private Dictionary<int, bool> mapEntryHasEvent = new Dictionary<int, bool>();
        private Dictionary<int, bool> mapFoldouts = new Dictionary<int, bool>();
        private void ResetMapEntryText()
        {
            mapEntryText.Clear();
            mapEntryNodeText.Clear();
            mapNodeDescription.Clear();
            mapEntryHasEvent.Clear();
            mapNodeHasSequence.Clear();
        }
        public void ResetMapEntryText(MapEntry entry)
        {
            if (entry == null) return;
            if (mapEntryText.ContainsKey(entry.id)) mapEntryText[entry.id] = null;
            if (mapEntryNodeText.ContainsKey(entry.id)) mapEntryNodeText[entry.id] = null;
            mapEntryHasEvent[entry.id] = FullCheckDoesMapEntryHaveEvent(entry);
        }
        private bool FullCheckDoesMapEntryHaveEvent(MapEntry entry)
        {
            if (entry == null) return false;
            if (entry.onExecute != null && entry.onExecute.GetPersistentEventCount() > 0) return true;
            if (!string.IsNullOrEmpty(entry.sceneEventGuid)) return true;
            return false;
        }

        private bool DrawMapEntryLinks(
    MapEntry entry,
    ref Link linkToDelete,
    ref MapEntry entryToLinkFrom,
    ref MapEntry entryToLinkTo,
    ref bool linkToAnotherConversation)
        {

            if (currentMapContainer == null) return false;

            bool changed = false;
            try
            {
                EditorGUI.BeginChangeCheck();
#if UNITY_EDITOR_OSX
                linkToDebug = EditorGUILayout.Toggle(new GUIContent("Hide Link Text", "In Unity 2019.2-2019.3, a bug in Unity for Mac can hang the editor when link text contains characters that the editor handles improperly. If Unity hangs when opening the Links To dropdown, tick this."), linkToDebug);
#endif
                if (EditorGUI.EndChangeCheck() || maplinkToDestinationsFromEntry != entry)
                {
                    PrepareLinkToDestinations(entry);
                }

                EditorGUI.BeginChangeCheck();

                int destinationIndex = EditorGUILayout.Popup(new GUIContent("Links To:", "Add a link to another entry. Select (New Entry) to create and link to a new entry."), 0, maplinkToDestinations);
                if (destinationIndex > 0)
                {
                    entryToLinkFrom = entry;
                    if (destinationIndex == 1)
                    { // (Another Conversation)                        
                        entryToLinkTo = null;
                        linkToAnotherConversation = true;
                    }
                    else if (destinationIndex == 2)
                    { // (New Entry)
                        entryToLinkTo = null;
                        linkToAnotherConversation = false;
                    }
                    else
                    {
                        int destinationID = AssetListIndexToID(destinationIndex, maplinkToDestinations);
                        entryToLinkTo = currentMapContainer.mapEntries.Find(e => e.id == destinationID);
                        if (entryToLinkTo == null)
                        {
                            entryToLinkFrom = null;
                            Debug.LogError(string.Format("{0}: Couldn't find destination dialogue entry in database.", DialogueDebug.Prefix));
                        }
                    }
                    //EditorGUILayout.EndHorizontal();
                    return false;
                }
                int linkIndexToMoveUp = -1;
                int linkIndexToMoveDown = -1;
                if ((entry != null) && (entry.outgoingLinks != null))
                {
                    for (int linkIndex = 0; linkIndex < entry.outgoingLinks.Count; linkIndex++)
                    {
                        Link link = entry.outgoingLinks[linkIndex];
                        EditorGUILayout.BeginHorizontal();

                        if (link.destinationConversationID == currentMapContainer.id)
                        {
                            // Fix any links whose originDialogueID doesn't match the origin entry's ID:
                            if (link.originDialogueID != entry.id) link.originDialogueID = entry.id;

                            // Is a link to an entry in the current conversation, so handle normally:
                            MapEntry linkEntry = database.GetMapEntry(link);
                            if (linkEntry != null)
                            {
                                string linkText = (linkEntry == null) ? string.Empty
                                    : (linkEntry.isGroup ? GetMapEntryText(linkEntry) : linkEntry.responseButtonText);
                                if (string.IsNullOrEmpty(linkText) ||
                                    (prefs.preferTitlesForLinksTo && linkEntry != null && !string.IsNullOrEmpty(linkEntry.Title)))
                                {
                                    linkText = "<" + linkEntry.Title + ">";
                                }
                                GUIStyle linkButtonStyle = GetLinkButtonStyle(linkEntry);
                                if (GUILayout.Button(linkText, linkButtonStyle))
                                {
                                    if (linkEntry != null && showNodeEditor)
                                    {
                                        MoveToEntry(linkEntry);
                                        SetCurrentMapEntry(linkEntry);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    return false;
                                }
                            }
                        }
                        else
                        {

                            // Cross-conversation link:
                            link.destinationConversationID = DrawConversationsPopup(link.destinationConversationID);
                            link.destinationDialogueID = DrawCrossConversationEntriesPopup(link.destinationConversationID, link.destinationDialogueID);
                            if (showNodeEditor && GUILayout.Button(new GUIContent("Go", "Jump to this dialogue entry."), EditorStyles.miniButton, GUILayout.Width(28)))
                            {
                                var linkEntry = database.GetMapEntry(link);
                                if (linkEntry != null)
                                {
                                    SetCurrentMapEntry(linkEntry);
                                    MoveToEntry(linkEntry);
                                }
                            }
                        }

                        EditorGUI.BeginDisabledGroup(linkIndex == 0);
                        if (GUILayout.Button(new GUIContent("↑", "Move upID"), EditorStyles.miniButton, GUILayout.Width(22))) linkIndexToMoveUp = linkIndex;
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.BeginDisabledGroup(linkIndex == entry.outgoingLinks.Count - 1);
                        if (GUILayout.Button(new GUIContent("↓", "Move downID"), EditorStyles.miniButton, GUILayout.Width(22))) linkIndexToMoveDown = linkIndex;
                        EditorGUI.EndDisabledGroup();
                        link.priority = (ConditionPriority)EditorGUILayout.Popup((int)link.priority, priorityStrings, GUILayout.Width(100));
                        bool deleted = GUILayout.Button(new GUIContent(" ", "Delete link."), "OL Minus", GUILayout.Width(16));
                        if (deleted) linkToDelete = link;
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (linkIndexToMoveUp != -1) MoveLink(entry, linkIndexToMoveUp, -1);
                if (linkIndexToMoveDown != -1) MoveLink(entry, linkIndexToMoveDown, 1);

            }
            catch (NullReferenceException)
            {
                // Hide error if it occurs.
            }
            finally
            {
                changed = EditorGUI.EndChangeCheck();
                if (changed) SetDatabaseDirty("Links Changed [1]");
            }
            return changed;
        }
        private void DrawMapEntryContents(
    MapEntry entry,
    ref Link linkToDelete,
    ref MapEntry entryToLinkFrom,
    ref MapEntry entryToLinkTo,
    ref bool linkToAnotherConversation)
        {
            bool changed = false;
            try
            {
                EditorGUILayout.BeginVertical("button");

                changed = DrawMapEntryFieldContents();

                // Links:
                changed = DrawMapEntryLinks(entry, ref linkToDelete, ref entryToLinkFrom, ref entryToLinkTo, ref linkToAnotherConversation) || changed;
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }

            if (changed)
            {
                ResetMapEntryText(entry);
                SetDatabaseDirty("Links Changed [2]");
            }
        }
        private void LinkToAnotherMap(MapEntry source)
        {
            Link link = new Link();
            link.originConversationID = currentMapContainer.id;
            link.originDialogueID = source.id;
            link.destinationConversationID = -1;
            link.destinationDialogueID = -1;
            source.outgoingLinks.Add(link);
            SetDatabaseDirty("Link to Map");
        }

        private void CreateLink(MapEntry source, MapEntry destination)
        {
            if ((source != null) && (destination != null))
            {
                Link link = new Link();
                link.originConversationID = currentMapContainer.id;
                link.originDialogueID = source.id;
                link.destinationConversationID = currentMapContainer.id;
                link.destinationDialogueID = destination.id;
                source.outgoingLinks.Add(link);
                SetDatabaseDirty("Create Link");
            }
        }
        private void DrawMapTree()
        {
            // Setup:
            Link linkToDelete = null;
            MapEntry entryToLinkFrom = null;
            MapEntry entryToLinkTo = null;
            bool linkToAnotherConversation = false;

            // Draw the tree:
            EditorWindowTools.StartIndentedSection();
            DrawMapNode(mapTree, null, ref linkToDelete, ref entryToLinkFrom, ref entryToLinkTo, ref linkToAnotherConversation);
            EditorWindowTools.EndIndentedSection();

            // Handle deletion:
            if (linkToDelete != null)
            {
                DeleteLink(linkToDelete);
                InitializeMapTree();
            }

            // Handle linking:
            if (entryToLinkFrom != null)
            {
                if (entryToLinkTo == null)
                {
                    if (linkToAnotherConversation)
                    {
                        LinkToAnotherMap(entryToLinkFrom);
                    }
                    else
                    {
                        LinkToNewMapEntry(entryToLinkFrom);
                    }
                }
                else
                {
                    CreateLink(entryToLinkFrom, entryToLinkTo);
                }
                InitializeMapTree();
            }

            // Draw orphans:
            DrawOrphansFoldout();
        }
        private void InitializeMapTree()
        {
            ValidateStartMapEntryID();
            ResetMapTree();
            BuildMapTree();
            ResetMapOrphanIDs();
        }
        private void ValidateStartMapEntryID()
        {
            if (startMapEntry == null) startMapEntry = (currentMapContainer != null) ? currentMapContainer.GetFirstMapEntry() : null;
            if (startMapEntry != null)
            {
                if (startMapEntry.MapID != currentMapContainer.id)
                {
                    startMapEntry.MapID = currentMapContainer.id;
                    SetDatabaseDirty("Check/Set START entry mapContainer ID");
                }
            }
        }
        private void ResetMapTree()
        {
            mapTree = null;
            mapOrphans.Clear();
            ResetLanguageList();
        }
        private void ResetMapOrphanIDs()
        {
            mapOrphanIDsMapContainerID = -1;
        }
        private void BuildMapTree()
        {
            if (currentMapContainer == null) return;
            List<MapEntry> visited = new List<MapEntry>();
            mapTree = BuildMapNode(startMapEntry, null, 0, visited);
            RecordMapOrphans(visited);
            BuildLanguageListFromMap();
        }
        private void BuildLanguageListFromMap()
        {
            languages.Clear();
            for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
            {
                var entry = currentMapContainer.mapEntries[i];
                BuildLanguageListFromFields(entry.fields);
            }
        }
        private void RecordMapOrphans(List<MapEntry> visited)
        {

            if (visited.Count < currentMapContainer.mapEntries.Count)
            {
                for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
                {
                    var entry = currentMapContainer.mapEntries[i];
                    if (!visited.Contains(entry))
                    {
                        mapOrphans.Add(new MapNode(entry, null, GetMapEntryStyle(entry), 0, false, false));
                    }
                }
            }
        }
        private MapNode BuildMapNode(MapEntry entry, Link originLink, int level, List<MapEntry> visited)
        {
            if (entry == null) return null;
            bool wasEntryAlreadyVisited = visited.Contains(entry);
            if (!wasEntryAlreadyVisited) visited.Add(entry);

            // Create this node:
            float indent = DialogueEntryIndent * level;
            bool isLeaf = (entry.outgoingLinks.Count == 0);
            bool hasFoldout = !(isLeaf || wasEntryAlreadyVisited);
            GUIStyle guiStyle = wasEntryAlreadyVisited ? grayGUIStyle
                : isLeaf ? GetMapLeafStyle(entry) : GetMapEntryStyle(entry);
            MapNode node = new MapNode(entry, originLink, guiStyle, indent, !wasEntryAlreadyVisited, hasFoldout);
            if (!mapEntryFoldouts.ContainsKey(entry.id)) mapEntryFoldouts[entry.id] = true;

            // Add children:
            if (!wasEntryAlreadyVisited)
            {
                for (int i = 0; i < entry.outgoingLinks.Count; i++)
                {
                    var link = entry.outgoingLinks[i];
                    if (link.destinationConversationID == currentMapContainer.id) // Only show connection if within same conversation.
                    {
                        node.children.Add(BuildMapNode(currentMapContainer.GetMapEntry(link.destinationDialogueID), link, level + 1, visited));
                    }
                }
            }
            return node;
        }
        private GUIStyle GetMapEntryStyle(MapEntry entry)
        {
            return ((entry != null) && database.IsPlayerID(entry.ActorID)) ? pcLineGUIStyle : npcLineGUIStyle;
        }

        private GUIStyle GetMapLeafStyle(MapEntry entry)
        {
            return ((entry != null) && database.IsPlayerID(entry.ActorID)) ? pcLineLeafGUIStyle : npcLineLeafGUIStyle;
        }

        private bool AreMapParticipantsValid()
        {
            if (!areParticipantsValid)
            {
                areParticipantsValid = (database.GetActor(currentMapContainer.ActorID) != null) && (database.GetActor(currentMapContainer.ConversantID) != null);
            }
            return areParticipantsValid;
        }
        public void DrawMapFieldsFoldout()
        {
            EditorGUILayout.BeginHorizontal();
            conversationFieldsFoldout = EditorGUILayout.Foldout(conversationFieldsFoldout, "All Fields");
            if (conversationFieldsFoldout)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Template", "Add any missing fields from the template."), EditorStyles.miniButton, GUILayout.Width(68)))
                {
                    ApplyTemplate(currentMapContainer.fields, GetTemplateFields(currentMapContainer));
                }
                if (GUILayout.Button(new GUIContent("Copy", "Copy these fields to the clipboard."), EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    CopyFields(currentMapContainer.fields);
                }
                EditorGUI.BeginDisabledGroup(clipboardFields == null);
                if (GUILayout.Button(new GUIContent("Paste", "Paste the clipboard into these fields."), EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    PasteFields(currentMapContainer.fields);
                }
                EditorGUI.EndDisabledGroup();
            }
            if (GUILayout.Button(new GUIContent(" ", "Add new m_field."), "OL Plus", GUILayout.Width(16)))
            {
                currentMapContainer.fields.Add(new Field());
                SetDatabaseDirty("Add Conversation Field");
            }
            EditorGUILayout.EndHorizontal();
            if (conversationFieldsFoldout)
            {
                if (actorID == NoID) actorID = currentMapContainer.ActorID;
                if (conversantID == NoID) conversantID = currentMapContainer.ConversantID;
                int oldActorID = actorID;
                int oldConversantID = conversantID;
                EditorGUI.BeginChangeCheck();
                DrawFieldsSection(currentMapContainer.fields);
                if (EditorGUI.EndChangeCheck())
                {
                    actorID = currentMapContainer.ActorID;
                    conversantID = currentMapContainer.ConversantID;
                    UpdateConversationParticipant(oldActorID, actorID);
                    UpdateConversationParticipant(oldConversantID, conversantID);
                }
            }
        }
        private MapEntry startMapEntry = null;
        private MapContainer currentMapContainer
        {
            get
            {
                return _currentMap;
            }
            set
            {
                _currentMap = value;
                if (value != null) currentMapContainerID = value.id;
            }
        }
        int currentMapContainerID;
        MultinodeMapSelection multinodeMapSelection = new MultinodeMapSelection();
        public class MultinodeMapSelection
        {
            public List<MapEntry> nodes = new List<MapEntry>();
            public List<EntryGroup> groups = new List<EntryGroup>();
            public List<Vector2> offsets = new List<Vector2>(); // Used when snapping to grid

            public void Clear()
            {
                nodes.Clear();
                groups.Clear();
                offsets.Clear();
            }
        }
        private void DeleteCurrentMapInNodeEditor()
        {
            nodeEditorDeleteCurrentMapContainer = false;
            if (currentMapContainer != null)
            {
                database.maps.Remove(database.maps.Find(c => c.id == currentMapContainer.id));
            }
            ResetMapSection();
            ActivateMapNodeEditorMode();
            inspectorSelection = database;
            SetDatabaseDirty("Delete Maps");
        }

        private void ResetNodeEditorMapList()
        {
            mapsTitles = GetMapTitles();
            SetMapDropdownIndex(GetCurrentMapsIndex());
        }
        private void SetMapDropdownIndex(int index)
        {
            mapContainerIndex = index;
        }
        private int GetCurrentMapsIndex()
        {
            if (currentMapContainer != null)
            {
                if (mapsTitles == null) mapsTitles = GetMapTitles();
                for (int i = 0; i < mapsTitles.Length; i++)
                {
                    if (string.Equals(currentMapContainer.Title, mapsTitles[i])) return i;
                }
            }
            return -1;
        }
        private string[] GetMapTitles()
        {
            int numDuplicates = 0;
            var titles = new List<string>();
            var titlesWithoutAmpersand = new List<string>();
            var useFilter = !string.IsNullOrEmpty(mapTitleFilter);
            var lowercaseFilter = useFilter ? mapTitleFilter.ToLower() : string.Empty;
            if (database != null)
            {
                foreach (var mapContainer in database.maps)
                {
                    if (mapContainer == null) continue;
                    // Make sure titles will work with GUI popup menus:
                    var title = mapContainer.Title;
                    if (title == null) continue;
                    if (useFilter && !title.ToLower().Contains(lowercaseFilter)) continue;
                    if (title.StartsWith("/"))
                    {
                        title = "?" + title;
                        mapContainer.Title = title;
                    }
                    if (title.EndsWith("/"))
                    {
                        title += "?";
                        mapContainer.Title = title;
                    }
                    if (title.Contains("//"))
                    {
                        title = title.Replace("//", "/");
                        mapContainer.Title = title;
                    }
                    if (titles.Contains(title))
                    {
                        numDuplicates++;
                        title += " " + numDuplicates;
                        mapContainer.Title = title;
                    }
                    titles.Add(title);
                    titlesWithoutAmpersand.Add(title.Replace("&", "<AMPERSAND>"));
                }
            }
            return titlesWithoutAmpersand.ToArray();
        }
        private void DrawNodeEditorMapPopup()
        {
            if (mapsTitles == null) mapsTitles = GetMapTitles();
            if (mapContainerIndex == -1 && currentMapContainer != null)
            {
                mapContainerIndex = GetCurrentMapsIndex();
            }
            int newIndex = EditorGUILayout.Popup(mapContainerIndex, mapsTitles, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (newIndex != mapContainerIndex)
            {
                if (currentMapContainer != null)
                {
                    prevMapStack.Add(currentMapContainer.Title);
                }
                nextMapStack.Clear();
                SetMapDropdownIndex(newIndex);
                OpenMapContainer(GetMapByTitleIndex(mapContainerIndex));
                InitializeMapTree();
                inspectorSelection = currentMapContainer;
            }
        }
        private MapContainer GetMapByTitleIndex(int index)
        {
            if (mapsTitles == null) mapsTitles = GetMapTitles();
            if (0 <= index && index < mapsTitles.Length)
            {
                return database.GetMapContainer(mapsTitles[index].Replace("<AMPERSAND>", "&"));
            }
            else
            {
                return null;
            }
        }
        private void DrawNodeEditorMapTopControls()
        {
            EditorGUILayout.BeginHorizontal();
            DrawPrevMapButton();
            DrawNodeEditorMapPopup();
            if (GUILayout.Button(new GUIContent("+", "Create a new Map container"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                AddNewMapContainerToNodeEditor();
            }
            DrawAIGenerateConversationButton();
            DrawNextMapButton();
            DrawMapFilter();
            DrawZoomSlider();
            DrawMapNodeEditorMenu();
            EditorGUILayout.EndHorizontal();
        }
        private void GotoStartMapNodePosition()
        {
            var startMapEntry = currentMapContainer.GetFirstMapEntry();
            if (startMapEntry == null) return;
            canvasScrollPosition = new Vector2(Mathf.Max(0, startMapEntry.canvasRect.x - ((position.width - startMapEntry.canvasRect.width) / 2)), Mathf.Max(0, startMapEntry.canvasRect.y - 8));
        }
        private void DrawMapNodeEditorMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Home Position"), false, GotoCanvasHomePosition);
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Center on START"), false, GotoStartMapNodePosition);
                    if (IsConversationActive())
                    {
                        menu.AddItem(new GUIContent("Center on Current Entry"), false, GotoCurrentRuntimeEntry);
                    }
                    menu.AddItem(new GUIContent("Conversation Properties"), false, InspectConversationProperties);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Center on START"));
                    menu.AddDisabledItem(new GUIContent("Conversation Properties"));
                }
                menu.AddItem(new GUIContent("New Conversation"), false, AddNewConversationToNodeEditor);
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Duplicate Conversation"), false, CopyConversationCallback, null);
                    menu.AddItem(new GUIContent("Delete Conversation"), false, DeleteConversationCallback, null);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Duplicate Conversation"));
                    menu.AddDisabledItem(new GUIContent("Delete Conversation"));
                }
                menu.AddItem(new GUIContent("Templates/New From Template/Built-In/Quest Conversation"), false, CreateQuestConversationFromTemplate);
                menu.AddItem(new GUIContent("Templates/New From Template/From Template JSON..."), false, CreateConversationFromTemplate);
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Templates/Save Template JSON..."), false, SaveConversationTemplate);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Templates/Save Template JSON..."));
                }
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Split Pipes Into Nodes/Process Conversation"), false, SplitPipesIntoEntries, null);
                    menu.AddItem(new GUIContent("Split Pipes Into Nodes/Trim Whitespace Around Pipes"), trimWhitespaceAroundPipes, ToggleTrimWhitespaceAroundPipes);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Split Pipes Into Nodes/Process Conversation"));
                    menu.AddDisabledItem(new GUIContent("Split Pipes Into Nodes/Trim Whitespace Around Pipes"));
                }
                menu.AddItem(new GUIContent("Sort/By Title"), false, SortConversationsByTitle);
                menu.AddItem(new GUIContent("Sort/By ID"), false, SortConversationsByID);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/This Conversation"), false, ConfirmReorderIDsThisConversation);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/All Conversations"), false, ConfirmReorderIDsAllConversations);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/Depth First Reordering"), reorderIDsDepthFirst, () => { reorderIDsDepthFirst = !reorderIDsDepthFirst; });
                menu.AddItem(new GUIContent("Show/Show All Actor Names"), prefs.showAllActorNames, ToggleShowAllActorNames);
                menu.AddItem(new GUIContent("Show/Show Non-Primary Actor Names"), prefs.showOtherActorNames, ToggleShowOtherActorNames);
                menu.AddItem(new GUIContent("Show/Show Actor Portraits"), prefs.showActorPortraits, ToggleShowActorPortraits);
                menu.AddItem(new GUIContent("Show/Show Descriptions"), prefs.showDescriptions, ToggleShowDescriptions);
                menu.AddItem(new GUIContent("Show/Show Full Text On Hover"), prefs.showFullTextOnHover, ToggleShowFullTextOnHover);
                menu.AddItem(new GUIContent("Show/Show Link Order On Arrows"), prefs.showLinkOrderOnConnectors, () => { prefs.showLinkOrderOnConnectors = !prefs.showLinkOrderOnConnectors; });
                menu.AddItem(new GUIContent("Show/Show End Node Markers"), prefs.showEndNodeMarkers, ToggleShowEndNodeMarkers);
                menu.AddItem(new GUIContent("Show/Show Node IDs"), prefs.showNodeIDs, ToggleShowNodeIDs);
                menu.AddItem(new GUIContent("Show/Show Titles Instead of Text"), prefs.showTitlesInsteadOfText, ToggleShowTitlesBeforeText);
                menu.AddItem(new GUIContent("Show/Show Primary Actors in Lower Right"), prefs.showParticipantNames, ToggleShowParticipantNames);
                menu.AddItem(new GUIContent("Show/Prefer Titles For 'Links To' Menus"), prefs.preferTitlesForLinksTo, TogglePreferTitlesForLinksTo);
                menu.AddItem(new GUIContent("Show/Node Width/1x"), canvasRectWidthMultiplier == 1, SetNodeWidthMultiplier, (int)1);
                menu.AddItem(new GUIContent("Show/Node Width/2x"), canvasRectWidthMultiplier == 2, SetNodeWidthMultiplier, (int)2);
                menu.AddItem(new GUIContent("Show/Node Width/3x"), canvasRectWidthMultiplier == 3, SetNodeWidthMultiplier, (int)3);
                menu.AddItem(new GUIContent("Show/Node Width/4x"), canvasRectWidthMultiplier == 4, SetNodeWidthMultiplier, (int)4);
                menu.AddItem(new GUIContent("Grid/No Snap"), prefs.snapToGridAmount < MinorGridLineWidth, SetSnapToGrid, 0f);
                menu.AddItem(new GUIContent("Grid/12 pixels"), Mathf.Approximately(12f, prefs.snapToGridAmount), SetSnapToGrid, 12f);
                menu.AddItem(new GUIContent("Grid/24 pixels"), Mathf.Approximately(24f, prefs.snapToGridAmount), SetSnapToGrid, 24f);
                menu.AddItem(new GUIContent("Grid/36 pixels"), Mathf.Approximately(36f, prefs.snapToGridAmount), SetSnapToGrid, 36f);
                menu.AddItem(new GUIContent("Grid/48 pixels"), Mathf.Approximately(48f, prefs.snapToGridAmount), SetSnapToGrid, 48f);
                menu.AddItem(new GUIContent("Grid/Snap All Nodes To Grid"), false, SnapAllNodesToGrid);
                menu.AddItem(new GUIContent("Search/Search Bar"), isSearchBarOpen, ToggleDialogueTreeSearchBar);
                menu.AddItem(new GUIContent("Search/Global Search and Replace..."), false, OpenGlobalSearchAndReplace);
                menu.AddItem(new GUIContent("Settings/Auto Arrange After Adding Node"), prefs.autoArrangeOnCreate, ToggleAutoArrangeOnCreate);
                menu.AddItem(new GUIContent("Settings/Add New Nodes to Right"), prefs.addNewNodesToRight, ToggleAddNewNodesToRight);
                menu.AddItem(new GUIContent("Settings/Confirm Node and Link Deletion"), confirmDelete, ToggleConfirmDelete);
                menu.AddItem(new GUIContent("Outline Mode"), false, ActivateOutlineMode);
                if (currentConversation == null)
                {
                    menu.AddDisabledItem(new GUIContent("Refresh"));
                }
                else
                {
                    menu.AddItem(new GUIContent("Refresh"), false, RefreshConversation);
                }
                AddRelationsInspectorMenuItems(menu);
                if (customNodeMenuSetup != null) customNodeMenuSetup(database, menu);
                menu.ShowAsContext();
            }
        }
        private void DrawMapFilter()
        {
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("MapFilterTextField");
            mapTitleFilter = EditorGUILayout.TextField(GUIContent.none, mapTitleFilter, MoreEditorGuiUtility.ToolbarSearchTextFieldName);
            GUI.SetNextControlName("MapClearClearButton");
            if (GUILayout.Button("Clear", MoreEditorGuiUtility.ToolbarSearchCancelButtonName))
            {
                mapTitleFilter = string.Empty;
                GUI.FocusControl("MapClearClearButton"); // Need to deselect text field to clear text field's display.
            }
            if (EditorGUI.EndChangeCheck())
            {
                ResetNodeEditorMapList();
            }
            // [Contributed by Vladimir Beletsky: Pressing Return jumps to first conversation matching filter.
            if (Event.current.keyCode == KeyCode.Return &&
                !string.IsNullOrEmpty(mapTitleFilter) &&
                GUI.GetNameOfFocusedControl() == "MapFilterTextField" &&
                database != null)
            {
                var filter = mapTitleFilter.ToLower();
                foreach (var map in database.maps)
                {
                    if (map == null) continue;
                    var title = map.Title.ToLower();
                    if (title.Contains(filter))
                    {
                        OpenMapContainer(map);
                        break;
                    }
                }
            }
        }
        private void DrawPrevMapButton()
        {
            EditorGUI.BeginDisabledGroup(nextMapStack.Count == 0);
            if (GUILayout.Button(GUIContent.none, GUI.skin.GetStyle("AC LeftArrow"), GUILayout.Width(12), GUILayout.Height(16)))
            {
                GotoPrevMap();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawNextMapButton()
        {
            EditorGUI.BeginDisabledGroup(nextMapStack.Count == 0);
            if (GUILayout.Button(GUIContent.none, GUI.skin.GetStyle("AC RightArrow"), GUILayout.Width(12), GUILayout.Height(16)))
            {
                GotoNextMap();
            }
            EditorGUI.EndDisabledGroup();
        }
        private void GotoPrevMap()
        {
            if (currentMapContainer != null)
            {
                nextMapStack.Insert(0, currentMapContainer.Title);
            }
            string mapContainerTitle = prevMapStack[prevMapStack.Count - 1];
            prevMapStack.RemoveAt(prevMapStack.Count - 1);
            OpenMapContainer(database.GetMapContainer(mapContainerTitle));
            InitializeMapTree();
            inspectorSelection = currentMapContainer;
            mapContainerIndex = GetCurrentMapsIndex();
        }

        private void GotoNextMap()
        {
            if (currentMapContainer != null)
            {
                prevMapStack.Add(currentMapContainer.Title);
            }
            string mapContainerTitle = nextMapStack[0];
            nextMapStack.RemoveAt(0);
            OpenMapContainer(database.GetMapContainer(mapContainerTitle));
            InitializeMapTree();
            inspectorSelection = currentMapContainer;
            mapContainerIndex = GetCurrentMapsIndex();
        }
        private void AddNewMapContainerToNodeEditor()
        {
            AddNewMapContainer();
            ActivateMapNodeEditorMode();
            inspectorSelection = currentMapContainer;
        }

        private MapContainer AddNewMapContainer()
        {
            Undo.RegisterCompleteObjectUndo(database, "New MapContainer");
            MapContainer newMapContainer = AddNewAsset<MapContainer>(database.maps);

            // Use same actors as previous MapContainer:
            if (currentMapContainer != null)
            {
                newMapContainer.ActorID = currentMapContainer.ActorID;
                newMapContainer.ConversantID = currentMapContainer.ConversantID;
                var startMapEntry = newMapContainer.GetFirstMapEntry();
                if (startMapEntry != null)
                {
                    startMapEntry.ActorID = currentMapContainer.ActorID;
                    startMapEntry.ConversantID = currentMapContainer.ConversantID;
                }
            }

            SetDatabaseDirty("Add New MapContainer");

            if (newMapContainer != null) OpenMapContainer(newMapContainer);
            return newMapContainer;
        }
        private void DrawMapCanvas()
        {
            // Make scrollbars invisible:
            GUIStyle verticalScrollbar = GUI.skin.verticalScrollbar;
            GUIStyle horizontalScrollbar = GUI.skin.horizontalScrollbar;
            GUI.skin.verticalScrollbar = GUIStyle.none;
            GUI.skin.horizontalScrollbar = GUIStyle.none;


            try
            {
                canvasScrollPosition = GUI.BeginScrollView(new Rect(0, 0, (1 / _zoom) * position.width, (1 / _zoom) * position.height), canvasScrollPosition, new Rect(0, 0, canvasScrollView.x, canvasScrollView.y), false, false);
                DrawCanvasBackground();
                DrawMapCanvasContents();
            }
            finally
            {
                GUI.EndScrollView();
                if (currentMapContainer != null)
                {
                    currentMapContainer.canvasScrollPosition = canvasScrollPosition;
                    currentMapContainer.canvasZoom = _zoom;
                }
            }

            //--- For debugging zoom:
            //EditorGUI.LabelField(new Rect(10, 60, 500, 30), "pos=" + canvasScrollPosition);
            //EditorGUI.LabelField(new Rect(10, 90, 500, 30), "mouse=" + Event.current.mousePosition);

            // Restore previous scrollbar style:
            GUI.skin.verticalScrollbar = verticalScrollbar;
            GUI.skin.horizontalScrollbar = horizontalScrollbar;
        }
        void DrawMapSectionNodeStyle()
        {
            if (!(Application.isPlaying && DialogueManager.hasInstance)) currentRuntimeEntry = null;

            CheckDialogueTreeGUIStyles();
            if (_zoom != currentEntryGroupHeadingZoom && entryGroupHeadingStyle != null)
            {
                currentEntryGroupHeadingZoom = _zoom;
                entryGroupHeadingStyle.fontSize = Mathf.RoundToInt((_zoom >= 1) ? entryGroupHeadingBaseFontSize
                    : (entryGroupHeadingBaseFontSize / _zoom));
                entryGroupHeadingHeight = Mathf.RoundToInt((_zoom >= 1) ? EntryGroupHeadingBaseHeight
                    : (EntryGroupHeadingBaseHeight / _zoom));
                entryGroupLabelStyle.fontSize = entryGroupHeadingStyle.fontSize;
            }

            if (nodeEditorDeleteCurrentMapContainer) DeleteCurrentMapInNodeEditor();
            //--- Unnecessary: if (inspectorSelection == null) inspectorSelection = currentMap;

            DrawNodeEditorMapTopControls();

            var topOffset = GetTopOffsetHeight();

            scaledPosition = new Rect(position.x, position.y, (1 / _zoom) * position.width, (1 / _zoom) * position.height);
            _zoomArea = new Rect(0, topOffset, position.width, position.height - topOffset);
            if (_zoom > 1)
            {
                _zoomArea = new Rect(0, topOffset, position.width + ((_zoom - 1) * position.width), position.height - topOffset + ((_zoom - 1) * (position.height - topOffset)));
            }
            nodeEditorVisibleRect = new Rect(canvasScrollPosition.x, canvasScrollPosition.y, position.width / _zoom, position.height / _zoom);
            EditorZoomArea.Begin(_zoom, _zoomArea);
            try
            {
                DrawMapCanvas();
                HandleEmptyMapCanvasEvents();
                HandleKeyMapEvents();
            }
            finally
            {
                EditorZoomArea.End();
            }

            DrawDatabaseName();
            if (prefs.showParticipantNames) DrawParticipantsOnCanvas();

            Handles.color = MajorGridLineColor;
            Handles.DrawLine(new Vector2(0, topOffset), new Vector2(position.width, topOffset));
        }
        private void DeleteMapLinkCallback(object o)
        {
            Link linkToDelete = o as Link;
            if (linkToDelete == null) return;
            if (!confirmDelete || EditorUtility.DisplayDialog("Delete selected link?", "You cannot undo this action.", "Delete", "Cancel"))
            {
                MapEntry origin = currentMapContainer.GetMapEntry(linkToDelete.originDialogueID);
                DeleteNodeLinkToMapID(origin, linkToDelete.destinationConversationID, linkToDelete.destinationDialogueID);
                InitializeMapTree();
                ResetMapEntryText();
                Repaint();
                SetDatabaseDirty("Delete Link");
            }
        }
        private void HandleKeyMapEvents()//단축키 설정
        {
            if (Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Home && !showQuickDialogueTextEntry)
                {
                    // Home (top of canvas):
                    GotoCanvasHomePosition();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.PageUp)
                {
                    // PageUp canvas:
                    PageUpCanvas();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.PageDown)
                {
                    // PageDown canvas:
                    PageDownCanvas();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (showQuickDialogueTextEntry)
                    {
                        CloseQuickDialogueTextEntry();
                    }
                    else if (string.Equals(GUI.GetNameOfFocusedControl(), "SearchTextField"))
                    {
                        // Find next search result if press enter in search text field:
                        SearchDialogueTree(1);
                    }
                }
                else if (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace)
                {
                    // Delete/backspace key:
                    if (showQuickDialogueTextEntry)
                    {
                        // Do nothing; using quick Dialogue Text entry.
                    }
                    else if (string.Equals(GUI.GetNameOfFocusedControl(), "SearchTextField"))
                    {
                        // Do nothing; is editing search bar text.
                    }
                    else if (selectedLink != null)
                    {
                        Event.current.Use();
                        DeleteMapLinkCallback(selectedLink);
                    }
                    else if (currentMapEntry != null)
                    {
                        Event.current.Use();
                        if (multinodeSelection.nodes.Count > 1)
                        {
                            DeleteMultipleMapEntriesCallback(currentMapContainer);
                        }
                        else if (currentMapEntry != startMapEntry)
                        {
                            DeleteMapEntryCallback(currentMapContainer);
                        }
                    }
                }
                else if (Event.current.keyCode == KeyCode.D && (Event.current.command || Event.current.control) && Event.current.alt)
                {
                    // Ctrl/Cmd+Alt+D (Duplicate) key:
                    if (currentMapEntry != null && !showQuickDialogueTextEntry)
                    {
                        Event.current.Use();
                        DuplicateMultipleEntries();
                    }
                }
                else if (Event.current.keyCode == KeyCode.N && (Event.current.command || Event.current.control) && Event.current.alt)
                {
                    wasShiftDown = Event.current.shift;
                    // Ctrl/Cmd+Alt+N (New) key:
                    if (currentMapEntry != null && !showQuickDialogueTextEntry)
                    {
                        AddChildCallback(currentMapEntry);
                    }
                    else
                    {
                        AddChildCallback(null);
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.C && (Event.current.command || Event.current.control) && Event.current.alt)
                {
                    // Ctrl/Cmd+Alt+C (Copy) key:
                    if (showQuickDialogueTextEntry)
                    {
                        // Do nothing; in quick dialogue text entry.
                    }
                    else if (multinodeSelection.nodes.Count > 1)
                    {
                        CopyMultipleEntriesCallback(null);
                    }
                    else if (currentMapEntry != null)
                    {
                        CopyEntryCallback(currentMapEntry);
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.V && (Event.current.command || Event.current.control) && Event.current.alt)
                {
                    // Ctrl/Cmd+Alt+V (Paste) key:
                    if (showQuickDialogueTextEntry)
                    {
                        // Do nothing; in quick dialogue text entry.
                    }
                    if (!IsNodeClipboardEmpty())
                    {
                        PasteMultipleEntriesCallback(currentMapEntry);
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Comma && (Event.current.command || Event.current.control) && Event.current.shift)
                {
                    // Ctrl/Cmd+Shift+< key: (previous conversation)
                    OpenPreviousConversation();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Period && (Event.current.command || Event.current.control) && Event.current.shift)
                {
                    // Ctrl/Cmd+Shift+> key: (next conversation)
                    OpenNextConversation();
                    Event.current.Use();
                }
            }
        }

        private void DrawQuickMapTextEntry()
        {
            if (currentMapEntry == null) return;
            GUI.SetNextControlName("QuickMapText");
            EditorGUI.BeginChangeCheck();
            currentMapEntry.DialogueText = GUI.TextArea(GetQuickMapTextRect(), currentMapEntry.DialogueText);
            if (EditorGUI.EndChangeCheck())
            {
                mapEntryNodeText[currentMapEntry.id] = BuildMapEntryNodeText(currentMapEntry);
            }
            GUI.FocusControl("QuickMapText");
        }
        private Rect GetQuickMapTextRect()
        {
            return quickDialogueTextEntryRect;
        }
        private string BuildMapEntryNodeText(MapEntry entry)//node TExt
        {
            var text = string.Empty;
            if (prefs.showTitlesInsteadOfText)
            {
                var title = entry.Title;
                if (!(string.IsNullOrEmpty(title) || string.Equals(title, "New Dialogue Entry"))) text = title;
            }
            if (string.IsNullOrEmpty(text)) text = entry.currentMenuText;
            if (string.IsNullOrEmpty(text)) text = entry.currentDialogueText;
            if (string.IsNullOrEmpty(text)) text = "<" + entry.Title + ">";
            if (entry.isGroup) text = "{group} " + text;
            if (text.Contains("\n")) text = text.Replace("\n", string.Empty);
            int extraLength = 0;
            if (prefs.showAllActorNames)
            {
                string actorName = GetActorNameByID(entry.ActorID);
                if (actorName != null) extraLength = actorName.Length;
                text = string.Format("{0}:\n{1}", actorName, text);
            }
            else if (prefs.showOtherActorNames && entry.ActorID != currentMapContainer.ActorID && (entry.ActorID != currentMapContainer.ConversantID))
            {
                text = string.Format("{0}: {1}", GetActorNameByID(entry.ActorID), text);
            }
            if (prefs.showNodeIDs)
            {
                text = "[" + entry.id + "] " + text;
            }
            if (!showNodeEditor)
            {
                if (!string.IsNullOrEmpty(entry.conditionsString)) text += " [condition]";
                if (!string.IsNullOrEmpty(entry.userScript)) text += " {script}";
            }
            //if ((entry.outgoingLinks == null) || (entry.outgoingLinks.Count == 0)) text += " [END]";
            return text.Substring(0, Mathf.Min(text.Length, canvasRectWidthMultiplier * MaxNodeTextLength + extraLength));
        }
        private void DrawMapCanvasContents()
        {
            if (currentMapContainer == null) return;
            newSelectedLink = null;
            DrawAllMapEntryGroups();
            DrawAllMapConnectors();
            DrawAllMapNodes();
            DrawLasso();
            CheckNewSelectedMapLink();
            if (showQuickDialogueTextEntry) DrawQuickMapTextEntry();
            else if (isRenamingEntryGroup) DrawEntryGroupRenameField();
            newSelectedLink = null;
        }
        private void CheckNewSelectedCLink()
        {
            if ((newSelectedLink != null) && (newSelectedLink != selectedLink))
            {
                selectedLink = newSelectedLink;
                inspectorSelection = selectedLink;
                isLassoing = false;
                multinodeSelection.Clear();
                Event.current.Use();
            }
            else if (currentEntry != null)
            {
                selectedLink = null;
            }
        }
        private void AddEntryToSelection(MapEntry entry)
        {
            newSelectedLink = null;
            currentMapEntry = entry;
            multinodeMapSelection.nodes.Add(entry);
            UpdateMapEntrySelection();
        }
        private void MakeMapCardinalLinkCallback(object o)
        {
            maplinkSourceEntry = o as MapEntry;
            isMakingCLink = (maplinkSourceEntry != null);
        }
        private void MakeMapLinkCallback(object o)
        {
            maplinkSourceEntry = o as MapEntry;
            isMakingLink = (maplinkSourceEntry != null);
        }
        private void DeleteNodeLinkToMapID(MapEntry origin, int destinationConversationID, int destinationDialogueID)
        {
            if (origin == null) return;
            Link link = origin.outgoingLinks.Find(x => (x.destinationConversationID == destinationConversationID) && (x.destinationDialogueID == destinationDialogueID));
            if (link == null) return;
            origin.outgoingLinks.Remove(link);
            SetDatabaseDirty("Delete Node Link");
        }
        private void DeleteMapEntryCallback(object o)
        {
            MapEntry entryToDelete = o as MapEntry;
            if (entryToDelete == null) return;
            if (!confirmDelete || EditorUtility.DisplayDialog("Delete selected entry?", "You cannot undo this action.", "Delete", "Cancel"))
            {
                if (entryToDelete.IsExistCardinalPoints())
                {
                    DeleteCardinalConnect(entryToDelete,CardinalPoint.West);
                    if (entryToDelete.postion.leftID != -1) currentMapContainer.GetMapEntry(entryToDelete.postion.leftID).postion.rightID=-1;
                }
                foreach (var origin in currentMapContainer.mapEntries)
                {
                    DeleteNodeLinkToMapID(origin, entryToDelete.MapID, entryToDelete.id);
                }
                MapEntry entry = currentMapContainer.mapEntries.Find(x => x.id == entryToDelete.id);
                entry.postion = new MapEntry.Postion();

                currentMapContainer.mapEntries.Remove(entry);
                InitializeMapTree();
                ResetMapEntryText();
                Repaint();
                SetDatabaseDirty("Delete Dialogue Entry");
            }
        }
        private void DeleteMultipleMapEntriesCallback(object o)
        {
            if (!confirmDelete || EditorUtility.DisplayDialog("Delete selected entries?", "You cannot undo this action.", "Delete", "Cancel"))
            {
                foreach (MapEntry entryToDelete in multinodeMapSelection.nodes)
                {
                    if (entryToDelete != startMapEntry)
                    {
                        foreach (var origin in currentMapContainer.mapEntries)
                        {
                            DeleteNodeLinkToMapID(origin, entryToDelete.MapID, entryToDelete.id);
                        }
                        MapEntry entry = currentMapContainer.mapEntries.Find(x => x.id == entryToDelete.id);
                        currentMapContainer.mapEntries.Remove(entry);
                    }
                }
                InitializeMapTree();
                ResetMapEntryText();
                Repaint();
                SetDatabaseDirty("Delete Map Entries");
            }
        }
        private void ShowNodeContextMenu(MapEntry entry)
        {
            EditorZoomArea.End();
            wasShiftDown = Event.current.shift;

            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("New CardinalPoint/New Upper"), false, AddMapUpChildCallback, entry);
            contextMenu.AddItem(new GUIContent("New CardinalPoint/New Down"), false, AddMapDownChildCallback, entry);
            contextMenu.AddItem(new GUIContent("New CardinalPoint/New Right"), false, AddMapRightChildCallback, entry);
            contextMenu.AddItem(new GUIContent("New CardinalPoint/New Left"), false, AddMapLeftChildCallback, entry);
            contextMenu.AddItem(new GUIContent("Make Cardinal Link"), false, MakeMapCardinalLinkCallback, entry);
            contextMenu.AddItem(new GUIContent("Create Child Node"), false, AddMapChildCallback, entry);
            contextMenu.AddItem(new GUIContent("Make Link"), false, MakeMapLinkCallback, entry);
            if ((multinodeMapSelection.nodes.Count > 1) && (multinodeMapSelection.nodes.Contains(entry)))
            {
                contextMenu.AddItem(new GUIContent("Copy"), false, CopyMultipleEntriesCallback, entry);
                if (IsNodeClipboardEmpty())
                {
                    contextMenu.AddDisabledItem(new GUIContent("Paste"));
                }
                else
                {
                    contextMenu.AddItem(new GUIContent("Paste"), false, PasteMultipleEntriesCallback, entry);
                }
                contextMenu.AddItem(new GUIContent("Delete"), false, DeleteMultipleMapEntriesCallback, entry);
            }
            else if (entry == startMapEntry)
            {
                contextMenu.AddDisabledItem(new GUIContent("Copy"));
                if (IsNodeClipboardEmpty())
                {
                    contextMenu.AddDisabledItem(new GUIContent("Paste"));
                }
                else
                {
                    contextMenu.AddItem(new GUIContent("Paste"), false, PasteEntryCallback, entry);
                }
                contextMenu.AddDisabledItem(new GUIContent("Delete"));
            }
            else
            {
                contextMenu.AddItem(new GUIContent("Copy"), false, CopyEntryCallback, entry);
                if (IsNodeClipboardEmpty())
                {
                    contextMenu.AddDisabledItem(new GUIContent("Paste"));
                }
                else
                {
                    contextMenu.AddItem(new GUIContent("Paste"), false, PasteEntryCallback, entry);
                }
                contextMenu.AddItem(new GUIContent("Delete"), false, DeleteMapEntryCallback, entry);
            }
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically"), false, ArrangeNodesCallback, AutoArrangeStyle.Vertically);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically (alternate)"), false, ArrangeNodesCallback, AutoArrangeStyle.VerticallyOld);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Horizontally"), false, ArrangeNodesCallback, AutoArrangeStyle.Horizontally);
            contextMenu.AddItem(new GUIContent("Snap All Nodes to Grid"), false, SnapAllNodesToGrid);

            AddCanvasContextMenuGotoItems(contextMenu);

            contextMenu.ShowAsContext();
            contextMenuPosition = Event.current.mousePosition;

            EditorZoomArea.Begin(_zoom, _zoomArea);
        }
        private void RemoveEntryFromSelection(MapEntry entry)
        {
            newSelectedLink = null;
            multinodeMapSelection.nodes.Remove(entry);
            if (multinodeMapSelection.nodes.Count == 0)
            {
                currentMapEntry = null;
            }
            else
            {
                currentMapEntry = multinodeMapSelection.nodes[multinodeMapSelection.nodes.Count - 1];
            }
            UpdateMapEntrySelection();
        }
        private bool LinkExists(MapEntry origin, MapEntry destination)
        {
            Link link = origin.outgoingLinks.Find(x => ((x.destinationConversationID == destination.MapID) && (x.destinationDialogueID == destination.id)));
            return (link != null);
        }
        private void FinishMakingMapCLink()
        {
            if ((maplinkSourceEntry != null) && (maplinkTargetEntry != null) &&
                (maplinkSourceEntry != maplinkTargetEntry))
            {
                float rx= maplinkSourceEntry.canvasRect.x - maplinkTargetEntry.canvasRect.x;
                float ry= maplinkSourceEntry.canvasRect.y - maplinkTargetEntry.canvasRect.y;
                if(Mathf.Abs(rx)> Mathf.Abs(ry))
                {
                    //좌우
                    if (rx > 0)
                    {
                        //좌
                        CardinalConnectMapEntry(maplinkSourceEntry, maplinkTargetEntry, CardinalPoint.West);
                    }
                    else
                    {
                        //우
                        CardinalConnectMapEntry(maplinkSourceEntry, maplinkTargetEntry, CardinalPoint.East);
                    }
                }
                else
                {
                    //위아래
                    if (ry > 0)
                    {
                        //위
                        CardinalConnectMapEntry(maplinkSourceEntry, maplinkTargetEntry, CardinalPoint.North);
                    }
                    else
                    {
                        //아래
                        CardinalConnectMapEntry(maplinkSourceEntry, maplinkTargetEntry, CardinalPoint.South);

                    }
                }
                InitializeMapTree();
                ResetMapEntryText();
                Repaint();
            }
            isMakingCLink = false;
            maplinkSourceEntry = null;
            maplinkTargetEntry = null;
            SetDatabaseDirty("Make CLink");
        }
        private void FinishMakingMapLink()
        {
            if ((maplinkSourceEntry != null) && (maplinkTargetEntry != null) &&
                (maplinkSourceEntry != maplinkTargetEntry) &&
                !LinkExists(maplinkSourceEntry, maplinkTargetEntry))
            {
                Link link = new Link();
                link.originConversationID = currentMapContainer.id;
                link.originDialogueID = maplinkSourceEntry.id;
                link.destinationConversationID = currentMapContainer.id;
                link.destinationDialogueID = maplinkTargetEntry.id;
                maplinkSourceEntry.outgoingLinks.Add(link);
                InitializeMapTree();
                ResetMapEntryText();
                Repaint();
            }
            isMakingLink = false;
            maplinkSourceEntry = null;
            maplinkTargetEntry = null;
            SetDatabaseDirty("Make Link");
        }

        private void HandleNodeEvents(MapEntry entry)
        {
            if (prefs.showFullTextOnHover && entry.canvasRect.Contains(Event.current.mousePosition))
            {
                currentHoveredMapEntry = entry;
            }
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (!showQuickDialogueTextEntry) GUI.FocusControl("SearchClearButton"); // Deselect search bar text input field.
                    if (showQuickDialogueTextEntry && GetQuickDialogueTextRect().Contains(Event.current.mousePosition))
                    {
                        // Do nothing; clicked in quick dialogue text entry.
                    }
                    else if (entry.canvasRect.Contains(Event.current.mousePosition))
                    {
                        if (IsRightMouseButtonEvent())
                        {
                            currentMapEntry = entry;
                            ShowNodeContextMenu(entry);
                            Event.current.Use();
                            CloseQuickDialogueTextEntry();
                        }
                        else if (Event.current.button == LeftMouseButton)
                        {
                            CloseQuickDialogueTextEntry();
                            newSelectedLink = null;
                            if (isMakingCLink)
                            {
                                FinishMakingMapCLink();
                            }
                            else if (isMakingLink)
                            {
                                FinishMakingMapLink();
                            }
                            else
                            {
                                mapNodeToDrag = entry;
                                dragged = false;
                                if (!IsShiftDown() && ((multinodeMapSelection.nodes.Count <= 1) || !multinodeMapSelection.nodes.Contains(entry)))
                                {
                                    if (IsAltDown()) // Alt+Click makes link.
                                    {
                                        maplinkSourceEntry = currentMapEntry;
                                        maplinkTargetEntry = entry;
                                        FinishMakingMapLink();
                                    }
                                    SetCurrentMapEntry(entry);
                                    if (Event.current.clickCount == 2 && !IsAltDown())
                                    {
                                        OpenQuickDialogueTextEntry();
                                    }
                                }
                            }
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (hasStartedSnapToGrid)
                    {
                        FinishSnapToGrid();
                        hasStartedSnapToGrid = false;
                    }
                    if (Event.current.button == LeftMouseButton)
                    {
                        if (!isMakingLink && entry.canvasRect.Contains(Event.current.mousePosition))
                        {
                            newSelectedLink = null;
                            if (isLassoing)
                            {
                                FinishMapLasso();
                                CloseQuickDialogueTextEntry();
                            }
                            else if (IsShiftDown())
                            {
                                if (multinodeMapSelection.nodes.Contains(entry))
                                {
                                    RemoveEntryFromSelection(entry);
                                }
                                else
                                {
                                    AddEntryToSelection(entry);
                                }
                                CloseQuickDialogueTextEntry();
                            }
                            else if (showQuickDialogueTextEntry && GetQuickDialogueTextRect().Contains(Event.current.mousePosition))
                            {
                                return;
                            }
                            else
                            {
                                if (!(dragged && (multinodeMapSelection.nodes.Count > 1)))
                                {
                                    SetCurrentMapEntry(entry);
                                }
                            }
                            mapNodeToDrag = null;
                            dragged = false;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if ((entry == mapNodeToDrag))
                    {
                        dragged = true;
                        MapDragMultiSelection();
                        Event.current.Use();
                    }
                    break;
            }
            if ((isMakingLink||isMakingCLink) && Event.current.isMouse)
            {
                if (entry.canvasRect.Contains(Event.current.mousePosition))
                {
                    maplinkTargetEntry = entry;
                }
            }
        }
        private void MapDragMultiSelection()
        {
            var snapToGrid = prefs.snapToGridAmount >= MinorGridLineWidth;

            if (!snapToGrid)
            {
                // Smooth drag all selected nodes (no snap to grid):
                for (int i = 0; i < multinodeMapSelection.nodes.Count; i++)
                {
                    var dragEntry = multinodeMapSelection.nodes[i];
                    dragEntry.canvasRect.x += Event.current.delta.x;
                    dragEntry.canvasRect.y += Event.current.delta.y;
                    dragEntry.canvasRect.x = Mathf.Max(1f, dragEntry.canvasRect.x);
                    dragEntry.canvasRect.y = Mathf.Max(1f, dragEntry.canvasRect.y);
                }
            }
            else if (mapNodeToDrag != null)
            {
                // Drag with grid snapping:
                if (!hasStartedSnapToGrid)
                {
                    // Record other nodes' offsets:
                    multinodeMapSelection.offsets.Clear();
                    for (int i = 0; i < multinodeMapSelection.nodes.Count; i++)
                    {
                        var node = multinodeMapSelection.nodes[i];
                        var offset = node.canvasRect.center - mapNodeToDrag.canvasRect.center;
                        multinodeMapSelection.offsets.Add(offset);
                    }
                }

                // Move main node:
                mapNodeToDrag.canvasRect.x = ((int)((Event.current.mousePosition.x - mapNodeToDrag.canvasRect.width / 2) / prefs.snapToGridAmount) * prefs.snapToGridAmount);
                mapNodeToDrag.canvasRect.y = ((int)((Event.current.mousePosition.y - mapNodeToDrag.canvasRect.height / 2) / prefs.snapToGridAmount) * prefs.snapToGridAmount);
                mapNodeToDrag.canvasRect.x = Mathf.Max(1f, mapNodeToDrag.canvasRect.x);
                mapNodeToDrag.canvasRect.y = Mathf.Max(1f, mapNodeToDrag.canvasRect.y);

                // Move other nodes according to offset from main node:
                for (int i = 0; i < multinodeMapSelection.nodes.Count; i++)
                {
                    var dragEntry = multinodeMapSelection.nodes[i];
                    if (dragEntry == mapNodeToDrag) continue;
                    dragEntry.canvasRect.center = mapNodeToDrag.canvasRect.center + multinodeMapSelection.offsets[i];
                    dragEntry.canvasRect.x = Mathf.Max(1f, dragEntry.canvasRect.x);
                    dragEntry.canvasRect.y = Mathf.Max(1f, dragEntry.canvasRect.y);
                }
            }

            for (int i = 0; i < multinodeMapSelection.groups.Count; i++)
            {
                var group = multinodeMapSelection.groups[i];

                if (snapToGrid)
                {
                    group.rect.x = (((int)group.rect.x) / prefs.snapToGridAmount) * prefs.snapToGridAmount;
                    group.rect.y = (((int)group.rect.y) / prefs.snapToGridAmount) * prefs.snapToGridAmount;
                }
                else
                {
                    group.rect.x += Event.current.delta.x;
                    group.rect.y += Event.current.delta.y;
                }
                group.rect.x = Mathf.Max(1f, group.rect.x);
                group.rect.y = Mathf.Max(1f, group.rect.y);
            }
            hasStartedSnapToGrid = true;
            SetDatabaseDirty("Drag");
        }
        private void CheckMapOrphanIDs()
        {
            if (currentMapContainer.id != mapOrphanIDsMapContainerID)
            {
                mapOrphanIDsMapContainerID = currentMapContainer.id;
                orphanIDs.Clear();
                for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
                {
                    orphanIDs.Add(currentMapContainer.mapEntries[i].id);
                }
                for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
                {
                    var entry = currentMapContainer.mapEntries[i];
                    for (int j = 0; j < entry.outgoingLinks.Count; j++)
                    {
                        var link = entry.outgoingLinks[j];
                        if (link.originConversationID == link.destinationConversationID)
                        {
                            orphanIDs.Remove(link.destinationDialogueID);
                        }
                    }
                }
            }
        }
        private void DrawAllMapNodes()
        {
            CheckMapOrphanIDs();
            for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
            {
                DrawMapEntryNode(currentMapContainer.mapEntries[i]);
            }
            var prevHoveredEntry = currentMapHoveredEntry;
            currentMapHoveredEntry = null;
            for (int i = currentMapContainer.mapEntries.Count - 1; i >= 0; i--)
            {
                HandleNodeEvents(currentMapContainer.mapEntries[i]);
            }
            if (currentMapHoveredEntry == null)
            {
                if (prevHoveredEntry != null)
                {
                    currentHoverGUIContent = null;
                }
            }
            else if (currentMapHoveredEntry != prevHoveredEntry)
            {
                var text = currentMapHoveredEntry.currentDialogueText;
                if (string.IsNullOrEmpty(text)) text = currentMapHoveredEntry.currentMenuText;
                if (string.IsNullOrEmpty(text)) text = currentMapHoveredEntry.Title;
                var linkText = GetLinkSummaryHoverText();
                if (!string.IsNullOrEmpty(linkText))
                {
                    text += linkText;
                }
                currentHoverGUIContent = string.IsNullOrEmpty(text) ? null : new GUIContent(text);
                if (currentHoverGUIContent != null)
                {
                    var guiStyle = GUI.skin.textField;
                    var size = guiStyle.CalcSize(new GUIContent(currentHoverGUIContent));
                    var canvasRect = currentMapHoveredEntry.canvasRect;
                    currentHoverRect = new Rect(canvasRect.x + (canvasRect.width / 2) - (size.x / 2), canvasRect.y + canvasRect.height, size.x + 1, size.y);
                }
            }
            if (Event.current.type == EventType.Repaint && currentHoverGUIContent != null)
            {
                if (!(showQuickDialogueTextEntry && currentMapHoveredEntry == currentMapEntry))
                {
                    GUI.Label(currentHoverRect, currentHoverGUIContent, GUI.skin.textField);
                }
            }
        }
        private void DrawAllMapConnectors()
        {
            for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
            {
                var entry = currentMapContainer.mapEntries[i];
                DrawCardianlPosition(entry);
                DrawEntryConnectors(entry);
            }
            if (isMakingLink) DrawNewMapLinkConnector();
            if (isMakingCLink) DrawNewMapCLinkConnector();
        }
        private void DrawNewMapLinkConnector()
        {
            if (isMakingLink && (maplinkSourceEntry != null))
            {
                Vector3 start = new Vector3(maplinkSourceEntry.canvasRect.center.x, maplinkSourceEntry.canvasRect.center.y, 0);
                if ((maplinkTargetEntry != null) && Event.current.isMouse)
                {
                    if (!maplinkTargetEntry.canvasRect.Contains(Event.current.mousePosition))
                    {
                        maplinkTargetEntry = null;
                    }
                }
                Vector3 end = (maplinkTargetEntry != null)
                    ? new Vector3(maplinkTargetEntry.canvasRect.center.x, maplinkTargetEntry.canvasRect.center.y, 0)
                    : new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y, 0);
                DrawLink(start, end, Color.white, false);
            }
        }
        private void DrawNewMapCLinkConnector()
        {
            if (isMakingCLink && (maplinkSourceEntry != null))
            {
                Vector3 start = new Vector3(maplinkSourceEntry.canvasRect.center.x, maplinkSourceEntry.canvasRect.center.y, 0);
                if ((maplinkTargetEntry != null) && Event.current.isMouse)
                {
                    if (!maplinkTargetEntry.canvasRect.Contains(Event.current.mousePosition))
                    {
                        maplinkTargetEntry = null;
                    }
                }
                Vector3 end = (maplinkTargetEntry != null)
                    ? new Vector3(maplinkTargetEntry.canvasRect.center.x, maplinkTargetEntry.canvasRect.center.y, 0)
                    : new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y, 0);
                DrawLink(start, end, Color.cyan, false);
            }
        }
        private bool IsCurrentRuntimeEntry(MapEntry entry)
        {
            return (currentRuntimeMapEntry != null) && (entry.MapID == currentRuntimeMapEntry.MapID) && (entry.id == currentRuntimeMapEntry.id);
        }
        public void DrawCardinalLink(Vector3 start, Vector3 end, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
#if UNITY_2022_1_OR_NEWER
            if (start.y < canvasScrollPosition.y && end.y < canvasScrollPosition.y) return;
            ClipLine(ref start, ref end);
#endif
            //Vector3 cross = Vector3.Cross((start - end).normalized, Vector3.forward);
            Texture2D connectionTexture = (Texture2D)UnityEditor.Graphs.Styles.connectionTexture.image;
            Handles.color = color;
            Handles.DrawAAPolyLine(connectionTexture, 4f, new Vector3[] { start, end });
            Vector3 diff = (start - end);
            Vector3 direction = diff.normalized;
            //if ((end.y - canvasScrollPosition.y - 3f) > 0)
            //{
            //    DrawArrow(cross, direction, end, color);
            //}
        }
        private void DrawCardianlPosition(MapEntry entry)
        {
            if (entry.postion.upID != -1)
            {
                DrawCardinalLink(entry.canvasRect.center, currentMapContainer.GetMapEntry(entry.postion.upID).canvasRect.center, Color.cyan);
            }
            if (entry.postion.downID != -1)
            {
                DrawCardinalLink(entry.canvasRect.center, currentMapContainer.GetMapEntry(entry.postion.downID).canvasRect.center, Color.cyan);
            }
            if (entry.postion.rightID != -1)
            {
                DrawCardinalLink(entry.canvasRect.center, currentMapContainer.GetMapEntry(entry.postion.rightID).canvasRect.center, Color.cyan);
            }
            if (entry.postion.leftID != -1)
            {
                DrawCardinalLink(entry.canvasRect.center, currentMapContainer.GetMapEntry(entry.postion.leftID).canvasRect.center, Color.cyan);
            }
        }
        private void HandleCConnectorEvents(Link link, Vector3 start, Vector3 end)
        {
            switch (Event.current.type)
            {
                case EventType.MouseUp:
                    bool isReallyLassoing = isLassoing && (Mathf.Abs(lassoRect.width) > 10f) && (Mathf.Abs(lassoRect.height) > 10f) && (selectedEntryGroup != null);
                    if (!isReallyLassoing && (Event.current.button == LeftMouseButton) &&
                        IsPointOnLineSegment(Event.current.mousePosition, start, end) &&
                        !IsMouseInQuickDialogueTextRect())
                    {
                        newSelectedLink = link;
                        currentEntry = null;
                        inspectorSelection = newSelectedLink;
                        if (IsMouseInQuickDialogueTextRect())
                        {
                            CloseQuickDialogueTextEntry();
                        }
                    }
                    break;
            }
        }
        private void HandleMapConnectorEvents(Link link, Vector3 start, Vector3 end)
        {
            switch (Event.current.type)
            {
                case EventType.MouseUp:
                    bool isReallyLassoing = isLassoing && (Mathf.Abs(lassoRect.width) > 10f) && (Mathf.Abs(lassoRect.height) > 10f) && (selectedEntryGroup != null);
                    if (!isReallyLassoing && (Event.current.button == LeftMouseButton) &&
                        IsPointOnLineSegment(Event.current.mousePosition, start, end) &&
                        !IsMouseInQuickDialogueTextRect())
                    {
                        newSelectedLink = link;
                        currentMapEntry = null;
                        inspectorSelection = newSelectedLink;
                        if (IsMouseInQuickDialogueTextRect())
                        {
                            CloseQuickDialogueTextEntry();
                        }
                    }
                    break;
            }
        }

        private void CheckNewSelectedMapLink()
        {
            if ((newSelectedLink != null) && (newSelectedLink != selectedLink))
            {
                selectedLink = newSelectedLink;
                inspectorSelection = selectedLink;
                isLassoing = false;
                multinodeMapSelection.Clear();
                Event.current.Use();
            }
            else if (currentMapEntry != null)
            {
                selectedLink = null;
            }
        }
        private void DrawEntryConnectors(MapEntry entry)//링크 선
        {
            if (entry == null) return;
            int numCrossConversationLinks = 0;
            DrawCardianlPosition(entry);
            for (int i = 0; i < entry.outgoingLinks.Count; i++)
            {
                var link = entry.outgoingLinks[i];
                if (link.destinationConversationID == currentMapContainer.id)
                {
                    // Fix any links whose originDialogueID doesn't match the origin entry's ID:
                    if (link.originDialogueID != entry.id) link.originDialogueID = entry.id;

                    // Only show connection if within same conversation:
                    MapEntry destination = currentMapContainer.mapEntries.Find(e => e.id == link.destinationDialogueID);
                    if (destination != null)
                    {
                        var startCenter = entry.canvasRect.center;
                        var endCenter = destination.canvasRect.center;
                        Vector3 start = new Vector3(startCenter.x, startCenter.y, 0);
                        Vector3 end = new Vector3(endCenter.x, endCenter.y, 0);
                        // Check if it's not in visible window and we can skip:
                        if (!(nodeEditorVisibleRect.Contains(startCenter) || nodeEditorVisibleRect.Contains(endCenter)))
                        {
                            var connectorRect = new Rect(Mathf.Min(startCenter.x, endCenter.x), Mathf.Min(startCenter.y, endCenter.y), Mathf.Abs(startCenter.x - endCenter.x), Mathf.Abs(startCenter.y - endCenter.y));
                            if (!nodeEditorVisibleRect.Overlaps(connectorRect)) continue; // Skip drawing if not in visible window.
                        }
                        Color connectorColor = (link == selectedLink) ? SelectedNodeColor : Color.white;
                        if (IsCurrentRuntimeEntry(entry))
                        {
                            // Show current runtime entry's links in color based on destination's Conditions:
                            connectorColor = IsValidRuntimeLink(link) ? Color.green : Color.red;
                        }
                        else if (entry == currentMapEntry)
                        {
                            // Show selected entry's links in special color:
                            connectorColor = OutgoingLinkColor;
                        }
                        else if (currentMapEntry != null && link.destinationDialogueID == currentMapEntry.id)
                        {
                            connectorColor = IncomingLinkColor;
                        }
                        DrawLink(start, end, connectorColor, link.priority != ConditionPriority.Normal);
                        HandleMapConnectorEvents(link, start, end);
                        if (prefs.showLinkOrderOnConnectors && entry.outgoingLinks.Count > 1)
                        {
                            Vector3 cross = Vector3.Cross((start - end).normalized, Vector3.forward);
                            Vector3 diff = (end - start);
                            Vector3 direction = diff.normalized;
                            Vector3 mid = ((0.5f * diff) + start) - (0.5f * cross);
                            Vector3 center = mid + direction;
                            GUIContent content = new GUIContent(i.ToString());
                            Vector2 size = EditorStyles.miniTextField.CalcSize(content);
                            EditorGUI.LabelField(new Rect(center.x - 6, center.y - 28, size.x + 2, size.y + 2), (i + 1).ToString(), EditorStyles.miniTextField);
                        }
                    }
                }
                else
                {
                    // Otherwise show special cross-conversation arrow link:
                    Vector3 start = new Vector3(entry.canvasRect.center.x, entry.canvasRect.center.y, 0);
                    Vector3 end = new Vector3(start.x, start.y + 26, 0);
                    DrawSpecialLink(start, end, ConnectorColor);
                    numCrossConversationLinks++;
                }
            }
            if (numCrossConversationLinks > 1)
            {
                // If more than one cross-conversation link, show the count:
                var originalColor = GUI.color;
                GUI.color = new Color(1, 0.6f, 0);
                EditorGUI.LabelField(new Rect(entry.canvasRect.center.x + 8, entry.canvasRect.center.y + 20, 50, 50), numCrossConversationLinks.ToString());
                GUI.color = originalColor;
            }
            if (prefs.showEndNodeMarkers && entry.outgoingLinks.Count == 0)
            {
                // If no links, show that it's an end node:
                Vector3 start = new Vector3(entry.canvasRect.center.x, entry.canvasRect.center.y, 0);
                Vector3 end = new Vector3(start.x, start.y + 26, 0);
                DrawSpecialLink(start, end, Color.red);
            }
        }
        private void DrawAllMapEntryGroups()
        {
            var originalColor = GUI.color;
            for (int i = 0; i < currentMapContainer.entryGroups.Count; i++)
            {
                var group = currentMapContainer.entryGroups[i];
                if (!nodeEditorVisibleRect.Overlaps(group.rect)) continue; // Skip drawing if not in visible window.//group is null
                GUI.color = group.color;
                if (boxTexture != null) GUI.DrawTexture(group.rect, boxTexture);
                if (!isRenamingEntryGroup)
                {
                    GUI.color = new Color(group.color.r, group.color.g, group.color.b, 1);
                    if (entryGroupHeadingStyle == null) InitEntryGroupHeadingStyle();
                    //GUI.Box(new Rect(group.rect.x, group.rect.y, group.rect.width, entryGroupHeadingHeight), group.name, entryGroupHeadingStyle);
                    GUI.Box(new Rect(group.rect.x, group.rect.y, group.rect.width, entryGroupHeadingHeight), GUIContent.none, entryGroupHeadingStyle);
                }
                GUI.color = originalColor;
                if (entryGroupLabelStyle == null) InitEntryGroupLabelStyle();
                GUI.Label(new Rect(group.rect.x, group.rect.y, group.rect.width, entryGroupHeadingHeight), group.name, entryGroupLabelStyle);
                var resizeRect = new Rect(group.rect.x + group.rect.width - 20, group.rect.y + group.rect.height - 20, 16, 16);
                if (resizeIcon != null) GUI.DrawTexture(resizeRect, resizeIcon);
            }
            GUI.color = originalColor;

        }
        private void DrawUnityMapEvents()
        {
            // Draw scene-independent OnExecute() event:
            if (database == null) return;
            EditorGUILayout.LabelField(new GUIContent("Scene-Independent Event", "This UnityEvent cannot point to scene objects. It can point to assets and prefabs."), EditorStyles.boldLabel);
            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(database);
            }
            if (serializedObjectCurrentEntry != currentEntry)
            {
                serializedObject.Update();
                serializedObjectCurrentEntry = currentEntry;
                var conversationsProperty = serializedObject.FindProperty("maps");
                if (conversationsProperty == null || !conversationsProperty.isArray) return;
                SerializedProperty conversationProperty = null;
                for (int i = 0; i < conversationsProperty.arraySize; i++)
                {
                    var sp = conversationsProperty.GetArrayElementAtIndex(i);
                    if (sp.FindPropertyRelative("id").intValue == currentConversation.id)
                    {
                        conversationProperty = sp;
                        break;
                    }
                }
                if (conversationProperty == null) return;
                var entriesProperty = conversationProperty.FindPropertyRelative("mapEntries");
                if (entriesProperty == null || !entriesProperty.isArray) return;
                SerializedProperty entryProperty = null;
                for (int i = 0; i < entriesProperty.arraySize; i++)
                {
                    var sp = entriesProperty.GetArrayElementAtIndex(i);
                    if (sp.FindPropertyRelative("id").intValue == currentEntry.id)
                    {
                        entryProperty = sp;
                        break;
                    }
                }
                if (entryProperty == null) return;
                onExecuteProperty = entryProperty.FindPropertyRelative("onExecute");
            }
            if (onExecuteProperty != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(onExecuteProperty);
                if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            }

            // Draw scene-specific event:
            var sceneEventGuid = currentEntry.sceneEventGuid;
            int sceneEventIndex = -1;
            if (string.IsNullOrEmpty(sceneEventGuid))
            {
                // If this entry is not associated with a scene event, show Add button:
                if (GUILayout.Button(new GUIContent("Add Scene Event", "Add a UnityEvent that operates on GameObjects in the currently-open scene.")))
                {
                    MakeSureDialogueSystemSceneEventsExists();
                    sceneEventIndex = DialogueSystemSceneEvents.AddNewDialogueEntrySceneEvent(out sceneEventGuid);
                    currentEntry.sceneEventGuid = sceneEventGuid;
                }
            }
            else
            {
                // Make sure our serialized object points to this scene's DialogueSystemSceneEvents:
                if (dialogueSystemSceneEvents == null || dialogueSystemSceneEventsSerializedObject == null)
                {
                    dialogueSystemSceneEvents = GameObjectUtility.FindFirstObjectByType<DialogueSystemSceneEvents>();
                    dialogueSystemSceneEventsSerializedObject = (dialogueSystemSceneEvents != null)
                        ? new SerializedObject(dialogueSystemSceneEvents) : null;
                }

                // Then check if the entry's scene event is defined in this scene:
                sceneEventIndex = DialogueSystemSceneEvents.GetDialogueEntrySceneEventIndex(sceneEventGuid, dialogueSystemSceneEvents);
            }
            if (sceneEventIndex == -1 && !string.IsNullOrEmpty(sceneEventGuid))
            {
                // If scene event is assigned but not in this scene, show Delete button:
                GUILayout.Label("Scene Event operates in another scene.");
                if (GUILayout.Button("Delete Scene Event"))
                {
                    currentEntry.sceneEventGuid = string.Empty;
                }
            }
            if (sceneEventIndex != -1 && dialogueSystemSceneEventsSerializedObject != null)
            {
                // Scene event is in this scene. Draw it:
                dialogueSystemSceneEventsSerializedObject.Update();
                var sceneEventsListProperty = dialogueSystemSceneEventsSerializedObject.FindProperty("dialogueEntrySceneEvents");
                if (sceneEventsListProperty != null && 0 <= sceneEventIndex && sceneEventIndex < sceneEventsListProperty.arraySize)
                {
                    var sceneEventProperty = sceneEventsListProperty.GetArrayElementAtIndex(sceneEventIndex);
                    EditorGUILayout.LabelField("Scene Event", EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(sceneEventProperty.FindPropertyRelative("guid"), true);
                    EditorGUI.EndDisabledGroup();
                    if (sceneEventProperty != null)
                    {
                        EditorGUILayout.PropertyField(sceneEventProperty.FindPropertyRelative("onExecute"), true);
                    }
                }
                dialogueSystemSceneEventsSerializedObject.ApplyModifiedProperties();
                if (GUILayout.Button("Delete Scene Event"))
                {
                    DialogueSystemSceneEvents.RemoveDialogueEntrySceneEvent(sceneEventGuid);
                    currentEntry.sceneEventGuid = string.Empty;
                }
            }
        }
        private void LinkToNewMapEntry(MapEntry source, bool useSameActorAssignments = false)
        {
            if (source != null)
            {
                MapEntry newEntry = CreateNewMapEntry(string.Empty);
                if (useSameActorAssignments)
                {
                    newEntry.ActorID = (source.ActorID == source.ConversantID) ? database.playerID : source.ActorID;
                    newEntry.ConversantID = source.ConversantID;
                }
                else
                {
                    newEntry.ActorID = source.ConversantID;
                    newEntry.ConversantID = (source.ActorID == source.ConversantID) ? database.playerID : source.ActorID;
                }
                newEntry.canvasRect = new Rect(source.canvasRect.x, source.canvasRect.y + source.canvasRect.height + 20, source.canvasRect.width, source.canvasRect.height);
                Link link = new Link();
                link.originConversationID = currentMapContainer.id;
                link.originDialogueID = source.id;
                link.destinationConversationID = currentMapContainer.id;
                link.destinationDialogueID = newEntry.id;
                source.outgoingLinks.Add(link);
                currentMapEntry = newEntry;
                SetDatabaseDirty("Link to New Entry");
            }
        }
        private int GetNextMapEntryID()
        {
            int highestID = -1;
            currentMapContainer.mapEntries.ForEach(entry => highestID = Mathf.Max(highestID, entry.id));
            return highestID + 1;
        }
        private MapEntry CreateNewMapEntry(string title)
        {
            MapEntry entry = template.CreateMapEntry(GetNextMapEntryID(), currentMapContainer.id, title ?? string.Empty);
            currentMapContainer.mapEntries.Add(entry);
            SetDatabaseDirty("Create New Dialogue Entry");
            return entry;
        }
        private void DeleteCardinalConnect(MapEntry source, MapEntry target, CardinalPoint point)
        {
            switch (point)
            {
                case CardinalPoint.North:
                    source.postion.upID = -1;
                    target.postion.downID = -1;
                    break;
                case CardinalPoint.South:
                    source.postion.downID = -1;
                    target.postion.upID = -1;
                    break;
                case CardinalPoint.West:
                    source.postion.leftID = -1;
                    target.postion.rightID = -1;
                    break;
                case CardinalPoint.East:
                    source.postion.rightID = -1;
                    target.postion.leftID = -1;
                    break;
            }
        }
        private void DeleteCardinalConnect(MapEntry source,CardinalPoint point)
        {
            switch (point)
            {
                case CardinalPoint.North:
                    if (source.postion.upID == -1) return;
                    currentMapContainer.GetMapEntry(source.postion.upID).postion.downID = -1;
                    source.postion.upID = -1;
                    break;
                case CardinalPoint.South:
                    if (source.postion.downID == -1) return;
                    currentMapContainer.GetMapEntry(source.postion.downID).postion.upID = -1;
                    source.postion.downID = -1;
                    break;
                case CardinalPoint.West:
                    if (source.postion.leftID == -1) return;
                    currentMapContainer.GetMapEntry(source.postion.leftID).postion.rightID = -1;
                    source.postion.leftID = -1;
                    break;
                case CardinalPoint.East:
                    if (source.postion.rightID == -1) return;
                    currentMapContainer.GetMapEntry(source.postion.rightID).postion.leftID = -1;
                    source.postion.rightID = -1;
                    break;
            }
        }
        private void CardinalConnectMapEntry(MapEntry source,MapEntry target,CardinalPoint point)
        {
            switch (point)
            {
                case CardinalPoint.North:
                    source.postion.upID = target.id;
                    target.postion.downID = source.id;
                    break;
                case CardinalPoint.South:
                    source.postion.downID = target.id;
                    target.postion.upID = source.id;
                    break;
                case CardinalPoint.West:
                    source.postion.leftID = target.id;
                    target.postion.rightID = source.id;
                    break;
                case CardinalPoint.East:
                    source.postion.rightID = target.id;
                    target.postion.leftID = source.id;
                    break;
            }
        }
        private void AlignCardinalPoint(MapEntry source, MapEntry target,CardinalPoint point)
        {
            switch (point)
            {
                case CardinalPoint.North:
                    target.canvasRect.x = source.canvasRect.x;
                    target.canvasRect.y = source.canvasRect.y - source.canvasRect.height - AutoHeightBetweenNodes;
                    break;
                case CardinalPoint.South:
                    target.canvasRect.x = source.canvasRect.x;
                    target.canvasRect.y = source.canvasRect.y + source.canvasRect.height + AutoHeightBetweenNodes;
                    break;
                case CardinalPoint.West:
                    target.canvasRect.x = source.canvasRect.x - source.canvasRect.width - AutoWidthBetweenNodes;
                    target.canvasRect.y = source.canvasRect.y;
                    break;
                case CardinalPoint.East:
                    target.canvasRect.x = source.canvasRect.x + source.canvasRect.width + AutoWidthBetweenNodes;
                    target.canvasRect.y = source.canvasRect.y;
                    break;
            }
        }
        enum CardinalPoint
        {
            North,South,West,East
        }
        private void AddMapUpChildCallback(object o)=>AddCardianlCallback(o, CardinalPoint.North);
        private void AddMapDownChildCallback(object o)=>AddCardianlCallback(o, CardinalPoint.South);
        private void AddMapLeftChildCallback(object o)=>AddCardianlCallback(o, CardinalPoint.West);
        private void AddMapRightChildCallback(object o)=>AddCardianlCallback(o, CardinalPoint.East);
        void AddCardianlCallback(object o,CardinalPoint point, bool useSameActorAssignments = false)
        {
            //MapEntry parentEntry = o as MapEntry;
            //if (parentEntry == null) parentEntry = startMapEntry;
            //LinkToNewMapEntry(parentEntry, wasShiftDown);
            //wasShiftDown = false;
            //InitializeMapTree();
            //if (prefs.addNewNodesToRight)
            //{
            //    currentMapEntry.canvasRect.x = parentEntry.canvasRect.x + parentEntry.canvasRect.width + AutoWidthBetweenNodes;
            //    currentMapEntry.canvasRect.y = parentEntry.canvasRect.y;
            //}
            //else
            //{
            //    currentMapEntry.canvasRect.x = parentEntry.canvasRect.x;
            //    currentMapEntry.canvasRect.y = parentEntry.canvasRect.y + parentEntry.canvasRect.height + AutoHeightBetweenNodes;
            //}
            //SetCurrentMapEntry(currentMapEntry);
            //inspectorSelection = currentMapEntry;
            //ResetMapEntryText();
            //if (prefs.autoArrangeOnCreate) AutoArrangeNodes(!prefs.addNewNodesToRight);
            //Repaint();
            MapEntry parentEntry = o as MapEntry;
            if (parentEntry == null) parentEntry = startMapEntry;
            MapEntry newEntry = CreateNewMapEntry(string.Empty);
            newEntry.canvasRect = new Rect(parentEntry.canvasRect.x, parentEntry.canvasRect.y - parentEntry.canvasRect.height - 20, parentEntry.canvasRect.width, parentEntry.canvasRect.height);

            CardinalConnectMapEntry(parentEntry, newEntry,point);
            AlignCardinalPoint(parentEntry, newEntry, point);
            //wasShiftDown = false;
            if (parentEntry != null)
            {

                if (useSameActorAssignments)
                {
                    newEntry.ActorID = (parentEntry.ActorID == parentEntry.ConversantID) ? database.playerID : parentEntry.ActorID;
                    newEntry.ConversantID = parentEntry.ConversantID;
                }
                else
                {
                    newEntry.ActorID = parentEntry.ConversantID;
                    newEntry.ConversantID = (parentEntry.ActorID == parentEntry.ConversantID) ? database.playerID : parentEntry.ActorID;
                }

            }

            InitializeMapTree();
            SetCurrentMapEntry(currentMapEntry);
            inspectorSelection = currentMapEntry;
            ResetMapEntryText();
            if (prefs.autoArrangeOnCreate) AutoArrangeMapNodes(!prefs.addNewNodesToRight);
            SetDatabaseDirty("Link to New Entry");
            Repaint();
        }
        private void AddMapChildCallback(object o)
        {
            MapEntry parentEntry = o as MapEntry;
            if (parentEntry == null) parentEntry = startMapEntry;
            LinkToNewMapEntry(parentEntry, wasShiftDown);
            wasShiftDown = false;
            InitializeMapTree();
            if (prefs.addNewNodesToRight)
            {
                currentMapEntry.canvasRect.x = parentEntry.canvasRect.x + parentEntry.canvasRect.width + AutoWidthBetweenNodes;
                currentMapEntry.canvasRect.y = parentEntry.canvasRect.y;
            }
            else
            {
                currentMapEntry.canvasRect.x = parentEntry.canvasRect.x;
                currentMapEntry.canvasRect.y = parentEntry.canvasRect.y + parentEntry.canvasRect.height + AutoHeightBetweenNodes;
            }
            SetCurrentMapEntry(currentMapEntry);
            inspectorSelection = currentMapEntry;
            ResetMapEntryText();
            if (prefs.autoArrangeOnCreate) AutoArrangeNodes(!prefs.addNewNodesToRight);
            Repaint();
        }
        private void CreateMapEntryGroup(Rect rect)
        {
            var entryGroup = new EntryGroup("Group", rect);
            currentMapContainer.entryGroups.Add(entryGroup);
            SetDatabaseDirty("Group");
        }

        private void FinishMapLasso()
        {
            if (currentMapContainer == null) return;
            isLassoing = false;
            lassoRect = new Rect(Mathf.Min(lassoRect.x, lassoRect.x + lassoRect.width),
                                 Mathf.Min(lassoRect.y, lassoRect.y + lassoRect.height),
                                 Mathf.Abs(lassoRect.width),
                                 Mathf.Abs(lassoRect.height));
            currentMapEntry = null;
            if (IsControlDown() && lassoRect.width > MapEntry.CanvasRectWidth && lassoRect.height > 2 * MapEntry.CanvasRectHeight)
            {
                CreateMapEntryGroup(lassoRect);
            }
            else
            {
                if (!IsShiftDown()) multinodeMapSelection.Clear();
                for (int i = 0; i < currentMapContainer.mapEntries.Count; i++)
                {
                    var entry = currentMapContainer.mapEntries[i];
                    if (lassoRect.Overlaps(entry.canvasRect))
                    {
                        currentMapEntry = entry;
                        if (!multinodeMapSelection.nodes.Contains(entry)) multinodeMapSelection.nodes.Add(entry);
                    }
                }
                for (int i = 0; i < currentMapContainer.entryGroups.Count; i++)
                {
                    var group = currentMapContainer.entryGroups[i];
                    if (lassoRect.Contains(group.rect.TopLeft()) && lassoRect.Contains(group.rect.BottomRight()))
                    {
                        if (!multinodeMapSelection.groups.Contains(group)) multinodeMapSelection.groups.Add(group);
                    }
                }
            }
            UpdateEntrySelection();
        }
        private void GetNodeStyle(MapEntry entry, out GUIStyle nodeStyle, out bool isSelected, out bool isCustomColor, out Color customColor)
        {
            isCustomColor = false;
            customColor = Color.white;
            isSelected = multinodeMapSelection.nodes.Contains(entry);
            if (IsCurrentRuntimeEntry(entry))
            {
                // Current runtime entry is green:
                nodeStyle = GetNodeStyle(Styles.Color.Green, isSelected);
            }
            else if (entry.id == 0)
            {
                // START node is orange:
                nodeStyle = GetNodeStyle(Styles.Color.Orange, isSelected);
            }
            else if (orphanIDs.Contains(entry.id) && (entry.id != 0))
            {
                // Orphaned nodes are red:
                nodeStyle = GetNodeStyle(Styles.Color.Red, isSelected);
            }
            else
            {
                // Check if actor has custom color:
                var actorID = entry.ActorID;
                if (actorHasCustomColorCache == null) actorHasCustomColorCache = new Dictionary<int, bool>();
                if (actorIsPlayerCache == null) actorIsPlayerCache = new Dictionary<int, bool>();
                if (actorCustomColorCache == null) actorCustomColorCache = new Dictionary<int, Color>();
                if (!actorHasCustomColorCache.ContainsKey(actorID) ||
                    !actorCustomColorCache.ContainsKey(actorID) ||
                    (actorCustomColorCache.ContainsKey(actorID) && (actorCustomColorCache[actorID].a == 0)))
                {
                    var actor = database.GetActor(actorID);
                    actorIsPlayerCache[actorID] = (actor != null) ? actor.IsPlayer : false;
                    if (actor != null && actor.FieldExists(NodeColorFieldTitle))
                    {
                        var colorFieldValue = actor.LookupValue(NodeColorFieldTitle);
                        if (!string.IsNullOrEmpty(colorFieldValue))
                        {
                            var actorColor = EditorTools.NodeColorStringToColor(colorFieldValue);
                            if (actorColor.r == 0 && actorColor.g == 0 && actorColor.b == 0)
                            {
                                actorColor = actor.IsPlayer ? Color.blue : Color.gray;
                            }
                            actorColor.a = 1;
                            actorCustomColorCache.Add(actorID, actorColor);
                            actorHasCustomColorCache[actorID] = true;
                        }
                        else
                        {
                            actorHasCustomColorCache[actorID] = false;
                        }
                    }
                    else
                    {
                        actorHasCustomColorCache[actorID] = false;
                    }
                }
                if (actorHasCustomColorCache[actorID])
                {
                    // Use actor's custom color:
                    isCustomColor = true;
                    customColor = actorCustomColorCache[actorID];
                    if (customColor.a == 0)
                    {
                        actorHasCustomColorCache.Remove(actorID);
                        actorCustomColorCache.Remove(actorID);
                        customColor = Color.gray;
                    }
                    if (m_customNodeStyle == null || m_customNodeStyle.normal.background == null)
                    {
#if UNITY_2019_3_OR_NEWER
                        m_customNodeStyle = new GUIStyle(GUI.skin.button);
#else
                        m_customNodeStyle = new GUIStyle(GUI.skin.box);
#endif
                        m_customNodeStyle.contentOffset = new Vector2(0, -4);
                        var nodeTexture = EditorGUIUtility.Load("Dialogue System/EditorNode.png") as Texture2D;
                        m_customNodeStyle.normal.background = nodeTexture ?? Texture2D.whiteTexture;
                        m_customNodeStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black;
                        m_customNodeStyle.wordWrap = false;
                        m_customNodeStyle.alignment = TextAnchor.MiddleCenter;
                    }
                    nodeStyle = m_customNodeStyle;
                }
                else
                {
                    // Use default colors (blue for player, gray for NPCs):
                    nodeStyle = actorIsPlayerCache[actorID]
                        ? nodeStyle = GetNodeStyle(Styles.Color.Blue, isSelected)
                    : nodeStyle = GetNodeStyle(Styles.Color.Gray, isSelected);
                }
            }
        }
        private string GetMapEntryNodeText(MapEntry entry)
        {
            if (entry == null) return string.Empty;
            string text;
            if (!mapEntryNodeText.TryGetValue(entry.id, out text) || text == null)
            {
                text = BuildMapEntryNodeText(entry);
                if (text == null) text = string.Empty;
                mapEntryNodeText[entry.id] = text;
            }
            return text;
        }
        private GUIContent GetMapEntryNodeDescription(MapEntry entry)
        {
            if (entry == null) return null;
            GUIContent description;
            if (!mapEntryNodeDescription.TryGetValue(entry.id, out description) || description == null)
            {
                var descriptionText = Field.LookupValue(entry.fields, "Description");
                if (descriptionText == null) descriptionText = string.Empty;
                dialogueEntryNodeDescription[entry.id] = !string.IsNullOrEmpty(descriptionText) ? new GUIContent(descriptionText) : null;
            }
            return description;
        }
        private bool DoesMapEntryHaveSequence(MapEntry entry)
        {
            if (entry == null) return false;
            bool value;
            if (!mapEntryNodeHasSequence.TryGetValue(entry.id, out value))
            {
                var sequence = entry.Sequence;
                value = !string.IsNullOrEmpty(sequence) &&
                    !(entry.id == 0 && (sequence == "None()" || sequence == "Continue()"));
                mapEntryNodeHasSequence[entry.id] = value;
            }
            return value;
        }
        private bool DoesMapEntryHaveEvent(MapEntry entry)
        {
            if (entry == null) return false;
            bool value;
            if (!mapEntryHasEvent.TryGetValue(entry.id, out value))
            {
                value = FullCheckDoesMapEntryHaveEvent(entry);
                mapEntryHasEvent[entry.id] = value;
            }
            return value;
        }
        private void DrawMapEntryNode(MapEntry entry)
        {
            if (!nodeEditorVisibleRect.Overlaps(entry.canvasRect)) return; // Skip drawing if not in visible window.

            GUIStyle nodeStyle;
            bool isSelected;
            bool isCustomColor;
            Color customColor;
            GetNodeStyle(entry, out nodeStyle, out isSelected, out isCustomColor, out customColor);

            if (isCustomColor && customColor.a == 0) customColor = Color.gray; // Safeguard.

            string nodeLabel = GetMapEntryNodeText(entry);
            if (showQuickDialogueTextEntry && entry == currentMapEntry) nodeLabel = string.Empty;

            var guicolor_backup = GUI.backgroundColor;

            var boxRect = new Rect(entry.canvasRect.x, entry.canvasRect.y, entry.canvasRect.width + 4, entry.canvasRect.height + 4);

            if (isCustomColor)
            {
                if (isSelected)
                {
                    var bigRect = new Rect(boxRect.x + 2, boxRect.y, boxRect.width - 4, boxRect.height - 2);
                    GUI.Box(bigRect, string.Empty, Styles.GetNodeStyle("node", Styles.Color.Blue, true));
                }
                GUI.backgroundColor = customColor;
            }
            isSelected = multinodeMapSelection.nodes.Contains(entry);
            nodeStyle = GetNodeStyle(Styles.Color.Green, isSelected);
            GUI.Box(boxRect, nodeLabel, nodeStyle);

            if (isCustomColor)
            {
                GUI.backgroundColor = guicolor_backup;
            }

            if (_zoom > 0.5f)
            {
                // Draw icons for Sequence, Conditions, Script, & Event:
                if (DoesMapEntryHaveSequence(entry))
                {
                    GUI.Label(new Rect((boxRect.x + boxRect.width) - 44, (boxRect.y + boxRect.height) - 15, 16, 16), new GUIContent(sequenceIcon, entry.Sequence));
                }

                if (!string.IsNullOrEmpty(entry.conditionsString))
                {
                    GUI.Label(new Rect((boxRect.x + boxRect.width) - 30, (boxRect.y + boxRect.height) - 15, 16, 16), new GUIContent(conditionsIcon, entry.conditionsString));
                }

                if (!string.IsNullOrEmpty(entry.userScript))
                {
                    GUI.Label(new Rect((boxRect.x + boxRect.width) - 16, (boxRect.y + boxRect.height) - 15, 16, 16), new GUIContent(scriptIcon, entry.userScript));
                }
                if (eventIcon != null && DoesMapEntryHaveEvent(entry))
                {
                    GUI.DrawTexture(new Rect((boxRect.x + boxRect.width) - 58, (boxRect.y + boxRect.height) - 15, 16, 16), eventIcon);
                }
            }

            if (prefs.showActorPortraits)
            {
                var portrait = GetActorPortrait(entry.ActorID);
                if (portrait != null)
                {
                    GUIDrawSprite(new Rect(boxRect.x - 30, boxRect.y, 30, 30), portrait);
                }
            }

            if (prefs.showDescriptions)
            {
                var descriptionGUIContent = GetMapEntryNodeDescription(entry);
                if (descriptionGUIContent != null)
                {
                    var height = EditorStyles.textField.CalcHeight(descriptionGUIContent, boxRect.width);
                    GUI.Label(new Rect(boxRect.x, boxRect.y + boxRect.height, boxRect.width, height), descriptionGUIContent, EditorStyles.textField);
                }
            }

            if (customDrawMapEntryNode != null)
            {
                customDrawMapEntryNode(database, entry, boxRect);
            }
        }
        private void ShowEmptyMapCanvasContextMenu()
        {
            EditorZoomArea.End();
            wasShiftDown = Event.current.shift;

            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Create Node"), false, AddMapChildCallback, null);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically"), false, ArrangeNodesCallback, AutoArrangeStyle.Vertically);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically (alternate)"), false, ArrangeNodesCallback, AutoArrangeStyle.VerticallyOld);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Horizontally"), false, ArrangeNodesCallback, AutoArrangeStyle.Horizontally);
            contextMenu.AddItem(new GUIContent("Snap All Nodes to Grid"), false, SnapAllNodesToGrid);
            if (IsNodeClipboardEmpty())
            {
                contextMenu.AddDisabledItem(new GUIContent("Paste Nodes"));
            }
            else
            {
                contextMenu.AddItem(new GUIContent("Paste Nodes"), false, PasteMultipleEntriesCallback, null);
            }
            contextMenu.AddItem(new GUIContent("Duplicate Map"), false, CopyMapCallback, null);
            contextMenu.AddItem(new GUIContent("Delete Map"), false, DeleteMapCallback, null);

            if (selectedEntryGroup != null)
            {
                contextMenu.AddSeparator(string.Empty);
                contextMenu.AddItem(new GUIContent("Group/Rename"), false, RenameEntryGroup, null);
                contextMenu.AddItem(new GUIContent("Group/Delete"), false, DeleteMapEntryGroup, null);
            }

            AddCanvasContextMenuGotoItems(contextMenu);

            contextMenu.ShowAsContext();
            contextMenuPosition = Event.current.mousePosition;

            EditorZoomArea.Begin(_zoom, _zoomArea);
        }
        private void DeleteMapEntryGroup(object o)
        {
            if (currentMapContainer == null || selectedEntryGroup == null) return;
            currentMapContainer.entryGroups.Remove(selectedEntryGroup);
            ClearSelectedEntryGroup();
            SetDatabaseDirty("Delete Group");
        }
        private void DeleteMapCallback(object o)
        {
            if (currentMapContainer == null) return;
            if (EditorUtility.DisplayDialog(string.Format("Delete '{0}'?", currentMapContainer.Title),
                "Are you sure you want to delete this mapContainer?\nYou cannot undo this operation!", "Delete", "Cancel"))
            {
                nodeEditorDeleteCurrentMapContainer = true;
            }
        }
        private void CopyMapCallback(object o)
        {
            if (currentMapContainer == null) return;
            if (EditorUtility.DisplayDialog(string.Format("Copy '{0}'?", currentMapContainer.Title), "Make a copy of this mapContainer?", "Copy", "Cancel"))
            {
                CopyMapcontainer();
            }
        }
        private MapEntry GetOrCreateFirstMapEntry()
        {
            if (currentMapContainer == null) return null;
            if (currentMapContainer.ActorID == 0) { currentMapContainer.ActorID = GetFirstPCID(); SetDatabaseDirty("Set Default Conversation Actor"); }
            if (currentMapContainer.ConversantID == 0) { currentMapContainer.ConversantID = GetFirstNPCID(); SetDatabaseDirty("Set Default Conversation Conversant"); }
            MapEntry entry = currentMapContainer.GetFirstMapEntry();
            if (entry == null)
            {
                entry = CreateNewMapEntry("START");
                entry.ActorID = currentMapContainer.ActorID;
                entry.ConversantID = currentMapContainer.ConversantID;
                SetDatabaseDirty("Create START dialogue entry");
            }
            if (entry.ActorID == 0) { entry.ActorID = currentMapContainer.ActorID; SetDatabaseDirty("Set START Actor"); }
            if (entry.MapID == 0) { entry.ConversantID = currentMapContainer.ConversantID; SetDatabaseDirty("Set START Conversant"); }
            return entry;
        }

        private void OpenMapContainer(MapContainer mapContainer)
        {
            ResetMapSection();
            SetCurrentMapContainer(mapContainer);
            startMapEntry = GetOrCreateFirstMapEntry();
            CheckNodeWidths();
            if (showNodeEditor) CheckMapNodeArrangement();
            AlignAllCardinalPosition();
        }
        private void CheckMapNodeArrangement()
        {
            if (startMapEntry == null) return;
            if ((startMapEntry.canvasRect.x == 0) && (startMapEntry.canvasRect.y == 0)) AutoArrangeMapNodes(!prefs.addNewNodesToRight);
        }
        public void AutoArrangeMapNodes(bool vertically)
        {
            AutoArrangeMapNodes(vertically ? AutoArrangeStyle.Vertically : AutoArrangeStyle.Horizontally);
        }
        private float GetTreeHeight(List<List<MapEntry>> tree)
        {
            float maxHeight = 0;
            for (int i = 0; i < tree.Count; i++)
            {
                var level = tree[i];
                float levelHeight = level.Count * (canvasMapRectHeight + AutoHeightBetweenNodes);
                maxHeight = Mathf.Max(maxHeight, levelHeight);
            }
            return maxHeight;
        }
        private void ArrangeLevel(List<MapEntry> nodes, float x, float y, float treeWidth, float treeHeight, bool vertically)
        {
            if (nodes == null || nodes.Count == 0) return;
            if (vertically)
            {
                float nodeCanvasWidth = treeWidth / nodes.Count;
                float nodeCanvasOffset = (nodeCanvasWidth - canvasMapRectWidth) / 2;
                for (int i = 0; i < nodes.Count; i++)
                {
                    float nodeX = x + (i * nodeCanvasWidth) + nodeCanvasOffset;
                    nodes[i].canvasRect = new Rect(nodeX, y, canvasMapRectWidth, canvasMapRectHeight);
                }
            }
            else
            {
                float nodeCanvasHeight = treeHeight / nodes.Count;
                float nodeCanvasOffset = (nodeCanvasHeight - canvasMapRectHeight) / 2;
                for (int i = 0; i < nodes.Count; i++)
                {
                    float nodeY = y + (i * nodeCanvasHeight) + nodeCanvasOffset;
                    nodes[i].canvasRect = new Rect(x, nodeY, canvasMapRectWidth, canvasMapRectHeight);
                }
            }
        }
        private float GetTreeWidth(List<List<MapEntry>> tree)
        {
            float maxWidth = 0;
            for (int i = 0; i < tree.Count; i++)
            {
                var level = tree[i];
                float levelWidth = level.Count * (canvasMapRectWidth + AutoWidthBetweenNodes);
                maxWidth = Mathf.Max(maxWidth, levelWidth);
            }
            return maxWidth;
        }

        private void ArrangeTree(List<List<MapEntry>> tree, AutoArrangeStyle style)
        {
            if (style == AutoArrangeStyle.Horizontally)
            {
                float treeHeight = GetTreeHeight(tree);
                float y = AutoStartY;
                if (orphans.Count > 0) y += canvasMapRectHeight + AutoHeightBetweenNodes;
                float x = AutoStartX;
                for (int level = 0; level < tree.Count; level++)
                {
                    ArrangeLevel(tree[level], x, y, 0, treeHeight, false);
                    x += canvasMapRectWidth + AutoWidthBetweenNodes;
                }
            }
            else
            {
                if (currentConversation == null || currentConversation.dialogueEntries == null || currentConversation.dialogueEntries.Count == 0) return;
                if (style == AutoArrangeStyle.VerticallyOld || (multinodeSelection != null && multinodeSelection.nodes.Count > 1))
                {
                    // Use old algorithm if specified or for subsections of conversation tree:
                    float treeWidth = GetTreeWidth(tree);
                    float x = AutoStartX;
                    if (orphans.Count > 0) x += canvasMapRectWidth + AutoWidthBetweenNodes;
                    float y = AutoStartY;
                    for (int level = 0; level < tree.Count; level++)
                    {
                        ArrangeLevel(tree[level], x, y, treeWidth, 0, true);
                        y += canvasMapRectHeight + AutoHeightBetweenNodes;
                    }
                }
                else
                {
                    // Use new algorithm provided by digiwombat [Fairmoon Museum]:
                    CalculatePositions(currentConversation.dialogueEntries[0], 0, 0);
                    visited.Clear();
                    subtreeVisited.Clear();
                    subTreeWidths.Clear();
                }
            }
        }
        public void AutoArrangeMapNodes(AutoArrangeStyle style)
        {
            InitializeMapTree();
            var tree = new List<List<MapEntry>>();
            ArrangeGatherMapChildren(mapTree, 0, tree);
            ArrangeTree(tree, style);
            ArrangeMapOrphans(style != AutoArrangeStyle.Horizontally);
            SetDatabaseDirty("Auto-Arrange Nodes");
        }
        private void ArrangeMapOrphans(bool vertically)
        {
            if (vertically)
            {
                float y = AutoStartY;
                foreach (var orphan in mapOrphans)
                {
                    var skip = multinodeMapSelection.nodes.Count > 1 && !multinodeMapSelection.nodes.Contains(orphan.entry);
                    if (skip) continue;
                    orphan.entry.canvasRect.x = AutoStartX;
                    orphan.entry.canvasRect.y = y;
                    y += orphan.entry.canvasRect.height + AutoHeightBetweenNodes;
                }
            }
            else
            {
                float x = AutoStartX;
                foreach (var orphan in mapOrphans)
                {
                    var skip = multinodeMapSelection.nodes.Count > 1 && !multinodeMapSelection.nodes.Contains(orphan.entry);
                    if (skip) continue;
                    orphan.entry.canvasRect.x = x;
                    x += orphan.entry.canvasRect.width + AutoWidthBetweenNodes;
                    orphan.entry.canvasRect.y = AutoStartY;
                }
            }
        }
        private void ArrangeGatherMapChildren(MapNode node, int level, List<List<MapEntry>> tree)
        {
            if (node == null) return;
            var skip = multinodeMapSelection.nodes.Count > 1 && !multinodeMapSelection.nodes.Contains(node.entry);
            while (tree.Count <= level)
            {
                tree.Add(new List<MapEntry>());
            }
            if (!(skip || tree[level].Contains(node.entry))) tree[level].Add(node.entry);
            if (node.hasFoldout)
            {
                for (int i = 0; i < node.children.Count; i++)
                {
                    var child = node.children[i];
                    ArrangeGatherMapChildren(child, level + 1, tree);
                }
            }
        }

        private void ResetMapTreeSection()
        {
            mapTreeFoldout = false;
            orphansFoldout = false;
            ResetMapEntryText();
            mapEntryFoldouts.Clear();
            ResetMapTree();
            currentMapEntry = null;
            ResetLuaWizards();
            allowEditStartEntry = false;
        }
        private void ResetMapSection()
        {
            SetCurrentMapContainer(null);
            conversationFieldsFoldout = false;
            actorField = null;
            conversantField = null;
            areParticipantsValid = false;
            startMapEntry = null;
            selectedLink = null;
            actorNamesByID.Clear();
            ResetMapTreeSection();
            ResetMapNodeSection();
        }
        private void ResetMapNodeSection()
        {
            isMakingLink = false;
            multinodeMapSelection.nodes.Clear();
            currentMapHoveredEntry = null;
            currentHoverGUIContent = null;
            ValidateMapMenuTitleIndex();
        }
        private void ValidateMapMenuTitleIndex()
        {
            UpdateMapTitles();
            if (database != null && mapContainerIndex >= database.maps.Count) mapContainerIndex = -1;
            if (mapContainerIndex == -1 && currentMapContainer != null)
            {
                var currentTitle = currentMapContainer.Title;
                for (int i = 0; i < mapsTitles.Length; i++)
                {
                    if (mapsTitles[i] == currentTitle)
                    {
                        mapContainerIndex = i;
                        break;
                    }
                }
            }
        }
        private void CopyMapContainerCallback(object o)
        {
            if (currentMapContainer == null) return;
            if (EditorUtility.DisplayDialog(string.Format("Copy '{0}'?", currentMapContainer.Title), "Make a copy of this Map?", "Copy", "Cancel"))
            {
                CopyMapcontainer();
            }
        }
        private int GetAvailableMapContainerID()
        {
            int highestID = database.baseID - 1;
            database.maps.ForEach(a => highestID = Mathf.Max(highestID, a.id));
            return Mathf.Max(1, highestID + 1);
        }
        private void CopyMapcontainer()
        {
            int oldID = currentMapContainer.id;
            int newID = GetAvailableMapContainerID();
            var copyTitle = GetAvailableCopyTitle();
            var copy = new MapContainer(currentMapContainer);
            copy.id = newID;
            copy.Title = copyTitle;
            foreach (var entry in copy.mapEntries)
            {
                entry.MapID = newID;
                foreach (var link in entry.outgoingLinks)
                {
                    if (link.originConversationID == oldID) link.originConversationID = newID;
                    if (link.destinationConversationID == oldID) link.destinationConversationID = newID;
                }
            }
            database.maps.Add(copy);
            SetCurrentMapContainer(copy);
            if (showNodeEditor) ActivateMapNodeEditorMode();
            SetDatabaseDirty("Copy Map");
        }
        private void ActivateMapNodeEditorMode()
        {
            SetShowNodeEditor(true);
            ResetNodeEditorMapList();
            if (currentMapContainer != null) OpenMapContainer(currentMapContainer);
            isMakingLink = false;
        }

        public void UpdateMapTitles()
        {
            mapsTitles = GetMapTitles();
        }
        private void SetCurrentMapContainer(MapContainer container)
        {
            if (verboseDebug) Debug.Log("<color=magenta>Set current container to ID=" + ((container != null) ? container.id : -1) + "</color>");
            ClearActorInfoCaches();
            if (container != null && container.id != currentMapContainerID) ResetCurrentMapEntryID();
            currentMapContainer = container;
            if (currentMapContainer != null)
            {
                canvasScrollPosition = currentMapContainer.canvasScrollPosition;
                _zoom = currentMapContainer.canvasZoom;
            }
        }
        private void ArrangeMapNodesCallback(object o) => AutoArrangeMapNodes((bool)o ? AutoArrangeStyle.Vertically : AutoArrangeStyle.Horizontally);
        private void ShowMapLinkContextMenu()
        {
            EditorZoomArea.End();

            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Delete Link"), false, DeleteMapLinkCallback, selectedLink);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically"), false, ArrangeMapNodesCallback, AutoArrangeStyle.Vertically);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Vertically (alternate)"), false, ArrangeMapNodesCallback, AutoArrangeStyle.VerticallyOld);
            contextMenu.AddItem(new GUIContent("Arrange Nodes/Horizontally"), false, ArrangeMapNodesCallback, AutoArrangeStyle.Horizontally);

            AddCanvasContextMenuGotoItems(contextMenu);

            contextMenu.ShowAsContext();
            contextMenuPosition = Event.current.mousePosition;

            EditorZoomArea.Begin(_zoom, _zoomArea);
        }
        private void HandleEmptyMapCanvasEvents() // Also handles entry group events.
        {
            wantsMouseMove = true;
            Event e = Event.current;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    GUI.FocusControl("SearchClearButton"); // Deselect search bar text input field.
                    isDraggingCanvas = IsDragCanvasEvent();
                    if (isDraggingCanvas) mapNodeToDrag = null;
                    CheckClickOnMapEntryGroup();
                    if (isMakingLink)
                    {
                        if ((Event.current.button == LeftMouseButton) || (Event.current.button == RightMouseButton))
                        {
                            FinishMakingMapLink();
                        }
                    }
                    else if (IsRightMouseButtonEvent())
                    {
                        if (selectedLink != null)
                        {
                            ShowMapLinkContextMenu();
                        }
                        else
                        {
                            if (currentMapContainer != null)
                            {
                                ShowEmptyMapCanvasContextMenu();
                            }
                        }
                        //CloseQuickDialogueTextEntry();
                    }
                    else if (Event.current.button == LeftMouseButton)
                    {
                        mapNodeToDrag = null;
                        if (selectedEntryGroup == null)
                        {
                            isLassoing = true;
                            lassoRect = new Rect(Event.current.mousePosition.x + canvasScrollPosition.x, Event.current.mousePosition.y + canvasScrollPosition.y, 1, 1);
                        }
                    }
                    break;
                case EventType.MouseUp:
                    isDraggingCanvas = false;
                    if (!IsMouseOnSelectedEntryGroupHeading())
                    {
                        ClearSelectedEntryGroup();
                    }
                    if (isLassoing)
                    {
                        FinishMapLasso();
                        newSelectedLink = null;
                    }
                    else if (newSelectedLink == null && Event.current.button != MiddleMouseButton && selectedEntryGroup == null &&
                        ((currentMapEntry == null) || !IsMouseInQuickDialogueTextRect()))
                    {
                        InspectConversationProperties();
                    }
                    else if (selectedEntryGroup != null && !IsMouseOnSelectedEntryGroupHeading())
                    {
                        ClearSelectedEntryGroup();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isDraggingCanvas)
                    {
                        canvasScrollPosition -= Event.current.delta;
                        canvasScrollPosition.x = Mathf.Clamp(canvasScrollPosition.x, 0, Mathf.Infinity);
                        canvasScrollPosition.y = Mathf.Clamp(canvasScrollPosition.y, 0, Mathf.Infinity);
                    }
                    else if (selectedEntryGroup != null)
                    {
                        if (isResizingEntryGroup)
                        {
                            ResizeSelectedEntryGroup();
                        }
                        else
                        {
                            DragSelectedEntryGroup();
                        }
                    }
                    else if (isLassoing)
                    {
                        lassoRect.width += Event.current.delta.x;
                        lassoRect.height += Event.current.delta.y;
                    }
                    break;
            }
            if (Event.current.isMouse) e.Use();
        }

        void DrawMapSection()
        {

            if (showNodeEditor)
            {
                DrawMapSectionNodeStyle();

            }
            else
            {
                DrawMapSectionOutlineStyle();
            }
        }
        private void DrawMapSectionOutlineStyle()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Conversations", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawOutlineMapEditorMenu();
            EditorGUILayout.EndHorizontal();
            DrawConversations();
        }
        private void InitializeMapContainer(MapContainer mapContainer)
        {
            mapContainer.Title = string.Format("New Map {0}", mapContainer.id);
            mapContainer.ActorID = database.playerID;
            MapEntry startMapEntry = new MapEntry();
            startMapEntry.fields = new List<Field>();
            InitializeFieldsFromTemplate(startMapEntry.fields, template.dialogueEntryFields);
            startMapEntry.Title = "START";
            startMapEntry.currentSequence = "None()";
            startMapEntry.ActorID = database.playerID;
            startMapEntry.canvasRect = new Rect(MapEntry.CanvasRectWidth, canvasMapRectHeight, canvasMapRectWidth, canvasMapRectHeight);
            mapContainer.mapEntries.Add(startMapEntry);
            SetDatabaseDirty("Initialize Conversation");
        }
        private void AddNewMapToOutlineEditor()
        {
            AddNewMapContainer();
        }
        private void DrawOutlineMapEditorMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Conversation"), false, AddNewMapToOutlineEditor);
                if (currentMapContainer != null)
                {
                    menu.AddItem(new GUIContent("Duplicate Conversation"), false, CopyMapContainerCallback, null);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Duplicate Conversation"));
                }
                menu.AddItem(new GUIContent("Templates/New From Template/Built-In/Quest Conversation"), false, CreateQuestConversationFromTemplate);
                menu.AddItem(new GUIContent("Templates/New From Template/From Template JSON..."), false, CreateConversationFromTemplate);
                if (currentMapContainer != null)
                {
                    menu.AddItem(new GUIContent("Templates/Save Template JSON..."), false, SaveConversationTemplate);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Templates/Save Template JSON..."));
                }
                if (currentMapContainer != null)
                {
                    menu.AddItem(new GUIContent("Split Pipes Into Entries"), false, SplitPipesIntoEntries, null);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Split Pipes Into Entries"));
                }
                menu.AddItem(new GUIContent("Sort/By Title"), false, SortConversationsByTitle);
                menu.AddItem(new GUIContent("Sort/By ID"), false, SortConversationsByID);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/This Conversation"), false, ConfirmReorderIDsThisConversation);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/All Conversations"), false, ConfirmReorderIDsAllConversations);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/Depth First Reordering"), reorderIDsDepthFirst, () => { reorderIDsDepthFirst = !reorderIDsDepthFirst; });
                menu.AddItem(new GUIContent("Show/Prefer Titles For 'Links To' Menus"), prefs.preferTitlesForLinksTo, TogglePreferTitlesForLinksTo);
                menu.AddItem(new GUIContent("Search Bar"), isSearchBarOpen, ToggleDialogueTreeSearchBar);
                menu.AddItem(new GUIContent("Nodes"), false, ActivateNodeEditorMode);
                if (currentMapContainer == null)
                {
                    menu.AddDisabledItem(new GUIContent("Refresh"));
                }
                else
                {
                    menu.AddItem(new GUIContent("Refresh"), false, RefreshConversation);
                }
                AddRelationsInspectorMenuItems(menu);
                menu.ShowAsContext();
            }
        }

    }
}
