namespace IsoTilemap
{
    public static class PresentationEntryQueries
    {
        const float GhostEpsilon = 1e-4f;
        const float OcclusionEpsilon = 1e-4f;

        public static float ResolveCharacterOcclusion(
            System.Guid tileId,
            TilePresentationEntryStore store,
            TileMapModel model)
        {
            float bestScalar = 0f;
            int bestPriority = int.MinValue;
            bool hasCandidate = false;

            if (store.TryGetEngagedEntry(
                    tileId,
                    PresentationConcern.CharacterOcclusion,
                    PresentationSource.BfsWallOcclusion,
                    out TilePresentationEntry bfs))
            {
                ConsiderCandidate(bfs.Scalar01, bfs.Priority, ref bestScalar, ref bestPriority, ref hasCandidate);
            }
            else if (model.TryGetTileOcclusionPresentation(tileId, out float modelBfs))
            {
                int bfsPriority = PresentationPriorityTable.Get(PresentationSource.BfsWallOcclusion);
                ConsiderCandidate(modelBfs, bfsPriority, ref bestScalar, ref bestPriority, ref hasCandidate);
            }

            if (store.TryGetEngagedEntry(
                    tileId,
                    PresentationConcern.CharacterOcclusion,
                    PresentationSource.ProximitySightLine,
                    out TilePresentationEntry prox))
            {
                ConsiderCandidate(prox.Scalar01, prox.Priority, ref bestScalar, ref bestPriority, ref hasCandidate);
            }

            return hasCandidate ? bestScalar : 0f;
        }

        static void ConsiderCandidate(
            float scalar01,
            int priority,
            ref float bestScalar,
            ref int bestPriority,
            ref bool hasCandidate)
        {
            if (!hasCandidate || priority > bestPriority)
            {
                bestScalar = scalar01;
                bestPriority = priority;
                hasCandidate = true;
            }
        }

        public static bool ResolveGhosted(System.Guid tileId, TilePresentationEntryStore store)
        {
            if (!store.TryGetEngagedEntry(
                    tileId,
                    PresentationConcern.GhostAmount,
                    PresentationSource.Ghost,
                    out TilePresentationEntry entry))
                return false;

            return entry.Scalar01 > GhostEpsilon;
        }

        public static bool ResolveSightLineBuildingHidden(System.Guid tileId, TilePresentationEntryStore store)
        {
            if (!store.TryGetEngagedEntry(
                    tileId,
                    PresentationConcern.SightLineBuildingHidden,
                    PresentationSource.BlockingBuildingMinFloor,
                    out TilePresentationEntry entry))
                return false;

            return entry.Scalar01 > OcclusionEpsilon;
        }
    }
}
