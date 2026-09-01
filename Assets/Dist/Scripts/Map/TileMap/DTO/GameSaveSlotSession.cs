// ============================================================
// GameSaveSlotSession — 씬 재로드 전 pending 슬롯 인덱스 (Dist.Map)
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class GameSaveSlotSession
    {
        static int? s_pendingLoadSlot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => s_pendingLoadSlot = null;

        public static void RequestLoad(int slotIndex)
        {
            if (!GameSaveSlotPaths.IsValidIndex(slotIndex))
                return;

            s_pendingLoadSlot = slotIndex;
        }

        public static bool TryConsumePendingLoad(out int slotIndex)
        {
            if (!s_pendingLoadSlot.HasValue)
            {
                slotIndex = -1;
                return false;
            }

            slotIndex = s_pendingLoadSlot.Value;
            s_pendingLoadSlot = null;
            return GameSaveSlotPaths.IsValidIndex(slotIndex);
        }
    }
}
