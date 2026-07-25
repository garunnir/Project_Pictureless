// ============================================================
// CombatActionUIFactory — 전투 액션 HUD 계층 생성 (Setup용)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CombatActionUIFactory
{
    public const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    public static readonly Vector2 PanelSize = new(320f, 36f);
    public static readonly Vector2 AnchoredPosition = new(0f, -56f);
    public const int FontSize = 14;

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.75f);

    public static UICombatActionPanel CreateDisplayRoot()
    {
        GameObject root = CreateRect("Hud_CombatAction", null, PanelColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = AnchoredPosition;
        rootRect.sizeDelta = PanelSize;

        TMP_Text actionText = CreateTmp(
            "Txt_CombatAction",
            root.transform,
            FontSize,
            TextAlignmentOptions.Center);
        Stretch(actionText.rectTransform, 8f, 8f, 4f, 4f);
        actionText.text = CombatActionDisplayFormat.Format(
            WeaponAction.Swing,
            WeaponActionMask.None,
            string.Empty);

        UICombatActionPanel panel = root.AddComponent<UICombatActionPanel>();
        panel.Wire(actionText);
        return panel;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static TMP_Text CreateTmp(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = LoadDefaultFont();
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static TMP_FontAsset LoadDefaultFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultUIFontPath);
#else
        return TMP_Settings.defaultFontAsset;
#endif
    }
}
