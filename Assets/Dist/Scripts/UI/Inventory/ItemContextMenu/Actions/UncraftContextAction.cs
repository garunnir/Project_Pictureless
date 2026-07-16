// ============================================================
// UncraftContextAction — 분해 리프 실행
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public sealed class UncraftContextAction : IContextMenuAction
{
    readonly RecipeData _recipe;
    readonly ItemStack _stack;
    readonly InventoryContainer _container;
    readonly InventorySession _session;

    public UncraftContextAction(
        RecipeData recipe,
        ItemStack stack,
        InventoryContainer container,
        InventorySession session)
    {
        _recipe = recipe;
        _stack = stack;
        _container = container;
        _session = session;
    }

    public string GetDisabledReason()
    {
        if (!CraftingService.CanUncraft(_recipe, _container))
            return ItemContextMenuLabels.UncraftBlocked;

        return null;
    }

    public void Execute()
    {
        if (_container == null || _session == null || _recipe == null || _stack == null)
            return;

        CraftingService.TryUncraft(_recipe, _stack, _container, _session);
    }
}
