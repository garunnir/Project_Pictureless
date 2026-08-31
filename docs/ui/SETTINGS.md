# Settings UI



> LLM/에이전트용 Dist 세팅 Overlay·HUD 레이아웃 편집·ESC Cancel 체인 SSOT.  

> 시간·배속: [`../time/TIME.md`](../time/TIME.md) · UI 폰트/레이아웃: [`UI_Scripts.md`](UI_Scripts.md)



경로(UI): `Assets/Dist/Scripts/UI/Settings/`  

경로(HUD layout): `Assets/Dist/Scripts/UI/HudLayout/`  

프리팹: `Assets/Dist/Visual/Prefabs/UIComponents/Settings/Grp_SettingsWindow.prefab`  

Editor: `Assets/Dist/Scripts/Editor/Settings/SettingsUISetupMenu.cs`



---



## 역할



- **ESC** → `UiCancelRouter`가 우선순위대로 Cancel 1명만 처리. 세팅은 **폴백**(Priority `-100`, 맨 뒤).

- 세팅 **Overlay** (`UICanvasLayer.Overlay`): 좌측 패널, HUD는 뒤에 보임.

- **Graphics** 카테고리 → **HUD 조정** 토글 → `HudLayoutEdit.IsActive`.

- **Graphics** → HUD 조정 아래 **개별 HUD 표시** 토글 4개 → `HudPopupVisibility` (`PlayerPrefs`).

- 세팅 열림 동안 `pause_menu` (World+Player `0`). HUD `gameplay_speed`와 **별개**.



---



## ESC Cancel 체인 (확장 SSOT)



| Priority | 상수 | 소비자 |

|----------|------|--------|

| 100 | `UiCancelPriority.ContextMenu` | `UiContextMenuCancelConsumer` |

| 80 | `UiCancelPriority.ModalPopup` | (예약) |

| 75 | `UiCancelPriority.FarmCellTarget` | `FarmCellTargetSession` |
| 76 | `UiCancelPriority.ConstructionCellTarget` | `ConstructionCellTargetSession` |
| 74 | `UiCancelPriority.FishCellTarget` | `FishCellTargetSession` |

| 60 | `UiCancelPriority.CharacterAction` | `CharacterActionCancelConsumer` (possessed `CancelAll`) |

| 40 | `UiCancelPriority.OverlayWindow` | (예약) |

| -100 | `UiCancelPriority.Settings` | `UISettingsController` (ESC 폴백) |



**추가 절차:** `UiCancelPriority`에 간격 두고 상수 추가 → `IUiCancelConsumer` 구현 → `UiCancelRouter.Register` (`OnEnable`). Settings/다른 소비자에 `if` 분기 **금지**.



입력 펄스: `InputManager.TryReadCancelPerformedThisFrame`.



---



## HUD 레이아웃 편집



| SSOT | 역할 |

|------|------|

| `HudLayoutEdit` | 편집 모드 플래그 + `Changed` |

| `HudLayoutIds` | Participant id SSOT |

| `HudPopupVisibility` | HUD 팝업 개별 표시 (`HudPopup.{id}.visible`, 기본 ON) |

| `HudLayoutParticipant` | HUD 레이어(`UICanvasLayer.HUD`)만. 평소 크롬 숨김, 조정 ON 시 헤더·크롬 + 드래그/리사이즈 |

| `HudLayoutStore` | `PlayerPrefs` 위치/크기 (participant id). 저장값 있을 때만 적용 |



**평소(OFF):** HUD `Area_Header`·크롬바·리사이즈 **표시 안 함**.  

**조정(ON):** `Area_Header`·크롬바 **상시 표시** + `Area_LayoutHit` 드래그·리사이즈.

**참조 배선 계약:** `HudLayoutParticipant`의 `_headerDrag/_layoutDrag/_chromeBar/_resizeHandles`는
런타임 탐색으로 채우지 않고, HUD 프리팹에 **직렬화 선배선**해야 한다.
`_resizeProximity`는 Time처럼 평소 근접 리빌이 있는 HUD만. Summary 등 조정 모드 전용 리사이즈는 `UIWindowResizeHandles`만(AlwaysHit).
누락 배선은 `Dist/MCP/HudLayout/Patch HUD Layout Participants`로 복구한다.

**리사이즈 제한 계약:** 기본은 제한 OFF.
- `_minSize = (0,0)`이면 최소 제한 없음
- `_maxSize` x/y 중 하나라도 `<= 0`이면 최대 제한 없음
- 제한이 필요할 때만 `HudLayoutParticipant` Inspector에서 값 지정



### 개별 HUD 표시 (레이아웃과 별개)



| id (`HudLayoutIds`) | 프리팹 | Loc |

|---------------------|--------|-----|

| `TimeDisplay` | `Grp_TimeDisplay` | `Settings.Hud.Time` |

| `TimeScaleHud` | `Hud_TimeScale` | `Settings.Hud.TimeScale` |

| `MessageLog` | `Hud_MessageLog` | `Settings.Hud.MessageLog` |

| `PlayerStatusSummary` | `Grp_PlayerStatusSummary` | `Settings.Hud.Summary` |



대상: 위 4개 (`UICanvasLayer.HUD`).  

**포함 금지:** 인벤·상태창·제작 등 `UICanvasLayer.Window` — Participant 미부착 + 레이어 게이트.



---



## Editor (Dist/MCP)



| 메뉴 | 역할 |

|------|------|

| `Dist/MCP/Settings/Create Settings Window Prefab If Missing` | Factory 1회 생성 + chrome patch |

| `Dist/MCP/Settings/Patch HUD Popup Toggles` | Graphics 페이지 HUD 4토글 Patch |

| `Dist/MCP/Settings/Setup Settings In Open Scene` | Controller + Canvas 서비스 (Router, ContextMenu consumer) |

| `Dist/MCP/Settings/Merge Localization Keys Into UI_ko` | Loc 키 |

| `Dist/MCP/HudLayout/Patch HUD Layout Participants` | HUD 프리팹 Participant 부착 |



Setup은 프리팹 **로드만**. 없으면 LogError — full bake 금지 (`ui-prefab-bake.mdc`).



---

## 에이전트 보고 규칙 (이 문서 범위 작업)

- 독립성 유지 모듈 전반(예: HUD layout, WindowChrome, CancelRouter, TimeScale, ContextMenu, Inventory 공용창, PlayerStatus 공용패널 등)을 수정할 때는
  **사전 이유 + 변경 범위 + 영향 모듈**을 먼저 보고한다.
- 독립성 유지 모듈 판단이 애매하면, 구현 전에 먼저 독립성 모듈 여부를 확인받는다.
- 위 규칙을 누락하고 임의 변경했을 경우, 후속 보고에 다음 문구를 포함한다:  
  `죄송합니다 주인님 으앙캥캥.`



---



## Loc 키



| Key | 용도 |

|-----|------|

| `Settings.WindowTitle` | 헤더 |

| `Settings.Category.Graphics` | 카테고리 |

| `Settings.HudLayoutAdjust` | HUD 조정 토글 |

| `Settings.Hud.Time` | 시계 HUD |

| `Settings.Hud.TimeScale` | 배속 HUD |

| `Settings.Hud.MessageLog` | 메시지 로그 |

| `Settings.Hud.Summary` | 상태 요약 |



문구 SSOT: `SettingsLabels`.

