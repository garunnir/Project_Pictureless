using Garunnir;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

public static partial class ExtendDEHooks
{
    #region langProperties
    static int actorID=-1;
    static Rect position;
    static bool foldLocDialogue;
    static string[] ToolbarLabels = { "Languages", "Fields" };
    [SerializeField]
    static private Vector2 m_fieldListScrollPosition;
    [SerializeField]
    static private Vector2 m_languageListScrollPosition;

    [SerializeField]
    static private int m_toolbarSelection = 0;

    [SerializeField]
    static private int m_selectedLanguageIndex = 0;

    [SerializeField]
    static private int m_selectedLanguageID = 0;

    static SerializedProperty m_copyedMainProp;
    static SerializedProperty m_MainProp;
    #endregion
    #region Variables

    public static bool isOpen { get { return instance != null; } }

    public static TextTableEditorWindow instance { get { return s_instance; } }

    private static TextTableEditorWindow s_instance = null;

    private const string WindowTitle = "Text Table";

    //private static GUIContent[] ToolbarLabels = new GUIContent[]
    //    { new GUIContent("Languages"), new GUIContent("Fields") };

    //[SerializeField]
    //private int m_textTableInstanceID;

    //[SerializeField]
    //private Vector2 m_languageListScrollPosition;

    //[SerializeField]
    //private Vector2 m_fieldListScrollPosition;

    //[SerializeField]
    //private int m_toolbarSelection = 0;

    //[SerializeField]
    //private int m_selectedLanguageIndex = 0;

    //[SerializeField]
    //private int m_selectedLanguageID = 0;

    //[SerializeField]
    //private string m_csvFilename = string.Empty;

    [SerializeField]
    private static bool m_isSearchPanelOpen = false;

    [SerializeField]
    private static string m_searchString = string.Empty;

    [SerializeField]
    private static string m_replaceString = string.Empty;

    [SerializeField]
    private static bool m_matchCase = false;

    private static TextTable m_textTable;

    private static bool m_needRefreshLists = true;
    private static ReorderableList m_languageList = null;
    private static ReorderableList m_fieldList = null;
    private static SerializedObject m_serializedObject = null;
    private static SerializedObject m_serializedObjectCopy = null;
    private static GUIStyle textAreaStyle = null;
    private static bool isTextAreaStyleInitialized = false;

    private const string EncodingTypeEditorPrefsKey = "PixelCrushers.EncodingType";
    private const string ToolbarSelectionPrefsKey = "PixelCrushers.TextTableEditor.Toolbar";
    private const string SearchBarPrefsKey = "PixelCrushers.TextTableEditor.SearchBar";
    private const double TimeBetweenUpdates = 10;

    private static bool m_needToUpdateSO;
    private static bool m_needToApplyBeforeUpdateSO;
    private static bool m_isPickingOtherTextTable;
    private static System.DateTime m_lastApply;
    private static int m_changeBarkField=-1;
    private static string m_changeBarkStrField="Input";

    [System.Serializable]
    public class SearchBarSettings
    {
        public bool open = false;
        public string searchString = string.Empty;
        public string replaceString = string.Empty;
        public bool matchCase = false;
    }

    #endregion
    #region Language List

    private static void ResetLanguagesTab()
    {
        m_languageList = null;
        m_languageListScrollPosition = Vector2.zero;
    }

