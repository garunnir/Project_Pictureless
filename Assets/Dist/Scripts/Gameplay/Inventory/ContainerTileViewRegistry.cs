// ============================================================
// ContainerTileViewRegistry — 월드 컨테이너 TileView 등록 (Applier 조회용)
// ============================================================

using System;
using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

public sealed class ContainerTileViewRegistry
{
    public static ContainerTileViewRegistry Instance { get; } = new();

    readonly Dictionary<Guid, TileView> _viewsByTileId = new();
    readonly Dictionary<string, Guid> _tileIdByContainerInstanceId = new();
    readonly Dictionary<Guid, string> _containerInstanceIdByTileId = new();

    ContainerTileViewRegistry() { }

    public void Register(Guid presentationTileId, TileView view, string containerInstanceId)
    {
        if (presentationTileId == Guid.Empty || view == null || string.IsNullOrEmpty(containerInstanceId))
            return;

        Unregister(presentationTileId, containerInstanceId);

        _viewsByTileId[presentationTileId] = view;
        _tileIdByContainerInstanceId[containerInstanceId] = presentationTileId;
        _containerInstanceIdByTileId[presentationTileId] = containerInstanceId;
    }

    public void Unregister(Guid presentationTileId, string containerInstanceId)
    {
        if (presentationTileId != Guid.Empty)
        {
            _viewsByTileId.Remove(presentationTileId);
            _containerInstanceIdByTileId.Remove(presentationTileId);
        }

        if (!string.IsNullOrEmpty(containerInstanceId))
            _tileIdByContainerInstanceId.Remove(containerInstanceId);
    }

    public bool TryGetView(Guid presentationTileId, out TileView view) =>
        _viewsByTileId.TryGetValue(presentationTileId, out view);

    public bool TryGetPresentationTileId(string containerInstanceId, out Guid presentationTileId)
    {
        presentationTileId = Guid.Empty;
        if (string.IsNullOrEmpty(containerInstanceId))
            return false;

        return _tileIdByContainerInstanceId.TryGetValue(containerInstanceId, out presentationTileId);
    }

    public bool TryGetViewByContainerInstanceId(string containerInstanceId, out TileView view)
    {
        view = null;
        return TryGetPresentationTileId(containerInstanceId, out Guid tileId) &&
               TryGetView(tileId, out view) &&
               view != null;
    }

    public void CollectTileIds(List<Guid> into)
    {
        if (into == null)
            return;

        foreach (Guid tileId in _viewsByTileId.Keys)
            into.Add(tileId);
    }
}
