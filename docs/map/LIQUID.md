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
| `OverCompressMl` | ≈196 | 수직 압축 시 아래 칸의 여유 |
| `MaxUpdatesPerTick` | 512 | `WorldClock.MinuteChanged` 1회당 처리할 dirty 셀 상한 |

## Flow 알고리즘 (`MapLiquidFlowSolver.cs`)

`WorldClock.MinuteChanged` 1회당 dirty 큐에서 최대 `MaxUpdatesPerTick`개를 pop해 처리한다. 순서:

1. **중력** — self에 바닥이 없으면(`!hub.CellHasFloor(self)`) 아래 칸과의 2-셀 stable-state를 계산해 그 값까지 즉시 채움
2. **수평 equalize** — 4방향 이웃과 `EffectiveMl` diff의 1/4을 이동. `diff <= MinFlowMl`이면 그 방향은 스킵(정지 조건)
3. **수직 탈출** — 2번에서 어느 방향으로도 옮길 게 없었는데(`diff`가 전부 `MinFlowMl` 이하) 여전히 `EffectiveMl > capMl`(진짜 오버플로)이면, 위 칸에 바닥이 없는 한 그 초과분을 위 칸에 **실제로** 옮긴다(표면 눈속임 아님, 진짜 `MapLiquidCell` 엔트리). 위 칸도 dirty로 등록되어 다음 처리에서 동일한 1~3 규칙을 재귀적으로 받는다.
4. 차단: `TryGetEdgeBetween` + `TileCollisionFlagsUtil.EdgeBlocksPassage`(수평), `CellHasFloor`(수직)

**거절이 없는 이유**: 오픈 지형에서 위 칸은 거의 항상 열려 있으므로 3번이 항상 탈출구를 제공한다. 완전 밀폐(위도 막힘)는 지형이 아니라 컨테이너(아이템/탱크) 정의의 몫이며, 그 경우는 `MapLiquidMlBridge.Pour` 호출 이전에 소비자가 걸러야 한다.

### 확산의 유한성

- 공간: 총 물량 `V`가 `N`칸에 퍼지면 칸당 `V/N`. `V/N <= MinFlowMl`이 되면 그 경계에서 정지 → `N_max ≈ V / MinFlowMl`
- 시간: 모든 이동이 `|diff|`를 줄이는 방향으로만 발생(단조감소) → 유한 스텝 내 반드시 정지, 진동(A↔B 왕복) 불가능

### 정적 셀 무연산 보증 (바다맵 폭주 방지)

세 곳에서 명시적으로 지켜야 한다:

1. **시드는 dirty를 유발하지 않는다** — `MapLiquidOverlay.SeedFromTileFlags`/`SeedEffectiveMl`은 `MarkDirty`를 호출하지 않음. 균일한 정지 바다는 시드 직후 dirty 큐가 비어 있다.
2. **FlowSolver는 순수 반응형** — `ProcessDirty`는 큐 pop만 한다. 전체 overlay를 훑는 폴링 로직 금지. dirty 진입점은 `MarkDirty` 호출(흐름 발생 시 이웃, `MapLiquidMlBridge.Pour/Draw`)뿐.
3. **렌더러/쿼리는 좌표 단건 조회만** — `MapLiquidQuery`는 전체 순회 API를 제공하지 않는다. 향후 렌더러도 `TileMapVisualizer`/`MapBloodHost`처럼 로드된 청크의 변경 셀만 갱신해야 한다(§ 남은 작업).
4. **맵 밖 무한 확산 차단** — `MapLiquidFlowSolver.IsTargetEligible`이 `hub.CellHasOccupancy`로 걸러, 맵에 정의되지 않은 셀로는 흐르지 않는다(플랜 문서에는 없던 추가 안전장치 — 미정의 void로의 무한 낙하/확산을 원천 차단).

## ml ↔ 셀 (`MapLiquidMlBridge.cs`)

비대칭 규칙, 항상 플레이어 손해 방향:

- **Pour**: 요청 ml **전액**이 셀에 반영, 호출부는 반환값(=요청값) 전량을 인벤에서 차감. cap 초과분은 소멸이 아니라 `MarkDirty`로 위임되어 다음 틱부터 FlowSolver가 이웃/위로 전달.
- **Draw**: `Min(요청, 셀 보유량)`만 정확히 지급, 낭비 없음.

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

## 배선

`TileMapManager.SetupMapLiquid()`가 `MapBloodHost`/`MapPlantHost`와 동일한 패턴으로 `MapLiquidHost`를 바인딩·로드한다. 저장은 `MapSavePipeline.Save(...)`/`MapFileSaver`가 `MapLiquidHost.Runtime.WriteToDto`를 호출한다.

## 남은 작업 (이 패스에서 미구현)

- **렌더러**: `Fill01` 기반 시각화(머티리얼/스프라이트). 현재는 정적 `SHALLOW_WATER`/`DEEP_WATER` 프리팹 외관 그대로.
- **Fish 연동**: `MapFishService`가 여전히 타일 플래그만 검사(`TileFlags.ShallowWater/DeepWater`). `Fill01` 임계 게이팅은 별도 승인 후 적용(게임플레이 영향 있는 변경).
- **Fields/emits**: BN `phase: liquid` 아이템 필드 증발/침전.
- **Consumers**: 비(rain), 젖음(wetness), 소화기(extinguisher) 등 다른 시스템과의 통합.
- **terrain별 capMl bake**: 현재 전 셀 `DefaultMaxVolumeMl` 공통.

## See also

`docs/map/SYSTEM.md` · `docs/map/FISHING.md` · `docs/equipment/BN_BAKE.md`(`volume_ml`, `phase: liquid`)
