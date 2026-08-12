// ============================================================
// HandActionBinding — DEPRECATED stub. Select SSOT is ItemInstance.
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// itemId 맵은 더 이상 select SSOT가 아니다. 라이브 경로는 ItemInstance.SelectedAction.
/// </summary>
public sealed class HandActionBinding
{
#pragma warning disable CS0067
    public event Action Changed;
#pragma warning restore CS0067

    public bool HasEntry(string itemId) => false;

    public bool TryGet(string itemId, out WeaponAction? action)
    {
        action = null;
        return false;
    }

    public void Set(string itemId, WeaponAction? action)
    {
    }

    public void Clear(string itemId)
    {
    }

    public void ForEach(Action<string, WeaponAction?> visitor)
    {
    }

    public WeaponAction? EnsureInitialized(
        ItemData item,
        ICharacterSkills skills)
    {
        return null;
    }
}
