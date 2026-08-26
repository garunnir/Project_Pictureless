// ============================================================
// MapFishRuntimeHooks — Dist.Map.Fish ↔ DistScript 런타임 브리지 계약
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

namespace IsoTilemap
{
    public delegate bool TryResolveActorCellDelegate(out Vector3Int cell);

    public struct MapFishRuntimeHooks
    {
        public Func<bool> IsMoodBlocked;
        public Func<ItemStack, InventoryContainer, bool> OwnsInBodyOrWield;
        public Func<string> FishBlockedLabel;
        public Func<ItemData, string> RollCatchItemId;
        public Action<string, int, Vector3> GrantItem;
        public TryResolveActorCellDelegate TryResolveActorCell;
        public Func<ItemStack, InventoryContainer, int> TryTakeFromStack;

        public static MapFishRuntimeHooks Default => new MapFishRuntimeHooks
        {
            IsMoodBlocked = () => false,
            OwnsInBodyOrWield = (_, _) => false,
            FishBlockedLabel = () => "낚시할 수 없음",
            RollCatchItemId = _ => null,
            GrantItem = (_, _, _) => { },
            TryResolveActorCell = (out Vector3Int cell) =>
            {
                cell = default;
                return false;
            },
            TryTakeFromStack = (_, _) => 0
        };
    }
}
