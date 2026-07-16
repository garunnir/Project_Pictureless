# Inventory UI 문서



경로: `Assets/Dist/Scripts/UI/Inventory/`  

프리팹: `Assets/Dist/Visual/Prefabs/UIComponents/Inventory/`

어셈블리: `Dist.Inventory.UI` (`Assets/Dist/Scripts/UI/Inventory/Dist.Inventory.UI.asmdef`)



---



## 절대 원칙 (의존성 분리)



- 인벤토리 UI는 일반 UI 시스템(`UIController`, `UIModel`, `UIStatus*`, `UIMap*`)에 의존하지 않는다.

- 일반 UI가 인벤토리 구현체를 직접 참조하지 않도록 `IInventoryOverlayController` 인터페이스 경계로 연결한다.

- 런타임 컨테이너 조회는 `InventoryContainerRegistry`의 `ContainerId` 경계로 수행한다.

- 인벤토리 런타임 데이터 접근은 `PlayerInventoryRuntime`, `InventorySession`, `InventoryContainer` 경계에서만 수행한다.

- 인벤토리 입력 정책은 `InputManager` SSOT만 사용하고, 별도 입력 라우터/채널을 만들지 않는다.

- 임시 우회(땜빵) 금지. 증상 회피용 분기보다 상태 소유권/이벤트 경계를 먼저 수정한다.

- 드래그 수명주기(`Begin/Commit/Cancel`)는 단일 상태 소유자에서 종료한다.

- `InventoryDragState.End()`는 `UIInventoryController.FinalizeItemDrag` / `CleanupIfNoWindowsOpen`에서만 호출한다. `OnDrop`은 이동·선택 해제만 수행하고, 종료는 `OnEndDrag` → `OnItemDragEnded` 한 경로로 모은다.
- 창 Rect 밖에서 `EndDrag`하면 `FinalizeItemDrag`가 `floor-loot` 컨테이너로 `MoveStacks`한다 (사이드바 바닥 탭 드롭과 동일 판정). 창 안 비드롭존(헤더 등)은 기존대로 취소.

- `LateUpdate`는 드래그 종료·고스트 위치가 아니라 창 위 포인터 캐시·Zoom/Aim 억제 전용이다.
- 드래그 고스트 위치는 `OnBeginDrag` → `BeginDragGhost`, `OnDrag` → `UpdateDragGhostPosition` 이벤트 경로만 사용한다.
- 아이템 드래그는 스크롤 오버레이(`InventoryScrollDragOverlay`)를 켜지 않는다. 오버레이는 리스트 스크롤 드래그 전용이다.



---



## 창 구조



- `UIInventoryListWindow` 하나의 뷰를 플레이어창/루팅창에서 **동일하게 재사용**한다.

- 차이는 `InventoryWindowMode` 데이터 바인딩뿐이다.

  - `PlayerOnly`: 플레이어 컨테이너(`player-body`) 단일 리스트. 중첩 가방 탭이 있으면 사이드바 표시, 없으면 숨김.

  - `NearbyOnly`: `NearbyContainerDetector`가 등록한 주변 컨테이너 전체를 사이드탭으로 표시 (플레이어 제외). `TrackLootContainer` 없음 — 반경 스캔만 사용.
  - 감지 SSOT: 컨테이너 후보 판정은 `InventoryContainerRegistry` provider 목록 + `CharacterState.ResolveGridCell`(WorldGrid 기준) 단일 경로를 사용한다. `ContainerGridRegistry`는 Nearby 판단 경로에서 사용하지 않는다.

