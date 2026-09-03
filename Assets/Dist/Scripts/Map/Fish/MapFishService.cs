// ============================================================
// MapFishService — 물 수심 인접·낚시 Cast·루트 SSOT
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

namespace IsoTilemap
{
    public static class MapFishService
    {
        static MapFishRuntimeHooks _hooks = MapFishRuntimeHooks.Default;

        public static void Configure(MapFishRuntimeHooks hooks) =>
            _hooks = hooks.IsMoodBlocked == null ? MapFishRuntimeHooks.Default : hooks;

        /// <summary>
        /// 액터 점유셀에서 XZ Chebyshev ≤ 1(자기 셀 제외), 동일 Y 인접 셀에
        /// 낚시 가능 수심의 물이 있으면 true.
        /// </summary>
        public static bool IsFishableAdjacent(Vector3Int actorCell)
        {
            for (int dx = -MapFishConsts.FishingAdjacentRangeCells;
                 dx <= MapFishConsts.FishingAdjacentRangeCells;
                 dx++)
            {
                for (int dz = -MapFishConsts.FishingAdjacentRangeCells;
                     dz <= MapFishConsts.FishingAdjacentRangeCells;
                     dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    var neighbor = new Vector3Int(actorCell.x + dx, actorCell.y, actorCell.z + dz);
                    if (CellHasFishableWater(neighbor))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 낚시·통발 대상 셀인지 — <paramref name="waterCell"/>부터 아래로 누적한 수심이
        /// <see cref="MapFishConsts.FishableColumnMl"/> 이상이어야 한다.
        /// </summary>
        /// <remarks>
        /// 국소 Fill01이 아니라 컬럼 누적을 보는 이유: 셀 하나는 cap에서 클램프되므로
        /// 얕은 물 한 겹과 깊은 분지를 Fill01만으로는 구분할 수 없다.
        /// </remarks>
        public static bool CellHasFishableWater(Vector3Int waterCell) =>
            MapLiquidQuery.ColumnMlDownward(waterCell) >= MapFishConsts.FishableColumnMl;

        /// <summary>
        /// 수중창(S3) — 발 위치 셀이 물에 잠겼는지 (CombatHitscan).
        /// 낚시와 달리 수심이 아니라 국소 <see cref="MapLiquidQuery.Fill01"/>만 본다.
        /// </summary>
        public static bool IsShooterInWater(Vector3 feetWorld)
        {
            MapPlantHost host = MapPlantHost.Runtime;
            if (host == null)
                return false;

            Vector3Int cell = host.ResolveCellFromWorld(feetWorld);
            return MapLiquidQuery.Fill01(cell) >= MapFishConsts.UnderwaterShooterFill01;
        }

        public static bool HasFishingQuality(ItemData item) =>
            ItemQualityUtil.HasQuality(
                item,
                MapFishConsts.FishingQualityId,
                MapFishConsts.MinFishingQualityLevel);

        public static int ResolveFishingQualityLevel(ItemData item)
        {
            if (item?.qualities == null)
                return 0;

            int best = 0;
            for (int i = 0; i < item.qualities.Count; i++)
            {
                QualityEntry quality = item.qualities[i];
                if (quality == null || string.IsNullOrEmpty(quality.id))
                    continue;
                if (!quality.id.Equals(MapFishConsts.FishingQualityId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (quality.level > best)
                    best = quality.level;
            }

            return best;
        }

        public static bool HasItemFlag(ItemData item, string flagId)
        {
            if (item?.flags == null || string.IsNullOrEmpty(flagId))
                return false;

            for (int i = 0; i < item.flags.Count; i++)
            {
                string flag = item.flags[i];
                if (!string.IsNullOrEmpty(flag) &&
                    flag.Equals(flagId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool CanCast(ItemStack stack, InventoryContainer container) =>
            GetCastSessionBlockedReason(stack, container) == null;

        public static string GetCastSessionBlockedReason(ItemStack stack, InventoryContainer container)
        {
            if (_hooks.IsMoodBlocked != null && _hooks.IsMoodBlocked())
                return BlockedLabel();
            if (!HasFishingQuality(stack?.Item) || stack.Count < 1)
                return BlockedLabel();
            if (_hooks.OwnsInBodyOrWield == null ||
                !_hooks.OwnsInBodyOrWield(stack, container))
                return BlockedLabel();
            if (!TryResolveActorCell(out Vector3Int actorCell) || !IsFishableAdjacent(actorCell))
                return BlockedLabel();

            return null;
        }

        public static bool CanCastAt(Vector3Int waterCell, ItemStack stack, InventoryContainer container) =>
            GetCastBlockedReasonAt(waterCell, stack, container) == null;

        public static string GetCastBlockedReasonAt(
            Vector3Int waterCell,
            ItemStack stack,
            InventoryContainer container)
        {
            string session = GetCastSessionBlockedReason(stack, container);
            if (session != null)
                return session;

            if (!CellHasFishableWater(waterCell))
                return BlockedLabel();

            if (!TryResolveActorCell(out Vector3Int actorCell) ||
                !IsWithinCastActionRange(actorCell, waterCell))
                return BlockedLabel();

            return null;
        }

        public static bool TryCast(ItemStack stack, InventoryContainer container) =>
            TryCastAt(default, stack, container, usePlayerCellFallback: true);

        public static bool TryCastAt(
            Vector3Int waterCell,
            ItemStack stack,
            InventoryContainer container,
            bool usePlayerCellFallback = false)
        {
            if (usePlayerCellFallback)
            {
                if (GetCastSessionBlockedReason(stack, container) != null)
                    return false;
                if (!TryResolveAdjacentFishableCell(out waterCell))
                    return false;
            }
            else if (GetCastBlockedReasonAt(waterCell, stack, container) != null)
            {
                return false;
            }

            if (_hooks.RollCatchItemId != null &&
                _hooks.GrantItem != null)
            {
                string itemId = _hooks.RollCatchItemId(stack.Item);
                if (!string.IsNullOrEmpty(itemId))
                    _hooks.GrantItem(itemId, 1, CellWorld(waterCell));
            }

            return true;
        }

        public static Vector3 CellArriveWorld(Vector3Int cell)
        {
            MapPlantHost host = MapPlantHost.Runtime;
            float cellSize = host != null ? host.CellSize : 1f;
            return TileHelper.ConvertGridToWorldPos(cell, cellSize);
        }

        public static float CellArriveStoppingDistance()
        {
            MapPlantHost host = MapPlantHost.Runtime;
            float cellSize = host != null ? host.CellSize : 1f;
            return cellSize * MapFishConsts.CellArriveStoppingCellFraction;
        }

        public static bool IsWithinCastActionRange(Vector3Int actorCell, Vector3Int targetCell)
        {
            if (actorCell.y != targetCell.y)
                return false;

            int dx = Mathf.Abs(actorCell.x - targetCell.x);
            int dz = Mathf.Abs(actorCell.z - targetCell.z);
            return Mathf.Max(dx, dz) <= MapFishConsts.CastActionRangeCells;
        }

        /// <summary>발밑 점유셀. Dist.Map.Fish는 Assembly-CSharp 타입을 참조하지 않으며 <see cref="MapFishRuntimeHooks.TryResolveActorCell"/>에 위임합니다.</summary>
        public static bool TryResolvePlayerCell(out Vector3Int cell) =>
            TryResolveActorCell(out cell);

        public static bool TryResolveActorCell(out Vector3Int cell)
        {
            if (_hooks.TryResolveActorCell != null && _hooks.TryResolveActorCell(out cell))
                return true;

            return TryResolvePlayerCell(out cell);
        }

        static bool TryResolveAdjacentFishableCell(out Vector3Int cell)
        {
            cell = default;
            if (!TryResolveActorCell(out Vector3Int actorCell))
                return false;

            for (int dx = -MapFishConsts.FishingAdjacentRangeCells;
                 dx <= MapFishConsts.FishingAdjacentRangeCells;
                 dx++)
            {
                for (int dz = -MapFishConsts.FishingAdjacentRangeCells;
                     dz <= MapFishConsts.FishingAdjacentRangeCells;
                     dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    var neighbor = new Vector3Int(actorCell.x + dx, actorCell.y, actorCell.z + dz);
                    if (!CellHasFishableWater(neighbor))
                        continue;

                    cell = neighbor;
                    return true;
                }
            }

            return false;
        }

        static Vector3 CellWorld(Vector3Int cell)
        {
            MapPlantHost host = MapPlantHost.Runtime;
            float cellSize = host != null ? host.CellSize : 1f;
            Vector3 pos = TileHelper.ConvertGridToWorldPos(cell, cellSize);
            pos.y += MapPlantConsts.OverlayYOffset;
            return pos;
        }

        static string BlockedLabel() =>
            _hooks.FishBlockedLabel != null ? _hooks.FishBlockedLabel() : "낚시할 수 없음";

        public static bool IsFishTrapItem(ItemData item) =>
            item != null &&
            string.Equals(item.id, MapFishConsts.FishTrapItemId, StringComparison.OrdinalIgnoreCase);

        public static bool HasTrapBaitLoaded(ItemStack stack)
        {
            return TryExtractTrapBait(stack, out _, out _);
        }

        public static bool CanDeployTrap(ItemStack stack, InventoryContainer container) =>
            GetDeployTrapSessionBlockedReason(stack, container) == null;

        public static string GetDeployTrapSessionBlockedReason(ItemStack stack, InventoryContainer container)
        {
            if (_hooks.IsMoodBlocked != null && _hooks.IsMoodBlocked())
                return BlockedLabel();
            if (!IsFishTrapItem(stack?.Item) || stack.Count < 1)
                return BlockedLabel();
            if (!HasTrapBaitLoaded(stack))
                return BlockedLabel();
            if (_hooks.OwnsInBodyOrWield == null ||
                !_hooks.OwnsInBodyOrWield(stack, container))
                return BlockedLabel();
            if (!TryResolveActorCell(out Vector3Int actorCell) || !IsFishableAdjacent(actorCell))
                return BlockedLabel();

            return null;
        }

        public static bool CanDeployTrapAt(Vector3Int waterCell, ItemStack stack, InventoryContainer container) =>
            GetDeployTrapBlockedReasonAt(waterCell, stack, container) == null;

        public static string GetDeployTrapBlockedReasonAt(
            Vector3Int waterCell,
            ItemStack stack,
            InventoryContainer container)
        {
            string session = GetDeployTrapSessionBlockedReason(stack, container);
            if (session != null)
                return session;

            MapFishTrapHost host = MapFishTrapHost.Runtime ?? MapFishTrapHost.EnsureRuntime();
            if (host == null || host.HasTrap(waterCell))
                return BlockedLabel();

            if (!CellHasFishableWater(waterCell))
                return BlockedLabel();

            if (!TryResolveActorCell(out Vector3Int actorCell) ||
                !IsWithinCastActionRange(actorCell, waterCell))
                return BlockedLabel();

            return null;
        }

        public static bool CanCollectTrapAt(Vector3Int waterCell) =>
            GetCollectTrapBlockedReasonAt(waterCell) == null;

        public static string GetCollectTrapBlockedReasonAt(Vector3Int waterCell)
        {
            MapFishTrapHost host = MapFishTrapHost.Runtime ?? MapFishTrapHost.EnsureRuntime();
            if (host == null || !host.HasTrap(waterCell))
                return BlockedLabel();

            host.CatchUpCell(waterCell);
            if (!host.TryGetTrap(waterCell, out FishTrapCell trap) || trap.AccumulatedFish <= 0)
                return BlockedLabel();

            if (!TryResolveActorCell(out Vector3Int actorCell) ||
                !IsWithinCastActionRange(actorCell, waterCell))
                return BlockedLabel();

            return null;
        }

        public static bool TryDeployTrapAt(
            Vector3Int waterCell,
            ItemStack stack,
            InventoryContainer container)
        {
            if (GetDeployTrapBlockedReasonAt(waterCell, stack, container) != null)
                return false;

            MapFishTrapHost host = MapFishTrapHost.Runtime ?? MapFishTrapHost.EnsureRuntime();
            if (host == null)
                return false;

            if (!TryExtractTrapBait(stack, out string baitId, out int baitRemaining))
                return false;

            int deployedMinute = MapClockSnapshot.CurrentWorldMinute();

            if (_hooks.TryTakeFromStack == null ||
                _hooks.TryTakeFromStack(stack, container) < 1)
                return false;

            if (!host.TryDeploy(waterCell, baitId, baitRemaining, deployedMinute))
                return false;

            return true;
        }

        public static bool TryCollectTrapAt(Vector3Int waterCell)
        {
            if (GetCollectTrapBlockedReasonAt(waterCell) != null)
                return false;

            MapFishTrapHost host = MapFishTrapHost.Runtime ?? MapFishTrapHost.EnsureRuntime();
            if (host == null)
                return false;

            if (!host.TryCollect(waterCell, out int fishGranted, out _))
                return false;

            if (fishGranted > 0 && _hooks.GrantItem != null)
                _hooks.GrantItem(MapFishConsts.DefaultFishItemId, fishGranted, CellWorld(waterCell));

            return true;
        }

        static bool TryExtractTrapBait(ItemStack stack, out string baitId, out int baitRemaining)
        {
            baitId = null;
            baitRemaining = 0;
            if (stack?.Item == null || stack.Instance == null)
                return false;

            if (stack.Instance.SupplyRounds > 0 &&
                !string.IsNullOrEmpty(stack.Instance.SupplyAmmoId) &&
                ToolAcceptsAmmo(stack.Item, stack.Instance.SupplyAmmoId))
            {
                baitId = stack.Instance.SupplyAmmoId;
                baitRemaining = stack.Instance.SupplyRounds;
                return true;
            }

            if (stack.Instance.ToolCharges > 0 &&
                TryResolveToolAmmoItemId(stack.Item, out baitId))
            {
                baitRemaining = stack.Instance.ToolCharges;
                return true;
            }

            return false;
        }

        static bool TryResolveToolAmmoItemId(ItemData toolItem, out string ammoItemId)
        {
            ammoItemId = null;
            if (toolItem?.tool?.ammo == null)
                return false;

            for (int i = 0; i < toolItem.tool.ammo.Count; i++)
            {
                string allowed = toolItem.tool.ammo[i];
                if (string.IsNullOrEmpty(allowed))
                    continue;

                ItemData asItem = GameplayData.GetItem(allowed);
                if (asItem?.ammo != null)
                {
                    ammoItemId = asItem.id;
                    return true;
                }

                ammoItemId = allowed;
                return true;
            }

            return false;
        }

        static bool ToolAcceptsAmmo(ItemData toolItem, string loadedAmmoId)
        {
            if (toolItem?.tool?.ammo == null || string.IsNullOrEmpty(loadedAmmoId))
                return false;

            ItemData loaded = GameplayData.GetItem(loadedAmmoId);
            string loadedType = loaded?.ammo?.ammo_type ?? loadedAmmoId;

            for (int i = 0; i < toolItem.tool.ammo.Count; i++)
            {
                string allowed = toolItem.tool.ammo[i];
                if (string.IsNullOrEmpty(allowed))
                    continue;
                if (allowed.Equals(loadedAmmoId, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (allowed.Equals(loadedType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
