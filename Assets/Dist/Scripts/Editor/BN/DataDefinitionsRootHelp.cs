// ============================================================
// DataDefinitionsRootHelp — 대분류 루트 선택 시 Odin Help
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;

[HideReferenceObjectPicker]
public sealed class DataDefinitionsRootHelp
{
    readonly string _title;
    readonly string _body;
    readonly Color _tint;

    public DataDefinitionsRootHelp(string title, string body)
        : this(title, body, new Color(0.45f, 0.55f, 0.7f))
    {
    }

    public DataDefinitionsRootHelp(string title, string body, Color tint)
    {
        _title = title;
        _body = body;
        _tint = tint;
    }

    string TitleText => _title;
    string SubtitleText => "Data Definitions · 대분류";
    string BodyText => _body;
    Color TitleColor => _tint;
    bool AlwaysShow => true;

    [Title("$TitleText", "$SubtitleText", TitleAlignments.Split)]
    [GUIColor(nameof(TitleColor))]
    [InfoBox("$BodyText", SdfIconType.InfoCircleFill, nameof(AlwaysShow))]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true)]
    [PropertyOrder(-10)]
    string Hint =>
        "<color=#cccccc>← 왼쪽 트리에서 </color>"
        + "<color=#ffffff><b>하위 leaf</b></color>"
        + "<color=#cccccc> 를 고르면 편집면이 열립니다.</color>";
}
