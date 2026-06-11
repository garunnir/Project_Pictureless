namespace IsoTilemap
{
    /// <summary>표현 entry를 쓰는 제공자. 같은 <see cref="PresentationConcern"/> 내 우선순위는 <see cref="PresentationPriorityTable"/>.</summary>
    public enum PresentationSource
    {
        BfsWallOcclusion,
        ProximitySightLine,
        Ghost,
        BlockingBuildingMinFloor,
        FloorVisibilityPolicy,
    }
}
