// ============================================================
// InventoryStacksChangeSet — Session 스택 변경 알림 payload
// ============================================================

using System.Collections.Generic;

public sealed class InventoryStacksChangeSet
{
    static readonly InventoryContainer[] EmptyContainers = System.Array.Empty<InventoryContainer>();

    readonly InventoryContainer[] _changed;
    readonly HashSet<InventoryContainer> _changedSet;

    InventoryStacksChangeSet(InventoryContainer[] changed, bool sidebarAffected, bool fullRefresh)
    {
        _changed = changed ?? EmptyContainers;
        SidebarAffected = sidebarAffected;
        FullRefresh = fullRefresh;

        if (_changed.Length == 0)
            _changedSet = null;
        else
        {
            _changedSet = new HashSet<InventoryContainer>(_changed);
        }
    }

    public bool SidebarAffected { get; }
    public bool FullRefresh { get; }
    public IReadOnlyList<InventoryContainer> ChangedContainers => _changed;

    public static InventoryStacksChangeSet Full { get; } =
        new InventoryStacksChangeSet(EmptyContainers, sidebarAffected: true, fullRefresh: true);

    public static InventoryStacksChangeSet Create(
        IReadOnlyList<InventoryContainer> changed,
        bool sidebarAffected)
    {
        if (changed == null || changed.Count == 0)
            return new InventoryStacksChangeSet(EmptyContainers, sidebarAffected, fullRefresh: false);

        var copy = new InventoryContainer[changed.Count];
        for (int i = 0; i < changed.Count; i++)
            copy[i] = changed[i];

        return new InventoryStacksChangeSet(copy, sidebarAffected, fullRefresh: false);
    }

    public bool Contains(InventoryContainer container) =>
        container != null && _changedSet != null && _changedSet.Contains(container);

    public bool ContainsInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId) || _changed.Length == 0)
            return false;

        for (int i = 0; i < _changed.Length; i++)
        {
            InventoryContainer container = _changed[i];
            if (container != null && container.InstanceId == instanceId)
                return true;
        }

        return false;
    }
}
