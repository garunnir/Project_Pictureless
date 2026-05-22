// ============================================================
// FloorRoomCache — band별 연결 바닥 방 geometry(visited) 캐시 (단일 진실원)
// ============================================================
// 계약:
// - 캐시 히트 = FloorRoomFloodFill 재실행만 생략. 소비자는 반환된 Visited/EmptyDiscovered를 사용.
// - 금지: 캐시 존재 여부로 오클루전 rebuild·ApplyTiles·층 sync 전체를 스킵하지 않음.
// - InvalidateAll = 맵 topology 변경 시에만. 플레이어 셀 이동만으로는 무효화하지 않음.
using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>BFS 옵션 프로필. 캐시 키에 포함되며 소비자별 계약을 분리합니다.</summary>
    public enum FloorRoomBfsProfile
    {
        /// <summary>벽 오클루전: <c>collectEmptyNeighbors: false</c></summary>
        Occlusion,

        /// <summary>층 가시성(구멍·아래층): <c>collectEmptyNeighbors: true</c></summary>
        Visibility,
    }

    public sealed class FloorRoomCache
    {
        private readonly FloorMapIndex _index;
        private readonly Dictionary<(int band, FloorRoomBfsProfile profile), List<FloorBfsResult>> _roomsByBandProfile =
            new();

        public FloorRoomCache(FloorMapIndex index) => _index = index;

        public FloorMapIndex Index => _index;

        /// <summary>맵 topology 변경 시 geometry 캐시 전체 무효화.</summary>
        public void InvalidateAll() => _roomsByBandProfile.Clear();

        public FloorBfsResult GetOrCompute(int band, int x, int z, FloorRoomBfsProfile profile)
        {
            var key = (band, profile);
            if (_roomsByBandProfile.TryGetValue(key, out var rooms))
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].Visited.Contains((x, z)))
                        return rooms[i];
                }
            }
            else
            {
                rooms = new List<FloorBfsResult>();
                _roomsByBandProfile[key] = rooms;
            }

            bool collectEmpty = profile == FloorRoomBfsProfile.Visibility;
            FloorBfsResult result = FloorRoomFloodFill.Run(_index, band, x, z, collectEmpty);
            rooms.Add(result);
            return result;
        }

        public HashSet<(int x, int z)> GetOrComputeVisited(
            int band, int x, int z, FloorRoomBfsProfile profile) =>
            GetOrCompute(band, x, z, profile).Visited;
    }
}
