namespace IsoTilemap
{
    public static class PresentationPriorityTable
    {
        public static int Get(PresentationSource source) =>
            source switch
            {
                PresentationSource.BfsWallOcclusion => 100,
                PresentationSource.ProximitySightLine => 50,
                PresentationSource.BlockingBuildingMinFloor => 80,
                PresentationSource.FloorVisibilityPolicy => 90,
                PresentationSource.Ghost => 10,
                _ => 0,
            };
    }
}
