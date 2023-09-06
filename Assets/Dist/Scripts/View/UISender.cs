using Garunnir;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISender : MonoBehaviour
{
    UIModel mother;   
    public enum Mode
    {
        inputfield, dropdown
    }
    public Mode mode;
    public TMP_Text title;
    public Form0 form0;
    public Form form;
    public TMP_InputField field;
    public TMP_Dropdown dropdown;
    public TMP_Text GetText() { return title; }
    public TMP_InputField GetInput() { return field; }
    public string GetTitle() => title.text;
    public string GetField() => field.text;
    public string[] dropList;
    public System.Enum Enum;
    private void Start()
    {
    }
    private void OnEnable()
    {
        title.text = UILocalizationManager.instance.textTable.GetField(GameManager.Instance.GetFormDic(form0, form)).texts[TextTable.currentLanguageID];
        mother = FindAnyObjectByType<UIModel>();
        if(mother != null)
        mother.field += FieldReturn;
        //보낼 대상에 고정한다.
    }
    void FieldReturn(object o, UIEventArgs e)
    {
        switch (mode)
        {
            case Mode.inputfield:
                //텍스트 테이블로 번역
                e.field += Utillity.TupleSigleConv(GetTitle(), true, GetField());
                break;
            case Mode.dropdown:
                e.field += Utillity.TupleSigleConv(GetTitle(), true, dropdown.options[dropdown.value]);
                break;
        }
    }
    //값을 요청하면 보낸다. 
}
