# Body (anatomy / climate)

> LLM/에이전트용 Dist 신체 트리·체온·절단·기후 호스트 SSOT.
> 인덱스: `docs/README.md`
> **anatomy / BodyTemp / sever / CharacterClimateHost 스크립트를 쓰거나 고치기 전에 이 문서를 읽는다.**

경로: `Assets/Dist/Scripts/Gameplay/Definitions/BN/` · `Assets/Dist/Scripts/Gameplay/Gear/BodyTemp.cs` · `Assets/Dist/Scripts/Entity/Character/CharacterClimateHost.cs`

관련: Wear/Wield·env 공식 [`docs/equipment/GEAR.md`](../equipment/GEAR.md) · 생성 스펙·PC/NPC [`docs/character/DEFINITION.md`](../character/DEFINITION.md) · 이동 배율 [`docs/locomotion/LOCOMOTION.md`](../locomotion/LOCOMOTION.md) · 시간 [`docs/time/TIME.md`](../time/TIME.md)

PC와 NPC를 이 문서에서 나누지 않는다. `CharacterKind` 없음. 조종 여부는 [`DEFINITION.md`](../character/DEFINITION.md) (`CharacterMotor.IsPossessed`).

---

## 역할

| Type | Role |
|------|------|
| `ICharacterBody` / `CharacterBody` | 소유권 트리. `RemovePart` / `TryAttach` / `ToDto` / `FromDto` |
| `BodyPartNode` / `BodyPartKind` | 노드. `Organic` / `Prosthetic` |
| `BodyPartIds` | ID·`ThermalParts`·`SeverableParts`·`FrostbiteParts` SSOT |
| `CharacterBodyHost` | 엔티티별 `ICharacterBody` 소유 (플레이어·NPC 공용) |
| `CharacterClimateHost` | 공용 체온·습윤 틱. frostbite/heat. 엔티티별 outdoor |
| `BodyTemp` | 부위별 °C. 코어 getter = chest |
| `WearEnvExposure` | 습윤 0..1 (공식은 GEAR Phase E) |
| `WeatherExposure` | `Resolve(kind, period, outdoor)` → ambient °C / wetness gain |
| `BodyDamageService` | HP 타격. 0 + severable → `RemovePart` |
| `BodyPartRestoreService` | `TryRegenerate` / `TryAttachProsthetic` |
| `BodyEffectTicker` | 효과 지속·출혈 (World delta) |
| `CharacterActionDelay` | `BodyPartEffect` → 행동 틱 배율. [`../character/ACTION.md`](../character/ACTION.md) |
| `BodyLocomotionPenalties` | 절단 절뚝 × `GearEnvPenalties` |
| `CharacterBodyDto` / `BodyTempDto` | JsonUtility 왕복. **세이브 UI 없음** |

---

## Anatomy

`CharacterBody.CreateHumanDefault` 트리. 루트: head, chest, 양 상완, 양 대퇴.

`ThermalParts` (10, 체온 틱·표시): `head`, `chest`, `upper_arm_l/r`, `hand_l/r`, `thigh_l/r`, `foot_l/r`.

`SeverableParts`: 팔/다리 체인만 (`head`/`neck`/`chest`/`belly`/`pelvis` 제외). HP 0이면 `BodyDamageService.ApplyHit` → `RemovePart`.

`FrostbiteParts`: `head`, `hand_l/r`, `foot_l/r`.

소켓: `GetSocketParentId`. 상완/대퇴는 루트(`null`) — `TryAttach(null, …)`이 루트로 채운다. 부모는 남고 하위는 소유권으로 함께 도달 불가.

---

## ClimateHost vs PlayerGearHost

경계 SSOT. Wear 합·헬멧 시야는 Gear. 체온 틱·실내외는 ClimateHost.

| | `PlayerGearHost` | `CharacterClimateHost` |
|--|------------------|------------------------|
| 누가 | 플레이어 Wear 호스트 (인벤 의존) | `CharacterBodyHost` 있는 엔티티 |
| 틱 | Gear timed + `HelmetVision` | `BodyTemp` / `WearEnvExposure` / frostbite·heat / env 이동 |
| 날씨 Kind | 씬 `WorldWeatherKind` (`SetWeatherKind`) | Kind만 읽음 |
| ambient 캐시 | ClimateHost에서 **포워드** (`Weather`) | `WeatherExposure` 1개. `Resolve(kind, period, outdoor)` |
| 실내외 | 안 함 | `TileMapCacheHub.IsOutdoorEvaluation` per entity |
| BodyTemp 소유 | ClimateHost에서 **포워드** | `_bodyTemp` 소유. `ApplyBodyTempDto` |
| Wear 없을 때 | (호스트 없음) | warmth 0, env_prot 0. Kind는 `Active` 또는 `Clear` |

