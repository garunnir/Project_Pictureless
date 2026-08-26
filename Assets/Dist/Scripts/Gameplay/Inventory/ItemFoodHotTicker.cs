// ============================================================
// ItemFoodHotTicker — clears IsHot after HotUntilWorldMinute
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemFoodHotTicker : MonoBehaviour
{
    [SerializeField] PlayerInventoryRuntime _runtime;

    void OnEnable()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
            clock.MinuteChanged += OnMinute;
    }

    void OnDisable()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
            clock.MinuteChanged -= OnMinute;
    }

    void OnMinute()
    {
        int now = CraftingWorldTime.AbsoluteWorldMinute;
        PlayerInventoryRuntime runtime = _runtime != null ? _runtime : PlayerInventoryRuntime.Active;
        if (runtime?.Session == null)
            return;

        var containers = runtime.Session.GetSidebarContainers();
        if (containers == null)
            return;

        for (int c = 0; c < containers.Count; c++)
        {
            var stacks = containers[c]?.Stacks;
            if (stacks == null)
                continue;
            for (int s = 0; s < stacks.Count; s++)
                stacks[s]?.Instance?.TickHotAt(now);
        }
    }
}
