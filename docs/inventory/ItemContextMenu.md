# ItemContextMenu (기술 메모)

경로: `Assets/Dist/Scripts/UI/Inventory/ItemContextMenu/`  
어셈블리: `Dist.Inventory.UI`  
사람용 안내: [`ItemContextMenu-usage.md`](ItemContextMenu-usage.md)

코어 Model/View는 [`Dist.UI.ContextMenu`](../ui/ContextMenu.md)에 있다.

## 역할

아이템 우클릭 → `ContextMenuModel` 빌드 → Windows형 캐스케이드 UI.

```text
우클릭 → UIItemContextMenu
      → ContextMenuBuilder + InventoryContextMenuCatalog
      → Contributors가 Roots(Entry 트리) 채움
      → View(Dist.UI.ContextMenu)가 Entry만 Bind / 리프 클릭 → IContextMenuAction.Execute
```

## 폴더 경계 (의존 규칙 — 반드시)

| 폴더 | 해도 됨 | 하면 안 됨 |
|------|---------|------------|
| `Contributors/`, `Actions/` | 인벤·BN 데이터, `RecipeCategoryLabels`, 서비스 호출 | Cascade View 직접 조작 |
| `UIItemContextMenu` | Build, 패널 수명, Cancel/바깥클릭, InputManager | 레시피 목록을 UI에 하드코딩 |
| `Model/` (이 폴더) | `IContextMenuContributor`, `ContextMenuBuilder`, 라벨 | View 조작 |

공용 `ContextMenuEntry` / View / Style: `Assets/Dist/Scripts/UI/ContextMenu/` (`Dist.UI.ContextMenu`).

## 항목 추가 체크리스트

1. `Actions/`에 `IContextMenuAction` 구현 (`GetDisabledReason` / `Execute`)
2. `Contributors/`에 `IContextMenuContributor` 구현 (Entry 트리 + Action 연결)
3. `InventoryContextMenuCatalog`에 등록 (순서 = 루트 표시 순서)
4. View/프리팹 변경 불필요 (보통)

## UX SSOT

타이밍·색·폭: `Dist.UI.ContextMenu`의 `View/ContextMenuStyle.cs`  
패널 폭은 Bind 시 행 라벨 최대 선호폭으로 `MinPanelWidth`~`MaxPanelWidth` clamp.

## Prefab

Editor: `InventoryUIHierarchyBuilder.BuildContextMenuRoot`  
에셋: `Assets/Dist/Visual/Prefabs/UIComponents/Inventory/ItemContextMenu.prefab`  
런타임 `AddComponent` 금지 — 템플릿 Instantiate만.
