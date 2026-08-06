// ============================================================
// GameDataEditorWindow — Reference(참조) / Custom(편집) 듀얼 데이터 브라우저
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEngine;

public sealed class GameDataEditorWindow : EditorWindow
{
    enum Source { Reference, Custom }
    enum Tab { Items, Recipes }

    [MenuItem("Tools/Game Data Browser")]
    static void Open() => GetWindow<GameDataEditorWindow>("Game Data");

    Source _source;
    Tab _tab;
    GameDatabase _bnDb;
    GameDatabase _customDb;

    ItemsFileRoot _customItemsRoot;
    RecipesFileRoot _customRecipesRoot;

    string _searchText = "";
    string _categoryFilter = "";
    Vector2 _listScroll;
    Vector2 _detailScroll;
    int _selectedIndex = -1;

    List<ItemData> _filteredItems = new();
    List<RecipeData> _filteredRecipes = new();
    string _lastSearch = "\0";
    string _lastCategory = "\0";
    Tab _lastTab;
    Source _lastSource;
    bool _dirty;
    ItemIconCatalog _iconCatalog;

    static readonly string[] _recipeCategories =
    {
        "", "CC_WEAPON", "CC_AMMO", "CC_FOOD", "CC_CHEM",
        "CC_ELECTRONIC", "CC_ARMOR", "CC_OTHER", "CC_ANIMALS"
    };
    static readonly string[] _recipeCategoryLabels =
    {
        "All", "Weapon", "Ammo", "Food", "Chem",
        "Electronic", "Armor", "Other", "Animals"
    };
    static readonly string[] _itemTypes =
    {
        "", "GENERIC", "COMESTIBLE", "ARMOR", "TOOL", "GUN",
        "AMMO", "BOOK", "MAGAZINE", "GUNMOD", "TOOL_ARMOR"
    };
    static readonly string[] _itemTypeLabels =
    {
        "All", "Generic", "Comestible", "Armor", "Tool", "Gun",
        "Ammo", "Book", "Magazine", "Gunmod", "ToolArmor"
    };

    int _categoryIndex;

    void OnEnable() => ReloadAll();

    void ReloadAll()
    {
        string bnPath = GameDataLoader.GetRefDataPath();
        _bnDb = GameDataLoader.LoadFromPaths(
            Path.Combine(bnPath, "items.json"),
            Path.Combine(bnPath, "recipes.json"));

        LoadCustomData();
        _iconCatalog = EnsureIconCatalog();
        InvalidateFilter();
    }

    void LoadCustomData()
    {
        string gamePath = GameDataLoader.GetGameDataPath();
        string itemsPath = Path.Combine(gamePath, "items.json");
        string recipesPath = Path.Combine(gamePath, "recipes.json");

        if (File.Exists(itemsPath))
        {
            string json = File.ReadAllText(itemsPath);
            _customItemsRoot = GameDataJson.Deserialize<ItemsFileRoot>(json);
        }
        else
        {
            _customItemsRoot = new ItemsFileRoot
            {
                _license = "Project proprietary",
                items = new List<ItemData>(),
                materials = new List<MaterialData>(),
                qualities = new List<QualityData>(),
            };
        }

        if (File.Exists(recipesPath))
        {
            string json = File.ReadAllText(recipesPath);
            _customRecipesRoot = GameDataJson.Deserialize<RecipesFileRoot>(json);
        }
        else
        {
            _customRecipesRoot = new RecipesFileRoot
            {
                _license = "Project proprietary",
                recipes = new List<RecipeData>(),
                uncraft = new List<RecipeData>(),
            };
        }

        _customDb = new GameDatabase(_customItemsRoot, _customRecipesRoot);
        _dirty = false;
    }

    void InvalidateFilter() { _lastSearch = "\0"; _lastCategory = "\0"; }

    GameDatabase ActiveDb => _source == Source.Reference ? _bnDb : _customDb;
    bool IsCustom => _source == Source.Custom;

    // ── OnGUI ──────────────────────────────────────────────────

