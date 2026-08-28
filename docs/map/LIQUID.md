# Map Liquid — 액체 시뮬레이션 SSOT

> LLM/에이전트용 Dist 맵 액체 SSOT. 스크립트 수정 전 이 문서를 읽는다.
> 진입: [`docs/map/SYSTEM.md`](SYSTEM.md) · 경로: `Assets/Dist/Scripts/Map/Liquid/`

## 개요

BN(Cataclysm: Bright Nights)의 셀당 1000L 용량·ml 콘텐츠와, Minecraft류 셀룰러 오토마타 흐름을 결합한
compressible-water CA. `SHALLOW_WATER`/`DEEP_WATER` 정적 바닥 태그를 동적 ml 레이어로 흡수한다.

## 셀 표현

`MapLiquidCell` (`Assets/Dist/Scripts/Map/Liquid/MapLiquidCell.cs`):

- `byte Level` — 대략치, 렌더 LOD·직렬화 압축용
- `ushort RemainderMl` — 정밀 잔여값
- `int EffectiveMl => Level * MlPerLevel + RemainderMl` — **flow 연산은 이 값만 사용**

상수 (`MapLiquidConsts.cs`):

| 상수 | 값 | 의미 |
|------|-----|------|
| `DefaultMaxVolumeMl` | 1,000,000 (BN 1000L) | 셀당 최대 ml (현재 전 셀 공통 — terrain별 bake는 향후 과제) |
| `MaxLevel` | 255 | byte 표현 상한 |
| `MlPerLevel` | ≈3921 | `DefaultMaxVolumeMl / MaxLevel` |
| `MinFlowMl` | ≈490 | 이 이하 diff는 흐르지 않음 — 진동·튐·폭주 방지의 단일 게이트 |
| `OverCompressMl` | ≈196 | 수직 압축 시 아래 칸의 여유. 중력 목표(1번)와 수직 탈출 임계(3번)가 **공유하는 상한** — § 확산의 유한성 |
| `MaxUpdatesPerTick` | 512 | `WorldClock.MinuteChanged` 1회당 처리할 dirty 셀 상한 |

## Flow 알고리즘 (`MapLiquidFlowSolver.cs`)

`WorldClock.MinuteChanged` 1회당 dirty 큐에서 최대 `MaxUpdatesPerTick`개를 pop해 처리한다. 순서:

1. **중력** — self에 바닥이 없으면(`!hub.CellHasFloor(self)`) 아래 칸과의 2-셀 stable-state를 계산해 그 값까지 즉시 채움
2. **수평 equalize** — 4방향 이웃과 `EffectiveMl` diff의 1/4을 이동. `diff <= MinFlowMl`이면 그 방향은 스킵(정지 조건)
3. **수직 탈출** — 2번에서 어느 방향으로도 옮길 게 없었는데(`diff`가 전부 `MinFlowMl` 이하) 여전히 `EffectiveMl > capMl + OverCompressMl`(압축 상한 초과)이면, 위 칸에 바닥이 없는 한 그 초과분을 위 칸에 **실제로** 옮긴다(표면 눈속임 아님, 진짜 `MapLiquidCell` 엔트리). 위 칸도 dirty로 등록되어 다음 처리에서 동일한 1~3 규칙을 재귀적으로 받는다.
4. 차단: `TryGetEdgeBetween` + `TileCollisionFlagsUtil.EdgeBlocksPassage`(수평), `CellHasFloor`(수직)

**거절이 없는 이유**: 오픈 지형에서 위 칸은 거의 항상 열려 있으므로 3번이 항상 탈출구를 제공한다. 완전 밀폐(위도 막힘)는 지형이 아니라 컨테이너(아이템/탱크) 정의의 몫이며, 그 경우는 `MapLiquidMlBridge.Pour` 호출 이전에 소비자가 걸러야 한다.

### 확산의 유한성

