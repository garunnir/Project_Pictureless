// ============================================================
// CatalogItemBrowser — Data Definitions Catalog/Items Odin 브라우저
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[HideReferenceObjectPicker]
public sealed class CatalogItemBrowser
{
    static readonly string[] ItemTypes =
    {
        "", "GENERIC", "COMESTIBLE", "ARMOR", "TOOL", "GUN",
        "AMMO", "BOOK", "MAGAZINE", "GUNMOD", "TOOL_ARMOR"
    };

    static readonly string[] ItemTypeLabels =
    {
        "All", "Generic", "Comestible", "Armor", "Tool", "Gun",
        "Ammo", "Book", "Magazine", "Gunmod", "ToolArmor"
    };

    const int ListPageSize = 200;
    const float ListPaneWidth = 400f;
    const float BrowserMinHeight = 520f;

    readonly CatalogSource _source;
    readonly List<ItemData> _filtered = new List<ItemData>();

    string _searchText = "";
    int _categoryIndex;
    string _categoryFilter = "";
    int _selectedIndex = -1;
    Vector2 _listScroll;
    Vector2 _detailScroll;
    string _lastSearch = "\0";
    string _lastCategory = "\0";
    DisplayLanguage _lastLanguage = (DisplayLanguage)(-1);

    bool _foldIdentity = true;
    bool _foldGameDetail;
    bool _foldPresentation;
    bool _foldIcon;
    bool _foldRelations;

    public CatalogItemBrowser(CatalogSource source)
    {
        _source = source;
    }

    public CatalogSource Source => _source;
    bool IsCustom => _source == CatalogSource.Custom;

    CatalogDataSession Session => CatalogDataSession.Instance;

    string BrowserTitle => IsCustom ? "Items · Custom" : "Items · BN Reference";
    string BrowserSubtitle => IsCustom ? "editable GameData JSON" : "read-only BN Reference";

    Color HeaderTint => IsCustom
        ? new Color(0.45f, 0.95f, 0.55f)
        : new Color(0.45f, 0.7f, 1f);

    bool AlwaysShow => true;
    string SourceHelp => IsCustom
        ? "Custom 항목만 편집·삭제·추가됩니다. Save Changes로 items.json에 씁니다."
        : "BN은 읽기 전용입니다. Copy to Custom으로 옮긴 뒤 Catalog/Items/Custom에서 편집하세요.";

