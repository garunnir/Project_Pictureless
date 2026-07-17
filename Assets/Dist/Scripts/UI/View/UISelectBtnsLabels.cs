// ============================================================
// UISelectBtnsLabels — 선택 버튼 팝업 표시 문구 SSOT
// ============================================================

public static class UISelectBtnsLabels
{
    const string KeyObjectNamePrefix = "UI.SelectBtn.ObjectNamePrefix";

    public static string FormatObjectName(string selection) =>
        Loc.Format(KeyObjectNamePrefix, selection ?? string.Empty);

    public static string GetSelectionLabel(string selection)
    {
        if (string.IsNullOrEmpty(selection))
            return string.Empty;

        return Loc.TryGet(selection, out string localizedSelection)
            ? localizedSelection
            : selection;
    }
}
