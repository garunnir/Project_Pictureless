// ============================================================
// ContainerTileViewRegistryBridge — 컨테이너 TileView registry를 Map에 노출
// ============================================================

using System;
using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ContainerTileViewRegistryBridge : MonoBehaviour, IExternalTileViewRegistry
{
    public bool TryGetView(Guid tileId, out TileView view) =>
        ContainerTileViewRegistry.Instance.TryGetView(tileId, out view);

    public void CollectSpawnedTileIds(List<Guid> into) =>
        ContainerTileViewRegistry.Instance.CollectTileIds(into);
}
