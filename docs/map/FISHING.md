# Fishing

> Dist 낚시: **동적 액체 레이어 수심** + **인접 판정 SSOT** + 낚싯대 Cast + 통발 + 수중창 사거리 보정.
> 액체 SSOT: [`map/LIQUID.md`](LIQUID.md) · 맵 호스트: [`map/SYSTEM.md`](SYSTEM.md) · 농사 패턴: [`farming/FARMING.md`](../farming/FARMING.md)

| 영역 | 경로 |
|------|------|
| 판정·Cast/Trap | `Assets/Dist/Scripts/Map/Fish/MapFishService.cs`, `MapFishTrapHost.cs` |
| 플래그 | `TileFlags` |
| 품질 | `ItemQualityUtil` |
| 루트 SO | `FishingLootCatalog` — `Assets/Dist/SOData/Gameplay/Fishing/` (bootstrap SerializeField) |
| Work 클립 SO | `FishWorkClipCatalog` — 동일 SOData · bootstrap 주입 |
| 인터랙션 | `Assets/Dist/Scripts/Interactions/Fishing/` |

---

## Water detection (MapLiquidQuery)

낚시·통발·수중창은 **`TileFlags.SHALLOW_WATER`/`DEEP_WATER` 바닥재가 아니라** `MapLiquidHost` 오버레이 ml을 본다. 상세: [`LIQUID.md`](LIQUID.md).

| API | 임계 | 용도 |
|-----|------|------|
| `MapLiquidQuery.ColumnMlDownward(cell)` | `≥ MapFishConsts.FishableColumnMl` (2,000,000 ml) | Cast · 통발 — **수직 2셀 이상** 수심 |
| `MapLiquidQuery.Fill01(cell)` | `≥ MapFishConsts.UnderwaterShooterFill01` (= `MapLiquidConsts.ShallowSeedFraction`) | 수중창 — 발밑 **국소** 잠김 |

`MapFishService.CellHasFishableWater` / `IsFishableAdjacent` · `IsShooterInWater` (구 `IsShooterOnWaterFloor`).

### Legacy tile flags (에디터 저작 + 이주 중)

| Flag | 역할 |
|------|------|
| `SHALLOW_WATER` / `DEEP_WATER` | 에디터 floor **마커 별칭** — 둘 다 같은 물. Save bake·시드 모두 **cap 가득**. 깊이 구분은 `ColumnMlDownward`/`Fill01`. Play 검증 후 SO 하나로 합치거나 삭제 예정 |
| `FISHABLE` | 에셋 태깅·후속 확장용 (현재 판정 미사용) |

에디터 절차: [`LIQUID.md` §에디터 저작](LIQUID.md#에디터-저작-워터-프리팹-타일).

인접 Cast: `IsFishableAdjacent` / `IsWithinCastActionRange` — XZ Chebyshev ≤ 1.

---

## Routes

| 경로 | 동작 |
|------|------|
| 인벤 낚싯대 RMB | «낚시» → 물 셀 타겟 → Cast |
| 인벤 `fish_trap` RMB | «삽탄»(미끼) / «통발 설치» → 물 셀 Deploy |
| 타일 CM (물+트랩) | «수확» → CollectTrap |
| `CharacterActionKind.Cell` | Arrive → Work → Apply; 이동 입력 `CancelAll` |

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

BN `UNDERWATER_GUN` 무기 — 발사자 walkable 셀의 **액체 Fill01**로 판정.

| 발사자 셀 | effective range |
|-----------|-----------------|
| `Fill01 ≥ UnderwaterShooterFill01` | 정상 (`gun.range` + 탄약) |
| 육지·얕은 물 등 | × `MapFishConsts.UnderwaterGunLandRangeMultiplier` (0.1) |

`CombatHitscan.EffectiveRange` · `MapFishService.IsShooterInWater`

---

## Play test (IsoLand)

씬 `Map/FishingTest` — 스폰 `(-2,1,-2)` 인접 물. `map01.json`은 `liquidCells` + `hasLiquidSnapshot: true` (물 floor face 제거됨). **현재 웅덩이는 1겹이라 `FishableColumnMl` 미달 — 낚시 불가.** 분지 저작 후 재검증.

Ensure: `Dist/MCP/Ensure Sample ScriptableObjects` → SOData 카탈로그. `MapGameplayBootstrap`에 loot/work 클립 할당.

---

## Known debt

- BN 전체 fish species·계절·날씨 — `FishingLootCatalog` 점진 확장
- 타일 CM Cast/Deploy (인벤 경로만 구현)
- `place_trap` 일반화 — 통발 전용
