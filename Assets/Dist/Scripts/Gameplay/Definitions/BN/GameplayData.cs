// ============================================================
// GameplayData — 게임플레이 데이터 SSOT (커스텀 우선 → 참조 fallback)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public static class GameplayData
{
    static IPlayerStats _stats;

    /// <summary>
    /// 플레이어 스킬/스탯 SSOT.
    /// 미주입 시에는 DefaultPlayerStats(인메모리)로 동작합니다.
    /// </summary>
    public static IPlayerStats Stats
    {
        get
        {
            if (_stats == null)
                _stats = new DefaultPlayerStats();
            return _stats;
        }
        set => _stats = value;
    }

    /// <summary>프로젝트 커스텀 데이터 (편집 가능)</summary>
    public static GameDatabase GameItems => GameDataLoader.GameData;

    /// <summary>참조 데이터 (CC BY-SA 3.0, 읽기 전용)</summary>
    public static GameDatabase RefData => GameDataLoader.RefData;

    // ── SSOT resolve: 커스텀 → 참조 순서로 검색 ─────────────

    public static ItemData GetItem(string id)
    {
        return GameItems?.GetItem(id) ?? RefData?.GetItem(id);
    }

    public static ContainerData GetContainer(string id)
    {
        return GameItems?.GetContainer(id) ?? RefData?.GetContainer(id);
    }

    public static List<RecipeData> GetRecipesForResult(string resultId)
    {
        var list = GameItems?.GetRecipesForResult(resultId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetRecipesForResult(resultId) ?? _emptyRecipes;
    }

    public static List<RecipeData> GetRecipesUsingIngredient(string itemId)
    {
        var list = GameItems?.GetRecipesUsingIngredient(itemId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetRecipesUsingIngredient(itemId) ?? _emptyRecipes;
    }

    public static List<RecipeData> GetUncraftForResult(string resultId)
    {
        var list = GameItems?.GetUncraftForResult(resultId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetUncraftForResult(resultId) ?? _emptyRecipes;
    }

    public static void ClearCache()
    {
        GameDataLoader.Unload();
    }

    static readonly List<RecipeData> _emptyRecipes = new(0);
}
