// ============================================================
// InventoryTransferDuration — MoveStacks/퀵이동/창밖투하 소요 시간 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 인벤↔인벤·가방 인출 공통.
/// 소스 ContainerData.draw_moves &gt; 0 이면 BN moves 우선, 아니면 weight/volume/(storage) 프록시.
/// </summary>
public static class InventoryTransferDuration
{
    const float BaseSeconds = 0.15f;
    const float WeightSecondsPerKg = 0.12f;
    const float VolumeSecondsPerLiter = 0.06f;
    const float NestDepthSeconds = 0.1f;
    const float StorageHintDivisor = 8f;
    /// <summary>BN: 100 moves ≈ 1초.</summary>
    const float SecondsPerMove = 0.01f;

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

        if (sourceDrawMoves > 0)
            return Mathf.Max(0f, sourceDrawMoves * SecondsPerMove);

        ItemData item = stack.Item;
        float seconds = BaseSeconds
            + stack.TotalWeight * WeightSecondsPerKg
            + stack.TotalVolume * VolumeSecondsPerLiter
            + Mathf.Max(0, nestDepth) * NestDepthSeconds;

        // armor.storage는 용량 힌트 — moves 미bake 시 인출 가산
        if (item.armor != null && item.armor.storage > 0)
            seconds += item.armor.storage / StorageHintDivisor * 0.05f;

        return Mathf.Max(0f, seconds);
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
