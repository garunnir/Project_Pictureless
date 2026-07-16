// ============================================================
// IContextMenuContributor — 컨텍스트 메뉴 루트/서브트리 제공
// ============================================================

using System.Collections.Generic;

/// <summary>
/// 우클릭 대상에 맞춰 Roots에 Entry를 추가한다. 없으면 아무 것도 넣지 않는다.
/// </summary>
public interface IContextMenuContributor
{
    void Contribute(ItemStack stack, InventoryContainer container, InventorySession session, List<ContextMenuEntry> roots);
}
