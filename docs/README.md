# Domain docs (LLM entry)

스택: [`tech-stack.md`](tech-stack.md)  
루트 인덱스: `CLAUDE.md` → **Domain docs**  
룰: `.cursor/rules/` (해당 스크립트 glob에서 자동)

**남은 일 / 구현 지침:** [`PLAN.md`](PLAN.md) — “뭐 남았지?”는 이 문서만. 구현 시 항목 지침을 따르고, 끝나면 그 항목을 삭제한다.

| Topic | Canonical | Rule |
|-------|-----------|------|
| Game time | [`time/TIME.md`](time/TIME.md) | `game-time.mdc` |
| Map / TileMap | [`map/SYSTEM.md`](map/SYSTEM.md) → TileMap 세부 | `map-system.mdc` · `tile-chunk-streaming.mdc` |
| Inventory UI | [`inventory/INVENTORY_UI.md`](inventory/INVENTORY_UI.md) | `inventory-ui.mdc` |
| Item catalog locale (name / desc / recipe cat / quality) | [`inventory/ITEM_NAMES.md`](inventory/ITEM_NAMES.md) | (Data Definitions hub · LocalizationBundle) |
| Equipment / Wear·Wield | [`equipment/GEAR.md`](equipment/GEAR.md) | (Character window · transfer duration) |
| Weapon anim / VFX folders | [`equipment/WEAPON_VISUAL.md`](equipment/WEAPON_VISUAL.md) | (hub → Pipeline → clips/prefabs) |
| BN converter whitelist | [`equipment/BN_BAKE.md`](equipment/BN_BAKE.md) | (`convert.py` → BNData; promote when Dist has a consumer) |
| UI MVC / font | [`ui/UI_Scripts.md`](ui/UI_Scripts.md) | `ui-prefab-layout.mdc` · `ui-font.mdc` |
| Settings / HUD layout | [`ui/SETTINGS.md`](ui/SETTINGS.md) | (ESC Cancel · HudLayoutEdit) |
| UI hover placement | [`ui/HOVER.md`](ui/HOVER.md) | (ContextMenu asm · keep-in-bounds) |
| Message log HUD | [`ui/MESSAGE_LOG.md`](ui/MESSAGE_LOG.md) | (UI 레이아웃·폰트 룰 공유) |
| Crafting window | [`crafting/CRAFTING.md`](crafting/CRAFTING.md) | (창 vs 아이템 메뉴 · 재료 풀 · 대체재 드롭) |
| Farming / plants | [`farming/FARMING.md`](farming/FARMING.md) | (Dist overlay view · TileFlags · world minutes) |
| Locomotion | [`locomotion/LOCOMOTION.md`](locomotion/LOCOMOTION.md) | `locomotion.mdc` |
| Character definition / spawn | [`character/DEFINITION.md`](character/DEFINITION.md) | — |
| Character sight fade (NPC mesh) | [`character/SIGHT_FADE.md`](character/SIGHT_FADE.md) | — |
| Character action / gauge | [`character/ACTION.md`](character/ACTION.md) | (행위자 큐·TickScale·CancelAll) |
| Body / anatomy / climate | [`body/BODY.md`](body/BODY.md) · 밸런스 인덱스 [`body/TUNING.md`](body/TUNING.md) | — |
| Needs / hunger / thirst / consume / sleep | [`needs/NEEDS.md`](needs/NEEDS.md) | — |
| Mood / thoughts / mental break | [`mood/MOOD.md`](mood/MOOD.md) | — |
| Legacy | [`legacy/LEGACY_README.md`](legacy/LEGACY_README.md) | `legacy.mdc` |

**스크립트를 수정하기 전에** 해당 행의 Canonical 문서를 읽는다.  
Assets 트리 안의 `.md`는 stub이며 본문은 여기만 진실원이다.
