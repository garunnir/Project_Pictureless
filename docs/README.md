# Domain docs (LLM entry)

스택: [`tech-stack.md`](tech-stack.md)  
루트 인덱스: `CLAUDE.md` → **Domain docs**  
룰: `.cursor/rules/` (해당 스크립트 glob에서 자동)

| Topic | Canonical | Rule |
|-------|-----------|------|
| Game time | [`time/TIME.md`](time/TIME.md) | `game-time.mdc` |
| Map / TileMap | [`map/SYSTEM.md`](map/SYSTEM.md) → TileMap 세부 | `map-system.mdc` · `tile-chunk-streaming.mdc` |
| Inventory UI | [`inventory/INVENTORY_UI.md`](inventory/INVENTORY_UI.md) | `inventory-ui.mdc` |
| Item display names / loc | [`inventory/ITEM_NAMES.md`](inventory/ITEM_NAMES.md) | (Data Definitions hub · LocalizationBundle) |
| Equipment / Wear·Wield | [`equipment/GEAR.md`](equipment/GEAR.md) | (Character window · transfer duration) |
| UI MVC / font | [`ui/UI_Scripts.md`](ui/UI_Scripts.md) | `ui-prefab-layout.mdc` · `ui-font.mdc` |
| UI hover placement | [`ui/HOVER.md`](ui/HOVER.md) | (ContextMenu asm · keep-in-bounds) |
| Message log HUD | [`ui/MESSAGE_LOG.md`](ui/MESSAGE_LOG.md) | (UI 레이아웃·폰트 룰 공유) |
| Locomotion | [`locomotion/LOCOMOTION.md`](locomotion/LOCOMOTION.md) | `locomotion.mdc` |
| Legacy | [`legacy/LEGACY_README.md`](legacy/LEGACY_README.md) | `legacy.mdc` |

**스크립트를 수정하기 전에** 해당 행의 Canonical 문서를 읽는다.  
Assets 트리 안의 `.md`는 stub이며 본문은 여기만 진실원이다.
