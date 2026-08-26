# Fishing

> Dist 낚시: **물 바닥재 플래그** + **인접 판정 SSOT** + 낚싯대 Cast + 통발 + 수중창 사거리 보정.
> 맵 호스트: [`map/SYSTEM.md`](SYSTEM.md) · 농사 패턴: [`farming/FARMING.md`](../farming/FARMING.md)

| 영역 | 경로 |
|------|------|
| 판정·Cast/Trap | `Assets/Dist/Scripts/Map/Fish/MapFishService.cs`, `MapFishTrapHost.cs` |
| 플래그 | `TileFlags` |
| 품질 | `ItemQualityUtil` |
| 루트 SO | `FishingLootCatalog` — SOData + `Resources/Fishing/FishingLootCatalog` |
| Work 클립 SO | `FishWorkClipCatalog` — SOData + `Resources/Fishing/FishWorkClipCatalog` |
| 인터랙션 | `Assets/Dist/Scripts/Interactions/Fishing/` |

---

## Tile flags

| Flag | 역할 |
|------|------|
| `SHALLOW_WATER` | 얕은 물 (`Floor/ShallowWater`) |
| `DEEP_WATER` | 깊은 물 (`Floor/DeepWater`) |
| `FISHABLE` | 낚시·트랩 설치 가능 표면 (물 타일에 동시 부여) |

`MapFishService` 물 판정은 **`SHALLOW_WATER` / `DEEP_WATER`** 기준. `FISHABLE`은 에셋 태깅·후속 확장용.

인접 Cast: `IsFishableAdjacent` / `IsWithinCastActionRange` — XZ Chebyshev ≤ 1.

---

## Routes

| 경로 | 동작 |
|------|------|
| 인벤 낚싯대 RMB | «낚시» → 물 셀 타겟 → Cast |
| 인벤 `fish_trap` RMB | «삽탄»(미끼) / «통발 설치» → 물 셀 Deploy |
| 타일 CM (물+트랩) | «수확» → CollectTrap |
| `CharacterActionKind.Map` | Arrive → Work → Apply; 이동 입력 `CancelAll` |

---

## Loot & rod quality

- `FishingLootCatalog`: shallow/deep 가중치, `FISH_POOR` / `FISH_GOOD` 배율
- BN `FISHING` quality: `ItemQualityUtil.HasQuality(item, "FISHING", 1)`

---

## Trap storage

`TileSaveData` trap 필드 (`fishTrapBaitId`, `fishTrapBaitRemaining`, `fishTrapDeployedMinute`, `fishTrapAccumulatedFish`) + `MapFishTrapSaveBuffer` DTO 왕복.

- 미끼: `fish_bait` → `fish_trap` `WeaponAmmoService` tool charges
- Tick: `WorldClock` + `MapFishConsts.TrapTickIntervalMinutes` (Catch-up on load / collect / CM open)

---

## Underwater gun (speargun)

BN `UNDERWATER_GUN` 무기 — 발사자 walkable 셀 바닥재로 판정.

| 발사자 셀 | effective range |
|-----------|-----------------|
| `SHALLOW_WATER` / `DEEP_WATER` | 정상 (`gun.range` + 탄약) |
| 육지 등 | × `MapFishConsts.UnderwaterGunLandRangeMultiplier` (0.1) |

`CombatHitscan.EffectiveRange` · `MapFishService.IsShooterOnWaterFloor`

---

## Play test (IsoLand)

씬 `Map/FishingTest` — 스폰 `(-2,1,-2)` 인접 물 바닥재. `map01.json` `floorFaces[]` 동기화 필요.

Ensure: `Dist/MCP/Ensure Sample ScriptableObjects` → Resources mirror 포함.

---

## Known debt

- 수영·잠수 (`DIVE_TANK`) — locomotion 별도
- BN 전체 fish species·계절·날씨 — `FishingLootCatalog` 점진 확장
- 타일 CM Cast/Deploy (인벤 경로만 구현)
- `place_trap` 일반화 — 통발 전용
