// ============================================================
// DistUiFont — Dist UI TMP 폰트 (Language/LocalizationBundle SSOT)
// ============================================================

using TMPro;
using UnityEngine;

/// <summary>
/// Path SSOT: docs/ui/UI_Scripts.md §Font · LocalizationBundle language fonts
/// </summary>
public static class DistUiFont
{
    public const string AssetPath = "Assets/Dist/Scripts/UI/Font/Galmuri-v2.40.3/Galmuri7 SDF.asset";

    /// <summary>Resources.Load 보조 키 (빌드용). Font 폴더에 Resources 복제가 있을 때만. Bundle이 런타임 SSOT.</summary>
    public const string ResourcesKey = "Galmuri7 SDF";

    static TMP_FontAsset _defaultFallback;
    static bool _warnedMissingDefault;

    public static TMP_FontAsset Get()
    {
        LocalizationBundle bundle = LocalizationBundle.Get();
        if (bundle != null)
        {
            TMP_FontAsset fromBundle = bundle.GetActiveFont();
            if (fromBundle != null)
                return fromBundle;
        }

        return GetDefaultFallback();
    }

    public static TMP_FontAsset GetFor(DisplayLanguage language)
    {
        LocalizationBundle bundle = LocalizationBundle.Get();
        if (bundle != null)
        {
            TMP_FontAsset fromBundle = bundle.GetFont(language);
            if (fromBundle != null)
                return fromBundle;
        }

        return GetDefaultFallback();
    }

    static TMP_FontAsset GetDefaultFallback()
    {
        if (_defaultFallback != null)
            return _defaultFallback;

#if UNITY_EDITOR
        _defaultFallback = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
#endif
        if (_defaultFallback == null)
            _defaultFallback = Resources.Load<TMP_FontAsset>(ResourcesKey);

        if (_defaultFallback == null && !_warnedMissingDefault)
        {
            _warnedMissingDefault = true;
            Debug.LogError(
                $"[DistUiFont] Galmuri7 SDF missing. Assign LocalizationBundle fonts or '{AssetPath}'.");
        }

        return _defaultFallback;
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
