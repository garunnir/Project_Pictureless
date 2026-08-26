// ============================================================
// CraftingEnvironmentProvider — nearby furniture + daylight for PSEUDO tools
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

/// <summary>
/// Scans TileMap cells around the player for crafting_flags / crafting_pseudo_item.
/// Bind from possessed player; sets <see cref="CraftingEnvironment.Active"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class CraftingEnvironmentProvider : MonoBehaviour, ICraftingEnvironment
{
    [SerializeField] CharacterState _characterState;
    [SerializeField, Min(0)] int _radiusCells = 2;

    readonly List<TileData> _cellTilesScratch = new();
    readonly HashSet<string> _craftFlags = new();
    readonly HashSet<string> _envTools = new();
    readonly Dictionary<string, int> _envQualities = new();
    int _lightLevel;
    bool _dirty = true;
    Vector3Int _lastScanCell = new(int.MinValue, int.MinValue, int.MinValue);
    TileMapCacheHub _hub;

    public bool IsDaylight =>
        WorldClock.Instance != null && WorldClock.Instance.Period == DayPeriod.Day;

    public int GetLightLevel()
    {
        EnsureScan();
        int light = _lightLevel;
        if (IsDaylight && light < 5)
            light = 5;
        return light;
    }

    public bool HasPseudoTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId))
            return false;

        EnsureScan();

        if (string.Equals(toolId, CraftingPseudoIds.Sunlight, System.StringComparison.Ordinal))
            return IsDaylight;

        if (string.Equals(toolId, CraftingPseudoIds.Fire, System.StringComparison.Ordinal))
            return HasFireEnv();

        if (string.Equals(toolId, CraftingPseudoIds.Apparatus, System.StringComparison.Ordinal))
            return HasApparatusEnv();

        return HasEnvTool(toolId);
    }

    public bool HasEnvTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId))
            return false;
        EnsureScan();
        return _envTools.Contains(toolId);
    }

    public int GetEnvQualityLevel(string qualityId)
    {
        if (string.IsNullOrEmpty(qualityId))
            return 0;
        EnsureScan();
        return _envQualities.TryGetValue(qualityId, out int level) ? level : 0;
    }

    void Awake()
    {
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();
    }

    void OnEnable()
    {
        CraftingEnvironment.Active = this;
        _dirty = true;
        if (_characterState != null)
            _characterState.GridPosChanged += OnGridPosChanged;
    }

    void OnDisable()
    {
        if (_characterState != null)
            _characterState.GridPosChanged -= OnGridPosChanged;
        if (ReferenceEquals(CraftingEnvironment.Active, this))
            CraftingEnvironment.Active = null;
    }

    void OnGridPosChanged(Vector3Int _) => _dirty = true;

    void EnsureScan()
    {
        Vector3Int cell = _characterState != null
            ? _characterState.ResolveCurrentGridCell()
            : Vector3Int.zero;

        if (!_dirty && cell == _lastScanCell)
            return;

        _dirty = false;
        _lastScanCell = cell;
        Rescan(cell);
    }

    void Rescan(Vector3Int origin)
    {
        _craftFlags.Clear();
        _envTools.Clear();
        _envQualities.Clear();
        _lightLevel = 0;

        if (_hub == null)
        {
            var map = FindFirstObjectByType<TileMapManager>();
            _hub = map != null ? map.MapCacheHub : null;
        }

        if (_hub == null)
            return;

        int r = _radiusCells;
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dz = -r; dz <= r; dz++)
            {
                var cell = new Vector3Int(origin.x + dx, origin.y, origin.z + dz);
                ScanCell(cell);
            }
        }
    }

    void ScanCell(Vector3Int cell)
    {
        _cellTilesScratch.Clear();
        if (!_hub.TryCollectTilesAtOccupiedCell(cell, _cellTilesScratch))
            return;

        for (int i = 0; i < _cellTilesScratch.Count; i++)
        {
            TileData tile = _cellTilesScratch[i];
            string prefabId = tile.identity.PrefabId;
            if (TilePrefabDB.TryResolveDefinition(prefabId, out TileDefinition def) && def?.flags != null)
            {
                for (int f = 0; f < def.flags.Count; f++)
                    AbsorbFlag(def.flags[f]);
            }

            FurnitureData furniture = ResolveFurniture(prefabId);
            if (furniture == null)
                continue;

            if (furniture.crafting_flags != null)
            {
                for (int f = 0; f < furniture.crafting_flags.Count; f++)
                    AbsorbFlag(furniture.crafting_flags[f]);
            }

            if (!string.IsNullOrEmpty(furniture.crafting_pseudo_item))
                _envTools.Add(furniture.crafting_pseudo_item);

            if (furniture.provides_qualities == null)
                continue;

            for (int q = 0; q < furniture.provides_qualities.Count; q++)
            {
                QualityEntry entry = furniture.provides_qualities[q];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;
                if (!_envQualities.TryGetValue(entry.id, out int have) || entry.level > have)
                    _envQualities[entry.id] = entry.level;
            }
        }
    }

    void AbsorbFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return;

        string key = flag.Trim();
        if (key.Length == 0)
            return;

        if (string.Equals(key, "FIRE_CONTAINER", System.StringComparison.OrdinalIgnoreCase))
            key = CraftingPseudoIds.CraftFlagFire;

        _craftFlags.Add(key);

        if (key.StartsWith("LIGHT_", System.StringComparison.OrdinalIgnoreCase))
        {
            string num = key.Substring(6);
            if (int.TryParse(num, out int level) && level > _lightLevel)
                _lightLevel = level;
        }
    }

    bool HasFireEnv()
    {
        if (_craftFlags.Contains(CraftingPseudoIds.CraftFlagFire) ||
            _craftFlags.Contains(CraftingPseudoIds.CraftFlagLit))
            return true;

        return _envTools.Contains(CraftingPseudoIds.HotplateTool) ||
               _envTools.Contains(CraftingPseudoIds.MultiCookerTool) ||
               _envTools.Contains(CraftingPseudoIds.CharSmokerTool) ||
               _envTools.Contains("fake_oven");
    }

    bool HasApparatusEnv()
    {
        bool smoker =
            _craftFlags.Contains(CraftingPseudoIds.CraftFlagSmoke) ||
            _craftFlags.Contains(CraftingPseudoIds.CraftFlagSmoker) ||
            _envTools.Contains(CraftingPseudoIds.CharSmokerTool);
        return smoker && HasFireEnv();
    }

    static FurnitureData ResolveFurniture(string prefabId)
    {
        if (string.IsNullOrEmpty(prefabId))
            return null;

        FurnitureData direct = GameplayData.GetFurniture(prefabId);
        if (direct != null)
            return direct;

        int slash = prefabId.LastIndexOf('/');
        string leaf = slash >= 0 && slash + 1 < prefabId.Length
            ? prefabId.Substring(slash + 1)
            : prefabId;
        return GameplayData.GetFurniture(leaf);
    }
}
