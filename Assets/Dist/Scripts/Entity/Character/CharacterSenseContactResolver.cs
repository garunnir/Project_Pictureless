// ============================================================
// CharacterSenseContactResolver — Vision > Hearing 단일 채널 SSOT
// ============================================================

using UnityEngine;

public enum SenseContactChannel
{
    None = 0,
    Vision = 1,
    Hearing = 2,
}

public static class CharacterSenseContactResolver
{
    public static SenseContactChannel Resolve(bool visionActive, bool hearingActive)
    {
        if (visionActive)
            return SenseContactChannel.Vision;
        if (hearingActive)
            return SenseContactChannel.Hearing;
        return SenseContactChannel.None;
    }

    public static Vector3 ResolveSteerGoal(
        SenseContactChannel channel,
        Transform targetTransform,
        Vector3 heardWorld)
    {
        if (channel == SenseContactChannel.Hearing)
            return heardWorld;

        return targetTransform != null ? targetTransform.position : heardWorld;
    }

    public static bool AllowsAttack(SenseContactChannel channel) =>
        channel == SenseContactChannel.Vision;

    public static bool AllowsAlert(SenseContactChannel channel) =>
        channel == SenseContactChannel.Vision;

    public static bool ShowsHearingPing(bool visionActive, bool hearingActive) =>
        Resolve(visionActive, hearingActive) == SenseContactChannel.Hearing;
}
