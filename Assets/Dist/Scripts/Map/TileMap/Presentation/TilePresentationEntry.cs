using System;

namespace IsoTilemap
{
    public readonly struct TilePresentationEntry : IEquatable<TilePresentationEntry>
    {
        public PresentationConcern Concern { get; }
        public PresentationSource Source { get; }
        public Guid TileId { get; }
        public float Scalar01 { get; }
        public int Priority { get; }

        public TilePresentationEntry(
            PresentationConcern concern,
            PresentationSource source,
            Guid tileId,
            float scalar01,
            int priority)
        {
            Concern = concern;
            Source = source;
            TileId = tileId;
            Scalar01 = scalar01;
            Priority = priority;
        }

        public bool Equals(TilePresentationEntry other) =>
            Concern == other.Concern &&
            Source == other.Source &&
            TileId.Equals(other.TileId) &&
            Scalar01.Equals(other.Scalar01) &&
            Priority == other.Priority;

        public override bool Equals(object obj) => obj is TilePresentationEntry other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Concern, Source, TileId, Scalar01, Priority);
    }
}
