// ============================================================
// InventoryTransferDuration — MoveStacks/퀵이동/창밖투하 소요 시간 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 인벤↔인벤·가방 인출 공통.
/// access = draw_moves→초(CombatMath.MovesPerSecond) + handling(weight/volume/nest).
/// </summary>
public static class InventoryTransferDuration
{
    const float BaseSeconds = 0.15f;
    const float WeightSecondsPerKg = 0.12f;
    const float VolumeSecondsPerLiter = 0.06f;
    const float NestDepthSeconds = 0.1f;

    public static int ResolveSourceDrawMoves(InventoryContainer source)
    {
        if (source?.Definition == null)
            return 0;
        return Mathf.Max(0, source.Definition.draw_moves);
    }

    public static float SecondsForStack(ItemStack stack, int nestDepth = 0, int sourceDrawMoves = 0)
    {
        if (stack?.Item == null)
            return 0f;

        float accessSeconds = sourceDrawMoves > 0
            ? sourceDrawMoves / CombatMath.MovesPerSecond
            : 0f;

        float handlingSeconds = BaseSeconds
            + stack.TotalWeight * WeightSecondsPerKg
            + stack.TotalVolume * VolumeSecondsPerLiter
            + Mathf.Max(0, nestDepth) * NestDepthSeconds;

        return Mathf.Max(0f, accessSeconds + handlingSeconds);
    }

    public static float SecondsForStacks(
        IReadOnlyList<ItemStack> stacks,
        int nestDepth = 0,
        int sourceDrawMoves = 0)
    {
        if (stacks == null || stacks.Count == 0)
            return 0f;

        float total = 0f;
        for (int i = 0; i < stacks.Count; i++)
            total += SecondsForStack(stacks[i], nestDepth, sourceDrawMoves);
        return total;
    }

    public static float SecondsForStacksFrom(InventoryContainer source, IReadOnlyList<ItemStack> stacks)
    {
        return SecondsForStacks(
            stacks,
            EstimateNestDepth(source),
            ResolveSourceDrawMoves(source));
    }

    public static float SecondsForStackFrom(InventoryContainer source, ItemStack stack)
    {
        return SecondsForStack(
            stack,
            EstimateNestDepth(source),
            ResolveSourceDrawMoves(source));
    }

    public static float SecondsForStackUnits(InventoryContainer source, ItemStack stack, int unitCount)
    {
        if (stack?.Item == null || unitCount <= 0)
            return 0f;

        if (stack.Nested != null || stack.LoadedMagazine != null)
            return SecondsForStackFrom(source, stack);

        int count = unitCount < stack.Count ? unitCount : stack.Count;
        float accessSeconds = ResolveSourceDrawMoves(source) > 0
            ? ResolveSourceDrawMoves(source) / CombatMath.MovesPerSecond
            : 0f;

        float handlingSeconds = BaseSeconds
            + stack.Item.Weight * count * WeightSecondsPerKg
            + stack.Item.Volume * count * VolumeSecondsPerLiter
            + Mathf.Max(0, EstimateNestDepth(source)) * NestDepthSeconds;

        return Mathf.Max(0f, accessSeconds + handlingSeconds);
    }

    /// <summary>소스 컨테이너가 player-body가 아니면 중첩/사이드 인출로 본다.</summary>
    public static int EstimateNestDepth(InventoryContainer source)
    {
        if (source == null)
            return 0;
        if (string.Equals(source.InstanceId, PlayerInventoryHost.DefaultInstanceId, System.StringComparison.Ordinal))
            return 0;
        return 1;
    }
}
