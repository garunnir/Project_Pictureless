namespace IsoTilemap
{
    public sealed class MapCollisionServices
    {
        public MapTopologyQuery Query { get; }
        public FloorBandResolver BandResolver { get; }
        public MapTopologyCollisionResolver CollisionResolver { get; }
        public MapLogicalFloorSupport FloorSupport { get; }
        public MapTopologyLineCast LineCast { get; }

        public MapCollisionServices(TileMapCacheHub hub, float cellSize, FloorBandResolver bandResolver)
        {
            Query = new MapTopologyQuery(hub, cellSize);
            BandResolver = bandResolver;
            CollisionResolver = new MapTopologyCollisionResolver(Query);
            FloorSupport = new MapLogicalFloorSupport(Query, bandResolver);
            LineCast = new MapTopologyLineCast(Query);
        }
    }
}
