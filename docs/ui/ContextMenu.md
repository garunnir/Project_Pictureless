# Dist.UI.ContextMenu

공용 컨텍스트 메뉴 코어 어셈블리.

## 포함

| 폴더/파일 | 내용 |
|------|------|
| `Model/` | Entry, Model, Action, chrome 라벨 |
| `View/` | Cascade 패널/행, Style |
| `UIContextMenuHost` | 공용 Show(model)/Hide 호스트 |
| `ContextMenuHostEvents` | 호스트 상호 Hide |
| `UIPopupPositioner` | 스크린 포인트 배치 (+ 호버용 offset·캔버스 clamp 오버로드) |
| `UIHoverStyle` / `UIHoverPanelShell` | 호버 정보창 Placement 셸 — [`HOVER.md`](HOVER.md) |

## 의존

- 이 asmdef → `Config`(Loc), Unity.TextMeshPro, Unity.InputSystem
- DistScript / Dist.Inventory.UI → 이 asmdef (역참조 없음)

## 플러그인 경계

- **넣지 말 것**: Craft/OpenLoot 등 도메인 Action·Contributor
- 아이템 Host: `Dist.Inventory.UI` (`UIItemContextMenu`)
- 타일 Catalog/픽: DistScript → `UIContextMenuHost.TryShow(model, pos)`
- 들기 슬롯 RMB: DistScript `WieldSlotContextMenuCatalog` → `UIContextMenuHost.TryShow` (인벤 Catalog와 분리)
- ContextMenu 레이어 부모: `UICanvasLayerHost`가 `UIContextMenuHost.TryResolveParent` 등록
