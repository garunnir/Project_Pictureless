// ============================================================
// UncraftContextContributor — 분해 루트/서브트리
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class UncraftContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item == null || roots == null)
            return;

        List<RecipeData> recipes = GameplayData.GetUncraftForResult(stack.ItemId);
        if (recipes == null || recipes.Count == 0)
            return;

        var leaves = new List<ContextMenuEntry>();
        var seen = new HashSet<string>();
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (recipe == null || string.IsNullOrEmpty(recipe.result) || string.IsNullOrEmpty(recipe.id))
                continue;
            if (!seen.Add(recipe.id))
                continue;

            leaves.Add(ContextMenuEntry.Leaf(
                $"uncraft:{recipe.id}",
                RecipeContextMenuText.FormatResultLabel(recipe),
                new UncraftContextAction(recipe, stack, container, session)));
        }

        if (leaves.Count == 0)
            return;

        leaves.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));

        if (leaves.Count == 1)
        {
            ContextMenuEntry only = leaves[0];
            roots.Add(ContextMenuEntry.Leaf(
                only.Id,
                ItemContextMenuLabels.UncraftPrefix + only.Label,
                only.Action));
            return;
        }

        roots.Add(ContextMenuEntry.Group("uncraft", ItemContextMenuLabels.Uncraft, leaves));
    }
}
