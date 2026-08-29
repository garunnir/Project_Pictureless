# Swim / Dive / DIVE_TANK

> Dist 수영·잠수·산소 SSOT.
> 인덱스: `docs/README.md` · 이동 일반: [`LOCOMOTION.md`](LOCOMOTION.md) · 액체: [`../map/LIQUID.md`](../map/LIQUID.md)
> **Swim 스크립트를 쓰거나 고치기 전에 이 문서를 읽는다.**
> 밸런스 상수 인덱스: [`../body/TUNING.md`](../body/TUNING.md) — 숫자는 코드 SSOT만 (`MapSwimConsts`).

경로: `Assets/Dist/Scripts/Map/Liquid/MapSwim*.cs` · `Assets/Dist/Scripts/Entity/Character/CharacterSwimHost.cs` · `CharacterBreathHost.cs`

---

## 모드

| Mode | 조건 | 이동 | 산소 |
|------|------|------|------|
| Dry | 물 없음 또는 얼음 `ProvidesSolidSupport` | 기존 보행 | 회복 |
| Wade | `Fill01 ≥ WadeFill01`(= `ShallowSeedFraction`) · `ColumnMl < SwimColumnMl` | 접지 · WadeFactor · 스프린트 약화 | 회복 |
| Swim | `ColumnMl ≥ SwimColumnMl` · Dive 아님 | 수면 스냅 · 스프린트 금지 | 회복 |
| Dive | Swim 가능 + Left Ctrl 홀드(또는 머리 잠김) | 컬럼 Y 클램프 + 수직 | 소모(탱크 없으면 BreathHold) |

발밑 셀: `OccupiedCellCoord` / `MapPlantHost.ResolveCellFromWorld` (`CharacterState.GridPos` raw 금지).

SSOT: `MapSwimQuery.Resolve` → `CharacterSwimHost` → `CharacterState` 플래그 · `PlayerMovement.SetSwimMovement` / `CharacterMotor.SetSwimMovement`.

---

## 산소 · DIVE_TANK

`CharacterBreathHost.Oxygen01` — **폐 손상 `BodyCapacity.Breathing`과 별개**.

| 상태 | Oxygen |
|------|--------|
| Dry/Wade/Swim | `OxygenRecoverPerSecond` |
| Dive + 탱크 활성 | 1 유지 · `ToolCharges`를 `DiveTankChargeIntervalSeconds`마다 소모 |
| Dive + 탱크 없음 | `BreathHoldDrainPerSecond` |

`Oxygen01 ≤ 0`이면서 Dive → `IsAsphyxiaDowned` → `CharacterPainHost` 쇼크(이동 잠금). 익사 중에는 Dive를 강제 해제하고 수면(Swim)으로 올린다. `Oxygen01 ≥ OxygenRecoverWakeThreshold`면 해제.

탱크: GameData `dive_tank` (`use_action.type = DIVE_TANK`) · 인벤 컨텍스트 **잠수 탱크 켜기/끄기** (`DiveTankService` / `DiveTankContextContributor`).

입력: Left Ctrl 홀드 = Dive (`InputManager` 런타임 액션).

---

## Wetness · 애니

액체 immersion gain은 `CharacterClimateHost`가 날씨 wetness gain과 `Max` (`MapSwimConsts.LiquidWetnessGain*`).

애니: `IsSwimming` bool (Swim 또는 Dive) → Move Layer `Swim` 상태 (`Swimming.anim`). `CharacterLocomotionAnim`.

---

## 경계

| 시스템 | 역할 |
|--------|------|
| `MapLiquidQuery` | ml / Fill01 / 고체 |
| `MapSwimQuery` | immersion 모드·수면/바닥 Y |
| `MapFishService.IsShooterInWater` | 수중창 — Wade와 같은 Fill01 임계 |
| `BodyCapacity.Breathing` | 폐 손상만 — 산소 타이머 아님 |
