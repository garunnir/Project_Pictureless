// ============================================================
// MapFishTrapHost — 물 셀 통발 상태·오버레이·Catch-up SSOT
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct FishTrapCell
    {
        public readonly Vector3Int Cell;
        public readonly string BaitId;
        public readonly int BaitRemaining;
        public readonly int DeployedWorldMinute;
        public readonly int LastTickWorldMinute;
        public readonly int AccumulatedFish;

        public FishTrapCell(
            Vector3Int cell,
            string baitId,
            int baitRemaining,
            int deployedWorldMinute,
            int lastTickWorldMinute,
            int accumulatedFish)
        {
            Cell = cell;
            BaitId = baitId ?? string.Empty;
            BaitRemaining = Math.Max(0, baitRemaining);
            DeployedWorldMinute = deployedWorldMinute;
            LastTickWorldMinute = lastTickWorldMinute;
            AccumulatedFish = Math.Max(0, accumulatedFish);
        }

        public bool IsActive => DeployedWorldMinute > 0;
    }

    [DisallowMultipleComponent]
    public sealed class MapFishTrapHost : MonoBehaviour
    {
        public static MapFishTrapHost Runtime { get; private set; }

        static readonly List<TileSaveData> SaveScratch = new();

        readonly Dictionary<Vector3Int, FishTrapCell> _traps = new();
        readonly Dictionary<Vector3Int, GameObject> _overlays = new();

        Transform _overlayRoot;
        float _cellSize = 1f;

        public event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset() => SaveScratch.Clear();

        void Awake()
        {
            Runtime = this;
            EnsureOverlayRoot();
            ApplyPendingLoad();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Runtime, this))
                Runtime = null;
        }

        public void BindMapContext(TileMapCacheHub hub, float cellSize)
        {
            _ = hub;
            _cellSize = Mathf.Max(1e-4f, cellSize);
            ApplyPendingLoad();
        }

        public static MapFishTrapHost EnsureRuntime()
        {
            if (Runtime != null)
                return Runtime;

            MapPlantHost plant = MapPlantHost.Runtime;
            TileMapCacheHub hub = TileMapCacheHub.Runtime;
            float cellSize = plant != null ? plant.CellSize : 1f;

            var go = new GameObject(nameof(MapFishTrapHost));
            var host = go.AddComponent<MapFishTrapHost>();
            host.BindMapContext(hub, cellSize);
            return host;
        }

        public static bool IsTrapOnlyRecord(TileSaveData td) =>
            MapFishTrapSaveBuffer.IsTrapOnlyRecord(td);

        public bool HasTrap(Vector3Int walkableCell) =>
            _traps.TryGetValue(walkableCell, out FishTrapCell trap) && trap.IsActive;

        public bool TryGetTrap(Vector3Int walkableCell, out FishTrapCell trap) =>
            _traps.TryGetValue(walkableCell, out trap) && trap.IsActive;

        public bool TryDeploy(
            Vector3Int walkableCell,
            string baitId,
            int baitRemaining,
            int deployedWorldMinute)
        {
            if (!MapFishService.CellHasFishableWaterFloor(walkableCell) || HasTrap(walkableCell))
                return false;

            var trap = new FishTrapCell(
                walkableCell,
                baitId,
                baitRemaining,
                deployedWorldMinute,
                deployedWorldMinute,
                accumulatedFish: 0);
            _traps[walkableCell] = trap;
            CatchUpCell(walkableCell, ref trap);
            _traps[walkableCell] = trap;
            RefreshOverlay(walkableCell, trap);
            SyncSaveBuffer();
            Changed?.Invoke();
            return true;
        }

        public bool TryCollect(Vector3Int walkableCell, out int fishGranted, out bool trapRemoved)
        {
            fishGranted = 0;
            trapRemoved = false;
            if (!TryGetTrap(walkableCell, out FishTrapCell trap))
                return false;

            CatchUpCell(walkableCell, ref trap);
            fishGranted = trap.AccumulatedFish;
            if (fishGranted > 0)
            {
                trap = new FishTrapCell(
                    trap.Cell,
                    trap.BaitId,
                    trap.BaitRemaining,
                    trap.DeployedWorldMinute,
                    trap.LastTickWorldMinute,
                    accumulatedFish: 0);
            }

            if (trap.BaitRemaining <= 0)
            {
                RemoveTrap(walkableCell);
                trapRemoved = true;
                return true;
            }

            _traps[walkableCell] = trap;
            RefreshOverlay(walkableCell, trap);
            SyncSaveBuffer();
            Changed?.Invoke();
            return true;
        }

        public void CatchUpCell(Vector3Int walkableCell)
        {
            if (!TryGetTrap(walkableCell, out FishTrapCell trap))
                return;

            CatchUpCell(walkableCell, ref trap);
            if (!trap.IsActive || (trap.BaitRemaining <= 0 && trap.AccumulatedFish <= 0))
            {
                RemoveTrap(walkableCell);
                return;
            }

            _traps[walkableCell] = trap;
            RefreshOverlay(walkableCell, trap);
            SyncSaveBuffer();
        }

        public void CatchUpAll()
        {
            if (_traps.Count == 0)
                return;

            var cells = new List<Vector3Int>(_traps.Keys);
            for (int i = 0; i < cells.Count; i++)
                CatchUpCell(cells[i]);
        }

        void ApplyPendingLoad()
        {
            IReadOnlyList<TileSaveData> pending = MapFishTrapSaveBuffer.TakeLoadRecords();
            if (pending.Count == 0)
                return;

            EnsureOverlayRoot();
            for (int i = 0; i < pending.Count; i++)
            {
                TileSaveData td = pending[i];
                var cell = new Vector3Int(td.x, td.y, td.z);
                var trap = new FishTrapCell(
                    cell,
                    td.fishTrapBaitId,
                    td.fishTrapBaitRemaining,
                    td.fishTrapDeployedMinute,
                    td.fishTrapDeployedMinute,
                    td.fishTrapAccumulatedFish);
                _traps[cell] = trap;
                CatchUpCell(cell, ref trap);
                _traps[cell] = trap;
                RefreshOverlay(cell, trap);
            }

            SyncSaveBuffer();
            Changed?.Invoke();
        }

        void SyncSaveBuffer()
        {
            SaveScratch.Clear();
            CollectSaveRecords(SaveScratch);
            MapFishTrapSaveBuffer.SetSaveRecords(SaveScratch);
        }

        void CollectSaveRecords(List<TileSaveData> tiles)
        {
            foreach (KeyValuePair<Vector3Int, FishTrapCell> pair in _traps)
            {
                FishTrapCell trap = pair.Value;
                if (!trap.IsActive)
                    continue;

                tiles.Add(new TileSaveData
                {
                    x = trap.Cell.x,
                    y = trap.Cell.y,
                    z = trap.Cell.z,
                    fishTrapBaitId = trap.BaitId,
                    fishTrapBaitRemaining = trap.BaitRemaining,
                    fishTrapDeployedMinute = trap.DeployedWorldMinute,
                    fishTrapAccumulatedFish = trap.AccumulatedFish,
                });
            }
        }

        static void CatchUpCell(Vector3Int walkableCell, ref FishTrapCell trap)
        {
            if (!trap.IsActive || trap.BaitRemaining <= 0)
                return;

            int now = MapClockSnapshot.CurrentWorldMinute();
            int cursor = trap.LastTickWorldMinute > 0
                ? trap.LastTickWorldMinute
                : trap.DeployedWorldMinute;
            int interval = MapFishConsts.TrapTickIntervalMinutes;
            if (interval < 1)
                interval = 1;

            int accumulatedFish = trap.AccumulatedFish;
            int baitRemaining = trap.BaitRemaining;
            string baitId = trap.BaitId;

            while (cursor + interval <= now && baitRemaining > 0)
            {
                cursor += interval;
                if (UnityEngine.Random.value <= MapFishConsts.TrapCatchChancePerTick)
                {
                    accumulatedFish++;
                    baitRemaining--;
                }
            }

            trap = new FishTrapCell(
                walkableCell,
                baitId,
                baitRemaining,
                trap.DeployedWorldMinute,
                cursor,
                accumulatedFish);
        }

        void RemoveTrap(Vector3Int walkableCell)
        {
            _traps.Remove(walkableCell);
            if (_overlays.TryGetValue(walkableCell, out GameObject overlay) && overlay != null)
                Destroy(overlay);
            _overlays.Remove(walkableCell);
            SyncSaveBuffer();
            Changed?.Invoke();
        }

        void RefreshOverlay(Vector3Int walkableCell, FishTrapCell trap)
        {
            if (!trap.IsActive)
            {
                RemoveTrap(walkableCell);
                return;
            }

            EnsureOverlayRoot();
            if (!_overlays.TryGetValue(walkableCell, out GameObject overlay) || overlay == null)
            {
                overlay = new GameObject("FishTrapOverlay");
                overlay.transform.SetParent(_overlayRoot, false);
                overlay.AddComponent<MapFishTrapInteractable>();
                _overlays[walkableCell] = overlay;
            }

            if (overlay.TryGetComponent(out MapFishTrapInteractable interactable))
                interactable.BindCell(walkableCell);

            MapFishTrapOverlayVisual.Apply(overlay.transform, walkableCell, _cellSize);
        }

        void EnsureOverlayRoot()
        {
            if (_overlayRoot != null)
                return;

            var root = new GameObject("MapFishTrapOverlays");
            root.transform.SetParent(transform, false);
            _overlayRoot = root.transform;
        }
    }
}
