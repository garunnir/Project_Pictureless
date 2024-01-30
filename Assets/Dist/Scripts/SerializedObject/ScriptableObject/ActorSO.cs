namespace Garunnir.Runtime.ScriptableObject
{
    using PixelCrushers.DialogueSystem;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using Garunnir;
    using PixelCrushers;
    using System.Text.RegularExpressions;
#if UNITY_EDITOR
    using UnityEditorInternal;
    using UnityEditor;
#endif
    using System.Linq;
    [CreateAssetMenu(fileName = "Character", menuName = "GameDataAsset/Character")]
    public class ActorSO : ScriptableObject
    {
        public Actor actor;
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(ActorSO))]
    public class ActorSOEditor : Editor
    {
        private static GUIContent displayNameLabel = new GUIContent("Display Name", "The name to show in UIs.");
        static bool m_StatusToggle = false;
        bool m_isBaseFold = false;
        bool m_actorTexturesFoldout = false;
        bool m_actorSpritesFoldout = false;
        Actor m_actor;
        ActorSO m_target;
        Texture2D m_cachedAlignment;
        Texture2D m_cachedAlignmentCursor;
        Rect m_alignRect;
        //DialogueDatabase m_database;
        private int m_SelectedActoridx = -1;
        private int m_SelectedActorFieldID = -1;

        private static TextTableModule m_textTable;
        private static TextTableModule m_actorNameLocTable;
        class TextTableModule
        {
            string m_foldLabel = "TextTableModule";
            string[] ToolbarLabels = { "Languages", "Fields" };
            bool m_FilterOption = false;
            string m_filterKeyword;

            bool m_foldLocDialogue = false;
            [SerializeField]
            private int m_toolbarSelection = 0;

            [SerializeField]
            private int m_selectedLanguageIndex = 0;

            [SerializeField]
            private int m_selectedLanguageID = 0;
            public int actorID = -1;
            #region langProperties
            Rect position;
            bool foldLocDialogue;
            private Vector2 m_fieldListScrollPosition;
            private Vector2 m_languageListScrollPosition;

            SerializedProperty m_copyedMainProp;
            SerializedProperty m_MainProp;
            #endregion
            #region Variables

            [SerializeField]
            private bool m_isSearchPanelOpen = false;

            [SerializeField]
            private string m_searchString = string.Empty;

            [SerializeField]
            private string m_replaceString = string.Empty;

            [SerializeField]
            private bool m_matchCase = false;

            private TextTable m_textTable;
            public TextTable GetTextTable() { return m_textTable; }

            private bool m_needRefreshLists = true;
            private ReorderableList m_languageList = null;
            private ReorderableList m_fieldList = null;
            private SerializedObject m_serializedObject = null;
            private SerializedObject m_serializedObjectCopy = null;
            private GUIStyle textAreaStyle = null;
            private bool isTextAreaStyleInitialized = false;

            private int m_changeBarkField = -1;
            private string m_changeBarkStrField = "Input";

            #endregion
            #region Language List




            private void ResetLanguagesTab()
            {
                m_languageList = null;
                m_languageListScrollPosition = Vector2.zero;
            }
            private void OnDrawLanguageListHeader(Rect rect)
            {
                EditorGUI.LabelField(rect, "Languages");
            }
            private void OnDrawLanguageListElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
                var languageKeyProperty = languageKeysProperty.GetArrayElementAtIndex(index);
                var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(index);
                EditorGUI.BeginDisabledGroup(languageValueProperty.intValue == 0);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight), languageKeyProperty, GUIContent.none, false);
                EditorGUI.EndDisabledGroup();
            }

            private void OnAddLanguageListElement(ReorderableList list)
            {
                m_serializedObject.ApplyModifiedProperties();
                m_textTable.AddLanguage("Language " + m_textTable.nextLanguageID);
                m_serializedObject.Update();
                ResetFieldsTab();
            }

            private bool OnCanRemoveLanguageListElement(ReorderableList list)
            {
                var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(list.index);
                return languageValueProperty.intValue > 0;
            }

            private void OnRemoveLanguageListElement(ReorderableList list)
            {
                var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
                var languageKeyProperty = languageKeysProperty.GetArrayElementAtIndex(list.index);
                var languageName = languageKeyProperty.stringValue;
                var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(list.index);
                var languageID = languageValueProperty.intValue;
                if (!EditorUtility.DisplayDialog("Delete " + languageName, "Are you sure you want to delete the language '" + languageName +
                    "' and all field values associated with it?", "OK", "Cancel")) return;
                m_serializedObject.ApplyModifiedProperties();
                m_textTable.RemoveLanguage(languageID);
                m_serializedObject.Update();
                ResetFieldsTab();
            }

            private int m_selectedLanguageListIndex = -1;

            private void OnSelectLanguageListElement(ReorderableList list)
            {
                m_selectedLanguageListIndex = list.index;
            }

            private void OnReorderLanguageListElement(ReorderableList list)
            {
                // Also reorder values:
                var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                var value = languageValuesProperty.GetArrayElementAtIndex(m_selectedLanguageListIndex).intValue;
                languageValuesProperty.DeleteArrayElementAtIndex(m_selectedLanguageListIndex);
                languageValuesProperty.InsertArrayElementAtIndex(list.index);
                languageValuesProperty.GetArrayElementAtIndex(list.index).intValue = value;
                ResetFieldsTab();
            }
            private void DrawLanguagesTab()
            {
                if (m_languageList == null)
                {
                    m_serializedObject ??= new SerializedObject(m_textTable);
                    m_languageList = new ReorderableList(m_serializedObject, m_serializedObject.FindProperty("m_languageKeys"), true, true, true, true);
                    m_languageList.drawHeaderCallback = OnDrawLanguageListHeader;
                    m_languageList.drawElementCallback = OnDrawLanguageListElement;
                    m_languageList.onAddCallback = OnAddLanguageListElement;
                    m_languageList.onCanRemoveCallback = OnCanRemoveLanguageListElement;
                    m_languageList.onRemoveCallback = OnRemoveLanguageListElement;
                    m_languageList.onSelectCallback = OnSelectLanguageListElement;
                    m_languageList.onReorderCallback = OnReorderLanguageListElement;
                }
                m_languageListScrollPosition = GUILayout.BeginScrollView(m_languageListScrollPosition, false, false);
                try
                {
                    m_languageList.DoLayoutList();
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }

            #endregion
            #region Field List

            private void ResetFieldsTab()
            {
                m_fieldList = null;
                m_fieldListScrollPosition = Vector2.zero;
                m_selectedLanguageIndex = 0;
                m_selectedLanguageID = 0;
            }


            private const float MinColumnWidth = 100;

            private string[] m_languageDropdownList = null;

            private class CachedFieldInfo
            {
                public SerializedProperty fieldNameProperty;
                public SerializedProperty fieldValueProperty;
                public string nameControl;
                public string valueControl;
                public CachedFieldInfo(int index, SerializedProperty fieldNameProperty, SerializedProperty fieldValueProperty)
                {
                    this.fieldNameProperty = fieldNameProperty;
                    this.fieldValueProperty = fieldValueProperty;
                    //this.nameControl = "["+actorID+"].Field" + + index;
                    this.nameControl = "Field" + +index;
                    this.valueControl = "Value" + index;
                }
            }
            private List<CachedFieldInfo> m_fieldCache = new List<CachedFieldInfo>();

            private SerializedProperty RebuildProperty(SerializedObject sobj)
            {
                if (!m_FilterOption) return null;
                sobj.Update();
                m_serializedObjectCopy = new SerializedObject(m_textTable);
                m_copyedMainProp = m_serializedObjectCopy.FindProperty("m_fieldValues");
                m_serializedObjectCopy.Update();
                m_MainProp = sobj.FindProperty("m_fieldValues");
                Debug.Log("!" + m_MainProp.arraySize + "/" + m_copyedMainProp.arraySize);
                for (int index = 0; index < m_copyedMainProp.arraySize; index++)
                {
                    var fieldNameProperty = m_copyedMainProp.GetArrayElementAtIndex(index).FindPropertyRelative("m_fieldName");
                    Debug.Log(fieldNameProperty.stringValue + "/S");
                    if (!fieldNameProperty.stringValue.Contains(m_filterKeyword))
                    {
                        Debug.Log(fieldNameProperty.stringValue + "/D");
                        m_copyedMainProp.DeleteArrayElementAtIndex(index);
                        index--;
                    }
                }
                Debug.Log(m_MainProp.arraySize + "/" + m_copyedMainProp.arraySize);


                return m_copyedMainProp;
            }
            private void Refresh()
            {
                m_serializedObject = new SerializedObject(m_textTable);
                m_MainProp = m_serializedObject.FindProperty("m_fieldValues");
                m_needRefreshLists = true;
            }

            private void Update()
            {
                if (!m_FilterOption) return;
                for (int index = 0; index < m_MainProp.arraySize; index++)
                {
                    var fieldValuePropertyMain = m_MainProp.GetArrayElementAtIndex(index);
                    var fieldNamePropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_fieldName");
                    string mainkey = fieldNamePropertyMain.stringValue;
                    Debug.Log(mainkey);
                    for (int i = 0; i < m_copyedMainProp.arraySize; i++)
                    {
                        var fieldValuePropertyCopy = m_copyedMainProp.GetArrayElementAtIndex(i);
                        var fieldNamePropertyCopy = fieldValuePropertyCopy.FindPropertyRelative("m_fieldName");
                        if (mainkey == fieldNamePropertyCopy.stringValue)//필드이름이 일치하면
                        {
                            var keysPropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_keys");
                            var valuesPropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_values");
                            int valueIndex = -1;
                            for (int j = 0; j < keysPropertyMain.arraySize; j++)
                            {
                                if (keysPropertyMain.GetArrayElementAtIndex(j).intValue == m_selectedLanguageID)//랭귀지 일치,
                                {
                                    valueIndex = j;
                                    valuesPropertyMain.GetArrayElementAtIndex(j).stringValue = fieldValuePropertyCopy.FindPropertyRelative("m_values").GetArrayElementAtIndex(j).stringValue;//메인에 복붙
                                    Debug.LogWarning(mainkey + fieldValuePropertyCopy.FindPropertyRelative("m_values").GetArrayElementAtIndex(j).stringValue);
                                    break;
                                }
                            }
                            if (valueIndex == -1)
                            {
                                //텍스트 테이블에 필드를 추가하면 언어인덱스가 비어있으므로 생성한다.
                                //텍스트필드에서 찾을게 아니라 메인시리얼라이즈오브젝트를 변형하고 적용하면 되는거였다니..
                                valueIndex = keysPropertyMain.arraySize;
                                keysPropertyMain.arraySize++;
                                keysPropertyMain.GetArrayElementAtIndex(valueIndex).intValue = m_selectedLanguageID;
                                valuesPropertyMain.arraySize++;
                                valuesPropertyMain.GetArrayElementAtIndex(valueIndex).stringValue = fieldValuePropertyCopy.FindPropertyRelative("m_values").GetArrayElementAtIndex(m_selectedLanguageID).stringValue;
                                Debug.LogWarning(mainkey + valuesPropertyMain.GetArrayElementAtIndex(valueIndex).stringValue);
                            }
                        }
                    }
                }
                m_MainProp.serializedObject.ApplyModifiedProperties();
            }
            private void RebuildFieldCache()
            {
                m_fieldCache.Clear();

                var fieldValuesProperty = m_FilterOption ? m_copyedMainProp : m_MainProp;
                for (int index = 0; index < fieldValuesProperty.arraySize; index++)
                {
                    var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(index);
                    var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
                    var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
                    var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");


                    var valueIndex = -1;
                    if (m_FilterOption && !fieldNameProperty.stringValue.Contains(m_filterKeyword))
                    {
                        continue;
                    }
                    for (int i = 0; i < keysProperty.arraySize; i++)
                    {

                        if (keysProperty.GetArrayElementAtIndex(i).intValue == m_selectedLanguageID)
                        {
                            valueIndex = i;
                            break;
                        }
                    }
                    if (valueIndex == -1)
                    {
                        valueIndex = keysProperty.arraySize;
                        keysProperty.arraySize++;
                        keysProperty.GetArrayElementAtIndex(valueIndex).intValue = m_selectedLanguageID;
                        valuesProperty.arraySize++;
                        valuesProperty.GetArrayElementAtIndex(valueIndex).stringValue = string.Empty;
                    }
                    var valueProperty = valuesProperty.GetArrayElementAtIndex(valueIndex);

                    m_fieldCache.Add(new CachedFieldInfo(index, fieldNameProperty, valueProperty));
                }
            }

            private void OnDrawFieldListHeader(Rect rect)
            {
                var headerWidth = rect.width - 14;
                var columnWidth = headerWidth / 2;
                EditorGUI.LabelField(new Rect(rect.x + 14, rect.y, columnWidth, rect.height), "Field");
                var popupRect = new Rect(rect.x + rect.width - columnWidth, rect.y, columnWidth, rect.height);
                var newIndex = EditorGUI.Popup(popupRect, m_selectedLanguageIndex, m_languageDropdownList);
                if (newIndex != m_selectedLanguageIndex)
                {
                    m_selectedLanguageIndex = newIndex;
                    var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                    var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(newIndex);
                    m_selectedLanguageID = languageValueProperty.intValue;
                    RebuildFieldCache();
                }
                if (!m_FilterOption) return;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh"))
                {
                    Refresh();
                }
                if (GUILayout.Button("Update"))
                {
                    Update();
                }
                GUILayout.EndHorizontal();
            }

            private void OnDrawFieldListElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                //if (rect.width <= 0) return;
                // Since lists can be very long, only draw elements within the visible window:
                //if (!(0 <= index && index < m_fieldCache.Count)) return;
                //var isElementVisible = rect.Overlaps(new Rect(0, m_fieldListScrollPosition.y, position.width, position.height));
                //if (!isElementVisible) return;

                //m_serializedObject.Update();
                if (index >= m_fieldCache.Count)
                {
                    return;
                }
                var columnWidth = (rect.width / 2) - 1;

                var info = m_fieldCache[index];

                GUI.SetNextControlName(info.nameControl);
                int buttonwidth = 20;

                string fieldname = info.fieldNameProperty.stringValue;
                if (m_FilterOption)
                {
                    string[] fieldnamesplit = fieldname.Split('.');
                    bool fieldNameVaild = fieldnamesplit.Length > 1;
                    if (m_changeBarkField == index)
                    {
                        GUI.backgroundColor = Color.green;
                        m_changeBarkStrField = GUI.TextField(new Rect(rect.x, rect.y + 1, columnWidth - buttonwidth, EditorGUIUtility.singleLineHeight), m_changeBarkStrField);
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        //GUI.enabled = false;
                        fieldname = (fieldnamesplit.Length > 1) ? fieldnamesplit[1] : "missing";
                        GUI.Label(new Rect(rect.x, rect.y + 1, columnWidth - buttonwidth, EditorGUIUtility.singleLineHeight), fieldname);
                        //EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, columnWidth-buttonwidth, EditorGUIUtility.singleLineHeight), info.fieldNameProperty, GUIContent.none, false);
                        //GUI.enabled = true;
                    }
                    if (GUI.Button(new Rect(rect.x + columnWidth - buttonwidth, rect.y + 1, buttonwidth, EditorGUIUtility.singleLineHeight), "C"))
                    {
                        if (m_changeBarkField == -1 || index != m_changeBarkField)
                        {
                            m_changeBarkField = index;
                            m_changeBarkStrField = fieldnamesplit[1];
                        }
                        else
                        {
                            for (int i = 0; i < m_MainProp.arraySize; i++)
                            {
                                if (m_MainProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_fieldName").stringValue == info.fieldNameProperty.stringValue)
                                {
                                    m_MainProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_fieldName").stringValue = fieldNameVaild ? m_filterKeyword + m_changeBarkStrField : fieldnamesplit[1];
                                    m_serializedObject.ApplyModifiedProperties();
                                    Refresh();
                                }
                            }
                            m_changeBarkField = -1;
                        }

                        Debug.Log("work");
                    }
                }
                else
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, columnWidth, EditorGUIUtility.singleLineHeight), info.fieldNameProperty, GUIContent.none, false);
                }

                GUI.SetNextControlName(info.valueControl);
                EditorGUI.PropertyField(new Rect(rect.x + rect.width - columnWidth, rect.y + 1, columnWidth, EditorGUIUtility.singleLineHeight), info.fieldValueProperty, GUIContent.none, false);
                if (info.fieldValueProperty != null)
                {
                    var focusedControl = GUI.GetNameOfFocusedControl();
                    if (string.Equals(info.nameControl, focusedControl) || string.Equals(info.valueControl, focusedControl))
                    {
                        m_selectedFieldListElement = index;
                        m_fieldList.index = index;
                    }
                }
                m_serializedObject.ApplyModifiedProperties();
            }

            private void OnAddFieldListElement(ReorderableList list)
            {
                m_serializedObject.Update();
                if (m_FilterOption)
                {
                    m_textTable.AddField(m_filterKeyword + "NewField " + m_textTable.nextFieldID);
                }
                else
                {
                    m_textTable.AddField("NewField " + m_textTable.nextFieldID);
                }
                m_serializedObject.ApplyModifiedProperties();

                RebuildProperty(m_serializedObject);
                RebuildFieldCache();
                Refresh();
                //Repaint();
            }
            /// <summary>
            /// 복사본의 아이디의 스트링값을 참고로 메인의 인덱스를 알아낸다.
            /// </summary>
            /// <param name="idx"></param>
            /// <returns></returns>
            private int FindSyncMain(int idx)
            {
                string srcname = m_copyedMainProp.GetArrayElementAtIndex(idx).FindPropertyRelative("m_fieldName").stringValue;
                for (int index = 0; index < m_MainProp.arraySize; index++)
                {
                    var fieldValuePropertyMain = m_MainProp.GetArrayElementAtIndex(index);
                    var fieldNamePropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_fieldName");
                    string mainkey = fieldNamePropertyMain.stringValue;
                    if (mainkey == srcname)
                    {
                        return index;
                    }
                }
                return -1;
            }
            private void OnRemoveFieldListElement(ReorderableList list)
            {
                int fieldID = m_FilterOption ? FindSyncMain(list.index) : list.index;
                var fieldValuesProperty = m_FilterOption ? m_copyedMainProp : m_MainProp;
                var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(list.index);
                var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
                var fieldName = fieldNameProperty.stringValue;
                if (!EditorUtility.DisplayDialog("Delete Field", "Are you sure you want to delete the field '" + fieldName +
                    "' and all values associated with it?", "OK", "Cancel")) return;
                m_serializedObject.Update();
                m_MainProp.DeleteArrayElementAtIndex(fieldID);
                m_serializedObject.ApplyModifiedProperties();
                RebuildProperty(m_serializedObject);
                RebuildFieldCache();
                Refresh();
            }

            private int m_selectedFieldListElement;

            private void OnSelectFieldListElement(ReorderableList list)
            {
                m_selectedFieldListElement = list.index;
            }

            private void OnReorderFieldListElement(ReorderableList list)
            {
                // Also reorder keys:
                var fieldKeysProperty = m_serializedObject.FindProperty("m_fieldKeys");
                var value = fieldKeysProperty.GetArrayElementAtIndex(m_selectedFieldListElement).intValue;
                fieldKeysProperty.DeleteArrayElementAtIndex(m_selectedFieldListElement);
                fieldKeysProperty.InsertArrayElementAtIndex(list.index);
                fieldKeysProperty.GetArrayElementAtIndex(list.index).intValue = value;
            }

            private void CheckMouseEvents()
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1) // Right-click
                {
                    var scrolledClickPosition = Event.current.mousePosition.y - 16;
                    var elementHeight = (EditorGUIUtility.singleLineHeight + 5);
                    var index = Mathf.FloorToInt(scrolledClickPosition / elementHeight);
                    if (0 <= index && index < m_fieldList.count)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Insert Field"), false, InsertFieldListElement, index);
                        menu.AddItem(new GUIContent("Delete Field"), false, DeleteFieldListElement, index);
                        menu.ShowAsContext();
                    }
                }
            }

            private void InsertFieldListElement(object data)
            {
                int index = (int)data;
                m_serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(m_textTable, "Insert Field");
                if (m_FilterOption)
                {
                    m_textTable.InsertField(index, m_filterKeyword + m_textTable.nextFieldID);
                }
                else
                {
                    m_textTable.InsertField(index, "new" + m_textTable.nextFieldID);
                }
                EditorUtility.SetDirty(m_textTable);
                m_serializedObject.Update();
                RebuildFieldCache();
                //Repaint();
            }

            private void DeleteFieldListElement(object data)
            {
                int index = (int)data;
                var info = m_fieldCache[index];
                if (EditorUtility.DisplayDialog("Delete Field", "Delete '" + info.fieldNameProperty.stringValue + "'?", "OK", "Cancel"))
                {
                    m_serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(m_textTable, "Delete Field");
                    m_textTable.RemoveField(info.fieldNameProperty.stringValue);
                    EditorUtility.SetDirty(m_textTable);
                    m_serializedObject.Update();
                    RebuildFieldCache();
                    //Repaint();
                }
            }

            private bool IsAnyFieldSelected()
            {
                return m_fieldList != null && 0 <= m_fieldList.index && m_fieldList.index < m_fieldList.serializedProperty.arraySize;
            }

            private void DrawFieldsTab()
            {
                DrawGrid();
                DrawEntryBox();
            }
            private void DrawGrid()
            {
                if (m_textTable == null) return;
                var entryBoxHeight = IsAnyFieldSelected() ? (6 * EditorGUIUtility.singleLineHeight) : 0;
                if (m_isSearchPanelOpen) entryBoxHeight += (4 * EditorGUIUtility.singleLineHeight);

                if (m_needRefreshLists || m_fieldList == null || m_languageDropdownList == null)
                {
                    m_needRefreshLists = false;
                    var por = m_FilterOption ? RebuildProperty(m_serializedObject) : m_MainProp ??= m_serializedObject.FindProperty("m_fieldValues");

                    m_fieldList = new ReorderableList(m_serializedObject, por, true, true, true, true);
                    m_fieldList.drawHeaderCallback = OnDrawFieldListHeader;
                    m_fieldList.drawElementCallback = OnDrawFieldListElement;
                    m_fieldList.onAddCallback = OnAddFieldListElement;
                    m_fieldList.onRemoveCallback = OnRemoveFieldListElement;
                    m_fieldList.onSelectCallback = OnSelectFieldListElement;
                    m_fieldList.onReorderCallback = OnReorderFieldListElement;

                    var languages = new List<string>();
                    var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
                    for (int i = 0; i < languageKeysProperty.arraySize; i++)
                    {
                        languages.Add(languageKeysProperty.GetArrayElementAtIndex(i).stringValue);
                    }
                    m_languageDropdownList = languages.ToArray();

                    RebuildFieldCache();
                }
                m_fieldList.DoLayoutList();

                CheckMouseEvents();
            }
            private void DrawEntryBox()
            {
                if (m_needRefreshLists || !IsAnyFieldSelected()) return;
                //var rect = new Rect(2, position.height - 6 * EditorGUIUtility.singleLineHeight, position.width - 4, 6 * EditorGUIUtility.singleLineHeight);
                var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));

                if (m_isSearchPanelOpen)
                {
                    var searchPanelHeight = (4 * EditorGUIUtility.singleLineHeight);
                    rect = new Rect(rect.x, rect.y - searchPanelHeight, rect.width, rect.height);
                }
                var fieldValuesProperty = m_FilterOption ? m_copyedMainProp : m_MainProp;
                var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(m_fieldList.index);
                var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
                var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");
                var valueIndex = -1;
                var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
                if (m_FilterOption && !fieldNameProperty.stringValue.Contains(m_filterKeyword))
                {
                    return;
                }
                for (int i = 0; i < keysProperty.arraySize; i++)
                {
                    if (keysProperty.GetArrayElementAtIndex(i).intValue == m_selectedLanguageID)
                    {
                        valueIndex = i;
                        break;
                    }
                }
                if (valueIndex == -1)
                {
                    valueIndex = keysProperty.arraySize;
                    keysProperty.arraySize++;
                    keysProperty.GetArrayElementAtIndex(valueIndex).intValue = m_selectedLanguageID;
                    valuesProperty.arraySize++;
                    valuesProperty.GetArrayElementAtIndex(valueIndex).stringValue = string.Empty;
                }
                else
                {

                }
                if (textAreaStyle == null || !isTextAreaStyleInitialized)
                {
                    isTextAreaStyleInitialized = true;
                    textAreaStyle = new GUIStyle(EditorStyles.textField);
                    textAreaStyle.wordWrap = true;
                }
                var valueProperty = valuesProperty.GetArrayElementAtIndex(valueIndex);
                valueProperty.stringValue = EditorGUI.TextArea(rect, valueProperty.stringValue, textAreaStyle);
            }


            #endregion
            #region Search

            private void OpenSearchPanel()
            {
                m_isSearchPanelOpen = !m_isSearchPanelOpen;
            }

            private void DrawSearchPanel()
            {
                var rect = new Rect(2, position.height - 5 * EditorGUIUtility.singleLineHeight, position.width - 4, 5 * EditorGUIUtility.singleLineHeight);
                var searchRect = new Rect(rect.x, rect.y + rect.height - 4 * EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight);
                var replaceRect = new Rect(rect.x, rect.y + rect.height - 3 * EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(rect.x, rect.y + rect.height - 2 * EditorGUIUtility.singleLineHeight + 4, rect.width, EditorGUIUtility.singleLineHeight);
                m_searchString = EditorGUI.TextField(searchRect, new GUIContent("Find", "Regular expressions allowed."), m_searchString);
                m_replaceString = EditorGUI.TextField(replaceRect, "Replace With", m_replaceString);
                var buttonWidth = 78f;
                var toggleWidth = 90f;
                m_matchCase = EditorGUI.ToggleLeft(new Rect(buttonRect.x + buttonRect.width - (4 * (2 + buttonWidth)) - toggleWidth, buttonRect.y, toggleWidth, buttonRect.height), "Match Case", m_matchCase);
                if (GUI.Button(new Rect(buttonRect.x + buttonRect.width - (4 * (2 + buttonWidth)), buttonRect.y, buttonWidth, buttonRect.height), "Find Next"))
                {
                    FindNext();
                }
                EditorGUI.BeginDisabledGroup(!IsAnyFieldSelected());
                if (GUI.Button(new Rect(buttonRect.x + buttonRect.width - (3 * (2 + buttonWidth)), buttonRect.y, buttonWidth, buttonRect.height), "Replace"))
                {
                    ReplaceCurrent();
                }
                EditorGUI.EndDisabledGroup();
                if (GUI.Button(new Rect(buttonRect.x + buttonRect.width - (2 * (2 + buttonWidth)), buttonRect.y, buttonWidth, buttonRect.height), "Replace All"))
                {
                    ReplaceAll();
                }
                if (GUI.Button(new Rect(buttonRect.x + buttonRect.width - (1 * (2 + buttonWidth)), buttonRect.y, buttonWidth, buttonRect.height), "Cancel"))
                {
                    m_isSearchPanelOpen = false;
                }
            }

            private void FindNext()
            {
                var found = false;
                int currentIndex = (m_fieldList.index + 1) % m_fieldList.count;
                var regexOptions = m_matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                int safeguard = 0;
                while (!found && safeguard < 9999)
                {
                    safeguard++;
                    var info = m_fieldCache[currentIndex];
                    if (Regex.IsMatch(info.fieldNameProperty.stringValue, m_searchString, regexOptions) ||
                        Regex.IsMatch(info.fieldValueProperty.stringValue, m_searchString, regexOptions))
                    {
                        found = true;
                        break;
                    }
                    else if (currentIndex == m_fieldList.index)
                    {
                        break; // Wrapped around, so stop.
                    }
                    else
                    {
                        currentIndex = (currentIndex + 1) % m_fieldList.count;
                    }
                }
                if (found)
                {
                    m_fieldList.index = currentIndex;
                    // Scroll to position:
                    var minScrollY = m_fieldList.index * (EditorGUIUtility.singleLineHeight + 5);
                    m_fieldListScrollPosition = new Vector2(m_fieldListScrollPosition.x, minScrollY);
                }
                else
                {
                    EditorUtility.DisplayDialog("Search Text Table", "String '" + m_searchString + "' not found in text table.", "OK");
                }
            }

            private void ReplaceCurrent()
            {
                if (!IsAnyFieldSelected()) return;
                var regexOptions = m_matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                m_fieldCache[m_fieldList.index].fieldNameProperty.stringValue = Regex.Replace(m_fieldCache[m_fieldList.index].fieldNameProperty.stringValue, m_searchString, m_replaceString, regexOptions);
                m_fieldCache[m_fieldList.index].fieldValueProperty.stringValue = Regex.Replace(m_fieldCache[m_fieldList.index].fieldValueProperty.stringValue, m_searchString, m_replaceString, regexOptions);
            }

            private void ReplaceAll()
            {
                if (!EditorUtility.DisplayDialog("Replace All", "Replace:\n'" + m_searchString + "'\nwith:\n'" + m_replaceString + "'\nin entire table for current language?", "OK", "Cancel")) return;
                var regexOptions = m_matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                EditorUtility.DisplayProgressBar("Replace All", "Processing text table.", 0);
                for (int i = 0; i < m_fieldCache.Count; i++)
                {
                    m_fieldCache[i].fieldNameProperty.stringValue = Regex.Replace(m_fieldCache[i].fieldNameProperty.stringValue, m_searchString, m_replaceString, regexOptions);
                    m_fieldCache[i].fieldValueProperty.stringValue = Regex.Replace(m_fieldCache[i].fieldValueProperty.stringValue, m_searchString, m_replaceString, regexOptions);

                }
                EditorUtility.ClearProgressBar();
            }

            #endregion
            #region Constructer
            public TextTableModule(TextTable table, string foldLabel = "TextTableModule")
            {
                m_textTable = table;
                m_FilterOption = false;
                m_foldLabel = foldLabel;
            }
            public TextTableModule(TextTable table, string filterKeyword, string foldLabel = "TextTableModule")
            {
                m_textTable = table;
                m_FilterOption = true;
                m_filterKeyword = filterKeyword;
                m_foldLabel = foldLabel;
            }

            #endregion
            #region filter
            public void SetFilter(string filterKeyword)
            {
                m_FilterOption = true;
                m_filterKeyword = filterKeyword;
            }
            public void DisableFilter()
            {
                m_FilterOption = false;
            }
            #endregion
            public void DrawTextTable(bool setDirty, Actor target)
            {
                if (m_textTable)
                {
                    m_foldLocDialogue = EditorGUILayout.Foldout(m_foldLocDialogue, new GUIContent(m_foldLabel, "Portrait images using texture assets."));
                    if (m_foldLocDialogue)
                    {
                        var newToolbarSelection = GUILayout.Toolbar(m_toolbarSelection, ToolbarLabels);
                        if (newToolbarSelection != m_toolbarSelection)
                        {
                            m_toolbarSelection = newToolbarSelection;
                            m_needRefreshLists = true;
                            actorID = target.id;
                        }
                        else if (setDirty)
                        {
                            m_toolbarSelection = 0;
                            m_needRefreshLists = true;
                            actorID = target.id;
                        }
                        switch (m_toolbarSelection)
                        {
                            case 0:
                                DrawLanguagesTab();
                                break;
                            case 1:
                                DrawFieldsTab();
                                break;
                        }
                    }
                }
            }


        }
        public override void OnInspectorGUI()
        {
            m_target ??= target as ActorSO;
            Show();
            m_isBaseFold = EditorGUILayout.Foldout(m_isBaseFold, "BaseEditor");
            if (m_isBaseFold)
                base.OnInspectorGUI();
        }
        void Show()
        {
            Actor target = m_target.actor;
            m_actorNameLocTable ??= new TextTableModule(AssetDatabase.LoadAssetAtPath(ConstDataTable.AssetPath.LocalizeTable.ActorName, typeof(TextTable)) as TextTable, "LocActorDisc");
            m_textTable ??= new TextTableModule(AssetDatabase.LoadAssetAtPath(ConstDataTable.AssetPath.LocalizeTable.ActorBark, typeof(TextTable)) as TextTable, "ActorBarkDisc");
            m_textTable ??= new TextTableModule(EditorGUILayout.ObjectField(m_textTable.GetTextTable(), typeof(TextTable), true) as TextTable);
            m_actor ??= m_target.actor;
            int langidx = m_textTable.GetTextTable().languages[ConstDataTable.DefalutLang];
            bool isTargetDataChanged = m_actorNameLocTable.actorID != target.id;
            if (isTargetDataChanged)
            {
                m_textTable.actorID = m_actorNameLocTable.actorID = target.id;
                m_textTable.SetFilter($"[{target.id}].");
            }
            m_actor.id = EditorGUILayout.IntField("ID", m_actor.id);
            EditorGUILayout.BeginHorizontal();
            m_SelectedActorFieldID = m_actorNameLocTable.GetTextTable().GetFieldID(m_actor.Name);
            string locName = m_actorNameLocTable.GetTextTable().GetFieldTextForLanguage(m_SelectedActorFieldID, langidx);
            EditorGUILayout.LabelField("Name", locName, GUILayout.Width(EditorGUIUtility.labelWidth * 2));
            if (isTargetDataChanged || m_SelectedActoridx == -1)
            {
                m_SelectedActoridx = m_actorNameLocTable.GetTextTable().GetFieldNames().ToList().IndexOf(m_actor.Name);
            }
            m_SelectedActoridx = EditorGUILayout.Popup(m_SelectedActoridx, m_actorNameLocTable.GetTextTable().GetFieldNames());
            if (m_SelectedActoridx != -1)
                m_actor.Name = m_actorNameLocTable.GetTextTable().GetFieldNames()[m_SelectedActoridx];
            EditorGUILayout.EndHorizontal();
            m_actorNameLocTable.DrawTextTable(isTargetDataChanged, target);
            DrawActorPortrait(m_actor);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Alignment");
            GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.Height(100), GUILayout.Width(100) });
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), m_cachedAlignment ??= EditorGUIUtility.Load("Custom/Alignment.png") as Texture2D);
            m_alignRect = GUILayoutUtility.GetLastRect();
            var rect = m_alignRect;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.mousePosition.x > m_alignRect.x &&
                Event.current.mousePosition.x < m_alignRect.x + m_alignRect.width && Event.current.mousePosition.y > m_alignRect.y && Event.current.mousePosition.y < m_alignRect.y + m_alignRect.height)
            {
                var calPos = Event.current.mousePosition - new Vector2(m_alignRect.x + 50, m_alignRect.y + 50);
                calPos = -calPos / 50;
                if (calPos.magnitude > 1)
                    calPos = calPos.normalized;
                target.alignment = calPos;
            }
            GUI.DrawTexture(new Rect(m_alignRect.x - target.alignment.x * 50 + 50 - 10, m_alignRect.y - target.alignment.y * 50 + 50 - 10, 20, 20), m_cachedAlignmentCursor ??= EditorGUIUtility.Load("Dialogue System/Event.png") as Texture2D);
            GUILayout.EndVertical();


            GUILayout.EndHorizontal();
            Rect windowRect = new Rect(10, 30, 200, 100);
            //여기에 테이블
            m_textTable.DrawTextTable(isTargetDataChanged, target);

            DrawDNDStatus(target, isTargetDataChanged);

            DrawWeapon(target, isTargetDataChanged);
        }
        #region Skill
        private void DrawSkill()
        {
            //스킬을 표시한다.
            //액티브 패시브.
            DrawActive();
            DrawPassive();
        }
        private void DrawActive()
        {
            //리오더블리스트로 보여주는게 좋을것 같음.
        }
        private void DrawPassive()
        {

        }
        #endregion
        private void DrawWeapon(Actor target, bool isTargetDataChanged)
        {
            if (!Field.FieldExists(target.fields, ConstDataTable.Equipment.Weapon)) return;
        }

        #region DNDStatus
        private void DrawDNDStatus(Actor target, bool isTargetDataChanged)
        {
            void SetIntField(string title, string label)
            {
                Field.SetValue(target.fields, title, EditorGUILayout.IntField(label, Field.LookupInt(target.fields, title)));
            }
            //어카지
            //필드 구성요소를 가져온다
            //빈필드일때 기본 구성요소를 제공한다
            m_StatusToggle = EditorGUILayout.Foldout(m_StatusToggle, "BasicStatus");
            if (isTargetDataChanged)
            {
                StatusInitialize(target);
            }
            if (m_StatusToggle)
            {
                EditorGUILayout.LabelField("캐릭터기본스텟");
                SetIntField(ConstDataTable.Actor.Status.Str, "STR");
                SetIntField(ConstDataTable.Actor.Status.Con, "CON");
                SetIntField(ConstDataTable.Actor.Status.Dex, "DEX");
                SetIntField(ConstDataTable.Actor.Status.Int, "INT");
                SetIntField(ConstDataTable.Actor.Status.Wis, "WIS");
                SetIntField(ConstDataTable.Actor.Status.Cha, "CHA");
                EditorGUILayout.LabelField("캐릭터자원");
                SetIntField(ConstDataTable.Actor.Status.Hp, "HP");
            }
        }
        static void StatusInitialize(Actor target)
        {
            void Init(string fieldname, int value = 5)
            {
                if (!Field.FieldExists(target.fields, fieldname))
                    Field.SetValue(target.fields, fieldname, value);
            }
            Init(ConstDataTable.Actor.Status.Str);
            Init(ConstDataTable.Actor.Status.Con);
            Init(ConstDataTable.Actor.Status.Dex);
            Init(ConstDataTable.Actor.Status.Int);
            Init(ConstDataTable.Actor.Status.Wis);
            Init(ConstDataTable.Actor.Status.Cha);
            Init(ConstDataTable.Actor.Status.Hp, 100);
        }
        #endregion

        private void DrawRevisableTextField(GUIContent label, Asset asset, DialogueEntry entry, List<Field> fields, string fieldTitle)
        {
            Field field = Field.Lookup(fields, fieldTitle);
            if (field == null)
            {
                field = new Field(fieldTitle, string.Empty, FieldType.Text);
                fields.Add(field);
            }
            DrawRevisableTextField(label, asset, entry, field);
        }
        private void DrawAIReviseTextButton(Asset asset, DialogueEntry entry, Field field)
        {
        }
        private void DrawRevisableTextField(GUIContent label, Asset asset, DialogueEntry entry, Field field)
        {
            if (field == null) return;
            EditorGUILayout.BeginHorizontal();
            field.value = EditorGUILayout.TextField(label, field.value);
            DrawAIReviseTextButton(asset, entry, field);
            EditorGUILayout.EndHorizontal();
        }
        private void DrawLocalizedVersions(Asset asset, List<Field> fields, string titleFormat, bool alwaysAdd, FieldType fieldType, bool useSequenceEditor = false)
        {
            //bool indented = false;
            //foreach (var language in languages)
            //{
            //    string localizedTitle = string.Format(titleFormat, language);
            //    Field field = Field.Lookup(fields, localizedTitle);
            //    if ((field == null) && (alwaysAdd || (Field.FieldExists(template.dialogueEntryFields, localizedTitle))))
            //    {
            //        field = new Field(localizedTitle, string.Empty, fieldType);
            //        fields.Add(field);
            //    }
            //    if (field != null)
            //    {
            //        if (!indented)
            //        {
            //            indented = true;
            //            EditorWindowTools.StartIndentedSection();
            //        }
            //        if (useSequenceEditor)
            //        {
            //            EditorGUILayout.BeginHorizontal();
            //            EditorGUILayout.LabelField(localizedTitle);
            //            GUILayout.FlexibleSpace();
            //            EditorGUILayout.EndHorizontal();
            //            field.value = EditorGUILayout.TextArea(field.value);
            //        }
            //        else
            //        {
            //            //[AI] EditorGUILayout.LabelField(localizedTitle);
            //            //field.value = EditorGUILayout.TextArea(field.value);
            //            DrawLocalizableTextAreaField(new GUIContent(localizedTitle), asset, null, field);
            //        }
            //        if (alreadyDrawn != null) alreadyDrawn.Add(field);
            //    }
            //}
            //if (indented) EditorWindowTools.EndIndentedSection();
        }

        private void DrawActorPortrait(Actor actor)
        {
            if (actor == null) return;

            // Display Name:
            var displayNameField = Field.Lookup(actor.fields, "Display Name");
            var hasDisplayNameField = (displayNameField != null);
            var useDisplayNameField = EditorGUILayout.Toggle(new GUIContent("Use Display Name", "Tick to use a Display Name in UIs that's different from the Name."), hasDisplayNameField);
            if (hasDisplayNameField && !useDisplayNameField)
            {
                actor.fields.Remove(displayNameField);
            }
            else if (useDisplayNameField)
            {
                if (!hasDisplayNameField && string.IsNullOrEmpty(actor.LookupValue("Display Name")))
                {
                    Field.SetValue(actor.fields, "Display Name", actor.Name);
                }
                DrawRevisableTextField(displayNameLabel, actor, null, actor.fields, "Display Name");
                DrawLocalizedVersions(actor, actor.fields, "Display Name {0}", false, FieldType.Text);
            }

            // Portrait Textures:
            m_actorTexturesFoldout = EditorGUILayout.Foldout(m_actorTexturesFoldout, new GUIContent("Portrait Textures", "Portrait images using texture assets."));
            if (m_actorTexturesFoldout)
            {

                try
                {
                    var newPortrait = EditorGUILayout.ObjectField(new GUIContent("Portraits", "This actor's portrait. Only necessary if your UI uses portraits."),
                                                             actor.portrait, typeof(Texture2D), false, GUILayout.Height(64)) as Texture2D;
                    if (newPortrait != actor.portrait)
                    {
                        var texpath = AssetDatabase.GetAssetPath(newPortrait);
                        if (texpath.Contains("Resources/"))
                        {
                            texpath = texpath.Split("Resources/")[1];
                            actor.textureName = texpath;
                        }
                        else
                        {
                            Debug.LogError("Is not Resources File");
                        }
                        actor.portrait = newPortrait;
                    }
                }
                catch (NullReferenceException)
                {
                }
                int indexToDelete = -1;
                if (actor.alternatePortraits == null) actor.alternatePortraits = new List<Texture2D>();
                for (int i = 0; i < actor.alternatePortraits.Count; i++)
                {
                    try
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        try
                        {
                            EditorGUILayout.BeginVertical(GUILayout.Width(27));
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(5), GUILayout.Height(16));
                            EditorGUILayout.LabelField(string.Format("[{0}]", i + 2), CenteredLabelStyle, GUILayout.Width(27));
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(5));
                            if (GUILayout.Button(new GUIContent(" ", "Delete this portrait."), "OL Minus", GUILayout.Width(16), GUILayout.Height(16)))
                            {
                                indexToDelete = i;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        finally
                        {
                            EditorGUILayout.EndVertical();
                        }

                        try
                        {
                            //EditorGUI.BeginChangeCheck();
                            actor.alternatePortraits[i] = EditorGUILayout.ObjectField(actor.alternatePortraits[i], typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
                            //if (EditorGUI.EndChangeCheck()) SetDatabaseDirty("Actor Portrait");
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (indexToDelete > -1)
                {
                    actor.alternatePortraits.RemoveAt(indexToDelete);
                }

                EditorGUILayout.LabelField(string.Empty, GUILayout.Height(4));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(" ", "Add new alternate portrait image."), "OL Plus", GUILayout.Height(16)))
                {
                    actor.alternatePortraits.Add(null);
                }
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(12));
                EditorGUILayout.EndHorizontal();
            }

            // Portrait Sprites:
            EditorGUILayout.BeginHorizontal();
            m_actorSpritesFoldout = EditorGUILayout.Foldout(m_actorSpritesFoldout, new GUIContent("Portrait Sprites", "Portrait images using sprite assets."));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (m_actorSpritesFoldout)
            {
                try
                {
                    var newPortrait = EditorGUILayout.ObjectField(new GUIContent("Portraits", "This actor's portrait. Only necessary if your UI uses portraits."),
                                                             actor.spritePortrait, typeof(Sprite), false, GUILayout.Height(64)) as Sprite;
                    if (newPortrait != actor.spritePortrait)
                    {
                        actor.spritePortrait = newPortrait;
                    }
                }
                catch (NullReferenceException)
                {
                }
                int indexToDelete = -1;
                if (actor.spritePortraits == null) actor.spritePortraits = new List<Sprite>();
                for (int i = 0; i < actor.spritePortraits.Count; i++)
                {
                    try
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        try
                        {
                            EditorGUILayout.BeginVertical(GUILayout.Width(27));
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(5), GUILayout.Height(16));
                            EditorGUILayout.LabelField(string.Format("[{0}]", i + 2), CenteredLabelStyle, GUILayout.Width(27));
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(5));
                            if (GUILayout.Button(new GUIContent(" ", "Delete this portrait."), "OL Minus", GUILayout.Width(16), GUILayout.Height(16)))
                            {
                                indexToDelete = i;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        finally
                        {
                            EditorGUILayout.EndVertical();
                        }

                        try
                        {
                            //EditorGUI.BeginChangeCheck();
                            actor.spritePortraits[i] = EditorGUILayout.ObjectField(actor.spritePortraits[i], typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64)) as Sprite;
                            //if (EditorGUI.EndChangeCheck()) SetDatabaseDirty("Actor Portrait");
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (indexToDelete > -1)
                {
                    actor.spritePortraits.RemoveAt(indexToDelete);
                }

                EditorGUILayout.LabelField(string.Empty, GUILayout.Height(4));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(" ", "Add new alternate portrait image."), "OL Plus", GUILayout.Height(16)))
                {
                    actor.spritePortraits.Add(null);
                }
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(12));
                EditorGUILayout.EndHorizontal();
            }

            // The rest: node color, Is Player, etc.
            //DrawActorNodeColor(actor);

            //EditorGUI.BeginChangeCheck();
            actor.IsPlayer = EditorGUILayout.Toggle(new GUIContent("Is Player", ""), actor.IsPlayer);
            //if (EditorGUI.EndChangeCheck()) SetDatabaseDirty("IsPlayer");

            //DrawActorPrimaryFields(actor);

        }
        private GUIStyle CenteredLabelStyle
        {
            get
            {
                GUIStyle centeredLabelStyle = new GUIStyle(EditorStyles.label);
                centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
                return centeredLabelStyle;
            }
        }

    }

#endif
}