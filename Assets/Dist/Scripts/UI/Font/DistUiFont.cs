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
    public const string AssetPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    /// <summary>Resources.Load 보조 키 (빌드용). Font 폴더에 Resources 복제가 있을 때만.</summary>
    public const string ResourcesKey = "Katuri SDF";

    static TMP_FontAsset _katuriFallback;
    static bool _warnedMissingKaturi;

    public static TMP_FontAsset Get()
    {
        LocalizationBundle bundle = LocalizationBundle.Get();
        if (bundle != null)
        {
            TMP_FontAsset fromBundle = bundle.GetActiveFont();
            if (fromBundle != null)
                return fromBundle;
        }

        return GetKaturiFallback();
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

        return GetKaturiFallback();
    }

    static TMP_FontAsset GetKaturiFallback()
    {
        if (_katuriFallback != null)
            return _katuriFallback;

#if UNITY_EDITOR
        _katuriFallback = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
#endif
        if (_katuriFallback == null)
            _katuriFallback = Resources.Load<TMP_FontAsset>(ResourcesKey);

        if (_katuriFallback == null && !_warnedMissingKaturi)
        {
            _warnedMissingKaturi = true;
            Debug.LogError(
                $"[DistUiFont] Katuri SDF missing. Assign LocalizationBundle fonts or '{AssetPath}'.");
        }

        return _katuriFallback;
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
