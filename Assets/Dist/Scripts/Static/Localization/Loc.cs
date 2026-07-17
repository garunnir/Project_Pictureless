// ============================================================
// Loc — 키 기반 표시 문구 파사드 (문구 SSOT는 LocalizationTable)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public static class Loc
{
    const string EmptyKeyMarker = "[Missing: EmptyKey]";
    const string MissingKeyMarkerFormat = "[Missing: {0}]";

    static ILocalizationSource _source;
    static bool _initialized;
    static readonly HashSet<string> ReportedMissingKeys = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>런타임/테스트용 소스 교체. null이면 다음 Get에서 Resources 재로드.</summary>
    public static void SetSource(ILocalizationSource source)
    {
        _source = source;
        _initialized = source != null;
        ReportedMissingKeys.Clear();
    }

    public static string Get(string key)
    {
        if (TryGet(key, out string text))
            return text;

        return ReportMissingKey(key);
    }

    public static bool TryGet(string key, out string text)
    {
        EnsureInitialized();

        if (_source != null &&
            _source.TryGet(key, out text) &&
            !string.IsNullOrEmpty(text))
            return true;

        text = null;
        return false;
    }

    public static string Format(string key, params object[] args)
    {
        string template = Get(key);
        if (args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException exception)
        {
            Debug.LogError($"[Loc] Invalid format for key '{key}': {exception.Message}");
            return ReportMissingKey(key);
        }
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

    static string ReportMissingKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            if (ReportedMissingKeys.Add(string.Empty))
                Debug.LogError("[Loc] Empty localization key.");
            return EmptyKeyMarker;
        }

        if (ReportedMissingKeys.Add(key))
            Debug.LogError($"[Loc] Missing localization key: {key}");

        return string.Format(MissingKeyMarkerFormat, key);
    }
}
