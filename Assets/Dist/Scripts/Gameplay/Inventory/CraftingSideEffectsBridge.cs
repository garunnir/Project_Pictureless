// ============================================================
// CraftingSideEffectsBridge — activity fatigue + craft morale
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CraftingSideEffectsBridge : MonoBehaviour
{
    void OnEnable() => CraftingService.CraftCompletedSideEffects += OnCraftCompleted;
    void OnDisable() => CraftingService.CraftCompletedSideEffects -= OnCraftCompleted;

    static void OnCraftCompleted(RecipeData recipe)
    {
        if (recipe == null)
            return;

        ApplyActivityFatigue(recipe.activity_level);
        if (recipe.morale_modifier != 0)
            CharacterMoodHost.Active?.AddMemory(ThoughtId.Crafted, recipe.morale_modifier);
    }

    static void ApplyActivityFatigue(string activityLevel)
    {
        if (string.IsNullOrEmpty(activityLevel))
            return;

        PlayerNeedsHost host = PlayerNeedsHost.Active;
        if (host == null)
            return;

        float add = ResolveFatigueAdd(activityLevel);
        if (add <= 0f)
            return;
        host.SetFatigue01(host.Fatigue01 + add);
    }

    static float ResolveFatigueAdd(string activityLevel)
    {
        string level = activityLevel.Trim().ToLowerInvariant();
        if (level.Contains("extreme") || level.Contains("extra"))
            return 0.08f;
        if (level.Contains("active") || level.Contains("hard"))
            return 0.05f;
        if (level.Contains("moderate") || level.Contains("brisk"))
            return 0.03f;
        if (level.Contains("light") || level.Contains("slow"))
            return 0.015f;
        return 0.02f;
    }
}