`PlayerGearHost.BodyTemperature` / `EnvExposure` / `Weather`는 ClimateHost 참조. 틱·ambient 캐시는 ClimateHost `Update`만 (`TimeScaleService.Delta(World)`). GearHost는 Kind-only — 인자 없는 `WeatherExposure.Resolve()`(Day+야외)를 호출하지 않음.

Checklist: [`.claude/checklists/migration-parity.md`](../../.claude/checklists/migration-parity.md).

### Parity (체온 호스트)

| Before | After (기본 경로) |
|--------|-------------------|
| `PlayerGearHost`가 `BodyTemp.Tick` + `WearEnvExposure.Tick` | `CharacterClimateHost`가 같은 World dt로 틱 |
| 단일 코어 °C (`TotalWarmth`) | `ThermalParts`별 °C. 코어 getter = chest |
| NPC env 배율 = 1 (Phase H 스탠드인) | ClimateHost → `CharacterMotor.SetEnvMovement` |
| outdoor 없음 | 엔티티 바닥 셀 `IsOutdoorEvaluation` |
| Kind만 `WeatherExposure.Resolve()` | `Resolve(kind, WorldClock.Period, outdoor)` |

---

## BodyTemp (per-part)

코어: `BodyTempC` / `Feeling` / `TargetTempC` = chest. 가슴 없으면 `ComfortBodyTempC`.

`Tick(dt, wetness01, ambientTempC, warmthIn, presentIn)` — 배열 길이 = `ThermalParts`. 없는 부위는 스킵.

| Name | Formula |
|------|---------|
| `Target` | `ambient + WarmthForPart × DegreesPerWarmth − Wetness01 × WetnessCool` (부위 min/max clamp) |
| 수렴 | `temp + (target − temp) × ConvergencePerSecond × dt` |
| Heat flow | arms←chest, hands←arms, legs←chest, feet←legs (`HeatFlow*PerSecond`) |

Consts: `ComfortBodyTempC=37`, 코어 min/max `27`/`43`, 말단 min/max `12`/`48`, `BaseAmbientTempC=18`, `DegreesPerWarmth=0.5`, `WetnessCoolDegreesC=2`, `ConvergencePerSecond=0.08`, `ComfortBandHalfWidthC=1`, `HypothermiaBodyTempC=32`.

`Feeling` / `GearEnvPenalties`는 **코어 Feeling만** (부위별 Feeling은 frostbite 판정용 `FeelingForPart`).

```mermaid
flowchart LR
  kind[WorldWeatherKind]
  period[WorldClock_Period]
  outdoor[IsOutdoorEvaluation]
  wx[WeatherExposure_Resolve]
  wear[WearStatsAggregator]
  env[WearEnvExposure_Tick]
  temp[BodyTemp_Tick]
  host[CharacterClimateHost]
  kind --> wx
  period --> wx
  outdoor --> wx
  wx -->|ambientTemp| temp
  wx -->|wetnessGain| env
  wear -->|WarmthForPart| temp
  wear -->|env_prot| env
  env -->|Wetness01| temp
  host --> env
  host --> temp
```

---

## Weather / outdoor

`WeatherExposure.Resolve(WeatherKind kind, DayPeriod period, bool outdoor)`:

- 실내: `IndoorAmbientTempC` (= Clear 18°C), wetness 0. 비/바람·기간 오프셋 무시
- 야외: kind ambient + `ResolvePeriodOffsetC` (`Night=-6`, `Dawn=-3`, Day/Dusk=0)
- Kind: Clear 18 / Rain 10 / Wind `Clear − WindChillDegreesC(4)`
- Wetness/s 야외: Clear 0 / Rain 0.02 / Wind 0.002

`CharacterClimateHost.ResolveOutdoor`: `OccupiedCellCoord` → `TileMapCacheHub.Runtime.IsOutdoorEvaluation(floor.y, x, z)`. 허브 없으면 outdoor true.

레거시 `Resolve()` = Day + outdoor true (Kind만 바꿀 때).

---

## Frostbite / heat

ClimateHost, World초.

