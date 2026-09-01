# Cooking (BN parity)

> Dist 요리 = 일반 제작 + 환경 PSEUDO 도구 + 식품 인스턴스 상태 + 숙련·광원·단순 조리 액션.
> 인덱스: [`docs/README.md`](../README.md)
> 베이크: [`equipment/BN_BAKE.md`](../equipment/BN_BAKE.md) · 제작 창: [`crafting/CRAFTING.md`](../crafting/CRAFTING.md)

경로(서비스): `Assets/Dist/Scripts/Inventory/CraftingService.cs` · `CraftingEnvironmentProvider.cs` · `CraftingLightGate.cs`  
경로(식품): `ItemInstance` Hot/Cooked · `ConsumeService`  
경로(숙련): `ICharacterProficiencies` / `DefaultCharacterProficiencies`  
경로(UI): `UICraftingWindow` · 인벤 Cook/Smoke 컨텍스트

---

## 패리티 계약

### 유지 (기존 제작)

- 사이드바 `CraftingMaterialPool`, 대체재 인덱스, 드롭=선택(이동 아님)
- 창: `time_minutes × 수량` 게임분 대기 후 `TryCraftMany`
- 컨텍스트 메뉴: 즉시 `TryCraft` 1회
- `RecipeKnowledge` 해금 (`autolearn` / `autolearn_skills` / `book_learn` / `decomp_learn` + 런타임 `RecipeMemory`). 분해 성공 시 `decomp_learn` 충족하면 영구 Known.

### 추가 (요리 BN)

1. `PSEUDO` 도구(`fire`, `apparatus`, `sunlight` 등)는 인벤 없이 **환경·가구·시간**으로 충족
2. `hot_result` 완료 → 결과 스택 **Hot**; 월드분 경과 후 상온
3. 조리 완료 식품은 RAW 칼로리 페널티 없음 (인스턴스 `Cooked` 또는 ItemData non-RAW)
4. `cooks_like` / `smoking_result` → 인벤 컨텍스트 “굽기/훈연” (레시피 목록 외)
5. `proficiencies` required → `CanCraft` 차단; `time_multiplier` → 제작 시간
6. `dehydrating` 레시피 → `sunlight` pseudo 필수
7. 멀티쿠커: 아이템 use → 필터된 제작 창 (`tools`에 `multi_cooker`)
8. 레시피 `flags`에 `DARK` → 최소 조도; 헤더 `Img_Light` + `CanCraft`
9. `activity_level` → `PlayerNeedsHost` 피로; `morale_modifier` → `CharacterMoodHost` memory

### 의도적 축소

- recipe `flags`는 화이트리스트만 (`DARK` 등). `batch_time_factors` / `contained` 미이식
- `RecipeMemory`(decomp 습득) — `PlayerProgressSaveDto` / map `playerProgressJson`에 저장
- 가구 연료·점화 시뮬 없음 — **점화 플래그 가구 = 열원** 이분법
- `parasites`, `freeze_point`, `monotony_penalty` Parked

---

## PSEUDO 도구

| id | Dist 해석 |
|----|-----------|
| `fire` | 인접 furniture `crafting_flags`에 `FIRE`/`LIT`, 또는 인벤 `hotplate`/`multi_cooker`/`char_smoker`/`toolset` (충전>0) |
| `apparatus` | 인접 `SMOKE`/`SMOKER` 가구 + `fire` 충족 |
| `sunlight` | `WorldClock.Period` == Day |

`CraftingEnvironmentProvider`가 품질(`COOK` 등)도 가구 `provides_qualities`에서 합산.

---

## 식품 인스턴스

| 상태 | 의미 |
|------|------|
| `IsHot` | `hot_result` 제작 직후. `HotCoolMinutes` 후 해제 |
| `IsCooked` | 조리 결과·굽기 변환. RAW 페널티 무시 |

병합: Hot ≠ non-Hot, Cooked ≠ non-Cooked (`ItemMergeKey`).

---

## Play 검증

1. 난로/모닥불 옆에서 `fire` 필요 레시피 (인벤 `fire` 없이)
2. `hotplate`만으로 실내 요리
3. 생고기 RAW 페널티 / 조리 후 정상
4. proficiency 미달 차단 · 시간 배율
5. `DARK` 레시피: 횃불 없으면 불가
6. 멀티쿠커 배치 3 · 취소 시 재료 보존
7. 비요리 제작 회귀 (`CC_WEAPON` 등)

---

## See also

[`crafting/CRAFTING.md`](../crafting/CRAFTING.md) · [`needs/NEEDS.md`](../needs/NEEDS.md) · [`mood/MOOD.md`](../mood/MOOD.md) · [`equipment/BN_BAKE.md`](../equipment/BN_BAKE.md)
