// ============================================================
// HandActionBinding — itemId → 손 사용 액션 영속 (탈착 후에도 유지)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 맵에 키가 없으면 Unset → 착용/들기 시 최고 DPS로 초기화.
/// null 값 = 수동 「없음」.
/// </summary>
public sealed class HandActionBinding
{
    readonly Dictionary<string, WeaponAction?> _map = new(StringComparer.Ordinal);

    public event Action Changed;

    public bool HasEntry(string itemId) =>
        !string.IsNullOrEmpty(itemId) && _map.ContainsKey(itemId);

    public bool TryGet(string itemId, out WeaponAction? action)
    {
        action = null;
        if (string.IsNullOrEmpty(itemId))
            return false;
        return _map.TryGetValue(itemId, out action);
    }

    public void Set(string itemId, WeaponAction? action)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (_map.TryGetValue(itemId, out WeaponAction? existing) && existing == action)
            return;

        _map[itemId] = action;
        Changed?.Invoke();
    }

    public void Clear(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;
        if (!_map.Remove(itemId))
            return;
        Changed?.Invoke();
    }

    /// <summary>Unset이면 최고 DPS 액션을 맵에 쓰고 반환. None이면 null.</summary>
    public WeaponAction? EnsureInitialized(
        ItemData item,
        int loadedRounds,
        ICharacterSkills skills)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return null;

        if (_map.TryGetValue(item.id, out WeaponAction? existing))
            return existing;

        WeaponAction? best = PrimaryWieldResolver.BestActionForItem(item, loadedRounds, skills);
        _map[item.id] = best;
        Changed?.Invoke();
        return best;
    }
}
