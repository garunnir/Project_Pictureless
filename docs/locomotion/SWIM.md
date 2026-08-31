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
| Dry | 물 없음 또는 얼음 `ProvidesSolidSupport` | 기존 보행 | 회복(공기권) |
| Wade | `Fill01 ≥ WadeFill01` · `ColumnMl < SwimColumnMl` | 접지 · WadeFactor · 스프린트 약화 | 회복(공기권) |
| Swim | `ColumnMl ≥ SwimColumnMl` · Dive 아님 | 수직 Space/Ctrl · Y 스냅 없음 | `HeadSubmerged`면 drain |
| Dive | Swim 가능 + Left Ctrl 홀드(또는 머리 잠김) | 컬럼 Y 클램프 + 수직 | `HeadSubmerged`면 drain |

발밑 셀: `OccupiedCellCoord` / `MapPlantHost.ResolveCellFromWorld` (`CharacterState.GridPos` raw 금지).

SSOT: `MapSwimQuery.Resolve` → `CharacterSwimHost` → `CharacterState` 플래그 · `PlayerMovement.SetSwimMovement` / `CharacterMotor.SetSwimMovement`.

---

## 수직 입력 · Y

| 입력 | 동작 |
|------|------|
| **Space 홀드** | 상승 (`+1` × `DiveVerticalSpeed`) |
| **Left Ctrl 홀드** | 하강 (`-1` × `DiveVerticalSpeed`) — Ctrl 우선 |
| 입력 없음 | 현재 깊이 유지 (수면/바닥 Y **텔레포트 없음**) |

Swim·Dive 모두 `CharacterLocomotion.ApplySwimVertical` 동일 물리.

**응급 상승**: `IsCapacityDowned` + `HeadSubmerged` → Space와 **동일 속도** `+1` (이동 잠금 중에도). Dive→Swim **모드 스냅 없음**.

---

## 산소 · DIVE_TANK

`body.BloodOxygen01` — 런타임 SSOT (`CharacterBreathHost` 틱). DTO 없음.

**합산 O2 풀 (초)** = `BaseBreathHoldSeconds × LungEff` + `ToolCharges × DiveTankSecondsPerCharge`

| 상태 | O2 |
|------|-----|
| `HeadSubmerged == false` | 회복 (`BloodOxygenRecoverPerSecond × LungEff`) |
| `HeadSubmerged == true` | drain (1 World초/초) · 탱크 활성 시 charge도 소모 |

SpO2 → `BodyCapacity.Consciousness` · `Breathing = LungEff × BloodOxygen01`.

**다운**: `IsCapacityDowned` (의식 &lt; 0.3 / Breathing ≤ 0 / Moving &lt; 0.15) → `CharacterPainHost` 쇼크.

**익사 사망**: 수중 SpO2 고갈 → 의식 0 → `IsFatal` / `IsDeadState` (기존 Defeat·Dead 파이프라인).

탱크: GameData `dive_tank` · 인벤 **잠수 탱크 켜기/끄기** (`DiveTankService` / `DiveTankContextContributor`).

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
| `BodyCapacity.Breathing` | `LungEff × BloodOxygen01` |
| `BodyCapacity.Consciousness` | 뇌 × 혈량 × 감염 × 독 × **SpO2** |
