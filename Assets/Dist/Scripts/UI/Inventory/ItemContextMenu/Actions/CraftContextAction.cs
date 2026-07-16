// ============================================================
// CraftContextAction — 제작 리프 실행
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public sealed class CraftContextAction : IContextMenuAction
{
    readonly RecipeData _recipe;
    readonly InventoryContainer _container;
    readonly InventorySession _session;

    public CraftContextAction(RecipeData recipe, InventoryContainer container, InventorySession session)
    {
        _recipe = recipe;
        _container = container;
        _session = session;
    }

    public string GetDisabledReason()
    {
        string knowledge = RecipeKnowledge.GetFailureReason(_recipe, _container);
        if (!string.IsNullOrEmpty(knowledge))
            return knowledge;

        if (!CraftingService.CanCraft(_recipe, _container))
            return ItemContextMenuLabels.CraftBlocked;

        return null;
    }

    public void Execute()
    {
        if (_container == null || _session == null || _recipe == null)
            return;

        CraftingService.TryCraft(_recipe, _container, _session);
    }
}