- 공간: 총 물량 `V`가 `N`칸에 퍼지면 칸당 `V/N`. `V/N <= MinFlowMl`이 되면 그 경계에서 정지 → `N_max ≈ V / MinFlowMl`
- 시간(수평): 2번의 이동은 `diff / 4`뿐이라 `|diff|`를 줄이는 방향으로만 발생하고 오버슈트가 없다(단조감소) → 유한 스텝 내 정지
- 시간(수직): 1번과 3번은 서로 반대 방향으로 물을 옮기므로 단조감소 논증이 통하지 않는다. 대신 **두 단계가 아래 칸 상한을 같은 값으로 공유**해 고정점을 만든다:
 - 1번의 목표 `StableBelowMl`은 최대 `capMl + OverCompressMl`
 - 3번은 `capMl + OverCompressMl` **초과분만** 위로 보냄
 - → 아래 칸이 상한 이하면 3번이 쉬고, 상한을 넘으면 3번이 상한까지 내린 뒤 1번의 `moveDown`이 0 이하가 되어 쉰다. 왕복 불가.
 - 3번째 분기(`total >= 2*capMl + OverCompressMl`)는 위 칸이 이미 `capMl` 이상이라 `room == 0`으로 3번이 진입만 하고 아무것도 옮기지 않는다.

**두 상한을 어긋나게 바꾸지 말 것.** 3번의 임계를 `capMl`로 낮추면 1번이 올린 압축분을 3번이 즉시 내리는 무한 왕복이 되고, dirty 큐가 비지 않아 § 정적 셀 무연산 보증까지 깨진다. 현재 `OverCompressMl`(≈196) < `MinFlowMl`(≈490)이라 그 왕복이 게이트에 우연히 걸리지만, `OverCompressMl`을 원전 수준(정원의 2%)으로 올리면 게이트가 막지 못한다 — 정지 보증은 상한 일치에서만 나온다.

### 정적 셀 무연산 보증 (바다맵 폭주 방지)

세 곳에서 명시적으로 지켜야 한다:

1. **시드는 dirty를 유발하지 않는다** — `MapLiquidOverlay.SeedFromTileFlags`/`SeedEffectiveMl`은 `MarkDirty`를 호출하지 않음. 균일한 정지 바다는 시드 직후 dirty 큐가 비어 있다.
2. **FlowSolver는 순수 반응형** — `ProcessDirty`는 큐 pop만 한다. 전체 overlay를 훑는 폴링 로직 금지. dirty 진입점은 `MarkDirty` 호출(흐름 발생 시 이웃, `MapLiquidMlBridge.Pour/Draw`)뿐.
3. **렌더러/쿼리는 좌표 단건 조회만** — `MapLiquidQuery`는 전체 순회 API를 제공하지 않는다. `MapLiquidSurfaceRenderer`도 오버레이를 매 프레임 순회하지 않고, `CellChanged` 통지를 받은 청크만 다시 굽는다(§ 수면 렌더러).
4. **맵 밖 무한 확산 차단** — `MapLiquidFlowSolver.IsTargetEligible`이 `hub.CellHasOccupancy`로 걸러, 맵에 정의되지 않은 셀로는 흐르지 않는다(플랜 문서에는 없던 추가 안전장치 — 미정의 void로의 무한 낙하/확산을 원천 차단).

## ml ↔ 셀 (`MapLiquidMlBridge.cs`)

비대칭 규칙, 항상 플레이어 손해 방향:

- **Pour**: 요청 ml **전액**이 셀에 반영, 호출부는 반환값(=요청값) 전량을 인벤에서 차감. cap 초과분은 소멸이 아니라 `MarkDirty`로 위임되어 다음 틱부터 FlowSolver가 이웃/위로 전달.
- **Draw**: `Min(요청, 셀 보유량)`만 정확히 지급, 낭비 없음.

## 수면 렌더러 (`MapLiquidSurfaceRenderer` / `MapLiquidChunkMesher`)

경로: `Assets/Dist/Scripts/Map/Liquid/`. 셰이더 `Assets/Dist/Visual/View/Shaders/Liquid/MapLiquidSurface.shader`, 머티리얼 `Assets/Dist/Resources/Map/MapLiquidSurface.mat`. 상수 SSOT는 `MapLiquidRenderConsts`.

머티리얼이 `Resources` 아래 있는 이유: `MapLiquidHost`는 씬에 배치되지 않고 `TileMapManager.SetupMapLiquid`이 `AddComponent`로 만든다. 따라서 Inspector 참조가 비어 있는 것이 기본값이고, 그대로 두면 셰이더가 빌드에 포함되지 않는다. 해석 순서는 **Inspector → `Resources.Load` → `Shader.Find`**.

### 통지 계약

`MapLiquidOverlay`가 두 이벤트를 낸다. sim의 dirty 큐와는 **별개**다.

