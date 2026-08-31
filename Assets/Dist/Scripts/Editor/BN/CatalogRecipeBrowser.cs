// ============================================================
// CatalogRecipeBrowser — Data Definitions Catalog/Recipes Odin 브라우저
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[HideReferenceObjectPicker]
public sealed class CatalogRecipeBrowser
{
    static readonly string[] RecipeCategories =
    {
        "", "CC_WEAPON", "CC_AMMO", "CC_FOOD", "CC_CHEM",
        "CC_ELECTRONIC", "CC_ARMOR", "CC_OTHER", "CC_ANIMALS"
    };

    static readonly string[] RecipeCategoryLabels =
    {
        "All", "Weapon", "Ammo", "Food", "Chem",
        "Electronic", "Armor", "Other", "Animals"
    };

    const int ListPageSize = 200;
    const float ListPaneWidth = 400f;
    const float BrowserMinHeight = 520f;

    readonly CatalogSource _source;
    readonly List<RecipeData> _filtered = new List<RecipeData>();

    string _searchText = "";
    int _categoryIndex;
    string _categoryFilter = "";
    int _selectedIndex = -1;
    Vector2 _listScroll;
    Vector2 _detailScroll;
    string _lastSearch = "\0";
    string _lastCategory = "\0";
    DisplayLanguage _lastLanguage = (DisplayLanguage)(-1);

    public CatalogRecipeBrowser(CatalogSource source)
    {
        _source = source;
    }

    public CatalogSource Source => _source;
    bool IsCustom => _source == CatalogSource.Custom;

    CatalogDataSession Session => CatalogDataSession.Instance;

    string BrowserTitle => IsCustom ? "Recipes · Custom" : "Recipes · BN Reference";
    string BrowserSubtitle => IsCustom ? "editable GameData JSON" : "read-only BN Reference";

    Color HeaderTint => IsCustom
        ? new Color(0.45f, 0.95f, 0.55f)
        : new Color(0.45f, 0.7f, 1f);

