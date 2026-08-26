// ============================================================
// CookItemContextAction — cooks_like / smoking_result execute
// ============================================================

public sealed class CookItemContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;
    readonly InventorySession _session;
    readonly string _resultId;
    readonly bool _smoke;

    public CookItemContextAction(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        string resultId,
        bool smoke)
    {
        _stack = stack;
        _container = container;
        _session = session;
        _resultId = resultId;
        _smoke = smoke;
    }

    public string GetDisabledReason()
    {
        ICraftingEnvironment env = CraftingEnvironment.Active;
        bool hasFire = env != null && env.HasPseudoTool(CraftingPseudoIds.Fire);
        if (!hasFire)
            hasFire = HasInventoryHeatTool();

        if (_smoke)
        {
            bool hasApparatus = env != null && env.HasPseudoTool(CraftingPseudoIds.Apparatus);
            if (!hasApparatus)
                return ItemContextMenuLabels.SmokeBlocked;
            if (!hasFire)
                return ItemContextMenuLabels.CookBlocked;
            return null;
        }

        return hasFire ? null : ItemContextMenuLabels.CookBlocked;
    }

    public void Execute()
    {
        CraftingService.TryTransformComestible(
            _stack,
            _container,
            _session,
            _resultId,
            requireFire: true,
            requireApparatus: _smoke);
    }

    bool HasInventoryHeatTool()
    {
        if (_container == null)
            return false;

        for (int i = 0; i < CraftingPseudoIds.HeatToolIds.Length; i++)
        {
            string id = CraftingPseudoIds.HeatToolIds[i];
            if (_container.CountItem(id) > 0)
                return true;
        }

        return false;
    }
}
