// ============================================================
// GameDataEditorWindow — Reference(참조) / Custom(편집) 듀얼 데이터 브라우저
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class GameDataEditorWindow : EditorWindow
{
    enum Source { Reference, Custom }
    enum Tab { Items, Recipes, Characters }

    [MenuItem("Tools/Data Definitions")]
    static void Open() => GetWindow<GameDataEditorWindow>("Data Definitions");

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
    List<CharacterDefinition> _characterDefs = new();
    List<CharacterDefinition> _filteredCharacters = new();
    SerializedObject _characterSerialized;
    CharacterDefinition _characterSerializedTarget;
    static readonly string[] _characterKindLabels = { "All", "Player", "Npc" };
    string _lastSearch = "\0";
    string _lastCategory = "\0";
    Tab _lastTab;
    Source _lastSource;
    DisplayLanguage _lastLanguage = (DisplayLanguage)(-1);
    bool _dirty;
    ItemIconCatalog _iconCatalog;
    LocalizationBundle _bundle;

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

    DisplayLanguage ActiveDisplayLanguage =>
        _bundle != null ? _bundle.ActiveLanguage : DisplayLanguage.Ko;

    bool HasUnsavedChanges => _dirty || ItemNameTable.IsGameDirty;

    void OnEnable() => ReloadAll();

    void OnDisable() => BindCharacterSerialized(null);

    void ReloadAll()
    {
        _bundle = EnsureLocalizationBundle();
        LocalizationBundle.ClearCache();
        _bundle = LocalizationBundle.Get();

        ItemNameTable.Reload();

        string bnPath = GameDataLoader.GetRefDataPath();
        _bnDb = GameDataLoader.LoadFromPaths(
            Path.Combine(bnPath, "items.json"),
            Path.Combine(bnPath, "recipes.json"));

        LoadCustomData();
        SeedCustomItemNames();
        _iconCatalog = EnsureIconCatalog();
        LoadCharacters();
        InvalidateFilter();
    }

    void SeedCustomItemNames()
    {
        if (_customItemsRoot?.items == null)
            return;

        for (int i = 0; i < _customItemsRoot.items.Count; i++)
        {
            ItemData item = _customItemsRoot.items[i];
            if (item == null || string.IsNullOrEmpty(item.id))
                continue;
            if (!string.IsNullOrEmpty(item.name))
                ItemNameTable.SeedFromItemNameIfMissing(item.id, item.name, DisplayLanguage.Ko);
        }
    }

    static LocalizationBundle EnsureLocalizationBundle()
    {
        LocalizationBundle bundle =
            AssetDatabase.LoadAssetAtPath<LocalizationBundle>(LocalizationBundle.AssetPath);
        if (bundle != null)
            return bundle;

        string dir = Path.GetDirectoryName(LocalizationBundle.AssetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        bundle = ScriptableObject.CreateInstance<LocalizationBundle>();
        TMP_FontAsset katuri =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DistUiFont.AssetPath);
        if (katuri != null)
        {
            bundle.EditorSetFont(DisplayLanguage.En, katuri);
            bundle.EditorSetFont(DisplayLanguage.Ko, katuri);
        }

        AssetDatabase.CreateAsset(bundle, LocalizationBundle.AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameDataEditor] Created {LocalizationBundle.AssetPath}");
        return bundle;
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
    bool IsCharactersTab => _tab == Tab.Characters;

    void LoadCharacters()
    {
        _characterDefs = CharacterDefinitionCatalog.LoadAll();
        BindCharacterSerialized(null);
        InvalidateFilter();
    }

    void BindCharacterSerialized(CharacterDefinition def)
    {
        if (_characterSerialized != null)
        {
            _characterSerialized.Dispose();
            _characterSerialized = null;
        }

        _characterSerializedTarget = def;
        if (def != null)
            _characterSerialized = new SerializedObject(def);
    }

    // ── OnGUI ──────────────────────────────────────────────────

    void OnGUI()
    {
        DrawSourceBar();
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        if (IsCharactersTab)
        {
            DrawCharacterList();
            DrawCharacterDetail();
        }
        else if (ActiveDb == null)
        {
            EditorGUILayout.HelpBox(
                "데이터를 로드할 수 없습니다.\nAssets/StreamingAssets/ 에 BNData/ 또는 GameData/ 폴더가 있는지 확인하세요.",
                MessageType.Warning);
        }
        else
        {
            DrawList();
            DrawDetail();
        }

        EditorGUILayout.EndHorizontal();

        DrawFooter();
    }

    void DrawSourceBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (IsCharactersTab)
        {
            EditorGUILayout.LabelField(
                "Characters (Dist SO)",
                EditorStyles.miniLabel,
                GUILayout.Width(360));
        }
        else
        {
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
        }

        GUILayout.FlexibleSpace();

        if (HasUnsavedChanges)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("Save Changes", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SaveAll();
            GUI.backgroundColor = prev;
        }

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
            ReloadAll();

        if (GUILayout.Button("Loc Bundle", EditorStyles.toolbarButton, GUILayout.Width(80)))
            PingLocalizationBundle();

        EditorGUILayout.EndHorizontal();
    }

    void PingLocalizationBundle()
    {
        if (_bundle == null)
            _bundle = EnsureLocalizationBundle();
        if (_bundle == null)
            return;

        Selection.activeObject = _bundle;
        EditorGUIUtility.PingObject(_bundle);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        int itemCount = ActiveDb != null ? ActiveDb.Items.Count : 0;
        int recipeCount = ActiveDb != null ? ActiveDb.Recipes.Count : 0;
        Tab newTab = (Tab)GUILayout.Toolbar((int)_tab,
            new[]
            {
                $"Items ({itemCount})",
                $"Recipes ({recipeCount})",
                $"Characters ({_characterDefs.Count})"
            },
            EditorStyles.toolbarButton, GUILayout.Width(420));
        if (newTab != _tab)
        {
            _tab = newTab;
            _selectedIndex = -1;
            _categoryIndex = 0;
            _categoryFilter = "";
            BindCharacterSerialized(null);
            InvalidateFilter();
        }

        GUILayout.Space(8);

        if (IsCharactersTab)
        {
            int newKindIdx = EditorGUILayout.Popup(
                _categoryIndex,
                _characterKindLabels,
                EditorStyles.toolbarPopup,
                GUILayout.Width(120));
            if (newKindIdx != _categoryIndex)
            {
                _categoryIndex = newKindIdx;
                _categoryFilter = _characterKindLabels[newKindIdx];
                _selectedIndex = -1;
                BindCharacterSerialized(null);
                InvalidateFilter();
            }
        }
        else
        {
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
        }

        GUILayout.Space(8);

        string newSearch = EditorGUILayout.TextField(_searchText,
            EditorStyles.toolbarSearchField, GUILayout.MinWidth(200));
        if (newSearch != _searchText)
        {
            _searchText = newSearch;
            _selectedIndex = -1;
            BindCharacterSerialized(null);
            InvalidateFilter();
        }

        if (IsCharactersTab)
        {
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
                AddNewCharacter();
        }
        else if (IsCustom)
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
        DisplayLanguage lang = ActiveDisplayLanguage;
        if (_lastSearch == _searchText && _lastCategory == _categoryFilter
            && _lastTab == _tab && _lastSource == _source && _lastLanguage == lang)
            return;

        _lastSearch = _searchText;
        _lastCategory = _categoryFilter;
        _lastTab = _tab;
        _lastSource = _source;
        _lastLanguage = lang;

        string lower = _searchText.ToLowerInvariant();

        if (_tab == Tab.Characters)
        {
            _filteredCharacters.Clear();
            for (int i = 0; i < _characterDefs.Count; i++)
            {
                CharacterDefinition def = _characterDefs[i];
                if (def == null)
                    continue;
                if (_categoryIndex == 1 && def.Kind != CharacterKind.Player)
                    continue;
                if (_categoryIndex == 2 && def.Kind != CharacterKind.Npc)
                    continue;
                if (!string.IsNullOrEmpty(lower))
                {
                    string display = def.DisplayNameOverride ?? string.Empty;
                    if (!(def.name ?? string.Empty).ToLowerInvariant().Contains(lower) &&
                        !(def.Id ?? string.Empty).ToLowerInvariant().Contains(lower) &&
                        !display.ToLowerInvariant().Contains(lower))
                        continue;
                }

                _filteredCharacters.Add(def);
            }

            return;
        }

        var db = ActiveDb;
        if (db == null) return;

        if (_tab == Tab.Items)
        {
            _filteredItems.Clear();
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
                    !(recipe.result ?? "").Contains(lower) &&
                    !(ItemNameTable.Get(recipe.result, lang) ?? "").ToLowerInvariant().Contains(lower))
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
                ? $"{_filteredItems[i].id}  —  {ItemNameTable.Get(_filteredItems[i].id, ActiveDisplayLanguage)}"
                : $"{_filteredRecipes[i].id}  [{_filteredRecipes[i].category}]";

            if (GUILayout.Toggle(selected, label, "SelectionRect"))
                if (!selected) _selectedIndex = i;
        }

        if (count > PAGE)
            EditorGUILayout.HelpBox($"검색을 좁혀주세요. {count - PAGE}개 항목 추가.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawCharacterList()
    {
        RebuildFilterIfNeeded();
        EditorGUILayout.BeginVertical(GUILayout.Width(320));
        int count = _filteredCharacters.Count;
        EditorGUILayout.LabelField($"{count} entries", EditorStyles.miniLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        const int PAGE = 200;
        int show = Mathf.Min(count, PAGE);

        for (int i = 0; i < show; i++)
        {
            bool selected = i == _selectedIndex;
            CharacterDefinition def = _filteredCharacters[i];
            string id = def != null ? def.Id : string.Empty;
            string kind = def != null ? def.Kind.ToString() : "?";
            string assetName = def != null ? def.name : "(missing)";
            string label = $"{kind}  {assetName}";
            if (!string.IsNullOrEmpty(id))
                label += $"  —  {id}";

            if (GUILayout.Toggle(selected, label, "SelectionRect") && !selected)
            {
                _selectedIndex = i;
                BindCharacterSerialized(def);
            }
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

    void DrawCharacterDetail()
    {
        EditorGUILayout.BeginVertical("box");
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        if (_selectedIndex < 0 || _selectedIndex >= _filteredCharacters.Count)
        {
            EditorGUILayout.LabelField("항목을 선택하세요", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        CharacterDefinition def = _filteredCharacters[_selectedIndex];
        if (def == null)
        {
            EditorGUILayout.HelpBox("에셋을 로드할 수 없습니다.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        if (_characterSerializedTarget != def)
            BindCharacterSerialized(def);

        EditorGUILayout.LabelField("Character (Dist SO)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "BN JSON이 아닙니다. 저장은 Unity 에셋(Ctrl+S). Save Changes는 아이템/레시피 전용입니다.",
            MessageType.None);

        _characterSerialized.Update();
        EditorGUI.BeginChangeCheck();
        SerializedProperty iterator = _characterSerialized.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == "m_Script" || iterator.name == "_alignment")
                continue;
            EditorGUILayout.PropertyField(iterator, true);
        }

        CharacterAlignmentDrawer.Draw(_characterSerialized.FindProperty("_alignment"));
        if (EditorGUI.EndChangeCheck())
        {
            _characterSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
            InvalidateFilter();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping Asset", GUILayout.Width(100)))
        {
            Selection.activeObject = def;
            EditorGUIUtility.PingObject(def);
        }

        if (GUILayout.Button("Delete Asset", GUILayout.Width(100)))
            DeleteSelectedCharacter();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    bool _foldIdentity = true;
    bool _foldGameDetail;
    bool _foldPresentation;
    bool _foldIcon;
    bool _foldRelations;

    void DrawItemDetail()
    {
        if (_selectedIndex >= _filteredItems.Count) return;
        var item = _filteredItems[_selectedIndex];
        var db = ActiveDb;

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
                EditField("ID", ref item.id);
                EditLocalizedItemName(item.id);
                EditField("Type", ref item.type);
                EditField("Category", ref item.category);
                EditIntField("Weight (g)", ref item.weight_g);
                EditIntField("Volume (ml)", ref item.volume_ml);
            }
            else
            {
                ReadField("ID", item.id);
                EditLocalizedItemName(item.id);
                ReadField("Type", item.type);
                ReadField("Category", item.category);
                ReadField("Weight", $"{item.weight_g} g");
                ReadField("Volume", $"{item.volume_ml} ml");
                if (item.materials is { Count: > 0 })
                    ReadField("Materials", string.Join(", ", item.materials));
                if (!string.IsNullOrEmpty(item.comestible_type))
                    ReadField("Comestible type", item.comestible_type);
            }

            EditorGUI.indentLevel--;
        }

        _foldGameDetail = EditorGUILayout.Foldout(
            _foldGameDetail,
            "Game Detail",
            true,
            EditorStyles.foldoutHeader);
        if (_foldGameDetail)
        {
            EditorGUI.indentLevel++;
            if (IsCustom)
                GameDataEditorDetailDrawers.DrawItemDetailEditable(item, MarkDirty);
            else
            {
                GameDataEditorDetailDrawers.DrawItemDetailReadOnly(item);
                if (item.qualities is { Count: > 0 })
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Qualities", EditorStyles.miniBoldLabel);
                    foreach (var q in item.qualities)
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
            _foldPresentation,
            "Combat Presentation",
            true,
            EditorStyles.foldoutHeader);
        if (_foldPresentation)
        {
            EditorGUI.indentLevel++;
            GameDataWeaponPresentationEditor.DrawSection(item, editable: IsCustom);
            EditorGUI.indentLevel--;
        }

        _foldIcon = EditorGUILayout.Foldout(
            _foldIcon,
            "Icon",
            true,
            EditorStyles.foldoutHeader);
        if (_foldIcon)
        {
            EditorGUI.indentLevel++;
            DrawItemIconSection(item);
            EditorGUI.indentLevel--;
        }

        _foldRelations = EditorGUILayout.Foldout(
            _foldRelations,
            "Recipes / Relations",
            true,
            EditorStyles.foldoutHeader);
        if (_foldRelations)
        {
            EditorGUI.indentLevel++;
            var recipes = db.GetRecipesForResult(item.id);
            if (recipes.Count > 0)
            {
                EditorGUILayout.LabelField("Recipes producing this", EditorStyles.miniBoldLabel);
                foreach (var r in recipes)
                    EditorGUILayout.LabelField($"  {r.id}  [{r.category}]");
            }
            else
            {
                EditorGUILayout.LabelField("(none produce this)", EditorStyles.miniLabel);
            }

            var usedIn = db.GetRecipesUsingIngredient(item.id);
            if (usedIn.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"Used as ingredient ({usedIn.Count})",
                    EditorStyles.miniBoldLabel);
                foreach (var r in usedIn.Take(20))
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
                _customItemsRoot.items.Remove(item);
                RebuildCustomDb();
                _selectedIndex = -1;
            }
        }
        else if (!string.IsNullOrEmpty(item.id))
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

        EditorGUILayout.HelpBox(
            "아이콘은 아이템 JSON이 아닙니다. Sprite 필드는 ItemIconCatalog 오버라이드입니다. 미할당이면 BN 타일셋(MSX++), 그것도 없으면 기본 아이콘입니다.",
            MessageType.None);

        ItemIconCatalog catalog = EnsureIconCatalog();
        if (catalog == null)
        {
            EditorGUILayout.HelpBox($"카탈로그를 만들 수 없습니다: {ItemIconCatalog.AssetPath}", MessageType.Error);
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
            DrawSpritePreview(preview, resolved);
            if (assigned == null && resolved != ItemVisualPresenter.GetDefaultIcon())
                EditorGUILayout.LabelField("Resolved from BN tileset", EditorStyles.miniLabel);
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
            if (resultItem != null)
                ReadField("Result Name", ItemNameTable.Get(resultItem.id, ActiveDisplayLanguage));
            else if (!string.IsNullOrEmpty(recipe.result))
                ReadField("Result Name", ItemNameTable.Get(recipe.result, ActiveDisplayLanguage));
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
        if (!HasUnsavedChanges) return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("unsaved changes", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    // ── CRUD ───────────────────────────────────────────────────

    void AddNewCharacter()
    {
        CharacterDefinition def = CharacterDefinitionCatalog.CreateNew();
        LoadCharacters();
        _tab = Tab.Characters;
        InvalidateFilter();
        RebuildFilterIfNeeded();
        _selectedIndex = _filteredCharacters.IndexOf(def);
        BindCharacterSerialized(def);
        Selection.activeObject = def;
        EditorGUIUtility.PingObject(def);
    }

    void DeleteSelectedCharacter()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _filteredCharacters.Count)
            return;

        CharacterDefinition def = _filteredCharacters[_selectedIndex];
        if (def == null)
            return;

        string path = AssetDatabase.GetAssetPath(def);
        if (string.IsNullOrEmpty(path))
            return;

        if (!EditorUtility.DisplayDialog(
                "Delete Character Definition",
                $"Delete asset '{def.name}'?\n{path}",
                "Delete",
                "Cancel"))
            return;

        BindCharacterSerialized(null);
        AssetDatabase.DeleteAsset(path);
        LoadCharacters();
        _selectedIndex = -1;
    }

    void AddNewItem()
    {
        string id = $"custom_item_{_customItemsRoot.items.Count}";
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
        _customItemsRoot.items.Add(item);
        ItemNameTable.Set(id, ActiveDisplayLanguage, "New Item");
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
        copy.name = string.Empty;
        _customItemsRoot.items.Add(copy);
        DisplayLanguage lang = ActiveDisplayLanguage;
        string display = ItemNameTable.Get(src.id, lang);
        if (!string.IsNullOrEmpty(display) && !display.StartsWith("[Missing:", StringComparison.Ordinal))
            ItemNameTable.Set(copy.id, lang, display);
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

    void SaveAll()
    {
        if (_dirty)
            SaveCustomData();

        if (ItemNameTable.IsGameDirty)
        {
            ItemNameTable.SaveGameOverlay();
            Debug.Log($"[GameDataEditor] Item names saved to {ItemNameTable.GetGameOverlayPath()}");
        }

        AssetDatabase.Refresh();
    }

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
    }

    // ── Field helpers ──────────────────────────────────────────

    void EditLocalizedItemName(string itemId)
    {
        DisplayLanguage lang = ActiveDisplayLanguage;
        string langCode = DisplayLanguageCodes.ToCode(lang);
        string current = ItemNameTable.TryGetRaw(itemId, lang, out string raw)
            ? raw
            : string.Empty;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Name ({langCode})", GUILayout.Width(120));
        string newVal = EditorGUILayout.TextField(current ?? string.Empty);
        EditorGUILayout.EndHorizontal();

        if (newVal != (current ?? string.Empty))
        {
            ItemNameTable.Set(itemId, lang, newVal);
            InvalidateFilter();
        }
    }

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
        EditorGUILayout.SelectableLabel(
            value ?? "—",
            EditorStyles.wordWrappedLabel,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }
}
