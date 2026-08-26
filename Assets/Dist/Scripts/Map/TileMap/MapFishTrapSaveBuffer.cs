// ============================================================
// MapFishTrapSaveBuffer — 통발 tiles[] DTO 왕복 (Dist.Map ↔ Dist.Map.Fish)
// ============================================================

using System.Collections.Generic;

namespace IsoTilemap
{
    public static class MapFishTrapSaveBuffer
    {
        static readonly List<TileSaveData> LoadPending = new();
        static readonly List<TileSaveData> SaveScratch = new();

        public static bool IsTrapOnlyRecord(TileSaveData td) =>
            td != null &&
            string.IsNullOrEmpty(td.prefabId) &&
            td.fishTrapDeployedMinute > 0;

        public static void QueueLoadRecords(IReadOnlyList<TileSaveData> tiles)
        {
            LoadPending.Clear();
            if (tiles == null)
                return;

            for (int i = 0; i < tiles.Count; i++)
            {
                TileSaveData td = tiles[i];
                if (!IsTrapOnlyRecord(td))
                    continue;
                LoadPending.Add(Clone(td));
            }
        }

        public static IReadOnlyList<TileSaveData> TakeLoadRecords()
        {
            if (LoadPending.Count == 0)
                return LoadPending;

            var copy = new List<TileSaveData>(LoadPending);
            LoadPending.Clear();
            return copy;
        }

        public static void SetSaveRecords(IReadOnlyList<TileSaveData> records)
        {
            SaveScratch.Clear();
            if (records == null)
                return;

            for (int i = 0; i < records.Count; i++)
            {
                TileSaveData td = records[i];
                if (td == null)
                    continue;
                SaveScratch.Add(Clone(td));
            }
        }

        public static void AppendSaveRecords(List<TileSaveData> tiles)
        {
            if (tiles == null)
                return;

            tiles.RemoveAll(IsTrapOnlyRecord);
            for (int i = 0; i < SaveScratch.Count; i++)
                tiles.Add(Clone(SaveScratch[i]));
        }

        static TileSaveData Clone(TileSaveData td) =>
            new TileSaveData
            {
                x = td.x,
                y = td.y,
                z = td.z,
                fishTrapBaitId = td.fishTrapBaitId,
                fishTrapBaitRemaining = td.fishTrapBaitRemaining,
                fishTrapDeployedMinute = td.fishTrapDeployedMinute,
                fishTrapAccumulatedFish = td.fishTrapAccumulatedFish,
            };
    }
}
