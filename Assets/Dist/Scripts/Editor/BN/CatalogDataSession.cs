// ============================================================
// CatalogDataSession — Data Definitions Catalog(JSON) 로드·저장 SSOT
// ============================================================

using System.Collections.Generic;
using System.IO;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

public enum CatalogSource
{
    Reference,
    Custom,
}

/// <summary>BN Reference / Custom JSON 공유 세션. Catalog 브라우저만 사용.</summary>
public sealed class CatalogDataSession
{
    public static CatalogDataSession Instance { get; } = new CatalogDataSession();

    GameDatabase _bnDb;
    GameDatabase _customDb;
    ItemsFileRoot _customItemsRoot;
    RecipesFileRoot _customRecipesRoot;
    ItemIconCatalog _iconCatalog;
    TraitIconCatalog _traitIconCatalog;
    LocalizationBundle _bundle;
    bool _dirty;

    public GameDatabase BnDb => _bnDb;
    public GameDatabase CustomDb => _customDb;
    public ItemsFileRoot CustomItemsRoot => _customItemsRoot;
    public RecipesFileRoot CustomRecipesRoot => _customRecipesRoot;
    public bool Dirty => _dirty;
    public bool HasUnsavedChanges => _dirty || ItemNameTable.IsGameDirty;

    public LocalizationBundle Bundle => _bundle;
    public DisplayLanguage ActiveDisplayLanguage =>
        _bundle != null ? _bundle.ActiveLanguage : DisplayLanguage.Ko;

    public GameDatabase GetDb(CatalogSource source) =>
        source == CatalogSource.Reference ? _bnDb : _customDb;

    public void MarkDirty() => _dirty = true;

    public void Reload()
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
        EnsureIconCatalog();
        EnsureTraitIconCatalog();
    }

    public void SaveAll()
    {
        if (_dirty)
            SaveCustomData();

        if (ItemNameTable.IsGameDirty)
        {
            ItemNameTable.SaveGameOverlay();
            Debug.Log($"[DataDefinitions] Item names saved to {ItemNameTable.GetGameOverlayPath()}");
        }

        AssetDatabase.Refresh();
    }

    public void RebuildCustomDb()
    {
        _customDb = new GameDatabase(_customItemsRoot, _customRecipesRoot);
    }

    public ItemIconCatalog EnsureIconCatalog()
    {
        if (_iconCatalog != null)
            return _iconCatalog;

        _iconCatalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(ItemIconCatalog.DefaultAssetPath);
        if (_iconCatalog != null)
        {
            ItemVisualPresenter.BindCatalog(_iconCatalog);
            return _iconCatalog;
        }

        _iconCatalog = DistScriptableObjectEnsure.LoadOrCreate<ItemIconCatalog>(ItemIconCatalog.DefaultAssetPath);
        Sprite fallback = LoadEmptyIconSprite();
        if (fallback != null)
            _iconCatalog.SetDefaultIcon(fallback);
        EditorUtility.SetDirty(_iconCatalog);
        AssetDatabase.SaveAssets();
        ItemVisualPresenter.BindCatalog(_iconCatalog);
        Debug.Log($"[DataDefinitions] Created {ItemIconCatalog.DefaultAssetPath}");
        return _iconCatalog;
    }

    public TraitIconCatalog EnsureTraitIconCatalog()
    {
        if (_traitIconCatalog != null)
            return _traitIconCatalog;

        _traitIconCatalog = AssetDatabase.LoadAssetAtPath<TraitIconCatalog>(TraitIconCatalog.DefaultAssetPath);
        if (_traitIconCatalog != null)
        {
            TraitVisualPresenter.BindCatalog(_traitIconCatalog);
            return _traitIconCatalog;
        }

        _traitIconCatalog = DistScriptableObjectEnsure.LoadOrCreate<TraitIconCatalog>(
            TraitIconCatalog.DefaultAssetPath);
        Sprite fallback = LoadEmptyIconSprite();
        if (fallback != null)
            _traitIconCatalog.SetDefaultIcon(fallback);
        EditorUtility.SetDirty(_traitIconCatalog);
        AssetDatabase.SaveAssets();
        TraitVisualPresenter.BindCatalog(_traitIconCatalog);
        Debug.Log($"[DataDefinitions] Created {TraitIconCatalog.DefaultAssetPath}");
        return _traitIconCatalog;
    }

    public void PingLocalizationBundle()
    {
        if (_bundle == null)
            _bundle = EnsureLocalizationBundle();
        if (_bundle == null)
            return;

        Selection.activeObject = _bundle;
        EditorGUIUtility.PingObject(_bundle);
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
            if (!string.IsNullOrEmpty(item.description))
                ItemNameTable.SeedFromDescriptionIfMissing(item.id, item.description, DisplayLanguage.Ko);
        }
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
        Debug.Log($"[DataDefinitions] Custom data saved to {gamePath}");
    }

    static LocalizationBundle EnsureLocalizationBundle()
    {
        LocalizationBundle bundle =
            AssetDatabase.LoadAssetAtPath<LocalizationBundle>(LocalizationBundle.DefaultAssetPath);
        if (bundle != null)
            return bundle;

        bundle = DistScriptableObjectEnsure.LoadOrCreate<LocalizationBundle>(
            LocalizationBundle.DefaultAssetPath);
        TMP_FontAsset katuri =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DistUiFont.AssetPath);
        if (katuri != null)
        {
            bundle.EditorSetFont(DisplayLanguage.En, katuri);
            bundle.EditorSetFont(DisplayLanguage.Ko, katuri);
        }

        EditorUtility.SetDirty(bundle);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DataDefinitions] Created {LocalizationBundle.DefaultAssetPath}");
        return bundle;
    }

    static Sprite LoadEmptyIconSprite()
    {
        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetsAtPath(ItemVisualPresenter.DefaultIconAssetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