    void OnGUI()
    {
        if (_bnDb == null && _customDb == null)
        {
            EditorGUILayout.HelpBox(
                "데이터를 로드할 수 없습니다.\nAssets/StreamingAssets/ 에 BNData/ 또는 GameData/ 폴더가 있는지 확인하세요.",
                MessageType.Warning);
            if (GUILayout.Button("Reload")) ReloadAll();
            return;
        }

        DrawSourceBar();
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawList();
        DrawDetail();
        EditorGUILayout.EndHorizontal();

        DrawFooter();
    }

    void DrawSourceBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var newSource = (Source)GUILayout.Toolbar((int)_source,
            new[] { "BN Reference (read-only)", "Custom (editable)" },
            EditorStyles.toolbarButton, GUILayout.Width(360));
        if (newSource != _source)
        {
            _source = newSource;
            _selectedIndex = -1;
            _categoryIndex = 0;
            _categoryFilter = "";
            InvalidateFilter();
        }

        GUILayout.FlexibleSpace();

        if (_dirty)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("Save Changes", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SaveCustomData();
            GUI.backgroundColor = prev;
        }

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
            ReloadAll();

        EditorGUILayout.EndHorizontal();
    }

    void DrawToolbar()
    {
        var db = ActiveDb;
        if (db == null) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        Tab newTab = (Tab)GUILayout.Toolbar((int)_tab,
            new[] { $"Items ({db.Items.Count})", $"Recipes ({db.Recipes.Count})" },
            EditorStyles.toolbarButton, GUILayout.Width(300));
        if (newTab != _tab) { _tab = newTab; _selectedIndex = -1; InvalidateFilter(); }

        GUILayout.Space(8);

        string[] catLabels = _tab == Tab.Items ? _itemTypeLabels : _recipeCategoryLabels;
        int newCatIdx = EditorGUILayout.Popup(_categoryIndex, catLabels,
            EditorStyles.toolbarPopup, GUILayout.Width(120));
        if (newCatIdx != _categoryIndex)
        {
            _categoryIndex = newCatIdx;
            _categoryFilter = _tab == Tab.Items
                ? _itemTypes[_categoryIndex]
                : _recipeCategories[_categoryIndex];
            _selectedIndex = -1;
            InvalidateFilter();
        }

        GUILayout.Space(8);

        string newSearch = EditorGUILayout.TextField(_searchText,
            EditorStyles.toolbarSearchField, GUILayout.MinWidth(200));
        if (newSearch != _searchText) { _searchText = newSearch; _selectedIndex = -1; InvalidateFilter(); }

        if (IsCustom)
        {
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                if (_tab == Tab.Items) AddNewItem();
                else AddNewRecipe();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── Filter ─────────────────────────────────────────────────

    void RebuildFilterIfNeeded()
    {
        if (_lastSearch == _searchText && _lastCategory == _categoryFilter
            && _lastTab == _tab && _lastSource == _source) return;

        _lastSearch = _searchText;
        _lastCategory = _categoryFilter;
        _lastTab = _tab;
        _lastSource = _source;

        var db = ActiveDb;
        if (db == null) return;
        string lower = _searchText.ToLowerInvariant();

        if (_tab == Tab.Items)
        {
            _filteredItems.Clear();
            foreach (ItemData item in db.Items)
            {
                if (!string.IsNullOrEmpty(_categoryFilter) &&
                    !string.Equals(item.type, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(lower) &&
                    !(item.id ?? "").Contains(lower) &&
                    !(item.name ?? "").ToLowerInvariant().Contains(lower))
                    continue;
                _filteredItems.Add(item);
            }
        }
        else
        {
            _filteredRecipes.Clear();
            foreach (RecipeData recipe in db.Recipes)
            {
                if (!string.IsNullOrEmpty(_categoryFilter) &&
                    !string.Equals(recipe.category, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(lower) &&
                    !(recipe.id ?? "").Contains(lower) &&
                    !(recipe.result ?? "").Contains(lower))
                    continue;
                _filteredRecipes.Add(recipe);
            }
        }
    }

    // ── List ───────────────────────────────────────────────────

    void DrawList()
    {
        RebuildFilterIfNeeded();
        EditorGUILayout.BeginVertical(GUILayout.Width(320));
        int count = _tab == Tab.Items ? _filteredItems.Count : _filteredRecipes.Count;
        EditorGUILayout.LabelField($"{count} entries", EditorStyles.miniLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        const int PAGE = 200;
        int show = Mathf.Min(count, PAGE);

        for (int i = 0; i < show; i++)
        {
            bool selected = i == _selectedIndex;
            string label = _tab == Tab.Items
                ? $"{_filteredItems[i].id}  —  {_filteredItems[i].name}"
                : $"{_filteredRecipes[i].id}  [{_filteredRecipes[i].category}]";

            if (GUILayout.Toggle(selected, label, "SelectionRect"))
                if (!selected) _selectedIndex = i;
        }

        if (count > PAGE)
            EditorGUILayout.HelpBox($"검색을 좁혀주세요. {count - PAGE}개 항목 추가.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ── Detail ─────────────────────────────────────────────────

    void DrawDetail()
    {
        EditorGUILayout.BeginVertical("box");
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        if (_selectedIndex < 0)
            EditorGUILayout.LabelField("항목을 선택하세요", EditorStyles.centeredGreyMiniLabel);
        else if (_tab == Tab.Items)
            DrawItemDetail();
        else
            DrawRecipeDetail();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawItemDetail()
    {
        if (_selectedIndex >= _filteredItems.Count) return;
        var item = _filteredItems[_selectedIndex];
        var db = ActiveDb;

        if (IsCustom)
        {
            EditorGUILayout.LabelField("Item (editable)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditField("ID", ref item.id);
            EditField("Name", ref item.name);
            EditField("Type", ref item.type);
            EditField("Category", ref item.category);
            EditIntField("Weight (g)", ref item.weight_g);
            EditIntField("Volume (ml)", ref item.volume_ml);

            GameDataEditorDetailDrawers.DrawItemDetailEditable(item, MarkDirty);
            GameDataWeaponPresentationEditor.DrawSection(item, editable: true);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Item", GUILayout.Width(100)))
            {
                _customItemsRoot.items.Remove(item);
                RebuildCustomDb();
                _selectedIndex = -1;
            }
        }
        else
        {
            EditorGUILayout.LabelField("Item (read-only)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            ReadField("ID", item.id);
            ReadField("Name", item.name);
            ReadField("Type", item.type);
            ReadField("Category", item.category);
            ReadField("Weight", $"{item.weight_g} g");
            ReadField("Volume", $"{item.volume_ml} ml");
        }

        if (item.materials is { Count: > 0 } && !IsCustom)
            ReadField("Materials", string.Join(", ", item.materials));

        if (!string.IsNullOrEmpty(item.comestible_type) && !IsCustom)
            ReadField("Comestible type", item.comestible_type);

        if (!IsCustom)
            GameDataEditorDetailDrawers.DrawItemDetailReadOnly(item);

        GameDataWeaponPresentationEditor.DrawSection(item, editable: IsCustom);

        DrawItemIconSection(item);

        if (!IsCustom && item.qualities is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Qualities", EditorStyles.miniBoldLabel);
            foreach (var q in item.qualities)
                EditorGUILayout.LabelField($"  {q.id} lv{q.level}");
        }

        if (!IsCustom && item.flags is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Flags", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"  {string.Join(", ", item.flags)}", EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.Space(8);
        var recipes = db.GetRecipesForResult(item.id);
        if (recipes.Count > 0)
        {
            EditorGUILayout.LabelField("Recipes producing this", EditorStyles.boldLabel);
            foreach (var r in recipes)
                EditorGUILayout.LabelField($"  {r.id}  [{r.category}]");
        }

        var usedIn = db.GetRecipesUsingIngredient(item.id);
        if (usedIn.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Used as ingredient ({usedIn.Count})", EditorStyles.boldLabel);
            foreach (var r in usedIn.Take(20))
                EditorGUILayout.LabelField($"  {r.id}");
            if (usedIn.Count > 20)
                EditorGUILayout.LabelField($"  ... +{usedIn.Count - 20} more");
        }

        if (!IsCustom && !string.IsNullOrEmpty(item.id))
        {
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Copy to Custom", GUILayout.Width(120)))
                CopyItemToCustom(item);
        }
    }

    void DrawItemIconSection(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Icon (ItemIconCatalog)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "아이콘은 JSON이 아니라 ItemIconCatalog SO에 저장됩니다. BN/Custom 공통으로 itemId 매핑합니다.",
            MessageType.None);

        ItemIconCatalog catalog = EnsureIconCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox($"카탈로그를 만들 수 없습니다: {ItemIconCatalog.AssetPath}", MessageType.Error);
            return;
        }

        EditorGUI.BeginChangeCheck();
        Sprite assigned = catalog.GetAssignedIcon(item.id);
        Sprite next = (Sprite)EditorGUILayout.ObjectField("Sprite", assigned, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            catalog.SetIcon(item.id, next);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            ItemVisualPresenter.InvalidateCache();
            ItemVisualPresenter.BindCatalog(catalog);
        }

        Sprite resolved = catalog.Resolve(item.id);
        if (resolved != null)
        {
            Rect preview = GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f));
            DrawSpritePreview(preview, resolved);
        }
        else
        {
            EditorGUILayout.LabelField("(no icon / fallback missing)", EditorStyles.miniLabel);
        }

        if (GUILayout.Button("Select Catalog Asset", GUILayout.Width(160)))
            Selection.activeObject = catalog;
    }

    static void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Rect texRect = sprite.textureRect;
        var uv = new Rect(
            texRect.x / sprite.texture.width,
            texRect.y / sprite.texture.height,
            texRect.width / sprite.texture.width,
            texRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
    }

    ItemIconCatalog EnsureIconCatalog()
    {
        if (_iconCatalog != null)
            return _iconCatalog;

        _iconCatalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(ItemIconCatalog.AssetPath);
        if (_iconCatalog != null)
        {
            ItemVisualPresenter.BindCatalog(_iconCatalog);
            return _iconCatalog;
        }

        string resourcesFolder = "Assets/Dist/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dist"))
                AssetDatabase.CreateFolder("Assets", "Dist");
            AssetDatabase.CreateFolder("Assets/Dist", "Resources");
        }

        _iconCatalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
        Sprite fallback = LoadEmptyIconSprite();
        if (fallback != null)
            _iconCatalog.SetDefaultIcon(fallback);

        AssetDatabase.CreateAsset(_iconCatalog, ItemIconCatalog.AssetPath);
        AssetDatabase.SaveAssets();
        ItemVisualPresenter.BindCatalog(_iconCatalog);
        Debug.Log($"[GameDataEditor] Created {ItemIconCatalog.AssetPath}");
        return _iconCatalog;
    }

    static Sprite LoadEmptyIconSprite()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ItemVisualPresenter.DefaultIconAssetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }

    void DrawRecipeDetail()
    {
        if (_selectedIndex >= _filteredRecipes.Count) return;
        var recipe = _filteredRecipes[_selectedIndex];
        var db = ActiveDb;

        if (IsCustom)
        {
            EditorGUILayout.LabelField("Recipe (editable)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditField("ID", ref recipe.id);
            EditField("Result", ref recipe.result);
            EditField("Category", ref recipe.category);
            EditField("Subcategory", ref recipe.subcategory);
            EditField("Skill", ref recipe.skill_used);
            EditIntField("Difficulty", ref recipe.difficulty);
            EditFloatField("Time (min)", ref recipe.time_minutes);
            EditIntField("Result Count", ref recipe.result_count);

            bool rev = EditorGUILayout.Toggle("Reversible", recipe.reversible);
            if (rev != recipe.reversible) { recipe.reversible = rev; _dirty = true; }
            bool auto = EditorGUILayout.Toggle("Autolearn", recipe.autolearn);
            if (auto != recipe.autolearn) { recipe.autolearn = auto; _dirty = true; }

            DrawEditableComponents(recipe);
            GameDataEditorDetailDrawers.DrawRecipeDetailEditable(recipe, MarkDirty);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Recipe", GUILayout.Width(120)))
            {
                _customRecipesRoot.recipes.Remove(recipe);
                RebuildCustomDb();
                _selectedIndex = -1;
            }
        }
        else
        {
            EditorGUILayout.LabelField("Recipe (read-only)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            ReadField("ID", recipe.id);
            ReadField("Result", recipe.result);
            var resultItem = db.GetItem(recipe.result);
            if (resultItem != null) ReadField("Result Name", resultItem.name);
            ReadField("Category", recipe.category);
            ReadField("Subcategory", recipe.subcategory);
            ReadField("Skill", recipe.skill_used);
            ReadField("Difficulty", recipe.difficulty.ToString());
            ReadField("Time", $"{recipe.time_minutes} min");
            ReadField("Result Count", recipe.result_count.ToString());
            ReadField("Reversible", recipe.reversible.ToString());
            ReadField("Autolearn", recipe.autolearn.ToString());
            GameDataEditorDetailDrawers.DrawRecipeDetailReadOnly(recipe);
        }

        if (!IsCustom && recipe.skills_required is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Required Skills", EditorStyles.miniBoldLabel);
            foreach (var sr in recipe.skills_required)
                EditorGUILayout.LabelField($"  {sr.skill} lv{sr.level}");
        }

        if (!IsCustom && recipe.qualities_required is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Required Qualities", EditorStyles.miniBoldLabel);
            foreach (var q in recipe.qualities_required)
                EditorGUILayout.LabelField($"  {q.id} lv{q.level}");
        }

        if (!IsCustom && recipe.tools is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tools", EditorStyles.miniBoldLabel);
            for (int i = 0; i < recipe.tools.Count; i++)
            {
                var slot = recipe.tools[i];
                if (slot.alternatives == null) continue;
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
                var slot = recipe.components[i];
                if (slot.alternatives == null) continue;
                string line = string.Join(" OR ",
                    slot.alternatives.Select(a => $"{a.item} x{a.count}"));
                EditorGUILayout.LabelField($"  Slot {i + 1}: {line}", EditorStyles.wordWrappedLabel);
            }
        }

        if (!IsCustom && recipe.byproducts is { Count: > 0 })
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Byproducts", EditorStyles.miniBoldLabel);
            foreach (var bp in recipe.byproducts)
                EditorGUILayout.LabelField($"  {bp.item} x{bp.count}");
        }

        if (!IsCustom && !string.IsNullOrEmpty(recipe.id))
        {
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Copy to Custom", GUILayout.Width(120)))
                CopyRecipeToCustom(recipe);
        }
    }

    void DrawEditableComponents(RecipeData recipe)
    {
        recipe.components ??= new List<ComponentSlot>();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Components", EditorStyles.miniBoldLabel);

        int removeSlot = -1;
        for (int i = 0; i < recipe.components.Count; i++)
        {
            var slot = recipe.components[i];
            slot.alternatives ??= new List<ComponentAlt>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Slot {i + 1}", GUILayout.Width(50));

            int removeAlt = -1;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                if (j > 0) EditorGUILayout.LabelField("OR", GUILayout.Width(20));
                var alt = slot.alternatives[j];
                string newItem = EditorGUILayout.TextField(alt.item, GUILayout.Width(100));
                int newCount = EditorGUILayout.IntField(alt.count, GUILayout.Width(40));
                if (newItem != alt.item || newCount != alt.count)
                {
                    alt.item = newItem;
                    alt.count = newCount;
                    _dirty = true;
                }
                if (GUILayout.Button("x", GUILayout.Width(20)))
                    removeAlt = j;
            }

            if (GUILayout.Button("+alt", GUILayout.Width(36)))
            {
                slot.alternatives.Add(new ComponentAlt { item = "item_id", count = 1 });
                _dirty = true;
            }
            if (GUILayout.Button("-", GUILayout.Width(20)))
                removeSlot = i;

            EditorGUILayout.EndHorizontal();

            if (slot.alternatives.Count > 0)
            {
                for (int j = 0; j < slot.alternatives.Count; j++)
                    GameDataEditorDetailDrawers.DrawEditableComponentFlags(slot.alternatives[j], MarkDirty);
            }

            if (removeAlt >= 0) { slot.alternatives.RemoveAt(removeAlt); _dirty = true; }
        }

        if (removeSlot >= 0) { recipe.components.RemoveAt(removeSlot); _dirty = true; }

        if (GUILayout.Button("+ Add Component Slot", GUILayout.Width(160)))
        {
            recipe.components.Add(new ComponentSlot
            {
                alternatives = new List<ComponentAlt>
                    { new() { item = "item_id", count = 1 } }
            });
            _dirty = true;
        }
    }

    // ── Footer ─────────────────────────────────────────────────

    void DrawFooter()
    {
        if (!_dirty) return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("unsaved changes", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    // ── CRUD ───────────────────────────────────────────────────

    void AddNewItem()
    {
        var item = new ItemData
        {
            id = $"custom_item_{_customItemsRoot.items.Count}",
            name = "New Item",
            type = "GENERIC",
            category = "other",
            weight_g = 100,
            volume_ml = 250,
            materials = new List<string>(),
            flags = new List<string>(),
            qualities = new List<QualityEntry>(),
        };
        _customItemsRoot.items.Add(item);
        RebuildCustomDb();
        _dirty = true;
    }

    void AddNewRecipe()
    {
        var recipe = new RecipeData
        {
            id = $"custom_recipe_{_customRecipesRoot.recipes.Count}",
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
        _customRecipesRoot.recipes.Add(recipe);
        RebuildCustomDb();
        _dirty = true;
    }

    void CopyItemToCustom(ItemData src)
    {
        var copy = GameDataJson.Clone(src);
        copy.id = $"{src.id}_custom";
        _customItemsRoot.items.Add(copy);
        RebuildCustomDb();
        _dirty = true;
        _source = Source.Custom;
        InvalidateFilter();
    }

    void CopyRecipeToCustom(RecipeData src)
    {
        var copy = GameDataJson.Clone(src);
        copy.id = $"{src.id}_custom";
        _customRecipesRoot.recipes.Add(copy);
        RebuildCustomDb();
        _dirty = true;
        _source = Source.Custom;
        InvalidateFilter();
    }

    void RebuildCustomDb()
    {
        _customDb = new GameDatabase(_customItemsRoot, _customRecipesRoot);
        InvalidateFilter();
    }

    void MarkDirty() => _dirty = true;

    // ── Save ───────────────────────────────────────────────────

    void SaveCustomData()
    {
        string gamePath = GameDataLoader.GetGameDataPath();
        Directory.CreateDirectory(gamePath);

        string itemsJson = GameDataJson.Serialize(_customItemsRoot);
        File.WriteAllText(Path.Combine(gamePath, "items.json"), itemsJson);

        string recipesJson = GameDataJson.Serialize(_customRecipesRoot);
        File.WriteAllText(Path.Combine(gamePath, "recipes.json"), recipesJson);

        _dirty = false;
        Debug.Log($"[GameDataEditor] Custom data saved to {gamePath}");
        AssetDatabase.Refresh();
    }

    // ── Field helpers ──────────────────────────────────────────

    void EditField(string label, ref string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        string newVal = EditorGUILayout.TextField(value ?? "");
        if (newVal != (value ?? "")) { value = newVal; _dirty = true; }
        EditorGUILayout.EndHorizontal();
    }

    void EditIntField(string label, ref int value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        int newVal = EditorGUILayout.IntField(value);
        if (newVal != value) { value = newVal; _dirty = true; }
        EditorGUILayout.EndHorizontal();
    }

    void EditFloatField(string label, ref float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        float newVal = EditorGUILayout.FloatField(value);
        if (!Mathf.Approximately(newVal, value)) { value = newVal; _dirty = true; }
        EditorGUILayout.EndHorizontal();
    }

    static void ReadField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        EditorGUILayout.LabelField(value ?? "—", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
    }
}
