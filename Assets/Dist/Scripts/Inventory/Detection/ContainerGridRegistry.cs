// ============================================================
// ContainerGridRegistry — 그리드 셀 기준 컨테이너 Provider 인덱스
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public sealed class ContainerGridRegistry
{
    public static ContainerGridRegistry Instance { get; } = new();

    readonly Dictionary<Vector3Int, List<IInventoryContainerProvider>> _byCell = new();
    readonly Dictionary<IInventoryContainerProvider, Vector3Int> _cellByProvider = new();

    ContainerGridRegistry() { }

    public void Register(IInventoryContainerProvider provider)
    {
        if (provider == null)
            return;

        Unregister(provider);

        Vector3Int cell = provider.GridPosition;
        if (!_byCell.TryGetValue(cell, out List<IInventoryContainerProvider> providers))
        {
            providers = new List<IInventoryContainerProvider>(1);
            _byCell[cell] = providers;
        }

        providers.Add(provider);
        _cellByProvider[provider] = cell;
    }

    public void Unregister(IInventoryContainerProvider provider)
    {
        if (provider == null)
            return;

        if (!_cellByProvider.TryGetValue(provider, out Vector3Int cell))
            return;

        _cellByProvider.Remove(provider);

        if (!_byCell.TryGetValue(cell, out List<IInventoryContainerProvider> providers))
            return;

        providers.Remove(provider);
        if (providers.Count == 0)
            _byCell.Remove(cell);
    }

    public void UpdateCell(IInventoryContainerProvider provider, Vector3Int oldCell, Vector3Int newCell)
    {
        if (provider == null || oldCell == newCell)
            return;

        Unregister(provider);
        Register(provider);
    }

    public void CollectAround(
        Vector3Int center,
        int radiusCells,
        List<IInventoryContainerProvider> results,
        bool sameFloorOnly = true,
        int verticalToleranceCells = 0)
    {
        if (results == null)
            return;

        results.Clear();

        if (radiusCells < 0)
            radiusCells = 0;

        int yMin = sameFloorOnly ? center.y : center.y - verticalToleranceCells;
        int yMax = sameFloorOnly ? center.y : center.y + verticalToleranceCells;

        for (int x = center.x - radiusCells; x <= center.x + radiusCells; x++)
        {
            for (int z = center.z - radiusCells; z <= center.z + radiusCells; z++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    var cell = new Vector3Int(x, y, z);
                    if (!_byCell.TryGetValue(cell, out List<IInventoryContainerProvider> providers))
                        continue;

                    for (int i = 0; i < providers.Count; i++)
                    {
                        IInventoryContainerProvider provider = providers[i];
                        if (provider != null && !results.Contains(provider))
                            results.Add(provider);
                    }
                }
            }
        }
    }
}
