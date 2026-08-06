// ============================================================
// GearActionDuration — Wear/TakeOff/Wield/Unwield 소요 시간(초) SSOT
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// ItemData에 wear_time 미bake — weight/volume 프록시.
/// BN bake omissions: docs/equipment/GEAR.md
/// </summary>
public static class GearActionDuration
{
    const float WearBaseSeconds = 1.5f;
    const float TakeOffBaseSeconds = 1f;
    const float WieldBaseSeconds = 0.35f;
    const float UnwieldBaseSeconds = 0.25f;
    const float WeightSecondsPerKg = 0.15f;
    const float VolumeSecondsPerLiter = 0.08f;

    public static float WearSeconds(ItemData item) =>
        WearBaseSeconds + BulkSeconds(item);

    public static float TakeOffSeconds(ItemData item) =>
        TakeOffBaseSeconds + BulkSeconds(item) * 0.75f;

    public static float WieldSeconds(ItemData item) =>
        WieldBaseSeconds + BulkSeconds(item) * 0.35f;

    public static float UnwieldSeconds(ItemData item) =>
        UnwieldBaseSeconds + BulkSeconds(item) * 0.25f;

    static float BulkSeconds(ItemData item)
    {
        if (item == null)
            return 0f;
        return item.Weight * WeightSecondsPerKg + item.Volume * VolumeSecondsPerLiter;
    }

    /// <summary>가방 인출이 있으면 Gear + Transfer 합산.</summary>
    public static float CombinedSeconds(float gearSeconds, float transferSeconds) =>
        Mathf.Max(0f, gearSeconds) + Mathf.Max(0f, transferSeconds);
}
