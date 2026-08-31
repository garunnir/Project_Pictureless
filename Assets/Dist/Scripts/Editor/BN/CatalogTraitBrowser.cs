// ============================================================
// CatalogTraitBrowser — Data Definitions Characters/Trait Icons Odin 브라우저
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[HideReferenceObjectPicker]
public sealed class CatalogTraitBrowser
{
    const float BrowserMinHeight = 420f;
    const float ListPaneWidth = 280f;

    int _selectedIndex;
    Vector2 _listScroll;
    Vector2 _detailScroll;

    bool AlwaysShow => true;

    [Title("Trait Icons", "TraitIds · TraitIconCatalog", TitleAlignments.Split)]
    [GUIColor(0.72f, 0.45f, 0.95f)]
    [InfoBox(
        "특성 ID는 TraitIds SSOT입니다. 아이콘은 TraitIconCatalog SO에만 할당합니다.\n"
        + "표시 이름은 Loc 키 PlayerStatus.Trait.{id} 입니다.",
        SdfIconType.InfoCircleFill,
        nameof(AlwaysShow))]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true)]
    [PropertyOrder(-5)]
    string Badge =>
        "<color=#cc99ff><b>TRAITS</b></color>  ·  TraitIconCatalog  ·  Unity asset (Ctrl+S)";

    CatalogDataSession Session => CatalogDataSession.Instance;

    [OnInspectorGUI, PropertyOrder(0)]
    void DrawBrowser()
    {
        TraitIconCatalog catalog = Session.EnsureTraitIconCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                $"카탈로그를 만들 수 없습니다: {TraitIconCatalog.DefaultAssetPath}",
                MessageType.Error);
            return;
        }

        string[] ids = TraitIds.All;
        if (_selectedIndex < 0 || _selectedIndex >= ids.Length)
            _selectedIndex = 0;

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(BrowserMinHeight), GUILayout.ExpandHeight(true));
        DrawList(ids);
        DrawDetail(ids, catalog);
        EditorGUILayout.EndHorizontal();
    }

    void DrawList(string[] ids)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(ListPaneWidth));
        EditorGUILayout.LabelField("Traits", EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            bool selected = i == _selectedIndex;
            string label = FormatListLabel(id);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selected, label, "Button"))
                _selectedIndex = i;
            if (GUILayout.Button("Copy", GUILayout.Width(48)))
                EditorGUIUtility.systemCopyBuffer = id;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawDetail(string[] ids, TraitIconCatalog catalog)
    {
        EditorGUILayout.BeginVertical();
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        string traitId = ids[_selectedIndex];
        CatalogBrowserFields.ReadFieldWithCopy("Trait ID", traitId);
        CatalogBrowserFields.ReadField("Display Name", Loc.Get(TraitIds.DisplayLocKey(traitId)));
        CatalogBrowserFields.ReadFieldWithCopy("Loc Key", TraitIds.DisplayLocKey(traitId));

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "아이콘은 TraitIds 코드/CharacterDefinition이 아닌 TraitIconCatalog에만 할당합니다.",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        Sprite assigned = catalog.GetAssignedIcon(traitId);
        Sprite next = (Sprite)EditorGUILayout.ObjectField("Icon", assigned, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            catalog.SetIcon(traitId, next);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            TraitVisualPresenter.InvalidateCache();
            TraitVisualPresenter.BindCatalog(catalog);
        }

        Sprite resolved = TraitVisualPresenter.GetDisplayIcon(traitId);
        if (resolved != null)
        {
            Rect preview = GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f));
            CatalogBrowserFields.DrawSpritePreview(preview, resolved);
            if (assigned == null && resolved != catalog.DefaultIcon)
                EditorGUILayout.LabelField("Resolved from default icon", EditorStyles.miniLabel);
        }
        else
            EditorGUILayout.LabelField("(no icon / default missing)", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        Sprite defaultIcon = catalog.DefaultIcon;
        Sprite nextDefault = (Sprite)EditorGUILayout.ObjectField(
            "Default Icon",
            defaultIcon,
            typeof(Sprite),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            catalog.SetDefaultIcon(nextDefault);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            TraitVisualPresenter.InvalidateCache();
            TraitVisualPresenter.BindCatalog(catalog);
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Select Catalog Asset", GUILayout.Width(160)))
            Selection.activeObject = catalog;

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    static string FormatListLabel(string traitId)
    {
        string name = Loc.Get(TraitIds.DisplayLocKey(traitId));
        if (string.IsNullOrEmpty(name) || name.StartsWith("[Missing:", System.StringComparison.Ordinal))
            return traitId;
        return $"{name}  ({traitId})";
    }
}
