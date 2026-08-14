// ============================================================
// CraftingFavoritesStore — 즐겨찾기 레시피·그리드/리스트 모드 PlayerPrefs SSOT
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CraftingFavoritesStore
{
    public const string FavouriteIdsKey = "Dist.Crafting.FavouriteRecipeIds";
    public const string ViewModeKey = "Dist.Crafting.ViewMode";
    public const string ViewModeGrid = "grid";
    public const string ViewModeList = "list";
    const char IdSeparator = ',';

    readonly HashSet<string> _ids = new(StringComparer.Ordinal);

    public bool IsGridMode { get; private set; } = true;

    public void Load()
    {
        _ids.Clear();
        string raw = PlayerPrefs.GetString(FavouriteIdsKey, string.Empty);
        if (!string.IsNullOrEmpty(raw))
        {
            string[] parts = raw.Split(IdSeparator);
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i];
                if (!string.IsNullOrEmpty(id))
                    _ids.Add(id);
            }
        }

        string mode = PlayerPrefs.GetString(ViewModeKey, ViewModeGrid);
        IsGridMode = !string.Equals(mode, ViewModeList, StringComparison.Ordinal);
    }

    public void Save()
    {
        var ids = new List<string>(_ids.Count);
        foreach (string id in _ids)
            ids.Add(id);

        ids.Sort(StringComparer.Ordinal);
        PlayerPrefs.SetString(FavouriteIdsKey, string.Join(IdSeparator.ToString(), ids));
        PlayerPrefs.SetString(ViewModeKey, IsGridMode ? ViewModeGrid : ViewModeList);
        PlayerPrefs.Save();
    }

    public bool Contains(string recipeId) =>
        !string.IsNullOrEmpty(recipeId) && _ids.Contains(recipeId);

    public bool Toggle(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId))
            return false;

        if (!_ids.Add(recipeId))
            _ids.Remove(recipeId);

        Save();
        return Contains(recipeId);
    }

    public void SetGridMode(bool grid)
    {
        if (IsGridMode == grid)
            return;

        IsGridMode = grid;
        Save();
    }
}