- 월드 컨테이너 표현은 **TilePresentationSystem** 단일 진입점 → `TileViewPresentationApplier`. UI는 Applier를 직접 호출하지 않는다.
- 루팅 파이프라인: `NearbyContainerDetector` → `LootProximityCoordinator` 이벤트 → `{ TilePresentationSystem, UIInventoryController }` 각각 구독. 컨테이너 TileView는 `EmphasisBlend`(살짝 밝게).
- `NearbyOnly`: 사이드탭이 없으면 아이템 리스트도 비움. 활성 탭 1개만 월드 하이라이트.
- 사이드탭 표현: `Normal` / `Selected` / `Dragging`. 중첩 가방 탭은 드래그 소스, 모든 탭은 드롭 타겟.
- 컨테이너 상호작용은 `Interactable`의 레거시 스프라이트 아웃라인을 사용하지 않는다. 포커스 시각효과는 타입별 전용 컴포넌트(`SpriteOutlineFocusVisual` 등)로 분리한다.
- `ContainerInteractable`는 런타임 `containerId` 충돌 시 자동으로 고유 suffix를 부여해 레지스트리 충돌을 방지한다.

- 프리팹 컴포넌트 배선은 프로젝트 공통 규칙(`.cursor/rules/collaboration-unity.mdc` §Prefab Component Wiring)을 따른다. 인벤 UI·월드 컨테이너 프리팹 모두 런타임 `AddComponent` 금지.

- 두 창은 **독립 open/close** (`TogglePrimaryWindow`, `ToggleLootWindow`). 레이아웃 프리팹은 **하나** (`Grp_InventoryListWindow`) — Primary/Loot는 인스턴스·모드·제목만 다름.
- `Area_InvInfo` (`Txt_Weight` / `Txt_Liter`): 선택 컨테이너 used/max 무게(kg)·부피(L).
- 창 위치는 **상단 헤더 드래그**로 자유 이동, **8방향 리사이즈 핸들**(상·하·좌·우 + 4모서리)로 크기 조절 (`WindowResizeEdge`, `InventoryWindowResizeHandler`).
- 크기 제한: 최소 320×240, 최대 Canvas의 75%×78% (`InventoryWindowLayout`).

- 사이드탭 클릭 시 해당 컨테이너 아이템 리스트로 즉시 전환.
- **창 선택 SSOT:** `UIInventoryListWindow.SelectedContainer` — 활성 탭 하이라이트와 리스트 `Bind`는 `SetActiveContainer` 단일 경로로만 갱신한다. 사이드바/리스트 단독 갱신 금지.
- **루팅 월드 SSOT:** `LootProximityCoordinator` — `NearbyOnly` 탭 클릭은 coordinator 경유 후 `ApplyActiveLootContainer` → `SetActiveContainer`로 창에 반영한다.



---



## 프리팹



### UI 프리팹



| 프리팹 | 역할 |

|--------|------|

| `Grp_InventoryListWindow` | 리스트 + 사이드바 창 (상단 드래그 헤더 포함) |

| `Grp_ItemListRow` | 아이템 행 (LeanPool) |

| `Grp_ContainerSlot` | 사이드바 컨테이너 슬롯 — 아이콘 SSOT는 `ContainerVisualPresenter` (월드 타일 thumbnail → provider SpriteRenderer → 중첩 가방은 item icon, `floor-loot`는 숨김) |

| `Grp_InventoryDragGhost` | 드래그 고스트 (`UICanvas` 하위, bake 또는 `Setup Canvas Overlays In Open Scene`) |

| `InventoryScrollDragOverlay` | 스크롤/드래그 중 전체 캔버스 레이캐스트 차단 (`UICanvas` 하위) |



폰트: 텍스트가 있는 행/슬롯 프리팹 TMP는 `Katuri SDF` 사용.

빈 아이콘 폴백: `ItemIconCatalog` (`Assets/Dist/Resources/ItemIconCatalog.asset`) → `ItemVisualPresenter`. 편집은 **Tools/Game Data Browser** 아이템 상세의 Icon 필드.

프리팹 갱신: `Dist/Inventory/Bake UI Prefabs` (bake 시점 `AddComponent`는 허용 — 런타임 폴백 아님).

캔버스 오버레이 배선: `Dist/Inventory/Setup Canvas Overlays In Open Scene` (IsoLand 등 씬 1회 실행).

### 월드 컨테이너 프리팹 (인벤 관련)



| 프리팹 | 필수 컴포넌트(예) |

|--------|------------------|

