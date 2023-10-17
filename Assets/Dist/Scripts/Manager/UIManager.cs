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
}
