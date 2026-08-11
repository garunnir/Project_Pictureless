// ============================================================
// DisplayLanguage — Dist 표시 언어 코드 (로케일 번들 SSOT)
// ============================================================

public enum DisplayLanguage
{
    En = 0,
    Ko = 1,
    Ja = 2,
}

public static class DisplayLanguageCodes
{
    public const string En = "en";
    public const string Ko = "ko";
    public const string Ja = "ja";

    public static string ToCode(DisplayLanguage language)
    {
        switch (language)
        {
            case DisplayLanguage.En:
                return En;
            case DisplayLanguage.Ja:
                return Ja;
            default:
                return Ko;
        }
    }

    public static DisplayLanguage FromCode(string code)
    {
        if (string.Equals(code, En, System.StringComparison.OrdinalIgnoreCase))
            return DisplayLanguage.En;
        if (string.Equals(code, Ja, System.StringComparison.OrdinalIgnoreCase))
            return DisplayLanguage.Ja;
        return DisplayLanguage.Ko;
    }
}
