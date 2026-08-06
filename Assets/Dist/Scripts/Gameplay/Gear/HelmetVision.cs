// ============================================================
// HelmetVision — 머리 덮개(헬멧) 착용 시 시야 배율 SSOT (Phase G)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// Worn armor covering head → VisionFactor &lt; 1.
/// Consumer: CameraZoomController ortho × VisionFactor via PlayerGearHost.
/// Docs: docs/equipment/GEAR.md Phase G.
/// </summary>
public static class HelmetVision
{
    /// <summary>시야 판정 부위 — covers에 포함되면 헬멧/머리 덮개.</summary>
    public const string VisionCoverPartId = BodyPartIds.Head;

    /// <summary>머리 덮개 착용 시 시야 배율 (1=정상).</summary>
    public const float HeadCoverVisionFactor = 0.85f;

    /// <summary>머리 덮개 없을 때 시야 배율.</summary>
    public const float FullVisionFactor = 1f;

    /// <summary>표시용 페널티 (1 − HeadCoverVisionFactor).</summary>
    public const float HeadCoverVisionPenalty =
        FullVisionFactor - HeadCoverVisionFactor;

    public static bool CoversHead(EquipmentWearState wear)
    {
        if (wear == null)
            return false;

        var worn = wear.Worn;
        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            if (stack?.Item == null)
                continue;
            if (GearHandleRules.CoversPart(stack.Item, VisionCoverPartId))
                return true;
        }

        return false;
    }

    /// <summary>1 = full vision; HeadCoverVisionFactor when head covered.</summary>
    public static float ComputeVisionFactor(EquipmentWearState wear) =>
        CoversHead(wear) ? HeadCoverVisionFactor : FullVisionFactor;

    public static int VisionPercent(float visionFactor) =>
        Mathf.RoundToInt(Mathf.Clamp01(visionFactor) * 100f);
}