| `Map/Furniture/Create.prefab` 등 | `ContainerInteractable`, `ContainerTileViewRegistrar`, (선택) `SpriteOutlineFocusVisual` |



- `_containerId`는 인스턴스마다 고유해야 한다. 동일 ID 복제 시 런타임 suffix가 붙지만, 프리팹 단계에서부터 중복을 피하는 것이 기준이다.



---



## DnD + 박스 선택 구조



- 다중 선택 SSOT: `InventoryListSelection`

- 드래그 상태 SSOT: `InventoryDragState` (`InventoryDragPayload`)

- 드래그 시작: `UIItemListRow` (`IBeginDragHandler`)

- 드롭 처리: `UIInventoryListDropZone` (`IDropHandler`) → `InventorySession.MoveStacks(...)`

- 데이터 갱신: `InventorySession` 이벤트별 갱신 범위 분리
  - `StacksChanged` → `UIInventoryController.OnInventoryDataChanged()` → `UIInventoryListWindow.OnStacksChanged()` → `RefreshListOnly()`
  - `SidebarChanged` → `UIInventoryController.OnSessionChanged()` → `UIInventoryListWindow.OnSidebarChanged()` → `RefreshSidebarAndSelection()` (사이드바 `Sync` diff, 슬롯 add/remove만)
  - 탭 클릭 / 컨테이너 선택 → `RefreshListOnly()`
- `UIItemListView.Bind` 직후 `LayoutRebuilder.ForceRebuildLayoutImmediate` + `Canvas.ForceUpdateCanvases()`로 동적 행 레이아웃 갱신
- `UIInventoryController.LateUpdate`: 포인터가 창 위에 있는지 캐시 후 변경 시에만 `SuppressPlayerAction` 호출



### 배선 규칙



- `UIInventoryListDropZone`, `InventoryListMarqueeSelector`는 `Content`가 아니라 `ScrollRect.viewport`에 부착한다.

- `Viewport`는 투명 `Image(raycastTarget=true)`로 포인터/드롭 이벤트를 받는다.

- 베이크 시 `InventoryUIHierarchyBuilder`가 viewport 기준 배선을 생성해야 한다.



### 용량 검증



- 다중 이동은 개별 `CanAccept`만으로 판단하지 않는다.

- `InventorySession.MoveStacks(...)`에서 합산 무게/부피를 누적 계산해 검증한다.



### 런타임 조회/시딩



- 각 Provider(`PlayerInventoryHost`, `ContainerInteractable`)는 `ContainerId`를 가진다.

- `InventoryContainerRegistry`가 `ContainerId -> InventoryContainer`를 관리한다.

- 시작 시 아이템 주입·바닥 소형 아이템 스폰은 `InventoryRuntimeTestSetup`(런타임 테스트 전용)으로 수행한다 (`PlayerInventoryRuntime._seedDemoItemsOnStart` 기본값 `false` — 중복 시딩 방지).



---



## 런처 아이콘



- `InventoryWindowLauncher`를 `UICanvas` 하위 버튼에 부착한다. 컨트롤러가 open 상태를 `SetOpen`으로 push → 아이콘 색(활성/비활성).

- `LauncherTarget.Primary` → 플레이어창 토글

- `LauncherTarget.Loot` → 루팅창 토글

- `I` 키는 `InputManager.PlayerInventoryTogglePerformed` → 플레이어창 토글만 수행한다.



---



## 확인 체크리스트 (IsoLand)



1. `I`로 플레이어창 열기/닫기, 루팅 아이콘으로 루팅창 독립 토글

2. `E`로 루팅창 열기 + 해당 컨테이너 탭 점프 (탭 목록은 주변 전체 유지)

3. 사이드탭 클릭 시 해당 컨테이너 아이템 리스트 즉시 표시

4. 상단 드래그로 창 이동, 8방향 핸들로 리사이즈 후 DnD/스크롤 정상

5. 박스 드래그로 다중 선택, 플레이어창 ↔ 루팅창 간 드롭

6. 용량 초과 시 이동 실패, 실패 드롭 시 드래그 상태/고스트 정상 정리


