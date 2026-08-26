// ============================================================
// PlayerStatusBleedDisplay — 출혈 drain·ETA (BodyEffectTicker 패리티, UI 전용)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public readonly struct PlayerStatusBleedSnapshot
{
    public readonly int TotalBleedIntensity;
    public readonly float OpenDrainPerSecond;
    public readonly float Blood01;

    public PlayerStatusBleedSnapshot(int totalBleedIntensity, float openDrainPerSecond, float blood01)
    {
        TotalBleedIntensity = totalBleedIntensity < 0 ? 0 : totalBleedIntensity;
        OpenDrainPerSecond = openDrainPerSecond < 0f ? 0f : openDrainPerSecond;
        Blood01 = blood01 < 0f ? 0f : blood01 > 1f ? 1f : blood01;
    }

    public bool HasAnyBleed => TotalBleedIntensity > 0;
    public bool HasOpenDrain => OpenDrainPerSecond > 0f;

    public float SecondsToEmpty =>
        HasOpenDrain && Blood01 > 0f ? Blood01 / OpenDrainPerSecond : float.PositiveInfinity;
}

public static class PlayerStatusBleedDisplay
{
    public const float ProseSevereDrainPerSecond = 0.008f;
    public const float ProseModerateDrainPerSecond = 0.003f;

    public static bool TrySnapshot(ICharacterBody body, out PlayerStatusBleedSnapshot snapshot)
    {
        snapshot = default;
        if (body == null)
            return false;

        int totalIntensity = 0;
        float drainPerSecond = 0f;
        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
            SumOrganic(body, roots[r], ref totalIntensity, ref drainPerSecond);

        if (totalIntensity <= 0)
            return false;

        float worldScale = 1f;
        TimeScaleService scales = TimeScaleService.Instance;
        if (scales != null)
            worldScale = scales.GetScale(TimeScaleChannel.World);

        snapshot = new PlayerStatusBleedSnapshot(
            totalIntensity,
            drainPerSecond * worldScale,
            body.Blood01);
        return true;
    }

    static void SumOrganic(
        ICharacterBody body,
        BodyPartNode node,
        ref int totalIntensity,
        ref float drainPerSecond)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return;

        int woundBleed = BleedIntensityOn(node, BodyPartEffectIds.Bleed);
        int organBleed = BleedIntensityOn(node, BodyPartEffectIds.OrganBleed);
        int partBleed = woundBleed + organBleed;
        if (partBleed > 0)
            totalIntensity += partBleed;

        if (partBleed > 0 && !HasEffect(node, BodyPartEffectIds.Bandaged))
        {
            drainPerSecond += woundBleed * BodyIllness.BleedBloodPerIntensityPerSecond
                              + organBleed * BodyIllness.OrganBleedBloodPerIntensityPerSecond;
        }

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            SumOrganic(body, children[i], ref totalIntensity, ref drainPerSecond);
    }

    static int BleedIntensityOn(BodyPartNode node, string effectId)
    {
        int sum = 0;
        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].EffectId != effectId)
                continue;
            int intensity = effects[i].Intensity;
            sum += intensity < 1 ? 1 : intensity;
        }

        return sum;
    }

    static bool HasEffect(BodyPartNode node, string effectId)
    {
        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].EffectId == effectId && effects[i].Intensity > 0)
                return true;
        }

        return false;
    }

    public static PlayerStatusBleedDisplay.ProseBand ResolveProseBand(float openDrainPerSecond, bool hasOpenDrain)
    {
        if (!hasOpenDrain)
            return ProseBand.Bandaged;
        if (openDrainPerSecond >= ProseSevereDrainPerSecond)
            return ProseBand.Severe;
        if (openDrainPerSecond >= ProseModerateDrainPerSecond)
            return ProseBand.Moderate;
        return ProseBand.Mild;
    }

    public enum ProseBand
    {
        Bandaged,
        Mild,
        Moderate,
        Severe,
    }
}
