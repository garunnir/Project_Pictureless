using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIVerticalView : UI
{
    public GameObject prf_component;
    public GameObject prf_text;
    public GameObject prf_img;
    public GameObject prf_bar;
    public VerticalLayoutGroup target;

    public override IEnumerator AddHideAct()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator AddShowAct()
    {
        throw new System.NotImplementedException();
    }
    internal GameObject ShowBar(string title,float fill,out float ratio)
    {
        GameObject obj = null;
        obj = Instantiate(prf_bar);
        ratio=SetOBJ(obj, 10);
        obj.GetComponentsInChildren<Image>().Last().fillAmount = fill;
        TMP_Text text1 = obj.GetComponentInChildren<TMP_Text>();
        text1.text = UILocalizationManager.instance.textTable.GetFieldText(title);
        text1.enabled = true;
        text1.font = UILocalizationManager.instance.localizedFonts.GetTextMeshProFont(Localization.language);
        return obj;
    }

    internal GameObject ShowImg(Texture2D tex, out float ratio)
    {
        var obj = Instantiate(prf_img);

        ratio=SetOBJ(obj, 1);
        if (tex != null)
        {
            var rawimg = obj.GetComponentInChildren<RawImage>();
            rawimg.texture = tex;
            Garunnir.UIUtility.AdjustSize(obj.GetComponent<RectTransform>().rect, rawimg.rectTransform, tex);
        }
        return obj;
    }

    internal GameObject ShowText(string title,string value, out float ratio)
    {
        var obj = Instantiate(prf_text);
        ratio = SetOBJ(obj, 10);
        TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
        text.text = UILocalizationManager.instance.textTable.GetFieldText(title) + ": " + value;
        text.font = UILocalizationManager.instance.localizedFonts.GetTextMeshProFont(Localization.language);
        return obj;
    }
    private float SetOBJ(GameObject obj, float ratio)
    {
        obj.transform.SetParent(target.transform);
        var dest= obj.GetComponent<RectTransform>();
        dest.offsetMax = new Vector2(rect.rect.width, rect.rect.width / ratio);
        dest.offsetMin = new Vector2(0,0);
        return dest.offsetMax.y;
        //dest.localScale = src.localScale;
        //dest.sizeDelta = src.sizeDelta;
        //dest.anchorMin=src.anchorMin;
        //dest.anchorMax=src.anchorMax;
        //dest.localPosition = src.localPosition;
        //dest.localRotation = src.localRotation;
        //dest.ForceUpdateRectTransforms();
        //dest.offsetMin=src.offsetMin;
        //dest.offsetMax=src.offsetMax;

    }
    internal void ClearComponents()
    {
        for (int i = 0; i < target.transform.childCount; i++)
        {
            Destroy(target.transform.GetChild(i).gameObject);
        }
    }
}
