using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

namespace Garunnir
{
    [CustomEditor(typeof(UISender), true)]
    public class UISenderEditor : Editor
    {
        GenericMenu mAdvancedDropdownMenu;
        GenericMenu mform0;
        GenericMenu mform;
        UISender storer;
        GUIContent content;
        private ReorderableList list;
        private void Awake()
        {
            storer = target as UISender;
        }
        protected virtual void OnEnable()
        {
            content = new GUIContent();
            //mAdvancedDropdownMenu.AddItem(new GUIContent(PlasticLocalization.GetString(PlasticLocalization.Name.UndoUnchangedButton)),false, () => { });

            GenericMenu ShowDrop<T>(T input) where T : Enum
            {
                GenericMenu tmp = new GenericMenu();
                string[] forms = System.Enum.GetNames(typeof(T));
                foreach (var item in forms)
                {
                    tmp.AddItem(new GUIContent(item), false, () =>
                    {
                        SetForm<T>(item);
                        EditorUtility.SetDirty(target);
                    });
                }
                return tmp;
            }
            mAdvancedDropdownMenu = ShowDrop(storer.mode);
            mform0 = ShowDrop(storer.form0);
            mform = ShowDrop(storer.form);
            
            var prop = serializedObject.FindProperty("dropList");
            list = new ReorderableList(serializedObject, prop);
            list.drawElementCallback = (rect, index, isActive, isFocused) => {
                var element = prop.GetArrayElementAtIndex(index);
                rect.height -= 4;
                rect.y += 2;
                EditorGUI.PropertyField(rect, element);
            };
            list.onAddCallback = (ReorderableList list) => {
                //요소를 추가
                prop.arraySize++;

                //마지막 요소를 선택상태로 만들기
                list.index = prop.arraySize - 1;

                //추가한 요소에 문자열을 추가하기(배열이 string[]일 것을 전제로 합니다）
                var element = prop.GetArrayElementAtIndex(list.index);
                element.stringValue = "New String " + list.index;
            };
            list.onChangedCallback = (ReorderableList list) =>
            {
                storer.dropdown.options.Clear();
                for (int i = 0; i < list.count; i++)
                {
                    TMPro.TMP_Dropdown.OptionData data = new TMPro.TMP_Dropdown.OptionData();
                    data.text = prop.GetArrayElementAtIndex(i).stringValue;
                    storer.dropdown.options.Add(data);
                }
            };

        }
        //private void OnDisable()
        //{
        //    storer = target as UISender;
        //    var prop = serializedObject.FindProperty("dropList");
        //    storer.dropdown.options.Clear();
        //    for (int i = 0; i < list.count; i++)
        //    {
        //        TMPro.TMP_Dropdown.OptionData data = new TMPro.TMP_Dropdown.OptionData();
        //        data.text = prop.GetArrayElementAtIndex(i).stringValue;
        //        storer.dropdown.options.Add(data);
        //    }
        //}
        public void SetForm<T>(string form)
        {
            if (typeof(T).Name == typeof(Form0).Name)
            {
                storer.form0 = (Form0)System.Enum.Parse(typeof(Form0), form);
            }
            else if (typeof(T).Name == typeof(Form).Name)
            {
                storer.form = (Form)System.Enum.Parse(typeof(Form), form);
            }
            else if (typeof(T).Name == typeof(UISender.Mode).Name)
            {
                storer.mode = (UISender.Mode)System.Enum.Parse(typeof(UISender.Mode), form);
            }
        }
        public override void OnInspectorGUI()
        {
            Draw();
        }
        protected virtual void Draw()
        {
            var dropDownRect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown);
            content.text = storer.form0.ToString();
            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, EditorStyles.toolbarDropDown)) mform0.DropDown(dropDownRect);
            content.text = storer.form.ToString();
            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, EditorStyles.toolbarDropDown)) mform.DropDown(dropDownRect);
            content.text = storer.mode.ToString();
            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, EditorStyles.toolbarDropDown)) mAdvancedDropdownMenu.DropDown(dropDownRect);
            storer.title = EditorGUILayout.ObjectField("title", storer.title, typeof(TMPro.TMP_Text), true) as TMPro.TMP_Text;
            switch (storer.mode)
            {
                case UISender.Mode.dropdown:
                    serializedObject.Update();
                    storer.dropdown = EditorGUILayout.ObjectField("dropdown", storer.dropdown, typeof(TMPro.TMP_Dropdown), true) as TMPro.TMP_Dropdown;
                    list.DoLayoutList();
                    serializedObject.ApplyModifiedProperties();
                    break;
                case UISender.Mode.inputfield:
                    storer.field = EditorGUILayout.ObjectField("inputfield", storer.field, typeof(TMPro.TMP_InputField), true) as TMPro.TMP_InputField;
                    break;
            }
        }

    }
}
