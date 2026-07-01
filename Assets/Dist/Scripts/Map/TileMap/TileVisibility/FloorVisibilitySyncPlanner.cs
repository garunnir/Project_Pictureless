// ============================================================
// FloorVisibilitySyncPlanner — ctx diff로 sync 후보 타일 universe 축소
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class FloorVisibilitySyncPlanner
    {
        readonly BuildingGroupRegistry _buildingRegistry;
        readonly TileMapCacheHub _hub;
        readonly HashSet<int> _blockingScratch = new();
        readonly HashSet<(int x, int z, int y)> _peekScratch = new();

        public FloorVisibilitySyncPlanner(
            BuildingGroupRegistry buildingRegistry,
            TileMapCacheHub hub)
        {
            _buildingRegistry = buildingRegistry;
            _hub = hub;
        }

        public void BuildCandidateTileIds(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            bool hasLastCtx,
            HashSet<Guid> structuralHidden,
            HashSet<Guid> candidates)
        {
            candidates.Clear();

            if (!hasLastCtx)
            {
                AddModeTransitionCandidates(ctx, in lastCtx, hasLastCtx: false, structuralHidden, candidates);
                return;
            }

            if (ctx.IsPlayerOutdoor != lastCtx.IsPlayerOutdoor)
            {
                AddModeTransitionCandidates(ctx, in lastCtx, hasLastCtx: true, structuralHidden, candidates);
                return;
            }

            if (ctx.IsPlayerOutdoor)
                AddOutdoorBlockingCandidates(in ctx, in lastCtx, candidates);
            else
                AddIndoorCandidates(in ctx, in lastCtx, candidates);
        }

        void AddOutdoorBlockingCandidates(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            HashSet<Guid> candidates)
        {
            CollectBlockingSymmetricDiff(in ctx, in lastCtx, _blockingScratch);
            AppendTilesForBuildings(_blockingScratch, candidates);
        }

        void AddIndoorCandidates(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            HashSet<Guid> candidates)
        {
            if (ctx.PlayerBuildingId > 0)
                AppendTilesForBuilding(ctx.PlayerBuildingId, candidates);

            if (lastCtx.PlayerBuildingId > 0 && lastCtx.PlayerBuildingId != ctx.PlayerBuildingId)
                AppendTilesForBuilding(lastCtx.PlayerBuildingId, candidates);

            CollectPeekSymmetricDiff(in ctx, in lastCtx, _peekScratch);
            AppendTilesForPeekCells(_peekScratch, candidates);
        }

        void AddModeTransitionCandidates(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            bool hasLastCtx,
            HashSet<Guid> structuralHidden,
            HashSet<Guid> candidates)
        {
            if (structuralHidden != null)
            {
                foreach (Guid tileId in structuralHidden)
                    candidates.Add(tileId);
            }

            if (hasLastCtx)
            {
                AppendTilesForBuildings(lastCtx.PlayerBlockingBuildingIds, candidates);
                if (lastCtx.PlayerBuildingId > 0)
                    AppendTilesForBuilding(lastCtx.PlayerBuildingId, candidates);
                AppendTilesForPeekCells(lastCtx.VisibleBelowCells, candidates);
            }

            AppendTilesForBuildings(ctx.PlayerBlockingBuildingIds, candidates);
            if (ctx.PlayerBuildingId > 0)
                AppendTilesForBuilding(ctx.PlayerBuildingId, candidates);
            AppendTilesForPeekCells(ctx.VisibleBelowCells, candidates);
        }

        static void CollectBlockingSymmetricDiff(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            HashSet<int> output)
        {
            output.Clear();
            AddSymmetricDiff(ctx.PlayerBlockingBuildingIds, lastCtx.PlayerBlockingBuildingIds, output);
        }

        static void CollectPeekSymmetricDiff(
            in FloorVisibilityContext ctx,
            in FloorVisibilityContext lastCtx,
            HashSet<(int x, int z, int y)> output)
        {
            output.Clear();
            AddSymmetricDiff(ctx.VisibleBelowCells, lastCtx.VisibleBelowCells, output);
        }

        static void AddSymmetricDiff(
            HashSet<int> current,
            HashSet<int> previous,
            HashSet<int> output)
        {
            if (current != null)
            {
                foreach (int id in current)
                {
                    if (previous == null || !previous.Contains(id))
                        output.Add(id);
                }
            }

            if (previous != null)
            {
                foreach (int id in previous)
                {
                    if (current == null || !current.Contains(id))
                        output.Add(id);
                }
            }
        }

        static void AddSymmetricDiff(
            HashSet<(int x, int z, int y)> current,
            HashSet<(int x, int z, int y)> previous,
            HashSet<(int x, int z, int y)> output)
        {
            if (current != null)
            {
                foreach (var cell in current)
                {
                    if (previous == null || !previous.Contains(cell))
                        output.Add(cell);
                }
            }

            if (previous != null)
            {
                foreach (var cell in previous)
                {
                    if (current == null || !current.Contains(cell))
                        output.Add(cell);
                }
            }
        }

        void AppendTilesForBuildings(HashSet<int> buildingIds, HashSet<Guid> candidates)
        {
            if (buildingIds == null || buildingIds.Count == 0 || _buildingRegistry == null)
                return;

            foreach (int buildingId in buildingIds)
                AppendTilesForBuilding(buildingId, candidates);
        }

        void AppendTilesForBuilding(int buildingId, HashSet<Guid> candidates)
        {
            if (_buildingRegistry == null || buildingId <= 0)
                return;

            _buildingRegistry.EnumerateTilesForBuilding(buildingId, tileId => candidates.Add(tileId));
        }

        void AppendTilesForPeekCells(
            HashSet<(int x, int z, int y)> peekCells,
            HashSet<Guid> candidates)
        {
            if (peekCells == null || peekCells.Count == 0 || _hub == null)
                return;

            foreach (var (x, z, y) in peekCells)
            {
                if (!_hub.TryGetCellTiles(x, z, y, out var tiles))
                    continue;

                for (int i = 0; i < tiles.Count; i++)
                    candidates.Add(tiles[i].tileDefId);
            }
        }
    }
}
