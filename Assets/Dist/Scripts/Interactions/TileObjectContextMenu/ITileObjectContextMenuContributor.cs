// ============================================================
// ITileObjectContextMenuContributor — 타일 오브젝트 컨텍스트 메뉴 Contributor
// ============================================================

using System.Collections.Generic;

public interface ITileObjectContextMenuContributor
{
    void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots);
}
