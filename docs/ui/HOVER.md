# UI Hover (Placement SSOT)

호버 **정보창**의 위치·캔버스 안 유지·레이어 계약. 콘텐츠·트리거는 기능별 Presenter.

룰/관련: [`ContextMenu.md`](ContextMenu.md) · [`../inventory/INVENTORY_UI.md`](../inventory/INVENTORY_UI.md)

## 포함 / 제외

| 포함 | 제외 |
|------|------|
| 인벤 `UIInventoryItemDetailPanel` | ContextMenu (자체 배치·clamp) |
| PlayerStatus Mood 툴팁 | DragGhost (`TopMost` 공유, 표시 시 last sibling) |
| PlayerStatus Body `UIPlayerStatusDetailPanel` (부위 트리) | Interaction hint (`PopUpManager`) |
| **공용 텍스트** `UITextHoverService` (장비·HUD 들기 등 `ShowText`) | |
| 이후 동급 follow-mouse / 앵커 정보창 | |

## SSOT

| 심볼 | 역할 |
|------|------|
| `UIHoverCanvasLayer` | 호버 부모 레이어 = `UICanvasLayer.TopMost`. `EnsureParent` / `BringToFront` |
| `UIPopupPositioner` | 스크린→부모 로컬. `clampToCanvas: true` 시 루트 Canvas rect 안 유지 |
| `UIHoverStyle` | `ScreenOffset` · `FollowMouse` only (clamp 필드 없음) |
| `UIHoverPanelShell` | `ShowAtScreen` / `ShowNearAnchor` / `Hide` / `SetScreenPosition`. 배치는 **항상** clamp on. `raycastTarget=false` |
| `UITextHoverService` | Canvas TopMost **공용 텍스트** 호버. `TryShowNearAnchor` / `HideOn`. Prefab=`TextHoverPanel` |

경로: `Assets/Dist/Scripts/UI/ContextMenu/` (`Dist.UI.ContextMenu`) · `UIHoverCanvasLayer` / `UITextHoverService`는 DistScript  
Setup: `Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene` (ghost + text hover)

## 계약

- Keep-in-bounds: 호버 셸 경로에서 clamp opt-out 없음. 기준은 **루트 Canvas rect**.
- ContextMenu용 `PlaceAtScreenPoint(panel, screen, canvas)`는 offset=0·clamp off (패리티).
- 콘텐츠 바인딩·숨김 시점(exit/드래그/창 닫힘/우클릭)은 Presenter·Controller. Mood 툴팁은 포인터 enter/exit·패널 disable만 — vital/`Refresh`로 Hide 하지 않음.
- 호버 패널 부모는 **`UIHoverCanvasLayer` (TopMost)**. Show 시 `BringToFront`(last sibling). DragGhost와 레이어 공유 — 고스트 Show 시 last sibling으로 호버 위.
- Positioner용 **center anchors** 필요.

## Presenter Style

| Presenter | Offset | Follow | 배치 |
|-----------|--------|--------|------|
| Item detail | `(16, -16)` | yes | TopMost, `ShowAtScreen` |
| Mood tooltip | `(0, 28)` | no | TopMost, `ShowNearAnchor` |
| Body detail | `(16, -16)` | no | TopMost, `ShowNearAnchor` (`UIPlayerStatusDetailPanel`). Status: 특이사항 없으면 Hide |
| Gear / HUD text | `(16, -16)` | no | TopMost, `UITextHoverService` (`DefaultStyle`) |
