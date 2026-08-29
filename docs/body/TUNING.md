# Body tuning index (LLM entry)

> **숫자 진실원은 코드만.** 이 문서는 심볼·소비처 인덱스다. 값을 여기에 적지 않는다 (`no-hardcoding`).
> 동작·계약: [`BODY.md`](BODY.md) · 틱 구현: `Assets/Dist/Scripts/Entity/Combat/BodyEffectTicker.cs`

경로 prefix: `Assets/Dist/Scripts/Gameplay/Definitions/BN/`

---

## 빠른 진입 (주제 → SSOT)

| 주제 | SSOT 클래스 | 주 소비 |
|------|-------------|---------|
| 출혈 drain·베임 파생 Bleed | `BodyIllness` | `BodyEffectTicker.TickBleedBlood`, `BodyInjury.EnsureBleedFromOpenCut` |
| Bleed → `infected` onset (타이머, 확률 없음) | `BodyIllness` | `BodyEffectTicker.TickInfectionOnset` |
| 감염 진행 vs 면역 레이스 | `BodyIllness` | `BodyEffectTicker.TickInfectionRace` |
| 붕대 dirty·onset·tend | `BodyIllness` | `BodyEffectTicker`, `BodyInjuryTend`, `BodyHealApply` |
| 독소·부패·MED | `BodyIllness` | `BodyEffectTicker.TickToxinClear`, `ConsumeService` |
| 항생제 면역 배율 | `BodyIllness` | `ConsumeService`, `BodyEffectTicker.AntibioticImmunityMul` |
| 부상 tend 속도 | `BodyIllness` | `BodyInjuryTend` |
| 절단 소켓·장기 파괴 Bleed | `BodyIllness` | `BodyDamageService` |
| 절단 오버킬 확률 구간 | `BodySeverOverkill` (`BodyDamageService.cs`) | `BodyDamageService.ApplyHit` |
| 고통·쇼크 | `BodyPain` | `CharacterPainHost`, `PlayerStatusMoodEntries` |
| 의식·여과·HUD 임계 | `BodyCapacity` | `BodyCapacity.Consciousness`, Status HUD |
| heal·붕대·지혈 적용 | `BodyHealApply` | `ConsumeService`, `BodyEffectTicker` (dirty read) |
| 수영·잠수·산소·탱크 | `MapSwimConsts` (`Assets/Dist/Scripts/Map/Liquid/`) | `CharacterSwimHost`, `CharacterBreathHost` — 계약 [`../locomotion/SWIM.md`](../locomotion/SWIM.md) |

---

## `BodyIllness`

### Bleed

| 심볼 | 소비 |
|------|------|
| `BleedBloodPerIntensityPerSecond` | `BodyEffectTicker` — 상처 bleed → `Blood01` drain |
| `OrganBleedBloodPerIntensityPerSecond` | `BodyEffectTicker` — `organ_bleed` drain |
| `CutBleedMinIntensity` | `BleedIntensityForCut` |
| `BleedIntensityForCut(cutHp)` | `BodyInjury.SyncBleedFromOpenCut`, `EnsureBleedFromOpenCut` |

### Infection onset (부위 `infected` 부여)

| 심볼 | 소비 |
|------|------|
| `InfectedOnsetSeconds` | `BodyEffectTicker.TickInfectionOnsetNode` — Bleed age 임계 |
| `BandageCleanInfectedOnsetMul` | `BodyEffectTicker.InfectedOnsetMul` — 깨끗한 붕대 지연 |
| `BandageDirtyInfectedOnsetMul` | 동일 — dirty max 가속 |

Onset은 **주사위 없음**. `bleed > 0`인 동안 age가 `InfectedOnsetSeconds`에 도달하면 `infected` 부여.

### Infection race (전신 `InfectionProgress01` vs `InfectionImmunity01`)

| 심볼 | 소비 |
|------|------|
| `InfectedProgressPerSecond` | `BodyEffectTicker.TickInfectionRace` |
| `ImmunityPerSecond` | 동일 × `BodyCapacity.BloodFiltration` × 항생제 배율 |
| `InfectionConsciousnessK` | `BodyCapacity.Consciousness` |
| `LowImmunityFiltration` | `PlayerStatusMoodEntries` — 저여과 무드 |

면역 승리(`immunity >= 1`): `BodyEffectTicker.ClearAllInfected`.

### Bandage

| 심볼 | 소비 |
|------|------|
| `BandageTendMul` | `BodyInjuryTend` |
| `BandageDirtyMax` | `BodyHealApply.BandageDirty01`, dirty cap |
| `BandageDirtyBloodPerPoint` | `BodyEffectTicker.AbsorbIntoBandage` |
| `BandageMaxIntensity` | `BodyHealApply.TryApplyBandage` |
| `BandageSecondsPerIntensity` / `BandageMaxDurationSeconds` | **Dist 미사용** (레거시 BN 참고) |

### Toxin · rot · MED

