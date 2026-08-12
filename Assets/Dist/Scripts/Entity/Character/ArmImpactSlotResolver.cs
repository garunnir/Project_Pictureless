// ============================================================
// ArmImpactSlotResolver — Impact 라이브러리 → thin Impact 키 투영
// ============================================================

using UnityEngine;

/// <summary>
/// ArmImpactKind 라이브러리 클립을 Impact thin 슬롯에 주입한다.
/// </summary>
public static class ArmImpactSlotResolver
{
    public static void ProjectImpact(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        ArmImpactKind kind,
        WieldHand hand)
    {
        if (resolved == null || catalog == null)
            return;

        AnimationClip thin = catalog.ImpactThinClip(kind);
        if (thin == null)
            return;

        ArmAnimSlotCatalog.ImpactLibraryEntry entry = catalog.FindImpact(kind);
        AnimationClip lib = PickHand(entry != null ? entry.clips : null, hand);
        AnimationClip mapped = ArmAnimSlotResolver.EffectiveClip(lib, resolved);
        resolved[thin] = mapped != null ? mapped : thin;
    }

    static AnimationClip PickHand(ArmAnimSlotCatalog.HandClips clips, WieldHand hand)
    {
        if (clips == null)
            return null;
        if (hand == WieldHand.Left)
            return clips.leftBase;
        if (hand == WieldHand.TwoHand)
            return clips.twoHandBase;
        return clips.rightBase;
    }
}
