// ============================================================
// CharacterSpawner — 셀 SSOT로 본체 프리팹 소환 후 맵·possess·NPC 등록
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

[DefaultExecutionOrder(20)]
[DisallowMultipleComponent]
public sealed class CharacterSpawner : MonoBehaviour
{
    [SerializeField] List<CharacterSpawnEntry> _entries = new();
    [SerializeField] TileMapManager _tileMapManager;
    [SerializeField] MapGameplayBootstrap _mapBootstrap;
    [SerializeField] PlayerManager _playerManager;
    [SerializeField] NpcManager _npcManager;
    [SerializeField, Min(0.0001f)] float _editorGizmoCellSize = 1f;

    void OnValidate()
    {
        if (_entries == null)
            return;
        for (int i = 0; i < _entries.Count; i++)
            _entries[i]?.SyncCellFromMarker();
    }

    void Start() => SpawnAll();

    public void SpawnAll()
    {
        if (_tileMapManager == null)
            _tileMapManager = FindFirstObjectByType<TileMapManager>();
        if (_mapBootstrap == null)
            _mapBootstrap = FindFirstObjectByType<MapGameplayBootstrap>();
        if (_playerManager == null)
            _playerManager = FindFirstObjectByType<PlayerManager>();
        if (_npcManager == null)
            _npcManager = FindFirstObjectByType<NpcManager>();

        IWorldGrid worldGrid = _tileMapManager != null ? _tileMapManager.WorldGrid : null;
        Transform parent = CharacterWorldRoot.Resolve();

        if (_entries == null)
            return;

        GameObject possessedBody = null;
        for (int i = 0; i < _entries.Count; i++)
        {
            CharacterSpawnEntry entry = _entries[i];
            if (entry == null || entry.definition == null)
            {
                Debug.LogError($"[CharacterSpawner] Entry {i} is missing definition.", this);
                continue;
            }

            Vector3Int cell = entry.ResolveCell();
            Vector3 world = worldGrid != null
                ? worldGrid.CellToWorld(cell)
                : TileHelper.ConvertGridToWorldPos(cell, _editorGizmoCellSize);

            GameObject instance = CharacterFactory.InstantiateInactive(entry.definition, world, parent);
            if (instance == null)
                continue;

            if (_mapBootstrap != null)
                _mapBootstrap.BindSpawnedCharacter(instance);
            else if (worldGrid != null)
            {
                CharacterState state = instance.GetComponent<CharacterState>();
                state?.BindWorldGrid(worldGrid);
            }

            AssignBodyInventoryId(instance, entry.role);
            instance.SetActive(true);
            CharacterSpawnGearApplier.Apply(entry.definition, instance);

            if (entry.role == CharacterSpawnRole.Possessed)
            {
                possessedBody = instance;
                continue;
            }

            if (_npcManager == null)
            {
                Debug.LogError("[CharacterSpawner] NpcManager missing for Npc spawn.", this);
                continue;
            }

            CharacterSpawnNpcSettings npc = entry.npc ?? new CharacterSpawnNpcSettings();
            _npcManager.Register(npc.ToAgentEntry(instance.transform));
        }

        if (possessedBody == null)
            return;

        if (_playerManager != null)
            _playerManager.Possess(possessedBody);
        else
            Debug.LogError("[CharacterSpawner] PlayerManager missing for Possessed spawn.", this);
    }

    void OnDrawGizmos()
    {
        if (_entries == null)
            return;

        if (!Application.isPlaying)
        {
            for (int i = 0; i < _entries.Count; i++)
                _entries[i]?.SyncCellFromMarker();
        }

        float cellSize = ResolveEditorCellSize();
        for (int i = 0; i < _entries.Count; i++)
        {
            CharacterSpawnEntry entry = _entries[i];
            if (entry == null)
                continue;

            if (entry.marker != null)
                continue;

            Vector3Int cell = entry.ResolveCell();
            TileHelper.DrawOccupiedCellWire(
                cell,
                cellSize,
                CharacterSpawnGizmoColors.ForRole(entry.role));
        }
    }

    static void AssignBodyInventoryId(GameObject instance, CharacterSpawnRole role)
    {
        if (!instance.TryGetComponent(out PlayerInventoryHost host))
            return;

        if (role == CharacterSpawnRole.Possessed)
            host.AssignInstanceId(PlayerInventoryHost.DefaultInstanceId);
        else
            host.AssignInstanceId(PlayerInventoryHost.CreateUniqueBodyInstanceId());
    }

    float ResolveEditorCellSize()
    {
        if (_tileMapManager != null && _tileMapManager.WorldGrid != null)
            return Mathf.Max(0.0001f, _tileMapManager.WorldGrid.CellSize);
        return Mathf.Max(0.0001f, _editorGizmoCellSize);
    }
}
