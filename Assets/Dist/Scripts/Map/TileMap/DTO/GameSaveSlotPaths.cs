// ============================================================
// GameSaveSlotPaths — 슬롯 저장 경로 SSOT (Dist.Map)
// ============================================================

using System.IO;
using UnityEngine;

namespace IsoTilemap
{
    public static class GameSaveSlotPaths
    {
        public const int SlotCount = 10;

        const string SavesFolderName = "saves";
        const string MapFilePrefix = "slot_";
        const string MetaFileSuffix = ".meta.json";

        public static string SavesDirectory =>
            Path.Combine(Application.persistentDataPath, SavesFolderName);

        public static string MapPath(int slotIndex) =>
            Path.Combine(SavesDirectory, $"{MapFilePrefix}{FormatSlot(slotIndex)}.json");

        public static string MetaPath(int slotIndex) =>
            Path.Combine(SavesDirectory, $"{MapFilePrefix}{FormatSlot(slotIndex)}{MetaFileSuffix}");

        public static bool IsValidIndex(int slotIndex) =>
            slotIndex >= 0 && slotIndex < SlotCount;

        public static void EnsureSavesDirectory()
        {
            string dir = SavesDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        static string FormatSlot(int slotIndex) => slotIndex.ToString("D2");
    }
}
