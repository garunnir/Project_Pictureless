// ============================================================
// DistUiFont — Dist UI TMP Katuri SDF SSOT (ui-font.mdc)
// ============================================================

using TMPro;
using UnityEngine;

/// <summary>
/// Path SSOT: docs/ui/UI_Scripts.md §Font · InventoryUIHierarchyBuilder.DefaultUIFontPath
/// </summary>
public static class DistUiFont
{
    public const string AssetPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    /// <summary>Resources.Load 보조 키 (빌드용). Font 폴더에 Resources 복제가 있을 때만.</summary>
    public const string ResourcesKey = "Katuri SDF";

    static TMP_FontAsset _cached;
    static bool _warnedMissing;

    public static TMP_FontAsset Get()
    {
        if (_cached != null)
            return _cached;

#if UNITY_EDITOR
        _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
#endif
        if (_cached == null)
            _cached = Resources.Load<TMP_FontAsset>(ResourcesKey);

        if (_cached == null && !_warnedMissing)
        {
            _warnedMissing = true;
            Debug.LogError(
                $"[DistUiFont] Katuri SDF missing. Assign '{AssetPath}' or Resources '{ResourcesKey}'.");
        }

        return _cached;
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = Get();
        if (font != null)
            text.font = font;
    }
}
