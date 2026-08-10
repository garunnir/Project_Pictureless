# UI Hover (Placement SSOT)

호버 **정보창**의 위치·캔버스 안 유지·레이어 계약. 콘텐츠·트리거는 기능별 Presenter.

룰/관련: [`ContextMenu.md`](ContextMenu.md) · [`../inventory/INVENTORY_UI.md`](../inventory/INVENTORY_UI.md)

## 포함 / 제외

| 포함 | 제외 |
|------|------|
| 인벤 `UIInventoryItemDetailPanel` | ContextMenu (자체 배치·clamp) |
| PlayerStatus Mood 툴팁 | DragGhost (`TopMost` 공유, 표시 시 last sibling) |
| PlayerStatus Body `UIPlayerStatusDetailPanel` | Interaction hint (`PopUpManager`) |
| 이후 동급 follow-mouse / 앵커 정보창 | |

## SSOT

| 심볼 | 역할 |
|------|------|
| `UIHoverCanvasLayer` | 호버 부모 레이어 = `UICanvasLayer.TopMost`. `EnsureParent` / `BringToFront` |
| `UIPopupPositioner` | 스크린→부모 로컬. `clampToCanvas: true` 시 루트 Canvas rect 안 유지 |
| `UIHoverStyle` | `ScreenOffset` · `FollowMouse` only (clamp 필드 없음) |
| `UIHoverPanelShell` | `ShowAtScreen` / `ShowNearAnchor` / `Hide` / `SetScreenPosition`. 배치는 **항상** clamp on. `raycastTarget=false` |

경로: `Assets/Dist/Scripts/UI/ContextMenu/` (`Dist.UI.ContextMenu`) · `UIHoverCanvasLayer`는 DistScript

## 계약

- Keep-in-bounds: 호버 셸 경로에서 clamp opt-out 없음. 기준은 **루트 Canvas rect**.
- ContextMenu용 `PlaceAtScreenPoint(panel, screen, canvas)`는 offset=0·clamp off (패리티).
- 콘텐츠 바인딩·숨김 시점(exit/드래그/창 닫힘/우클릭)은 Presenter·Controller.
- 호버 패널 부모는 **`UIHoverCanvasLayer` (TopMost)**. Show 시 `BringToFront`(last sibling). DragGhost와 레이어 공유 — 고스트 Show 시 last sibling으로 호버 위.
- Positioner용 **center anchors** 필요.

## Presenter Style

| Presenter | Offset | Follow | 배치 |
|-----------|--------|--------|------|
| Item detail | `(16, -16)` | yes | TopMost, `ShowAtScreen` |
| Mood tooltip | `(0, 28)` | no | TopMost, `ShowNearAnchor` |
| Body detail | `(16, -16)` | no | TopMost, `ShowNearAnchor` |
