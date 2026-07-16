// ============================================================
// Loc — 키 기반 표시 문구 파사드 (소스 미등록 시 fallback)
// ============================================================

using UnityEngine;

public static class Loc
{
    static ILocalizationSource _source;
    static bool _initialized;

    /// <summary>런타임/테스트용 소스 교체. null이면 다음 Get에서 Resources 재로드.</summary>
    public static void SetSource(ILocalizationSource source)
    {
        _source = source;
        _initialized = source != null;
    }

    public static string Get(string key, string fallback)
    {
        EnsureInitialized();

        if (_source != null &&
            _source.TryGet(key, out string text) &&
            !string.IsNullOrEmpty(text))
            return text;

        return fallback;
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        string template = Get(key, fallback);
        if (args == null || args.Length == 0)
            return template;
        return string.Format(template, args);
    }

    static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        if (_source != null)
            return;

        LocalizationTable table = Resources.Load<LocalizationTable>(ConstDataTable.AssetPath.LocalizeTable.UI);
        if (table != null)
            _source = table;
    }
}
