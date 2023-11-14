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
    [SerializeField] UIController m_Controller;
    #endregion
    public void Init()
    {
        m_resolutionUpperRect = m_upperWindowRect.rect;
        m_Controller.Init();
    }

    public static void AdjustSize(Rect refMaxRange, RectTransform target, Texture texture)
    {
        AdjustSize(refMaxRange,target,texture,Vector2.one*0.5f);
    }
    public static void AdjustSize(Rect refMaxRange, RectTransform target, Texture texture,Vector2 anchor)
    {
        target.sizeDelta = Vector2.zero;
        target.localPosition = Vector2.zero;
        float heightfactor = (float)texture.height / texture.width;
        float rectHeightFactor = (float)refMaxRange.height / refMaxRange.width;
        if (rectHeightFactor > heightfactor)
        {
            float adjust = target.rect.width * heightfactor;//height pixel
            var calY = adjust - target.rect.height;
            target.sizeDelta = new Vector2(0, calY);
            target.localPosition -= Vector3.up*calY *(anchor.y-0.5f);
        }
        else
        {
            float adjust = target.rect.height / heightfactor;//height pixel
            var calX = adjust - target.rect.width;
            target.sizeDelta = new Vector2(calX, 0);
            target.localPosition -= Vector3.right * calX * (anchor.x - 0.5f);
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
    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);

        float newRectX = transform.position.x + (Screen.width / 2) - (size.x * (1 - transform.pivot.x));
        float newRectY = Mathf.Abs(transform.position.y - (Screen.height / 2)) - (size.y * (1 - transform.pivot.y));

        return new Rect(newRectX, newRectY, size.x, size.y);
    }
}
