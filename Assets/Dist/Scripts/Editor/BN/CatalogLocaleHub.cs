// ============================================================
// CatalogLocaleHub — Catalog/Item Names (Odin)
// ============================================================

using System.IO;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[HideReferenceObjectPicker]
public sealed class CatalogLocaleHub
{
    bool AlwaysShow => true;
    string OverlayPath => ItemNameTable.GetGameOverlayPath() ?? "—";

    [Title("Item Names", "Catalog locale overlay", TitleAlignments.Split)]
    [GUIColor(1f, 0.85f, 0.35f)]
    [InfoBox(
        "표시 이름·설명은 ItemNameTable (GameData/item_names.json).\n"
        + "Items 상세에서 Name/Description 편집. UI_ko chrome과는 별개.\n"
        + "언어·TMP 폰트는 LocalizationBundle.",
        SdfIconType.Translate,
        nameof(AlwaysShow))]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true)]
    [PropertyOrder(-5)]
    string Badge =>
        "<color=#ffdd66><b>LOCALE</b></color>  ·  overlay  ·  "
        + "<color=#88ccff>Loc Bundle</color>";

    [ShowInInspector, ReadOnly, LabelText("Overlay", SdfIconType.FileEarmarkText)]
    [PropertyOrder(0)]
    string OverlayDisplay => OverlayPath;

    [Button(SdfIconType.PinMapFill, "Ping Loc Bundle")]
    [GUIColor(0.45f, 0.75f, 1f)]
    [PropertyOrder(1)]
    void PingLocBundle() => CatalogDataSession.Instance.PingLocalizationBundle();

    [Button(SdfIconType.Folder2Open, "Reveal Overlay")]
    [GUIColor(0.55f, 0.9f, 0.55f)]
    [PropertyOrder(2)]
    void RevealOverlay()
    {
        string overlay = ItemNameTable.GetGameOverlayPath();
        if (string.IsNullOrEmpty(overlay))
            return;
        string full = Path.GetFullPath(overlay);
        if (File.Exists(full))
            EditorUtility.RevealInFinder(full);
        else
            EditorUtility.DisplayDialog("Missing", $"File not found:\n{full}", "OK");
    }
}
