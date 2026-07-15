// ============================================================
// CraftingService — 합성 가능 여부 확인 + 재료 소비 + 결과 생성
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public static class CraftingService
{
    public static bool CanCraft(RecipeData recipe, InventoryContainer container)
    {
        if (recipe == null || container == null || string.IsNullOrEmpty(recipe.result))
            return false;

        if (recipe.components == null)
            return true;

        for (int i = 0; i < recipe.components.Count; i++)
        {
            ComponentSlot slot = recipe.components[i];
            if (slot.alternatives == null || slot.alternatives.Count == 0)
                return false;

            bool slotSatisfied = false;
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                ComponentAlt alt = slot.alternatives[j];
                if (container.CountItem(alt.item) >= alt.count)
                {
                    slotSatisfied = true;
                    break;
                }
            }

            if (!slotSatisfied)
                return false;
        }

        return true;
    }

    public static bool TryCraft(
        RecipeData recipe,
        InventoryContainer container,
        InventorySession session)
    {
        if (!CanCraft(recipe, container))
            return false;

        for (int i = 0; i < recipe.components.Count; i++)
        {
            ComponentSlot slot = recipe.components[i];
            for (int j = 0; j < slot.alternatives.Count; j++)
            {
                ComponentAlt alt = slot.alternatives[j];
                if (container.CountItem(alt.item) >= alt.count)
                {
                    container.RemoveItem(alt.item, alt.count);
                    break;
                }
            }
        }

        int resultCount = recipe.result_count > 0 ? recipe.result_count : 1;
        container.AddItem(recipe.result, resultCount);

        session?.NotifyExternalStacksChanged();
        return true;
    }
}