| 이벤트 | 발생 | 렌더러 반응 |
|--------|------|-------------|
| `CellChanged(cell)` | `AddEffectiveMl` (증감·제거 전부) | 해당 청크를 dirty로. 경계 셀이면 맞닿은 청크, 모서리 셀이면 대각 청크까지 (코너를 공유하므로) |
| `BulkChanged` | `Clear` / `LoadFromDto` / `SeedFromTileFlags` 완료 | 전체 무효화 후 청크 목록 재구성 |

시드는 셀 단위로 통지하지 않는다(§정적 셀 무연산 보증 1항 유지). 바다맵 시드에서 수십만 건 통지가 나가지 않게 `SeedFromTileFlags`가 끝난 뒤 `BulkChanged` 1회만 낸다.

### 이음매 없는 연결

수면 높이는 셀이 아니라 **격자 코너**에서 결정된다. 코너 값은 그 코너를 공유하는 4개 셀의 평균이고, 이웃 셀을 오버레이에서 직접 읽으므로 청크 경계 밖 셀도 같은 값이 나온다. 따라서 인접 셀·인접 청크가 같은 코너에서 **정확히 같은 높이**를 만든다.

| 상황 | 처리 |
|------|------|
| 이웃 수위가 다름 | 코너 평균으로 경사 — 계단 없음 |
| 물가(마른 이웃) | 마른 이웃을 0으로 쳐 수면이 내려앉음. 색은 젖은 이웃 평균만 써서 얕아 보이지 않게 분리 |
| 위 칸에도 물(잠긴 셀) | Fill 1로 취급. 코너 4개 중 하나라도 잠겼으면 코너를 셀 천장(1.0)으로 올려 **층 사이 구멍**을 막는다 |
| 물끼리 맞닿은 면 | 측면을 만들지 않음 — 내부 면 알파 겹침 없음 |

파도·폼 노이즈 UV는 셀 로컬이 아니라 **월드 XZ**라 패턴도 경계에서 이어진다.

### 정점 색 계약 (`MapLiquidChunkMesher` → 셰이더)

| 채널 | 의미 |
|------|------|
| `r` | `depth01` — 젖은 이웃 기준 Fill01 평균. 얕은색↔깊은색 lerp |
| `g` | `foam01` — 마른 이웃 비율. 물가 폼 밴드 |
| `b` | `isTop` — 1 = 수면(윗면), 0 = 측면 |

### 렌더 경로

- 씬 **Depth/Opaque 텍스처를 쓰지 않는다.** 깊이는 시뮬 `Fill01`, 물가는 이웃 마스크가 대신하므로 URP 에셋의 `m_RequireDepthTexture`/`m_RequireOpaqueTexture`를 켤 필요가 없다.
- 화면 픽셀화는 `DistPixelisationFeature`(스크린 포스트)가 일괄 적용한다 — 물 셰이더는 ProPixelizer 오브젝트 셰이더가 아니어도 된다(맵 타일도 동일).
- 셰이더 시간은 `_MapLiquidTime` 전역 프로퍼티. 렌더러가 `TimeScaleService.TimeNow(TimeScaleChannel.World)`로 매 프레임 채워, 배속·정지가 파도에 반영된다(`Time.timeScale` 미사용).
- 그리기는 `Graphics.RenderMesh` 청크 단위. 청크 분할 SSOT는 `TileMapChunkStreamer.ChunkSize`이며, 스트리밍이 없으면 `MapLiquidRenderConsts.FallbackChunkSize`.

### 비용 계약

| 항목 | 계약 |
|------|------|
| 매 프레임 순회 | **로드된 청크**만(`CollectLoadedChunks` ∩ 물 청크). 맵 전체 물 청크 수와 무관 |
| 층 스캔 범위 | 청크마다 **자기 Y 범위**만. 리메시 때 액체가 남아 있는 범위로 다시 좁힌다 (삼각형 유무가 아니라 **액체 유무** 기준 — 사방이 물인 내부 층은 노출면이 0개라 삼각형도 0개다) |
| 빈 층 | 셀 수만큼의 조회 후 즉시 스킵 — 코너 계산·정점 생성 없음 |
| 리메시 예산 | 신규 `MaxChunkBuildPerFrame`, 갱신 `MaxChunkRemeshPerFrame`. 갱신은 FIFO 큐라 starvation 없음 |
| 대량 교체 | `BulkChanged`가 한 프레임에 여러 번 와도 재구성은 1회로 합친다 |

