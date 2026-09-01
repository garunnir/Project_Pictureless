// ============================================================
// PlayerProgressSnapshotPending — 맵 로드 → DistScript 복원 브리지 (Dist.Map)
// ============================================================

using System;

namespace IsoTilemap
{
    /// <summary>
    /// <see cref="MapSaveJsonDto"/>에서 playerProgress를 꺼내 DistScript가 possess 후 복원한다.
    /// </summary>
    public static class PlayerProgressSnapshotPending
    {
        public static bool HasPending { get; private set; }
        public static string PendingJson { get; private set; }

        /// <summary>맵 로드 직후 TileMapManager가 호출.</summary>
        public static Action<MapSaveJsonDto> OnMapDtoLoaded;

        public static void SetFromMapDto(MapSaveJsonDto dto)
        {
            HasPending = dto != null && dto.hasPlayerProgressSnapshot && !string.IsNullOrEmpty(dto.playerProgressJson);
            PendingJson = HasPending ? dto.playerProgressJson : null;
            OnMapDtoLoaded?.Invoke(dto);
        }

        public static bool TryTakePending(out string json)
        {
            json = PendingJson;
            bool had = HasPending;
            HasPending = false;
            PendingJson = null;
            return had && !string.IsNullOrEmpty(json);
        }
    }
}
