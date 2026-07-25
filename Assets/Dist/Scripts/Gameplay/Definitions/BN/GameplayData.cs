// ============================================================
// GameplayData ? ????? ??? SSOT (??? ?? ? ?? fallback)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class GameplayData
{
    static IPlayerStats _stats;
    static ICharacterBody _body;
    static IPlayerVitals _vitals;
    static DefaultCharacterDefeat _defeat;

    /// <summary>
    /// ???? ??/?? ?? ?? (???? ??; NPC? ?? ICharacterSkills).
    /// </summary>
    public static IPlayerStats Stats
    {
        get
        {
            if (_stats == null)
                _stats = new DefaultPlayerStats();
            return _stats;
        }
        set
        {
            _stats = value;
            InvalidateDefeat();
        }
    }

    /// <summary>?? ?? API. Stats? DefaultPlayerStats? ?? ??.</summary>
    public static ICharacterSkills CharacterSkills =>
        Stats is DefaultPlayerStats dps ? dps.Skills : null;

    /// <summary>
    /// ?? ??? ?? SSOT.
    /// </summary>
    public static ICharacterBody Body
    {
        get
        {
            if (_body == null)
            {
                _body = CharacterBody.CreateHumanDefault(Stats.GetStat(AttributeIds.Str));
                if (_stats is DefaultPlayerStats dps)
                    dps.BindBody(_body);
                InvalidateDefeat();
            }

            return _body;
        }
        set
        {
            _body = value;
            if (_stats is DefaultPlayerStats dps)
                dps.BindBody(_body);
            InvalidateDefeat();
        }
    }

    /// <summary>
    /// ?? ???(??/??/????) SSOT.
    /// </summary>
    public static IPlayerVitals Vitals
    {
        get
        {
            if (_vitals == null)
                _vitals = new DefaultPlayerVitals();
            return _vitals;
        }
        set => _vitals = value;
    }

    /// <summary>
    /// ???? ?? ??/?? ?? (Body ? Skills).
    /// </summary>
    public static ICharacterDefeat Defeat
    {
        get
        {
            if (_defeat == null)
                _defeat = new DefaultCharacterDefeat(Body, CharacterSkills);
            return _defeat;
        }
        set
        {
            InvalidateDefeat();
            if (value is DefaultCharacterDefeat concrete)
                _defeat = concrete;
            else if (value != null)
                Debug.LogWarning("[GameplayData] Defeat setter expects DefaultCharacterDefeat; ignored.");
        }
    }

    /// <summary>???? ??? ??? (?? ??)</summary>
    public static GameDatabase GameItems => GameDataLoader.GameData;

    /// <summary>?? ??? (CC BY-SA 3.0, ?? ??)</summary>
    public static GameDatabase RefData => GameDataLoader.RefData;

    public static ItemData GetItem(string id)
    {
        return GameItems?.GetItem(id) ?? RefData?.GetItem(id);
    }

    public static ContainerData GetContainer(string id)
    {
        return GameItems?.GetContainer(id) ?? RefData?.GetContainer(id);
    }

    public static MaterialData GetMaterial(string id)
    {
        return GameItems?.GetMaterial(id) ?? RefData?.GetMaterial(id);
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

    static void InvalidateDefeat()
    {
        _defeat?.Dispose();
        _defeat = null;
    }

    static readonly List<RecipeData> _emptyRecipes = new(0);
}