### 배선

`TileMapManager.SetupMapLiquid()`가 `BindMapContext` → `BindRenderContext` → `LoadFromDto` 순으로 호출한다. **`BindRenderContext`가 `LoadFromDto`보다 먼저**여야 로드 시 발생하는 `BulkChanged`를 렌더러가 받는다. 머티리얼은 `MapLiquidSurfaceRenderer`의 Inspector에 지정한다(비우면 `Shader.Find` 폴백 — 에디터 전용이므로 빌드에서는 반드시 지정).

## 에디터 저작 (워터 프리팹 타일)

에디터에는 `MapLiquidHost`가 없으므로 **워터 floor 프리팹을 깔고 Save**하는 것이 liquid 저작 경로다.

| 단계 | 동작 |
|------|------|
| 1 | `TileMapManager` → Load Editor (또는 기존 TileContainer) |
| 2 | `Floor/ShallowWater` 또는 `Floor/DeepWater` 프리팹을 floor face로 배치 (`TileView.gridPos` = CellBelow 앵커) |
| 3 | `MapFileSaver` → **Save Map To JSON** |
| 4 | `MapLiquidAuthoringBake`가 `SHALLOW_WATER`/`DEEP_WATER` floorFaces → walkable 셀(`CellAbove` = 앵커 y+1) `liquidCells`로 bake, `hasLiquidSnapshot: true` |

시드량: Deep = `DefaultMaxVolumeMl`, Shallow = `× ShallowSeedFraction` (`MapLiquidAuthoringBake.TryResolveSeedMl` — 런타임 `SeedFromTileFlags`와 동일).

**우선순위 (저장):** Play `MapLiquidHost` → 에디터 워터 floor bake → 디스크 liquidCells 계승.  
워터 face가 **하나도 없으면** bake하지 않고 기존 `liquidCells`를 계승한다(타일만 지운다고 liquid가 비워지지 않음 — 비우려면 JSON의 `liquidCells`를 비우거나 Play에서 Draw).

**Play 겹침:** bake 후에도 floorFaces에 워터 프리팹이 남아 있으면 타일 메시와 수면 렌더러가 겹칠 수 있다. 수면만 보려면 floorFaces에서 워터를 제거한 뒤 다시 저장(이때는 liquidCells가 계승됨). 워터 SO·프리팹 자체 삭제는 Play 검증 후(§남은 작업).

## 저장 (`MapLiquidCellSaveData` / `MapSaveJsonDto.liquidCells`)

```json
"liquidCells": [{ "x":0,"y":1,"z":0, "typeId":"water", "level":5, "remainderMl":250 }],
"hasLiquidSnapshot": true
```

- `hasLiquidSnapshot == true` → 저장된 상태만 신뢰, 재시드 안 함(플레이어가 비운 웅덩이가 다시 안 참)
- `false`(레거시/최초 로드) → `SeedFromTileFlags`로 1회 시드(`SHALLOW_WATER` = capMl × 0.35, `DEEP_WATER` = capMl)
- `EffectiveMl == 0` 셀은 저장 시 제외(스파스 유지)

## 조회 (`MapLiquidQuery.cs`)

`GetEffectiveMl(cell)` / `Fill01(cell)` / `TryGetTypeId(cell)` / `HasAnyLiquid(cell)` — 전부 좌표 단건 조회. `MapLiquidHost.Runtime`이 없으면 0/false 반환(안전 폴백).

### 국소 충만도 vs 컬럼 수심

`Fill01`만으로는 **몇 셀 깊이인가**를 알 수 없다. 한 셀은 `capMl + OverCompressMl`에서 클램프되므로 얕은 물 한 겹과 깊은 분지의 최상단 셀이 같은 `Fill01 = 1`을 낸다. 깊이를 요구하는 소비처는 `ColumnMlDownward(topCell)`을 써야 한다.

`ColumnMlDownward`는 `topCell`에서 아래로 내려가며 ml을 누적하고, **물이 없는 셀을 만나면 즉시 멈춘다**. 상한은 `MapLiquidConsts.MaxColumnScanCells`이며, 물이 끊기는 지점에서 먼저 멈추므로 실제 비용은 보통 1~2셀이다(§정적 셀 무연산 보증 3항 — 전체 순회 아님).

