// ============================================================
// WieldSlotContextRequest — 들기 슬롯 RMB 컨텍스트 입력
// ============================================================

using System;

public sealed class WieldSlotContextRequest
{
    public CharacterGearService Gear;
    public string ItemId;
    public WieldSlotId Slot;
    public Action OnChanged;
}
