using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    Rect m_resolutionUpperRect;
    public Rect GetUpperRect() => m_resolutionUpperRect;
    [SerializeField] RectTransform m_upperWindowRect;
    public RectTransform GetUpperRT() => m_upperWindowRect;
    #region view
    [SerializeField] RawImage m_Background;
    public void SetBackground(RawImage raw) => m_Background = raw;
    public RawImage GetBackground() => m_Background;

    #endregion
    private void Awake()
    {
        Init();
    }
    void Init()
    {
        m_resolutionUpperRect = m_upperWindowRect.rect;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public static void AdjustSize(Rect refMaxRange, RectTransform target, Texture texture)
    {
        target.sizeDelta = Vector2.zero;
        float heightfactor = (float)texture.height / texture.width;
        float rectHeightFactor = (float)refMaxRange.height / refMaxRange.width;
        if (rectHeightFactor > heightfactor)
        {
            float adjust = target.rect.width * heightfactor;//height pixel
            target.sizeDelta = new Vector2(0, adjust - target.rect.height);
        }
        else
        {
            float adjust = target.rect.height / heightfactor;//height pixel
            target.sizeDelta = new Vector2(adjust - target.rect.width, 0);
        }
    }
    public static void CopyValues(RectTransform target, RectTransform source)
    {
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.rotation = source.rotation;
        target.localScale = source.localScale;
    }
    public static void CopyValuesCover(RectTransform target, RectTransform source)
    {
        ////target.sizeDelta = source.sizeDelta;
        //target.anchorMax = new(0.5f, 0.5f);
        //target.anchorMin = new(0.5f, 0.5f);
        //target.pivot = new(0.5f, 0.5f);
        ////target.ForceUpdateRectTransforms();
        //target.offsetMin = new((target.anchorMin.x - 1) * source.rect.width, (target.anchorMin.y - 1) * source.rect.height);
        //target.offsetMax = new((1 - target.anchorMax.x) * source.rect.width, (1 - target.anchorMax.y) * source.rect.height);
        //target.position = source.position;
        //target.localScale = source.localScale;

        //source.anchoredPosition;
        Transform parent = target.parent;
        target.SetParent(source.parent);
        target.ForceUpdateRectTransforms();
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.rotation = source.rotation;
        target.localScale = source.localScale;
        target.parent = parent;
    }
    public static void Cover(RectTransform target, RectTransform source)
    {

    }
}
