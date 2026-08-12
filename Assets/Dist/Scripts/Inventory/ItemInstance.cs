// ============================================================
// ItemInstance — 스택과 별개의 아이템 개체 (uid·선택 액션·약실)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// 아이템 실체. 합칠지는 ItemMergePolicy. SelectedAction·Chamber는 여기만.
/// Unset(null)이면 WeaponPresentation 기본 행. 약실은 탄 SSOT (메거진이 아님).
/// </summary>
public sealed class ItemInstance
{
    public ItemData Item { get; }
    public int DamageLevel { get; }

    /// <summary>생성 시 부여. 런타임 식별은 개체 참조. 세이브용 예약.</summary>
    public Guid Uid { get; }

    /// <summary>null = unset → SO default row. 해제(unwield) 후에도 이 인스턴스에 유지.</summary>
    public WeaponAction? SelectedAction { get; set; }

    /// <summary>약실 잔여. 발사 SSOT. 메거진 보급과 별개.</summary>
    public int ChamberRounds { get; private set; }

    /// <summary>약실에 들어간 탄 ItemData.id. 잔여 0이면 비움.</summary>
    public string ChamberAmmoId { get; private set; }

    /// <summary>메거진 아이템의 보급 잔여. 약실이 아님.</summary>
    public int SupplyRounds { get; private set; }

    public ItemInstance(ItemData item, int damageLevel = 0)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        DamageLevel = Math.Max(0, damageLevel);
        Uid = Guid.NewGuid();
        SelectedAction = null;
        ChamberRounds = 0;
        ChamberAmmoId = null;
        SupplyRounds = 0;
    }

    public void SetChamberRounds(int rounds)
    {
        ChamberRounds = Math.Max(0, rounds);
        if (ChamberRounds <= 0)
            ChamberAmmoId = null;
    }

    public void SetSupplyRounds(int rounds) =>
        SupplyRounds = Math.Max(0, rounds);

    public bool TryAddChamberRound(int capacity, string ammoId = null)
    {
        if (capacity <= 0 || ChamberRounds >= capacity)
            return false;
        ChamberRounds++;
        if (!string.IsNullOrEmpty(ammoId))
            ChamberAmmoId = ammoId;
        return true;
    }

    public bool TryConsumeChamberRound()
    {
        if (ChamberRounds <= 0)
            return false;
        ChamberRounds--;
        if (ChamberRounds <= 0)
            ChamberAmmoId = null;
        return true;
    }

    public bool TryTakeSupplyRound()
    {
        if (SupplyRounds <= 0)
            return false;
        SupplyRounds--;
        return true;
    }
}