| 소비처 | 판정 | 이유 |
|--------|------|------|
| 낚시 Cast · 통발 배치 (`MapFishService.CellHasFishableWater`) | `ColumnMlDownward ≥ MapFishConsts.FishableColumnMl` | 물고기가 살 **수심**이 필요 — 얕은 한 겹은 제외 |
| 수중창 S3 (`MapFishService.IsShooterInWater`) | `Fill01 ≥ MapFishConsts.UnderwaterShooterFill01` | 사수가 잠겼는지만 보면 됨 — 발밑 국소 상태 |

`FishableColumnMl = 2,000,000`은 셀 클램프(약 1,065,390)의 2배 근처라 **수직 2셀 이상**을 강제한다. 즉 평지에 물을 부어 한 겹 깔아도 낚시는 안 되고, 분지(아래 셀이 점유돼 있고 위 셀에는 바닥이 없는 지형)가 필요하다.

`UnderwaterShooterFill01`은 구 `SHALLOW_WATER` 시드 비율(`MapLiquidConsts.ShallowSeedFraction`)을 그대로 참조한다 — 얕은 물에서도 되던 기존 동작과의 패리티가 이 상수의 존재 이유이므로 값을 따로 적지 않는다.

### 분지가 성립하는 조건 (지형 계약)

물이 수직으로 쌓이려면 각 셀이 `hub.CellHasOccupancy`를 만족해야 한다(`IsTargetEligible`). 점유는 `FloorMapIndex.RebuildOccupancy`가 만들며, 관련 규칙은:

- floor face 1장은 `CellBelow`(= JSON 앵커 `y`)와 `CellAbove`(= 앵커 `+1`, walkable) **양쪽**을 점유로 등록한다. walkable 셀은 `CellAbove`다 — **JSON의 `y`는 물이 있을 셀이 아니다.**
- wall edge는 맞닿은 두 셀을 점유로 등록하되 바닥을 주지 않는다.

따라서 "바닥 없이 점유된 셀"은 wall edge로만 표현된다. 2셀 분지 = 바닥 face로 하단 셀을 만들고, 그 위 셀은 wall edge(pit 테두리)로 점유만 준다. 뚫린 평지 위 허공은 점유가 없어 물이 쌓이지 않는다.

## 배선

`TileMapManager.SetupMapLiquid()`가 `MapBloodHost`/`MapPlantHost`와 동일한 패턴으로 `MapLiquidHost`를 바인딩·로드한다. 저장은 `MapSavePipeline.Save(...)`/`MapFileSaver`가 `MapLiquidHost.Runtime.WriteToDto`를 호출한다.

## 남은 작업 (이 패스에서 미구현)

- **층 가시성**: 수면 렌더러는 `IFloorVisibilitySync`에 물려 있지 않다. `PlayerFloorVisibilityDriver`가 sink 1개만 받는 구조라, 위층 물이 아래층을 가릴 수 있다. 실내 다층 물이 생기면 composite sink로 연결할 것.
- **워터타일 에셋 제거(이주 3단계)**: 에디터 저작은 아직 `Floor/ShallowWater`·`Floor/DeepWater` + Save bake(`MapLiquidAuthoringBake`). Play 검증 후 SO·프리팹·씬 레지스트리 삭제 → 그다음 `SeedFromTileFlags`·`TileFlags.ShallowWater/DeepWater`·bake 경로 제거. **Play 검증 전에는 지우지 않는다.**
- **낚시 가능 지형 없음**: `map01`의 웅덩이는 한 겹(최대 약 1,065,390 ml)이라 `FishableColumnMl` 미달 — 현재 맵에서 낚시는 의도적으로 불가다. 분지를 저작해야 낚시가 다시 열린다(§분지가 성립하는 조건).
- **Fields/emits**: BN `phase: liquid` 아이템 필드 증발/침전.
- **Consumers**: 비(rain), 젖음(wetness), 소화기(extinguisher) 등 다른 시스템과의 통합.
- **terrain별 capMl bake**: 현재 전 셀 `DefaultMaxVolumeMl` 공통.

## See also

`docs/map/SYSTEM.md` · `docs/map/FISHING.md` · `docs/equipment/BN_BAKE.md`(`volume_ml`, `phase: liquid`)