| 효과 | 조건 | 부여 |
|------|------|------|
| `BodyPartEffectIds.Frostbite` | `FrostbiteParts`가 Cold 이하 `FrostbiteOnsetSeconds`(30) 유지 | 해당 부위, 이미 있으면 스킵 |
| `BodyPartEffectIds.Heat` | 코어 Feeling ≥ Hot `HeatOnsetSeconds`(20) | `chest` |
| 극단 코어 피해 | `BodyTempC` ≤ min 또는 ≥ max | `BodyDamageService.ApplyHit(chest, ExtremeCoreDamage=1)` 매 `ExtremeCoreDamageIntervalSeconds`(4) |

HUD: `PlayerStatusMoodEffectCatalog` — Frostbite→`Hypothermia`, Heat→`Overheated`. 코어 Feeling 행은 `PlayerStatusMoodEntries.CollectCoreFeeling` (`HypothermiaBodyTempC` / TooCold / TooHot / Warm / Comfortable).

체온 탭 그래픽: `PlayerStatusBodyGraphicDisplay` — 부위 `TryGetPartTempC` vs Comfort 편차. 없는 부위 `present=false`.

---

## Sever / restore

| API | 계약 |
|-----|------|
| `ICharacterBody.RemovePart` | 부모 컬렉션에서 노드 제거. 소켓(부모) 유지 |
| `ICharacterBody.TryAttach(parentId, node)` | 런타임 복원 전용. `parentId` 비면 루트. 같은 partId 있으면 false |
| `BodyDamageService.ApplyHit` | 메인 컨디션 HP. 0이고 `IsSeverable`이면 `RemovePart` |
| `BodyPartRestoreService.TryRegenerate` | `TryCreateLimbFrom` + `TryAttach` + `Regenerating` 효과 |
| `BodyPartRestoreService.TryAttachProsthetic` | 같은 부착, `BodyPartKind.Prosthetic`, Regenerating 없음 |

이미 있는 부위·비-severable·부모 없음 → restore false. Prosthetic는 `BodyEffectTicker` 출혈 스킵.

질량: `CharacterAppearanceHost.RemainingMassKg` = `bodyMassKg` − 없는 `partMasses` kg. **과적(encumbrance) 미연동** — [`DEFINITION.md`](../character/DEFINITION.md).

---

## DTO (왕복, 저장 UI 아님)

`JsonUtility.ToJson` / `FromJson`. 세이브 슬롯·파일 경로 없음. 에디터 ContextMenu: `CharacterBodyHost` `CharacterBodyDtoRoundTrip.Execute`, `CharacterClimateHost` `BodyTemp.ExecuteDtoRoundTripVerify`. 런타임 적용: `CharacterBodyHost.ApplyBodyDto`, `CharacterClimateHost.ApplyBodyTempDto`.

| DTO | 필드 | 왕복 |
|-----|------|------|
| `CharacterBodyDto` | `parts[]` | `ICharacterBody.ToDto` / `FromDto` |
| `CharacterBodyPartDto` | `partId`, `parent`, `kind`, `hasCondition`, `conditionCur`/`Max`, `effects[]` | 트리 재부착 |
| `BodyPartEffectDto` | `effectId`, `intensity`, `remainingSeconds` | |
| `BodyTempDto` | `parts[]` (`partId`, `tempC`)만. wetness 없음 | `BodyTemp.ToDto` / `FromDto`. 생략 부위는 tracked 아님 |

---

## Locomotion env

ClimateHost가 **같은 값**을 넣는다:

- `CharacterMotor.SetEnvMovement(factor)` — NPC·비possessed 포함
- possessed: `PlayerMovement.SetEnvMovement(factor)` 동일 값

`factor = BodyLocomotionPenalties.CombinedMoveSpeedFactor(body, Feeling, Wetness01)`  
= `GearEnvPenalties.MoveSpeedFactor` (코어 Feeling + wetness) × 절뚝.

절뚝: 대퇴 없음 `0.5` (그 쪽 발은 스택하지 않음). 대퇴 있고 발만 없음 `0.8`. 양측 곱.

LiftStrain 배율만 `PlayerGearHost` (별 슬롯). 히트 배율: `CharacterAttacker.ResolveAttackerEnvAccuracyFactor` — ClimateHost `Feeling` + wetness. ClimateHost 없으면 1.

상세: [`LOCOMOTION.md`](../locomotion/LOCOMOTION.md) · [`GEAR.md`](../equipment/GEAR.md) Phase H.

---

## Pending

| 항목 | 상태 |
|------|------|
| 세이브/로드 UI | 없음 — DTO 왕복만 |
| 과적 ↔ `RemainingMassKg` | 미연동 |
| 낮/밤 라이팅 | TIME.md Pending. Period는 ambient만 |
