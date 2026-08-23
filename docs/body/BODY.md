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
| `BodyPartIds` | ID·`ThermalParts`·`SeverableParts`·`FrostbiteParts`·`VitalOrgans` SSOT |
| `CharacterBodyHost` | 엔티티별 `ICharacterBody` 소유 (플레이어·NPC 공용) |
| `CharacterClimateHost` | 공용 체온·습윤 틱. frostbite/heat. 엔티티별 outdoor |
| `BodyTemp` | 부위별 °C. 코어 getter = chest |
| `WearEnvExposure` | 습윤 0..1 (공식은 GEAR Phase E) |
| `WeatherExposure` | `Resolve(kind, period, outdoor)` → ambient °C / wetness gain |
| `BodyDamageService` | HP 타격. severable 0 → `RemovePart`. 뇌 제외 장기/몸통 0 → 강한 Bleed |
| `BodyCapacity` | 의식·펌프·호흡·여과·소화·이동·조작. **IsFatal = 의식 ≤ 0만** |
| `BodyIllness` / `BodyPain` | 출혈·감염·독소 상수 / PainTotal SSOT |
| `OrganHitResolver` | 머리/가슴/배 피격 → 장기 분배 (mitigate 후) |
| `BodyPartRestoreService` | `TryRegenerate` / `TryAttachProsthetic` |
| `BodyEffectTicker` | 효과 지속·Bleed→Blood01·발밑 맵 혈흔 drip·감염 레이스·독소 감쇠 (World delta) |
| `CharacterActionDelay` | 효과 배율 × Manipulation TickScale. [`../character/ACTION.md`](../character/ACTION.md) |
| `BodyLocomotionPenalties` | 절단 절뚝 × `GearEnvPenalties` (이속; Moving 용량과 비곱) |
| `CharacterBodyDto` / `BodyTempDto` | JsonUtility 왕복. **세이브 UI 없음**. Blood/Toxin/감염은 DTO 없음 |

---

## Anatomy

`CharacterBody.CreateHumanDefault` 트리. 루트: head, chest, 양 상완, 양 대퇴.

`VitalOrgans` (조준·절단·체온 밖): `brain`(head), `heart`/`lung_l`/`lung_r`(chest), `liver`/`stomach`/`kidney_l`/`kidney_r`(belly). 장기 HP는 `OrganCondition*` (STR 가산 없음).

`ThermalParts` (10, 체온 틱·표시): `head`, `chest`, `upper_arm_l/r`, `hand_l/r`, `thigh_l/r`, `foot_l/r`.

`SeverableParts`: 팔/다리 체인만 (`head`/`neck`/`chest`/`belly`/`pelvis`·장기 제외). HP 0이면 `BodyDamageService.ApplyHit` → `RemovePart`.

`FrostbiteParts`: `head`, `hand_l/r`, `foot_l/r`.

`StatusConditionParts`: Main + VitalOrgans (상태창 행).

소켓: `GetSocketParentId`. 상완/대퇴는 루트(`null`) — `TryAttach(null, …)`이 루트로 채운다. 부모는 남고 하위는 소유권으로 함께 도달 불가.

---

## Death / capacity

**사망 (`IsDeadState` / `BodyCapacity.IsFatal`)** = **의식 ≤ 0** 만. 바닐라 림월드의 심장·간 즉사는 Dist에 없음.

의식이 0이 되는 원인:

| 원인 | 설명 |
|------|------|
| 뇌 없음/HP0 또는 머리 부모 HP0 | 유일한 장기 즉사 |
| `Blood01` ≤ 0 | 과다출혈 |
| `InfectionProgress01` ≥ 1 | 감염이 면역을 이김 |
| `EffectivePain01` ≥ 1 | 고통이 의식을 0으로 |
| `Toxin01` ≥ 1 | 독소 |

펌프·호흡·여과·소화·이동·조작 0은 사망이 아님. 심장·폐·간·신장·위·목·가슴·배 HP0 → **강한 Bleed** (`BodyIllness.OrganDestroyedBleed*`), 즉사 아님. 가슴/배 0이면 무효 자식 장기마다 Bleed.

쓸어짐 (`CharacterPainHost.IsPainShocked`): 고통 ≥ 0.8 **또는** `BodyCapacity.IsCapacityDowned` (의식 &lt; 0.3 / Moving &lt; 0.15 / Breathing ≤ 0). Defeat/Dead 아님.

이속: `BodyLocomotionPenalties` 절뚝 유지. Moving을 이속에 곱하지 않음.

런타임만 (DTO 없음, FromDto 후 리셋): `Blood01`(기본 1), `Toxin01`, `InfectionProgress01`, `InfectionImmunity01`.

출혈 틱: Bleed intensity 합 → Blood01 감소 (부위 ApplyHit 없음). drain 누적 ≥ 문턱 시 발 월드에 맵 혈흔 스탬프 (`MapBloodHost`, [`docs/map/DATA.md`](../map/DATA.md)). 출혈 ≥ `InfectedOnsetSeconds` → Infected. 면역 × 여과 vs 진행. 독소는 여과로 감쇠.

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

관성 질량(밀침): `RemainingMassKg` + 착용·들기 kg. 과적 이동 배율에 kg를 다시 넣지 않는다. 소비처: `CombatImpulse.InertialMassKg` → `CharacterHitReact` / `CharacterAttacker.AddRecoilKick`.

상세: [`LOCOMOTION.md`](../locomotion/LOCOMOTION.md) · [`GEAR.md`](../equipment/GEAR.md) Phase H.

---

## PainTotal / 고통 쇼크

`BodyPain` / `CombatPain.PainTotal01` = 부위(+장기) 손실 HP 비율 × 부위 가중. 상해 타입 로그 없음. HitTag는 J가 아님.

`EffectivePain01` = PainTotal × painFactor (`adrenaline`이면 `AdrenalinePainFactor`).  
의식 = … × (1 − EffectivePain01) — **고통 1이면 사망**.

`CharacterPainHost`: effective ≥ `PainShockThreshold`(0.8) **또는** `BodyCapacity.IsCapacityDowned`이면 살아 있는 다운 — `SetMoveLocked` + 액션/큐 취소. Defeat/Dead가 아니다. 기상은 문턱 아래.

루팅: `PlayerInventoryHost.IsAvailableToPlayer`가 self이거나 `IsDefeated || IsPainShocked`.

HUD: `PlayerStatusMoodEntries`가 effective Pain ≥ `PainHudMin`이면 `MoodIconId.Pain`, ≥ `SeverePainHudMin`이면 `SeverePain`. 독소·저여과(`LowImmunity`)도 수집.

절단된 부위는 `GetConditionMax==0`이라 PainTotal에서 skip.

상수·Hurt 밀침: [`LOCOMOTION.md`](../locomotion/LOCOMOTION.md) 피격 밀침 / 상수 표.

---

## Pending

| 항목 | 상태 |
|------|------|
| 세이브/로드 UI | 없음 — DTO 왕복만 |
| 과적 ↔ `RemainingMassKg` | 이동 과적은 미연동. 밀침 질량은 `CombatImpulse.InertialMassKg`가 소비 |
| 낮/밤 라이팅 | TIME.md Pending. Period는 ambient만 |
