// ============================================================
// IWieldSlotContextMenuContributor — 들기 슬롯 RMB 메뉴 Contributor
// ============================================================

using System.Collections.Generic;

public interface IWieldSlotContextMenuContributor
{
    void Contribute(WieldSlotContextRequest request, List<ContextMenuEntry> roots);
}
