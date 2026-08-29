# Message Log — 플레이어 정보 피드

> LLM/에이전트용 Dist 메시지 로그 SSOT.  
> 진입: `docs/README.md` · 스택: `docs/tech-stack.md` · UI 폰트/레이아웃: `docs/ui/UI_Scripts.md`  
> **메시지 로그·Append 호출·HUD 배선을 쓰거나 고치기 전에 이 문서를 읽는다.**

경로(코어): `Assets/Dist/Scripts/Gameplay/MessageLog/`  
경로(UI): `Assets/Dist/Scripts/UI/MessageLog/`  
경로(Editor): `Assets/Dist/Scripts/Editor/MessageLog/`  
프리팹: `Assets/Dist/Visual/Prefabs/UIComponents/MessageLog/Hud_MessageLog.prefab`

---

## 역할

Elona / Cataclysm DDA식 **상시·비차단** 텍스트 피드. 플레이어에게 **어느 정도 중요한** 추가 정보만 남긴다.  
디버그 콘솔(`IngameDebugConsole`)과 입·문구·수명을 공유하지 않는다.

---

## 중요도 게이트 (제품 정책)

| 남김 | 예 |
|------|-----|
| 예 | 플레이어 피해, 기습, 치명/패배, (향후) 의사결정에 영향 있는 상태·퀘스트·획득 등 |
| 아니오 | 매 프레임/틱, 빗나감 스팸, 적끼리 전투, “정상 소모”급 바이탈, 디버그 |

- **1차 게이트**: `GameplayMessageLog.Append`를 호출할지 말지 (호출부가 판단).
- **UI**: 받은 줄을 표시만 한다. 중요도 필터를 UI에서 다시 하지 않는다.
- `MessageLogImportance` (`Normal` / `Critical`)는 **표시 강조**용이다.

---

## 아키텍처

```text
CharacterAttacker.AnyAttackResolved ──┐
GameplayData.Defeat.Changed ──────────┼─► MessageLogPlayerCombatSink
PlayerMovement.AnyImmobileMoveAttempted ► MessageLogPlayerEncumbranceSink
PlayerNeedsHost.AnyNeedsVomit/Fatal/Warning ► MessageLogNeedsSink
CharacterMoodHost break start/end ─────► GameplayMessageLog
                                      ▼
                              GameplayMessageLog (ring buffer)
                                      │
                                      ▼
MessageLogUIBridge → MessageLogViewModel → UIMessageLogController → UIMessageLogPanel
                                                              (UICanvasLayer.HUD)
```

Time HUD와 동일: Bridge → ViewModel → Controller → Panel.

---

## API

```csharp
GameplayMessageLog.Append(
    MessageLogCategory.Combat,
    MessageLogImportance.Normal,
    Loc.Format("msg.combat.player_hit", partLabel, damage));

IReadOnlyList<MessageLogEntry> lines = GameplayMessageLog.GetSnapshot(); // 오래→최신
```

용량 SSOT: `GameplayMessageLog.Capacity` (100).

### 1차 피드 (구현됨)

| 사건 | 조건 | 카테고리 / Importance |
|------|------|------------------------|
| 피격 | `Target.Body == GameplayData.Body` 이고 hit | Combat / Normal |
| 패배 | `Defeat.Changed`에서 패배 **진입** | Status / Critical |
| 과적 Extreme 이동 시도 | `PlayerEncumbranceHost.Stage == Extreme` 이고 이동 입력 | Status / Normal |
| 과식 구토 | `PlayerNeedsHost` 팽만 중 재섭취 | Status / Normal |
| 아사/탈수 | stored kcal≤0 또는 thirst≤0 (1회) | Status / Critical |
| 허기/갈증 경고 | 매 6 월드시간, kcal% below 70/50/25/10 또는 갈증 danger | Status / Normal |
| 정신붕괴 시작/종료 | `CharacterMoodHost` Wander 양도 | Status / Critical·Normal |

**남기지 않음**: miss, 플레이어→적 공격, NPC↔NPC, 출혈 틱, 바이탈 소량, **Light~Heavy 과적**(상태 HUD 아이콘만).

플레이어 판정: `ReferenceEquals(body, GameplayData.Body)` (`NpcManager` 감지와 동일).
과적 Extreme 로그: Extreme 구간당 이동 시도 **1회** (`MessageLogPlayerEncumbranceSink`).

---

## Loc 키

| Key | 문구 |
|-----|------|
| `msg.combat.player_hit` | `{0}에 {1}의 피해를 입었다.` |
| `msg.combat.surprise_dealt` | `기습이 적중했다.` |
| `msg.combat.surprise_taken` | `기습을 당했다.` |
| `msg.combat.surprise_neck` | `목이 노려졌다.` |
| `msg.combat.surprise_stun` | `기습에 정신을 잃었다.` |
| `msg.status.defeat_body` | `치명상을 입고 쓰러졌다.` |
| `msg.status.defeat_collapse` | `정신이 무너져 쓰러졌다.` |
| `msg.status.encumbrance_immobile` | `너무 무거워서 움직일 수 없다.` |
| `msg.status.needs_vomit` | `너무 많이 먹어 토했다.` |
| `msg.status.needs_starve` | `굶주림으로 쓰러졌다.` |
| `msg.status.needs_dehydrate` | `갈증으로 쓰러졌다.` |
| `msg.status.needs_hunger_70` | `배가 고프다.` |
| `msg.status.needs_hunger_50` | `많이 배가 고프다.` |
| `msg.status.needs_hunger_25` | `매우 배가 고프다.` |
| `msg.status.needs_hunger_10` | `굶주리고 있다.` |
| `msg.status.needs_thirst_danger` | `목이 타들어간다.` |
| `msg.status.mood_break_wander` | `순간 이성을 잃고 배회하기 시작했다.` |
| `msg.status.mood_break_end` | `이성을 되찾았다.` |
| `MessageLog.Title` | `메시지` |

부위 표시: 기존 `PlayerStatus.Part.{id}`.

메뉴 (MCP): `Dist/MCP/MessageLog/Merge Localization Keys Into UI_ko`

---

## HUD 배선

1. `Dist/MCP/MessageLog/Create Hud_MessageLog Prefab If Missing` (없을 때만 생성; 레이아웃 덮어쓰기 금지)
2. `Dist/MCP/MessageLog/Setup Message Log HUD In Open Scene`
3. `Dist/MCP/WindowChrome/Patch Fold Close Buttons` (헤더·접기·끄기. 기존 프리팹에 헤더가 없을 때)
   - Canvas: `MessageLogUIBridge`
   - `System/Msg`: `MessageLogPlayerCombatSink`, `MessageLogPlayerEncumbranceSink`, `MessageLogNeedsSink`, `UIMessageLogController`
   - `Layer_HUD`: `Hud_MessageLog` 인스턴스

레이아웃 Rect·폰트 크기는 프리팹 SSOT (`MessageLogUIFactory` 초기값 / 손수 조정). 런타임 덮어쓰기 금지.

폰트: `Galmuri7 SDF` (`DistUiFont` / `MessageLogUIFactory.DefaultUIFontPath`).

---

## 패리티

| 항목 | 기대 |
|------|------|
| 상시 표시 | HUD 항상 표시 (헤더 근접 시 접기/끄기). 끄기 = 세션 동안 숨김, 복원 런처 없음 |
| 비차단 | 메시지와 무관하게 입력 유지 |
| 스크롤 | 최신 하단; 사용자가 위로 올려 보면 stick 해제 |
| 중요·플레이어만 | miss·비플레이어·공격 성공 무로그 |
| 디버그 분리 | 콘솔과 분리 |
