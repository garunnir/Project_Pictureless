# Farming

> Dist 농사: 맵 뷰는 Dist 오버레이, 규칙은 `TileDefinition` 플래그. 청크 TileView와 모델이 분리된다.
> 인덱스: [`docs/README.md`](../README.md) · 맵 호스트: [`map/SYSTEM.md`](../map/SYSTEM.md) · 시계: [`time/TIME.md`](../time/TIME.md)

경로(호스트·모델·뷰): `Assets/Dist/Scripts/Map/Plant/`  
경로(규칙): `PlantGrowth` (`Gameplay/Definitions/BN/PlantGrowth.cs`) · `TileFlags`  
경로(행위): `MapPlantService` · 인벤/타일 컨텍스트 메뉴 (plant / harvest / till / fertilize)

---

## View = Dist vs rules = flags

맵에 보이는 작물은 BN furniture `symbol` / `looks_like` / `examine_action` / tileset 스프라이트가 **아니다**.  
뷰는 Dist primitive(또는 Dist 프리팹)만: `MapPlantOverlayVisual`이 `PlantGrowthStage`마다 메시·색·스케일을 고른다.

규칙은 Dist `TileDefinition.flags` (BN-style 이름):

| Flag | 역할 |
|------|------|
| `PLANTABLE` | 심기 게이트. 바닥(예: GrassFloor) 또는 가구(예: `Furniture/Planter`) |
| `PLOWABLE` / `DIGGABLE` | 경작(till) 게이트. DIG 품질 도구 |
| `GREENHOUSE` | 서리 스킵. 바닥 또는 가구. OccupiedCell `PLANTABLE` 가구도 서리 스킵에 참여 |

BN `terrain_furniture.json`의 farming whitelist는 규칙·시드 데이터의 소스일 뿐, 맵 뷰에 쓰지 않는다. Dist `f_planter` 스프라이트 임포트 없음. 화분 대용은 Dist `Planter` TileDefinition이 기존 Crate 프리팹을 재사용한다.

---

## Chunk model vs view

`MapPlantOverlay` 모델(`plantCells` + till `HashSet`)은 청크 TileView unload와 무관하다. `MapPlantHost`가 로드/세이브하고, 뷰는 호스트 아래 Dist 오버레이 GO만 그린다. 혈흔의 `MapBloodHost`와 같은 패턴 — 그리기만 인스턴스/GO, 모델은 맵 JSON 별 레이어.

경작(till)도 같은 오버레이 모델이다. 경작된 셀은 `IsTilled`이면 바닥 플래그 없이 `PLANTABLE` 게이트를 통과한다. till 전용 Dist 메시는 없다.

---

## Grow from world minutes

분당 growth tick 없음. `elapsed = CurrentWorldMinute - plantedWorldMinute`를 `seed.grow_minutes`와 비교한다 (`PlantGrowth.Resolve`).

| Stage | 조건 (effective grow 기준) |
|-------|---------------------------|
| Seed | `elapsed < 0.25 × grow` |
| Seedling | `≥ 0.25` |
| Mature | `≥ 0.75` |
| Harvestable | `≥ grow` |
| Withered | `≥ grow + WitherSlackMinutes` 또는 frost kill |

맵 오버레이 색/스케일은 같은 `PlantGrowthStage`를 쓴다. 오버레이 Resolve는 비료+경과만 반영한다 (아래 Weather).

---

## Seasons / frost

`WorldCalendar.Season` / `SpanIncludesSeason`(심은 날~현재 날 inclusive)으로 겨울 구간을 본다. 서리는 기후 frostbite가 아니라 그 day-span 플래그 (`PlantGrowthContext.FrostKills`).

서리 적용: 야외이고 온실/화분이 아닐 때. 실내(`IsOutdoorCell == false`)는 스킵. `GREENHOUSE` 타일 또는 OccupiedCell `PLANTABLE` 가구(Planter)도 스킵. 판정은 inspect/CatchUp 경로의 `PlantGrowth.Resolve`.

---

## Fertilizer

셀당 1회 (`PlantCell.Fertilized`). `FERTILIZER` 플래그 아이템. `PlantGrowth.FertilizerGrowFactor`(0.5)로 필요 grow 분을 줄인다.

---

## Weather Kind assumption

성장 배율은 `PlayerGearHost.Active.WorldWeatherKind` 하나를 전제로 한다 (월드 타일 날씨 그리드 없음).

| Kind | Factor |
|------|--------|
| Clear (그 외) | `WeatherClearGrowFactor` (1) |
| Rain | `WeatherRainGrowFactor` (0.75, 빠름) |
| Wind | `WeatherWindGrowFactor` (1.25, 느림) |

inspect/harvest CatchUp는 이 Kind를 쓴다. 맵 오버레이 외형은 Clear factor로 그린다.

---

## No hunger CatchUp

식물 CatchUp은 wither/byproduct·제거만. `PlayerNeedsHost` 허기/갈증을 맵 로드나 시간 점프로 따라잡지 않는다.
