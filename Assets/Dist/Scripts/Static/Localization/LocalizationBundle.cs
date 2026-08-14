// ============================================================
// LocalizationBundle — 활성 표시 언어 + 언어별 TMP 폰트 SSOT
// ============================================================

using System;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationBundle", menuName = "Dist/Localization Bundle")]
public sealed class LocalizationBundle : ScriptableObject
{
    public const string AssetPath = "Assets/Dist/Resources/Localization/LocalizationBundle.asset";
    public const string ResourcesLoadName = "Localization/LocalizationBundle";

    [SerializeField] DisplayLanguage _activeLanguage = DisplayLanguage.Ko;
    [SerializeField] TMP_FontAsset _fontEn;
    [SerializeField] TMP_FontAsset _fontKo;
    [SerializeField] TMP_FontAsset _fontJa;

    static LocalizationBundle _cached;
    static bool _warnedMissing;

    public DisplayLanguage ActiveLanguage
    {
        get => _activeLanguage;
        set => _activeLanguage = value;
    }

    public string ActiveLanguageCode => DisplayLanguageCodes.ToCode(_activeLanguage);

    public static LocalizationBundle Get()
    {
        if (_cached != null)
            return _cached;

#if UNITY_EDITOR
        _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationBundle>(AssetPath);
#endif
        if (_cached == null)
            _cached = Resources.Load<LocalizationBundle>(ResourcesLoadName);

        if (_cached == null && !_warnedMissing)
        {
            _warnedMissing = true;
            Debug.LogError(
                $"[LocalizationBundle] Missing. Create via menu Dist/Localization Bundle at '{AssetPath}'.");
        }

        return _cached;
    }

    public static void ClearCache()
    {
        _cached = null;
        _warnedMissing = false;
    }

    public TMP_FontAsset GetFont(DisplayLanguage language)
    {
        TMP_FontAsset mapped = language switch
        {
            DisplayLanguage.En => _fontEn,
            DisplayLanguage.Ja => _fontJa,
            _ => _fontKo,
        };

        if (mapped != null)
            return mapped;

        if (language == DisplayLanguage.Ja && _fontKo != null)
        {
            Debug.LogWarning(
                "[LocalizationBundle] ja font unset; falling back to ko (Galmuri7). Glyphs may tofu.");
            return _fontKo;
        }

        return _fontKo != null ? _fontKo : _fontEn;
    }

    public TMP_FontAsset GetActiveFont() => GetFont(_activeLanguage);

#if UNITY_EDITOR
    public void EditorSetFont(DisplayLanguage language, TMP_FontAsset font)
    {
        switch (language)
        {
            case DisplayLanguage.En:
                _fontEn = font;
                break;
            case DisplayLanguage.Ja:
                _fontJa = font;
                break;
            default:
                _fontKo = font;
                break;
        }
    }

    public TMP_FontAsset EditorGetFont(DisplayLanguage language) => language switch
    {
        DisplayLanguage.En => _fontEn,
        DisplayLanguage.Ja => _fontJa,
        _ => _fontKo,
    };
#endif
}
