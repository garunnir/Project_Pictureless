// ============================================================
// MulticookerContextAction — opens crafting window in multicooker filter
// ============================================================

using UnityEngine;

public sealed class MulticookerContextAction : IContextMenuAction
{
    public string GetDisabledReason() => null;

    public void Execute()
    {
        UICraftingController controller = Object.FindFirstObjectByType<UICraftingController>();
        if (controller == null)
        {
            Debug.LogWarning("[MulticookerContextAction] UICraftingController missing.");
            return;
        }

        controller.OpenMulticooker();
    }
}
