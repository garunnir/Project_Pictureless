using System;

namespace IsoTilemap
{
    public readonly struct PresentationQuery
    {
        public Guid? TileId { get; }
        public PresentationConcern? Concern { get; }
        public PresentationSource? Source { get; }

        /// <summary>true면 <see cref="TilePresentationEntryStore.IsSourceEngaged"/>인 Source만.</summary>
        public bool OnlyEngagedSources { get; }

        /// <summary>true면 해당 타일에 Set된 entry만 (TileId 지정 시).</summary>
        public bool OnlyEngagedForTile { get; }

        public PresentationQuery(
            Guid? tileId = null,
            PresentationConcern? concern = null,
            PresentationSource? source = null,
            bool onlyEngagedSources = true,
            bool onlyEngagedForTile = true)
        {
            TileId = tileId;
            Concern = concern;
            Source = source;
            OnlyEngagedSources = onlyEngagedSources;
            OnlyEngagedForTile = onlyEngagedForTile;
        }

        public static PresentationQuery ForTile(Guid tileId, bool onlyEngaged = true) =>
            new PresentationQuery(tileId, onlyEngagedSources: onlyEngaged, onlyEngagedForTile: onlyEngaged);
    }
}
