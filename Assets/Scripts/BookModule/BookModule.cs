using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BookModule : MonoBehaviour
{
    // Start is called before the first frame update
    public GUIStyle textStyle;
    public RectTransform rect;
    public TMP_Text tmp;

    private void Start()
    {
        print("d");


        string text = "Your long text here...";
        //tmp.font=textStyle.font; 
        tmp.fontSize=textStyle.fontSize;
        //int maxCharacterCount = CalculateMaxCharacterCount(text, textStyle, rect);

        //GUILayout.BeginArea(rect);
        //GUILayout.Label("Max Characters: " + maxCharacterCount, textStyle);
        //GUILayout.EndArea();
        ////
        Rect fittingRect = CalculateFittingRect(text, textStyle, new Rect(10, 10, 200, 200));
        rect.offsetMax = new Vector2( fittingRect.x+fittingRect.width,fittingRect.y+fittingRect.height);
        rect.offsetMin = new Vector2(fittingRect.x,fittingRect.y);
        //GUI.Label(fittingRect, text, textStyle);
    }

    private int CalculateMaxCharacterCount(string text, GUIStyle style, Rect rect)
    {
        GUIContent content = new GUIContent(text);
        float textHeight = style.CalcHeight(content, rect.width);
        int maxCharacterCount = Mathf.FloorToInt(text.Length * (rect.height / textHeight));
        return maxCharacterCount;
    }

    private Rect CalculateFittingRect(string text, GUIStyle style, Rect baseRect)
    {
        GUIContent content = new GUIContent(text);
        float textHeight = style.CalcHeight(content, baseRect.width);
        int lines = Mathf.CeilToInt(textHeight / style.lineHeight);

        Rect fittingRect = new Rect(baseRect.x, baseRect.y, baseRect.width, style.lineHeight * lines);
        return fittingRect;
    }
}
