namespace IsoTilemap
{
    public sealed class MapCollisionServices
    {
        public MapTopologyQuery Query { get; }
        public MapTopologyCollisionResolver CollisionResolver { get; }
        public MapLogicalFloorSupport FloorSupport { get; }
        public MapTopologyLineCast LineCast { get; }
        public MapTopologyDepenetration Depenetration { get; }

        public MapCollisionServices(TileMapCacheHub hub, float cellSize)
        {
            Query = new MapTopologyQuery(hub, cellSize);
            CollisionResolver = new MapTopologyCollisionResolver(Query);
            Depenetration = new MapTopologyDepenetration(Query, CollisionResolver);
            FloorSupport = new MapLogicalFloorSupport(Query, Depenetration);
            LineCast = new MapTopologyLineCast(Query);
        }

        public static MapCollisionServices Create(TileMapCacheHub hub, float cellSize) =>
            new MapCollisionServices(hub, cellSize);
    }
}