    private static void DrawLanguagesTab(DialogueDatabase database, Actor asset)
    {
        if (m_languageList == null)
        {
            m_serializedObject ??= new SerializedObject(database.CharDialogueTable);
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

    private static void OnDrawLanguageListHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Languages");
    }
    #region Field List

    private static void ResetFieldsTab()
    {
        m_fieldList = null;
        m_fieldListScrollPosition = Vector2.zero;
        m_selectedLanguageIndex = 0;
        m_selectedLanguageID = 0;
    }

    private static void DrawFieldsTab(DialogueDatabase database, Actor asset)
    {
        DrawGrid();
        DrawEntryBox();
        //if (m_isSearchPanelOpen)
        //{
        //    DrawSearchPanel();
        //}
        //else
        //{
        //    DrawEntryBox();
        //}
    }

    private const float MinColumnWidth = 100;

    private static string[] m_languageDropdownList = null;

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
            this.nameControl = "Field" + + index;
            this.valueControl = "Value" + index;
        }
    }
    private static List<CachedFieldInfo> m_fieldCache = new List<CachedFieldInfo>();

    private static void DrawGrid()
    {
        if (m_textTable == null) return;
        var entryBoxHeight = IsAnyFieldSelected() ? (6 * EditorGUIUtility.singleLineHeight) : 0;
        if (m_isSearchPanelOpen) entryBoxHeight += (4 * EditorGUIUtility.singleLineHeight);

        if (m_needRefreshLists || m_fieldList == null || m_languageDropdownList == null)
        {
            m_needRefreshLists = false;
            var por = RebuildProperty(m_serializedObject);

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
    private static SerializedProperty RebuildProperty(SerializedObject sobj)
    {
        sobj.Update();
        m_serializedObjectCopy = new SerializedObject(m_textTable);
        m_copyedMainProp = m_serializedObjectCopy.FindProperty("m_fieldValues");
        m_serializedObjectCopy.Update();
        m_MainProp = sobj.FindProperty("m_fieldValues");
        Debug.Log("!"+m_MainProp.arraySize + "/" + m_copyedMainProp.arraySize);
        for (int index = 0; index < m_copyedMainProp.arraySize; index++)
        {
            var fieldNameProperty = m_copyedMainProp.GetArrayElementAtIndex(index).FindPropertyRelative("m_fieldName");
            Debug.Log(fieldNameProperty.stringValue + "/S");
            if (!fieldNameProperty.stringValue.Contains("[" + actorID + "]."))
            {
                Debug.Log(fieldNameProperty.stringValue+"/D");
                m_copyedMainProp.DeleteArrayElementAtIndex(index);
                index--;
            }
        }
        Debug.Log(m_MainProp.arraySize+"/"+ m_copyedMainProp.arraySize);
        return m_copyedMainProp;
    }
    private static void Refresh()
    {
        m_serializedObject = new SerializedObject(m_textTable);
        m_MainProp= m_serializedObject.FindProperty("m_fieldValues");
        m_needRefreshLists = true;
    }

    private static void Update()
    {
        for (int index = 0; index < m_MainProp.arraySize; index++)
        {
            var fieldValuePropertyMain = m_MainProp.GetArrayElementAtIndex(index);
            var fieldNamePropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_fieldName");
            string mainkey = fieldNamePropertyMain.stringValue;
            Debug.Log(mainkey);
            for (int i = 0; i<m_copyedMainProp.arraySize; i++)
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
    private static void RebuildFieldCache()
    {
        m_fieldCache.Clear();

        var fieldValuesProperty = m_copyedMainProp;
        for (int index = 0; index < fieldValuesProperty.arraySize; index++)
        {
            var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(index);
            var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
            var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
            var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");


            var valueIndex = -1;
            if (!fieldNameProperty.stringValue.Contains("[" + actorID + "]."))
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

    private static void OnDrawFieldListHeader(Rect rect)
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

    private static void OnDrawFieldListElement(Rect rect, int index, bool isActive, bool isFocused)
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
        string[] fieldnamesplit = fieldname.Split('.');
        bool fieldNameVaild = fieldnamesplit.Length > 1;


        if (m_changeBarkField==index)
        {
            GUI.backgroundColor = Color.green;
            m_changeBarkStrField = GUI.TextField(new Rect(rect.x, rect.y + 1, columnWidth-buttonwidth, EditorGUIUtility.singleLineHeight),m_changeBarkStrField);
            GUI.backgroundColor = Color.white;
        }
        else
        {
            //GUI.enabled = false;
            fieldname = (fieldnamesplit.Length > 1) ? fieldnamesplit[1]: "missing";
            GUI.Label(new Rect(rect.x, rect.y + 1, columnWidth - buttonwidth, EditorGUIUtility.singleLineHeight), fieldname);
            //EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, columnWidth-buttonwidth, EditorGUIUtility.singleLineHeight), info.fieldNameProperty, GUIContent.none, false);
            //GUI.enabled = true;
        }
        if (GUI.Button(new Rect(rect.x+ columnWidth- buttonwidth, rect.y + 1, buttonwidth, EditorGUIUtility.singleLineHeight), "C"))
        {
            if(m_changeBarkField == -1 || index != m_changeBarkField)
            {
                m_changeBarkField = index;
                m_changeBarkStrField = fieldnamesplit[1];
            }
            else
            {
                for (int i = 0; i < m_MainProp.arraySize; i++)
                {
                    if(m_MainProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_fieldName").stringValue == info.fieldNameProperty.stringValue)
                    {
                        m_MainProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_fieldName").stringValue = fieldNameVaild ?  $"[{actorID}].{m_changeBarkStrField}": fieldnamesplit[1];
                        m_serializedObject.ApplyModifiedProperties();
                        Refresh();
                    }
                }
                m_changeBarkField = -1;
            }

            Debug.Log("work");
        }
        if (info.fieldValueProperty != null)
        {
            GUI.SetNextControlName(info.valueControl);
            EditorGUI.PropertyField(new Rect(rect.x + rect.width - columnWidth, rect.y + 1, columnWidth, EditorGUIUtility.singleLineHeight), info.fieldValueProperty, GUIContent.none, false);
            var focusedControl = GUI.GetNameOfFocusedControl();
            if (string.Equals(info.nameControl, focusedControl) || string.Equals(info.valueControl, focusedControl))
            {
                m_selectedFieldListElement = index;
                m_fieldList.index = index;
            }
        }
        m_serializedObject.ApplyModifiedProperties();
    }

    private static void OnAddFieldListElement(ReorderableList list)
    {
        m_serializedObject.Update();
        m_textTable.AddField("["+actorID+"].NewField " + m_textTable.nextFieldID);
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
    private static int FindSyncMain(int idx)
    {
        string srcname=m_copyedMainProp.GetArrayElementAtIndex(idx).FindPropertyRelative("m_fieldName").stringValue;
        for (int index = 0; index < m_MainProp.arraySize; index++)
        {
            var fieldValuePropertyMain = m_MainProp.GetArrayElementAtIndex(index);
            var fieldNamePropertyMain = fieldValuePropertyMain.FindPropertyRelative("m_fieldName");
            string mainkey = fieldNamePropertyMain.stringValue;
            if(mainkey == srcname)
            {
                return index;
            }
        }
        return -1;
    }
    private static void OnRemoveFieldListElement(ReorderableList list)
    {
        var fieldID = FindSyncMain(list.index);
        var fieldValuesProperty = m_copyedMainProp;
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

    private static int m_selectedFieldListElement;

    private static void OnSelectFieldListElement(ReorderableList list)
    {
        m_selectedFieldListElement = list.index;
    }

    private static void OnReorderFieldListElement(ReorderableList list)
    {
        // Also reorder keys:
        var fieldKeysProperty = m_serializedObject.FindProperty("m_fieldKeys");
        var value = fieldKeysProperty.GetArrayElementAtIndex(m_selectedFieldListElement).intValue;
        fieldKeysProperty.DeleteArrayElementAtIndex(m_selectedFieldListElement);
        fieldKeysProperty.InsertArrayElementAtIndex(list.index);
        fieldKeysProperty.GetArrayElementAtIndex(list.index).intValue = value;
    }

    private static void CheckMouseEvents()
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

    private static void InsertFieldListElement(object data)
    {
        int index = (int)data;
        m_serializedObject.ApplyModifiedProperties();
        Undo.RecordObject(m_textTable, "Insert Field");
        m_textTable.InsertField(index, "[" + actorID+"]."+m_textTable.nextFieldID);
        EditorUtility.SetDirty(m_textTable);
        m_serializedObject.Update();
        RebuildFieldCache();
        //Repaint();
    }

    private static void DeleteFieldListElement(object data)
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

    private static bool IsAnyFieldSelected()
    {
        return m_fieldList != null && 0 <= m_fieldList.index && m_fieldList.index < m_fieldList.serializedProperty.arraySize;
    }

    private static void DrawEntryBox()
    {
        if (m_needRefreshLists || !IsAnyFieldSelected()) return;
        //var rect = new Rect(2, position.height - 6 * EditorGUIUtility.singleLineHeight, position.width - 4, 6 * EditorGUIUtility.singleLineHeight);
        var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));

        if (m_isSearchPanelOpen)
        {
            var searchPanelHeight = (4 * EditorGUIUtility.singleLineHeight);
            rect = new Rect(rect.x, rect.y - searchPanelHeight, rect.width, rect.height);
        }
        var fieldValuesProperty = m_copyedMainProp;
        var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(m_fieldList.index);
        var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
        var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");
        var valueIndex = -1; 
        var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
        if (!fieldNameProperty.stringValue.Contains("[" + actorID + "]."))
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
    private static void OnDrawLanguageListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
        var languageKeyProperty = languageKeysProperty.GetArrayElementAtIndex(index);
        var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
        var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(index);
        EditorGUI.BeginDisabledGroup(languageValueProperty.intValue == 0);
        EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight), languageKeyProperty, GUIContent.none, false);
        EditorGUI.EndDisabledGroup();
    }

    private static void OnAddLanguageListElement(ReorderableList list)
    {
        m_serializedObject.ApplyModifiedProperties();
        m_textTable.AddLanguage("Language " + m_textTable.nextLanguageID);
        m_serializedObject.Update();
        ResetFieldsTab();
    }

    private static bool OnCanRemoveLanguageListElement(ReorderableList list)
    {
        var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
        var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(list.index);
        return languageValueProperty.intValue > 0;
    }

    private static void OnRemoveLanguageListElement(ReorderableList list)
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

    private static int m_selectedLanguageListIndex = -1;

    private static void OnSelectLanguageListElement(ReorderableList list)
    {
        m_selectedLanguageListIndex = list.index;
    }

    private static void OnReorderLanguageListElement(ReorderableList list)
    {
        // Also reorder values:
        var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
        var value = languageValuesProperty.GetArrayElementAtIndex(m_selectedLanguageListIndex).intValue;
        languageValuesProperty.DeleteArrayElementAtIndex(m_selectedLanguageListIndex);
        languageValuesProperty.InsertArrayElementAtIndex(list.index);
        languageValuesProperty.GetArrayElementAtIndex(list.index).intValue = value;
        ResetFieldsTab();
    }

    #endregion
    #region Search

    private static void OpenSearchPanel()
    {
        m_isSearchPanelOpen = !m_isSearchPanelOpen;
    }

    private static void DrawSearchPanel()
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

    private static void FindNext()
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

    private static void ReplaceCurrent()
    {
        if (!IsAnyFieldSelected()) return;
        var regexOptions = m_matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
        m_fieldCache[m_fieldList.index].fieldNameProperty.stringValue = Regex.Replace(m_fieldCache[m_fieldList.index].fieldNameProperty.stringValue, m_searchString, m_replaceString, regexOptions);
        m_fieldCache[m_fieldList.index].fieldValueProperty.stringValue = Regex.Replace(m_fieldCache[m_fieldList.index].fieldValueProperty.stringValue, m_searchString, m_replaceString, regexOptions);
    }

    private static void ReplaceAll()
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

    static bool m_StatusToggle = false;

    static void ShowDNDStatus(Actor target, bool isTargetDataChanged) 
    {
        void SetIntField(string title,string label)
        {
            Field.SetValue(target.fields, title, EditorGUILayout.IntField(label, Field.LookupInt(target.fields, title)));
        }
        //어카지
        //필드 구성요소를 가져온다
        //빈필드일때 기본 구성요소를 제공한다
        m_StatusToggle = EditorGUILayout.Foldout(m_StatusToggle,"BasicStatus");
        if(isTargetDataChanged)
        {
            StatusInitialize(target);
        }
        if(m_StatusToggle)
        {
            SetIntField(ConstDataTable.ActorStatus.Str, "STR");
            SetIntField(ConstDataTable.ActorStatus.Con, "CON");
            SetIntField(ConstDataTable.ActorStatus.Dex, "DEX");
            SetIntField(ConstDataTable.ActorStatus.Int, "INT");
            SetIntField(ConstDataTable.ActorStatus.Wis, "WIS");
            SetIntField(ConstDataTable.ActorStatus.Cha, "CHA");
        }
    }
    static void StatusInitialize(Actor target) 
    {
        void Init(string fieldname,int value=5)
        {
            if (!Field.FieldExists(target.fields, fieldname))
                Field.SetValue(target.fields, fieldname, value);
        }
        Init(ConstDataTable.ActorStatus.Str);
        Init(ConstDataTable.ActorStatus.Con);
        Init(ConstDataTable.ActorStatus.Dex);
        Init(ConstDataTable.ActorStatus.Int);
        Init(ConstDataTable.ActorStatus.Wis);
        Init(ConstDataTable.ActorStatus.Cha);
    }
}
