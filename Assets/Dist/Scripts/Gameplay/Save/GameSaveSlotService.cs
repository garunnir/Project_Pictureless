// ============================================================
// GameSaveSlotService — 10슬롯 저장/불러오기 public API
// ============================================================

using System;
using System.IO;
using IsoTilemap;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSaveSlotService
{
    public static int SlotCount => GameSaveSlotPaths.SlotCount;
    public static GameSaveSlotInfo[] QuerySlotInfos()
    {
        var infos = new GameSaveSlotInfo[GameSaveSlotPaths.SlotCount];
        for (int i = 0; i < infos.Length; i++)
            infos[i] = QuerySlotInfo(i);
        return infos;
    }

    public static GameSaveSlotInfo QuerySlotInfo(int slotIndex)
    {
        if (!GameSaveSlotPaths.IsValidIndex(slotIndex))
            return default;

        string mapPath = GameSaveSlotPaths.MapPath(slotIndex);
        if (!File.Exists(mapPath))
        {
            return new GameSaveSlotInfo(
                slotIndex,
                hasData: false,
                savedAtUtcTicks: 0,
                dayIndex: 0,
                minuteOfDay: 0,
                hasClockSnapshot: false);
        }

        GameSaveSlotMetaDto meta = TryReadMeta(slotIndex);
        if (meta != null && meta.hasData)
        {
            return new GameSaveSlotInfo(
                slotIndex,
                meta.hasData,
                meta.savedAtUtcTicks,
                meta.dayIndex,
                meta.minuteOfDay,
                meta.hasClockSnapshot);
        }

        return new GameSaveSlotInfo(
            slotIndex,
            hasData: true,
            savedAtUtcTicks: File.GetLastWriteTimeUtc(mapPath).Ticks,
            dayIndex: 0,
            minuteOfDay: 0,
            hasClockSnapshot: false);
    }

    public static bool CanLoad(int slotIndex) =>
        GameSaveSlotPaths.IsValidIndex(slotIndex) &&
        File.Exists(GameSaveSlotPaths.MapPath(slotIndex));

    public static bool TrySaveSlot(int slotIndex, out string error)
    {
        error = null;
        if (!GameSaveSlotPaths.IsValidIndex(slotIndex))
        {
            error = "Invalid slot index.";
            return false;
        }

        TileMapManager manager = UnityEngine.Object.FindFirstObjectByType<TileMapManager>();
        if (manager == null)
        {
            error = "TileMapManager not found.";
            return false;
        }

        string path = GameSaveSlotPaths.MapPath(slotIndex);
        if (!manager.SaveTo(path))
        {
            error = "Map save failed.";
            return false;
        }

        WriteMeta(slotIndex, TryReadMapDto(path));
        Debug.Log($"[GameSaveSlotService] Saved slot {slotIndex + 1}: {path}");
        return true;
    }

    public static bool TryLoadSlot(int slotIndex, out string error)
    {
        error = null;
        if (!CanLoad(slotIndex))
        {
            error = "Slot is empty.";
            return false;
        }

        GameSaveSlotSession.RequestLoad(slotIndex);
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
        return true;
    }

    static void WriteMeta(int slotIndex, MapSaveJsonDto mapDto)
    {
        GameSaveSlotPaths.EnsureSavesDirectory();
        var meta = new GameSaveSlotMetaDto
        {
            hasData = true,
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            hasClockSnapshot = mapDto != null && mapDto.hasClockSnapshot,
            dayIndex = mapDto != null ? mapDto.dayIndex : 0,
            minuteOfDay = mapDto != null ? mapDto.minuteOfDay : 0
        };

        string json = JsonUtility.ToJson(meta, prettyPrint: true);
        File.WriteAllText(GameSaveSlotPaths.MetaPath(slotIndex), json);
    }

    static MapSaveJsonDto TryReadMapDto(string mapPath)
    {
        if (string.IsNullOrEmpty(mapPath) || !File.Exists(mapPath))
            return null;

        try
        {
            string json = File.ReadAllText(mapPath);
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return JsonUtility.FromJson<MapSaveJsonDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSaveSlotService] Map read failed: {mapPath}\n{e.Message}");
            return null;
        }
    }

    static GameSaveSlotMetaDto TryReadMeta(int slotIndex)
    {
        string path = GameSaveSlotPaths.MetaPath(slotIndex);
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return JsonUtility.FromJson<GameSaveSlotMetaDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSaveSlotService] Meta read failed slot {slotIndex}: {e.Message}");
            return null;
        }
    }
}