| 심볼 | 소비 |
|------|------|
| `ToxinClearPerSecond` | `BodyEffectTicker.TickToxinClear` |
| `ToxinConsciousnessK` | `BodyCapacity.Consciousness` |
| `ToxinFiltrationK` | `BodyCapacity.BloodFiltration` |
| `ToxinMoodMin` | `PlayerStatusMoodEntries` |
| `RotToxinAdd` | `ConsumeService` (부패 섭취) |
| `MedToxinClear` | `ConsumeService.ApplyMedIllnessRelief` |
| `MedBleedIntensityReduce` | `ConsumeService` (베임 남은 부위는 틱에 복구) |

### Injury tend

| 심볼 | 소비 |
|------|------|
| `BruiseHealSeconds` | `InjuryHealSecondsPerHp` 기저 |
| `InjuryHealSecondsPerHp` | `BodyInjuryTend` (타박 1×) |
| `CutTendBruiseMul` / `GunshotTendBruiseMul` / `FractureTendBruiseMul` | `*TendSecondsPerHp` |

### Antibiotic

| 심볼 | 소비 |
|------|------|
| `MedImmunityGainMulWeak` / `Regular` / `Strong` | `ImmunityGainMul` → `TickInfectionRace` |
| `MedImmunityDurationSeconds` | `ConsumeService.TryApplyAntibiotic` |
| `AntibioticIntensity*` | `TryAntibioticIntensity` |

### Sever stump · organ destroyed Bleed

| 심볼 | 소비 |
|------|------|
| `SeverStumpBleedFinger` … `SeverStumpBleedRootLimb` | `BodyDamageService.ApplySeverStumpBleed` |
| `OrganDestroyedBleedHeart` … `Default` | `BodyDamageService` 장기 HP0 |

### Misc

| 심볼 | 비고 |
|------|------|
| `PrototypeBleedSeconds` | 프로토타입/디버그용 (onset보다 짧게) |

---

## `BodyPain`

| 심볼 | 소비 |
|------|------|
| `PainShockThreshold` / `PainWakeThreshold` | `CharacterPainHost` 쇼크 래치 |
| `PainHudMin` / `SeverePainHudMin` | `PlayerStatusMoodEntries` |
| `InjuryPainPerHp` | `BodyInjury.PainPerIntensity` (bruise/cut/gunshot) |
| `BleedPainPerIntensity` / `FracturePainPerIntensity` | `BodyPain.PartPain` |
| `AdrenalinePainFactor` | `BodyPain.PainFactor` |
| `WeightHead` … `WeightOther` | `BodyPain.PartWeight` |

---

## `BodyCapacity`

| 심볼 | 소비 |
|------|------|
| `ConsciousnessDownedThreshold` | `IsCapacityDowned` |
| `ConsciousnessHudMin` | Status 무드 Fading |
| `BloodHudMin` / `BloodHudCritical` | Pale 무드·툴팁 |
| `MovingDownedThreshold` | `IsCapacityDowned` |
| `MissingFootAsMove` / `MissingHandAsManip` | 다리·팔 결손 이동/조작 |
| `ManipTickMin` | `CharacterActionDelay` |

의식 공식 계수(`InfectionConsciousnessK`, `ToxinConsciousnessK`)는 `BodyIllness`.

---

## `BodySeverOverkill` (`BodyDamageService.cs`)

| 심볼 | 소비 |
|------|------|
| `CutMin` / `CutMax` | 사지 HP0 시 파괴 확률 구간 (`cut`) |
| `BulletMin` / `BulletMax` | 사지 `bullet` |
| `BashMin` / `BashMax` | 사지 `bash`·기타 |
| `CoreCut*` / `CoreBullet*` / `CoreBash*` | 머리/목/몸통/장기 — 사지보다 낮은 구간 |

감염 확률과 **무관** (파괴 성공 여부만). HP 0 ≠ 파괴.

---

## `BodyHealApply` (heal use_action)

| 항목 | SSOT |
|------|------|
| 즉시 HP·붕대·지혈 로직 | `BodyHealApply.TryApply` |
| 붕대 dirty 읽기 | `BandageDirty01`, `BandageDirty01Under` |
| eligibility | `CanApplyTo`, `TryCollectEligibleParts` |

붕대·감염 **수치**는 `BodyIllness`; heal power는 BN `UseActionData` (`GameItemDetailTypes`).

---

## `MapSwimConsts` (수영·산소)

경로: `Assets/Dist/Scripts/Map/Liquid/MapSwimConsts.cs` · 계약: [`../locomotion/SWIM.md`](../locomotion/SWIM.md)

| 심볼 그룹 | 소비 |
|-----------|------|
| `WadeFill01` / `SwimColumnMl` | `MapSwimQuery` |
| `WadeSpeedFactor` / `SwimSpeedFactor` / `DiveSpeedFactor` / `DiveVerticalSpeed` | `CharacterSwimHost`, `CharacterLocomotion` |
| `Oxygen*` / `BreathHoldDrainPerSecond` / `DiveTank*` | `CharacterBreathHost` |
| `LiquidWetnessGain*` | `CharacterClimateHost` |

---

## Needs (신체 밖)

위장·갈증·수면 τ: `PlayerNeedsSettings` + [`../needs/NEEDS.md`](../needs/NEEDS.md).