    [Title("$BrowserTitle", "$BrowserSubtitle", TitleAlignments.Split)]
    [GUIColor(nameof(HeaderTint))]
    [InfoBox("$SourceHelp", SdfIconType.InfoCircleFill, nameof(AlwaysShow))]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true)]
    [PropertyOrder(-20)]
    string SourceBadge =>
        IsCustom
            ? "<color=#66ff99><b>CUSTOM</b></color>  ·  editable  ·  Save Changes"
            : "<color=#66aaff><b>BN REFERENCE</b></color>  ·  read-only  ·  Copy to Custom";

    [OnInspectorGUI, PropertyOrder(0)]
    void DrawBrowser()
    {
        CatalogDataSession session = Session;
        GameDatabase db = session.GetDb(_source);
        if (db == null)
        {
            EditorGUILayout.HelpBox(
                "데이터를 로드할 수 없습니다.\nAssets/StreamingAssets/ 에 BNData/ 또는 GameData/ 폴더가 있는지 확인하세요.",
                MessageType.Warning);
            return;
        }

        DrawFilterBar(session);
        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(BrowserMinHeight), GUILayout.ExpandHeight(true));
        DrawList(session, db);
        DrawDetail(session, db);
        EditorGUILayout.EndHorizontal();
    }

    void DrawFilterBar(CatalogDataSession session)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        int newCatIdx = EditorGUILayout.Popup(
            _categoryIndex,
            ItemTypeLabels,
            EditorStyles.toolbarPopup,
            GUILayout.Width(120));
        if (newCatIdx != _categoryIndex)
        {
            _categoryIndex = newCatIdx;
            _categoryFilter = ItemTypes[_categoryIndex];
            _selectedIndex = -1;
            InvalidateFilter();
        }

        string newSearch = EditorGUILayout.TextField(
            _searchText,
            EditorStyles.toolbarSearchField,
            GUILayout.MinWidth(160));
        if (newSearch != _searchText)
        {
            _searchText = newSearch;
            _selectedIndex = -1;
            InvalidateFilter();
        }

        if (IsCustom && GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
            AddNewItem(session);

        EditorGUILayout.EndHorizontal();
    }

    void DrawList(CatalogDataSession session, GameDatabase db)
    {
        RebuildFilterIfNeeded(session, db);
        EditorGUILayout.BeginVertical(GUILayout.Width(ListPaneWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField($"{_filtered.Count} entries", EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
        int show = Mathf.Min(_filtered.Count, ListPageSize);
        DisplayLanguage lang = session.ActiveDisplayLanguage;

        for (int i = 0; i < show; i++)
        {
            bool selected = i == _selectedIndex;
            ItemData item = _filtered[i];
            string label = $"{item.id}  —  {ItemNameTable.Get(item.id, lang)}";
            if (GUILayout.Toggle(selected, label, "SelectionRect") && !selected)
                _selectedIndex = i;
        }

        if (_filtered.Count > ListPageSize)
            EditorGUILayout.HelpBox(
                $"검색을 좁혀주세요. {_filtered.Count - ListPageSize}개 항목 추가.",
                MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawDetail(CatalogDataSession session, GameDatabase db)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));

        if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
            EditorGUILayout.LabelField("항목을 선택하세요", EditorStyles.centeredGreyMiniLabel);
        else
            DrawItemDetail(session, db, _filtered[_selectedIndex]);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawItemDetail(CatalogDataSession session, GameDatabase db, ItemData item)
    {
        _foldIdentity = EditorGUILayout.Foldout(
            _foldIdentity,
            IsCustom ? "Identity (editable)" : "Identity (read-only)",
            true,
            EditorStyles.foldoutHeader);
        if (_foldIdentity)
        {
            EditorGUI.indentLevel++;
            if (IsCustom)
            {
                CatalogBrowserFields.EditField("ID", ref item.id, session.MarkDirty);
                CatalogBrowserFields.EditLocalizedItemName(item.id, session, InvalidateFilter);
                CatalogBrowserFields.EditField("Type", ref item.type, session.MarkDirty);
                CatalogBrowserFields.EditField("Category", ref item.category, session.MarkDirty);
                CatalogBrowserFields.EditIntField("Weight (g)", ref item.weight_g, session.MarkDirty);
                CatalogBrowserFields.EditIntField("Volume (ml)", ref item.volume_ml, session.MarkDirty);
            }
            else
            {
                CatalogBrowserFields.ReadField("ID", item.id);
                CatalogBrowserFields.EditLocalizedItemName(item.id, session, InvalidateFilter);
                CatalogBrowserFields.ReadField("Type", item.type);
                CatalogBrowserFields.ReadField("Category", item.category);
                CatalogBrowserFields.ReadField("Weight", $"{item.weight_g} g");
                CatalogBrowserFields.ReadField("Volume", $"{item.volume_ml} ml");
                if (item.materials is { Count: > 0 })
                    CatalogBrowserFields.ReadField("Materials", string.Join(", ", item.materials));
                if (!string.IsNullOrEmpty(item.comestible_type))
                    CatalogBrowserFields.ReadField("Comestible type", item.comestible_type);
            }

            EditorGUI.indentLevel--;
        }

        _foldGameDetail = EditorGUILayout.Foldout(
            _foldGameDetail, "Game Detail", true, EditorStyles.foldoutHeader);
        if (_foldGameDetail)
        {
            EditorGUI.indentLevel++;
            CatalogBrowserFields.EditLocalizedItemDescription(item.id, session);
            if (IsCustom)
                GameDataEditorDetailDrawers.DrawItemDetailEditable(item, session.MarkDirty);
            else
            {
                GameDataEditorDetailDrawers.DrawItemDetailReadOnly(item);
                if (item.qualities is { Count: > 0 })
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Qualities", EditorStyles.miniBoldLabel);
                    foreach (QualityEntry q in item.qualities)
                        EditorGUILayout.LabelField($"  {q.id} lv{q.level}");
                }

                if (item.flags is { Count: > 0 })
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Flags", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(
                        $"  {string.Join(", ", item.flags)}",
                        EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUI.indentLevel--;
        }

        _foldPresentation = EditorGUILayout.Foldout(
            _foldPresentation, "Combat Presentation", true, EditorStyles.foldoutHeader);
        if (_foldPresentation)
        {
            EditorGUI.indentLevel++;
            GameDataWeaponPresentationEditor.DrawItemBindingSection(item, editable: IsCustom);
            EditorGUI.indentLevel--;
        }

        _foldIcon = EditorGUILayout.Foldout(_foldIcon, "Icon", true, EditorStyles.foldoutHeader);
        if (_foldIcon)
        {
            EditorGUI.indentLevel++;
            DrawItemIconSection(item, session);
            EditorGUI.indentLevel--;
        }

        _foldRelations = EditorGUILayout.Foldout(
            _foldRelations, "Recipes / Relations", true, EditorStyles.foldoutHeader);
        if (_foldRelations)
        {
            EditorGUI.indentLevel++;
            List<RecipeData> recipes = db.GetRecipesForResult(item.id);
            if (recipes.Count > 0)
            {
                EditorGUILayout.LabelField("Recipes producing this", EditorStyles.miniBoldLabel);
                foreach (RecipeData r in recipes)
                    EditorGUILayout.LabelField($"  {r.id}  [{r.category}]");
            }
            else
                EditorGUILayout.LabelField("(none produce this)", EditorStyles.miniLabel);

            List<RecipeData> usedIn = db.GetRecipesUsingIngredient(item.id);
            if (usedIn.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"Used as ingredient ({usedIn.Count})",
                    EditorStyles.miniBoldLabel);
                foreach (RecipeData r in usedIn.Take(20))
                    EditorGUILayout.LabelField($"  {r.id}");
                if (usedIn.Count > 20)
                    EditorGUILayout.LabelField($"  ... +{usedIn.Count - 20} more");
            }

            EditorGUI.indentLevel--;
        }

        if (IsCustom)
        {
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Delete Item", GUILayout.Width(100)))
            {
                session.CustomItemsRoot.items.Remove(item);
                session.RebuildCustomDb();
                session.MarkDirty();
                _selectedIndex = -1;
                InvalidateFilter();
            }
        }
        else if (!string.IsNullOrEmpty(item.id))
        {
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Copy to Custom", GUILayout.Width(120)))
                CopyItemToCustom(session, item);
        }
    }

    void DrawItemIconSection(ItemData item, CatalogDataSession session)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        EditorGUILayout.HelpBox(
            "아이콘은 아이템 JSON이 아닙니다. Sprite 필드는 ItemIconCatalog 오버라이드입니다. 미할당이면 BN 타일셋(MSX++), 그것도 없으면 기본 아이콘입니다.",
            MessageType.None);

        ItemIconCatalog catalog = session.EnsureIconCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox(
                $"카탈로그를 만들 수 없습니다: {ItemIconCatalog.DefaultAssetPath}",
                MessageType.Error);
            return;
        }

        EditorGUI.BeginChangeCheck();
        Sprite assigned = catalog.GetAssignedIcon(item.id);
        Sprite next = (Sprite)EditorGUILayout.ObjectField("Override", assigned, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            catalog.SetIcon(item.id, next);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            ItemVisualPresenter.InvalidateCache();
            ItemVisualPresenter.BindCatalog(catalog);
        }

        Sprite resolved = ItemVisualPresenter.GetDisplayIcon(item.id);
        if (resolved != null)
        {
            Rect preview = GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f));
            CatalogBrowserFields.DrawSpritePreview(preview, resolved);
            if (assigned == null && resolved != ItemVisualPresenter.GetDefaultIcon())
                EditorGUILayout.LabelField("Resolved from BN tileset", EditorStyles.miniLabel);
        }
        else
            EditorGUILayout.LabelField("(no icon / fallback missing)", EditorStyles.miniLabel);

        if (GUILayout.Button("Select Catalog Asset", GUILayout.Width(160)))
            Selection.activeObject = catalog;
    }

    void AddNewItem(CatalogDataSession session)
    {
        string id = $"custom_item_{session.CustomItemsRoot.items.Count}";
        var item = new ItemData
        {
            id = id,
            name = string.Empty,
            type = "GENERIC",
            category = "other",
            weight_g = 100,
            volume_ml = 250,
            materials = new List<string>(),
            flags = new List<string>(),
            qualities = new List<QualityEntry>(),
        };
        session.CustomItemsRoot.items.Add(item);
        ItemNameTable.Set(id, session.ActiveDisplayLanguage, "New Item");
        session.RebuildCustomDb();
        session.MarkDirty();
        InvalidateFilter();
        RebuildFilterIfNeeded(session, session.CustomDb);
        _selectedIndex = _filtered.IndexOf(item);
    }

    void CopyItemToCustom(CatalogDataSession session, ItemData src)
    {
        ItemData copy = GameDataJson.Clone(src);
        copy.id = $"{src.id}_custom";
        copy.name = string.Empty;
        session.CustomItemsRoot.items.Add(copy);
        DisplayLanguage lang = session.ActiveDisplayLanguage;
        string display = ItemNameTable.Get(src.id, lang);
        if (!string.IsNullOrEmpty(display) && !display.StartsWith("[Missing:", StringComparison.Ordinal))
            ItemNameTable.Set(copy.id, lang, display);
        string description = ItemNameTable.Get(ItemLocaleKind.Description, src.id, lang);
        if (!string.IsNullOrEmpty(description) &&
            !description.StartsWith("[Missing:", StringComparison.Ordinal))
            ItemNameTable.Set(ItemLocaleKind.Description, copy.id, lang, description);
        session.RebuildCustomDb();
        session.MarkDirty();
        EditorUtility.DisplayDialog(
            "Copied",
            $"Copied to Custom as '{copy.id}'. Open Catalog/Items/Custom to edit.",
            "OK");
    }

    void InvalidateFilter()
    {
        _lastSearch = "\0";
        _lastCategory = "\0";
    }

    void RebuildFilterIfNeeded(CatalogDataSession session, GameDatabase db)
    {
        DisplayLanguage lang = session.ActiveDisplayLanguage;
        if (_lastSearch == _searchText && _lastCategory == _categoryFilter && _lastLanguage == lang)
            return;

        _lastSearch = _searchText;
        _lastCategory = _categoryFilter;
        _lastLanguage = lang;

        string lower = _searchText.ToLowerInvariant();
        _filtered.Clear();
        foreach (ItemData item in db.Items)
        {
            if (!string.IsNullOrEmpty(_categoryFilter) &&
                !string.Equals(item.type, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(lower))
            {
                string displayName = ItemNameTable.Get(item.id, lang);
                if (!(item.id ?? "").ToLowerInvariant().Contains(lower) &&
                    !(displayName ?? "").ToLowerInvariant().Contains(lower))
                    continue;
            }

            _filtered.Add(item);
        }
    }
}