    bool AlwaysShow => true;
    string SourceHelp => IsCustom
        ? "Custom 레시피만 편집·삭제·추가됩니다. Save Changes로 recipes.json에 씁니다."
        : "BN은 읽기 전용입니다. Copy to Custom으로 옮긴 뒤 Catalog/Recipes/Custom에서 편집하세요.";

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
            RecipeCategoryLabels,
            EditorStyles.toolbarPopup,
            GUILayout.Width(120));
        if (newCatIdx != _categoryIndex)
        {
            _categoryIndex = newCatIdx;
            _categoryFilter = RecipeCategories[_categoryIndex];
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
            AddNewRecipe(session);

        EditorGUILayout.EndHorizontal();
    }

    void DrawList(CatalogDataSession session, GameDatabase db)
    {
        RebuildFilterIfNeeded(session, db);
        EditorGUILayout.BeginVertical(GUILayout.Width(ListPaneWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField($"{_filtered.Count} entries", EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
        int show = Mathf.Min(_filtered.Count, ListPageSize);

        for (int i = 0; i < show; i++)
        {
            bool selected = i == _selectedIndex;
            RecipeData recipe = _filtered[i];
            string label = $"{recipe.id}  [{recipe.category}]";
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
            DrawRecipeDetail(session, db, _filtered[_selectedIndex]);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawRecipeDetail(CatalogDataSession session, GameDatabase db, RecipeData recipe)
    {
        DisplayLanguage lang = session.ActiveDisplayLanguage;

        if (IsCustom)
        {
            EditorGUILayout.LabelField("Recipe (editable)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            CatalogBrowserFields.EditField("ID", ref recipe.id, session.MarkDirty);
            CatalogBrowserFields.EditField("Result", ref recipe.result, session.MarkDirty);
            CatalogBrowserFields.EditField("Category", ref recipe.category, session.MarkDirty);
            CatalogBrowserFields.EditField("Subcategory", ref recipe.subcategory, session.MarkDirty);
            CatalogBrowserFields.EditField("Skill", ref recipe.skill_used, session.MarkDirty);
            CatalogBrowserFields.EditIntField("Difficulty", ref recipe.difficulty, session.MarkDirty);
            CatalogBrowserFields.EditFloatField("Time (min)", ref recipe.time_minutes, session.MarkDirty);
            CatalogBrowserFields.EditIntField("Result Count", ref recipe.result_count, session.MarkDirty);

            bool rev = EditorGUILayout.Toggle("Reversible", recipe.reversible);
            if (rev != recipe.reversible)
            {
                recipe.reversible = rev;
                session.MarkDirty();
            }

            bool auto = EditorGUILayout.Toggle("Autolearn", recipe.autolearn);
            if (auto != recipe.autolearn)
            {
                recipe.autolearn = auto;
                session.MarkDirty();
            }

            DrawEditableComponents(session, recipe);
            GameDataEditorDetailDrawers.DrawRecipeDetailEditable(recipe, session.MarkDirty);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Recipe", GUILayout.Width(120)))
            {
                session.CustomRecipesRoot.recipes.Remove(recipe);
                session.RebuildCustomDb();
                session.MarkDirty();
                _selectedIndex = -1;
                InvalidateFilter();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Recipe (read-only)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            CatalogBrowserFields.ReadField("ID", recipe.id);
            CatalogBrowserFields.ReadField("Result", recipe.result);
            ItemData resultItem = db.GetItem(recipe.result);
            if (resultItem != null)
                CatalogBrowserFields.ReadField("Result Name", ItemNameTable.Get(resultItem.id, lang));
            else if (!string.IsNullOrEmpty(recipe.result))
                CatalogBrowserFields.ReadField("Result Name", ItemNameTable.Get(recipe.result, lang));
            CatalogBrowserFields.ReadField("Category", recipe.category);
            CatalogBrowserFields.ReadField("Subcategory", recipe.subcategory);
            CatalogBrowserFields.ReadField("Skill", recipe.skill_used);
            CatalogBrowserFields.ReadField("Difficulty", recipe.difficulty.ToString());
            CatalogBrowserFields.ReadField("Time", $"{recipe.time_minutes} min");
            CatalogBrowserFields.ReadField("Result Count", recipe.result_count.ToString());
            CatalogBrowserFields.ReadField("Reversible", recipe.reversible.ToString());
            CatalogBrowserFields.ReadField("Autolearn", recipe.autolearn.ToString());
            GameDataEditorDetailDrawers.DrawRecipeDetailReadOnly(recipe);
        }

        if (!IsCustom && recipe.skills_required is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Required Skills", EditorStyles.miniBoldLabel);
            foreach (SkillReq sr in recipe.skills_required)
                EditorGUILayout.LabelField($"  {sr.skill} lv{sr.level}");
        }

        if (!IsCustom && recipe.qualities_required is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Required Qualities", EditorStyles.miniBoldLabel);
            foreach (QualityEntry q in recipe.qualities_required)
                EditorGUILayout.LabelField($"  {q.id} lv{q.level}");
        }

        if (!IsCustom && recipe.tools is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tools", EditorStyles.miniBoldLabel);
            for (int i = 0; i < recipe.tools.Count; i++)
            {
                ToolSlot slot = recipe.tools[i];
                if (slot.alternatives == null)
                    continue;
                string line = string.Join(" OR ",
                    slot.alternatives.Select(a =>
                        a.charges < 0 ? a.tool : $"{a.tool} ({a.charges})"));
                EditorGUILayout.LabelField($"  Slot {i + 1}: {line}");
            }
        }

        if (!IsCustom && recipe.components is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Components", EditorStyles.miniBoldLabel);
            for (int i = 0; i < recipe.components.Count; i++)
            {
                ComponentSlot slot = recipe.components[i];
                if (slot.alternatives == null)
                    continue;
                string line = string.Join(" OR ",
                    slot.alternatives.Select(a => $"{a.item} x{a.count}"));
                EditorGUILayout.LabelField($"  Slot {i + 1}: {line}", EditorStyles.wordWrappedLabel);
            }
        }

        if (!IsCustom && recipe.byproducts is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Byproducts", EditorStyles.miniBoldLabel);
            foreach (Byproduct bp in recipe.byproducts)
                EditorGUILayout.LabelField($"  {bp.item} x{bp.count}");
        }

        if (!IsCustom && !string.IsNullOrEmpty(recipe.id))
        {
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Copy to Custom", GUILayout.Width(120)))
                CopyRecipeToCustom(session, recipe);
        }
    }

    void DrawEditableComponents(CatalogDataSession session, RecipeData recipe)
    {
        recipe.components ??= new List<ComponentSlot>();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Components", EditorStyles.miniBoldLabel);

        int removeSlot = -1;
        for (int i = 0; i < recipe.components.Count; i++)
        {
            ComponentSlot slot = recipe.components[i];
            slot.alternatives ??= new List<ComponentAlt>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Slot {i + 1}", GUILayout.Width(50));

            int removeAlt = -1;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                if (j > 0)
                    EditorGUILayout.LabelField("OR", GUILayout.Width(20));
                ComponentAlt alt = slot.alternatives[j];
                string newItem = EditorGUILayout.TextField(alt.item, GUILayout.Width(100));
                int newCount = EditorGUILayout.IntField(alt.count, GUILayout.Width(40));
                if (newItem != alt.item || newCount != alt.count)
                {
                    alt.item = newItem;
                    alt.count = newCount;
                    session.MarkDirty();
                }

                if (GUILayout.Button("x", GUILayout.Width(20)))
                    removeAlt = j;
            }

            if (GUILayout.Button("+alt", GUILayout.Width(36)))
            {
                slot.alternatives.Add(new ComponentAlt { item = "item_id", count = 1 });
                session.MarkDirty();
            }

            if (GUILayout.Button("-", GUILayout.Width(20)))
                removeSlot = i;

            EditorGUILayout.EndHorizontal();

            if (slot.alternatives.Count > 0)
            {
                for (int j = 0; j < slot.alternatives.Count; j++)
                    GameDataEditorDetailDrawers.DrawEditableComponentFlags(
                        slot.alternatives[j], session.MarkDirty);
            }

            if (removeAlt >= 0)
            {
                slot.alternatives.RemoveAt(removeAlt);
                session.MarkDirty();
            }
        }

        if (removeSlot >= 0)
        {
            recipe.components.RemoveAt(removeSlot);
            session.MarkDirty();
        }

        if (GUILayout.Button("+ Add Component Slot", GUILayout.Width(160)))
        {
            recipe.components.Add(new ComponentSlot
            {
                alternatives = new List<ComponentAlt>
                {
                    new ComponentAlt { item = "item_id", count = 1 }
                }
            });
            session.MarkDirty();
        }
    }

    void AddNewRecipe(CatalogDataSession session)
    {
        var recipe = new RecipeData
        {
            id = $"custom_recipe_{session.CustomRecipesRoot.recipes.Count}",
            result = "",
            category = "CC_OTHER",
            subcategory = "",
            skill_used = "fabrication",
            difficulty = 0,
            time_minutes = 5,
            result_count = 1,
            components = new List<ComponentSlot>(),
            tools = new List<ToolSlot>(),
            qualities_required = new List<QualityEntry>(),
            skills_required = new List<SkillReq>(),
        };
        session.CustomRecipesRoot.recipes.Add(recipe);
        session.RebuildCustomDb();
        session.MarkDirty();
        InvalidateFilter();
        RebuildFilterIfNeeded(session, session.CustomDb);
        _selectedIndex = _filtered.IndexOf(recipe);
    }

    void CopyRecipeToCustom(CatalogDataSession session, RecipeData src)
    {
        RecipeData copy = GameDataJson.Clone(src);
        copy.id = $"{src.id}_custom";
        session.CustomRecipesRoot.recipes.Add(copy);
        session.RebuildCustomDb();
        session.MarkDirty();
        EditorUtility.DisplayDialog(
            "Copied",
            $"Copied to Custom as '{copy.id}'. Open Catalog/Recipes/Custom to edit.",
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
        foreach (RecipeData recipe in db.Recipes)
        {
            if (!string.IsNullOrEmpty(_categoryFilter) &&
                !string.Equals(recipe.category, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(lower) &&
                !(recipe.id ?? "").Contains(lower) &&
                !(recipe.result ?? "").Contains(lower) &&
                !(ItemNameTable.Get(recipe.result, lang) ?? "").ToLowerInvariant().Contains(lower))
                continue;
            _filtered.Add(recipe);
        }
    }
}
