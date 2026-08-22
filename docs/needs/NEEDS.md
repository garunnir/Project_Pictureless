# Needs (hunger / thirst / consume)

> LLM/에이전트용 Dist 위장·저장 kcal·갈증·섭취 SSOT.
> 진입: `docs/README.md`
> **PlayerNeedsHost / ConsumeService / 허기·갈증 HUD·상태창 산문을 쓰거나 고치기 전에 이 문서를 읽는다.**

경로(호스트): `Assets/Dist/Scripts/Entity/Character/PlayerNeedsHost.cs`  
경로(섭취·부패): `Assets/Dist/Scripts/Gameplay/Needs/`  
경로(HUD·산문): `Assets/Dist/Scripts/UI/PlayerStatus/` · `UI/Character/UICharacterWindow.cs`  
설정 SO: `Assets/Dist/SOData/Gameplay/Needs/PlayerNeedsSettings.asset`  
메시지: [`../ui/MESSAGE_LOG.md`](../ui/MESSAGE_LOG.md) (`MessageLogNeedsSink`)  
인벤 우클릭: [`../inventory/INVENTORY_UI.md`](../inventory/INVENTORY_UI.md) · [`../inventory/ItemContextMenu.md`](../inventory/ItemContextMenu.md)

비율·용량·경고 임계는 `PlayerNeedsSettings` 한곳. 본문 매직 복붙 금지.

---

## 역할

| Type | Role |
|------|------|
| `PlayerNeedsSettings` | 위장 ml·소화율·일일 kcal/갈증·활동 배율·무드/산문 비율·팽만/부패/경고 SSOT |
| `PlayerNeedsHost` | possessed 플레이어 위장·대사 틱. `Active` |
| `ConsumeService` | 인벤 1개 Eat/Drink/Use → 위장·대사·MED heal. 실행은 `TryBegin` (`CharacterHandWork`) |
| `ConsumeDuration` | Eat/Drink mealtime=250 moves→초 (`CombatMath.MovesPerSecond`). Use/MED는 0. 아이템 JSON 없음 |
| `ItemRot` | `CreatedWorldMinute` + 부패 판정. 호스트가 possessed+open 컨테이너 스캔 |
| `PlayerStatusMoodEntries` | HUD 허기/갈증/기분 슬롯 (바이탈 Low/Critical `MoodIconId.Hunger` 없음) |
| `PlayerStatusVitalDisplay` | 상태창 남은 일수 허기 밴드 · BN식 갈증 밴드 · Survival≥2 수치 게이트 |

`VitalKeys.Hunger` 현재값은 **저장 kcal**. 최댓값은 호스트가 `MaxStoredKcal`로 맞춘다.

---

## 틱 (`WorldClock.MinuteChanged`)

```text
digest mlWater (빠름) / mlFood / kcal→stored
  → burn stored × activity (Sprint > Busy > Walk > Idle)
  → drain thirst
  → rot scan possessed + open loot
  → stored≤0 또는 thirst≤0 → chest ApplyHit 전부 (1회, Defeat BodyFatal)
  → 매 N 월드시간 → AnyNeedsWarning
```

활동 배율·일일 burn/drain은 Settings. 분당량은 `MinutesPerDay`.

---

## 섭취

우클릭 `ConsumeContextContributor` → `ConsumeContextAction` → `ConsumeService.TryBegin`.

손 파이프(`CharacterHandWork`, [`GEAR.md`](../equipment/GEAR.md)): 든 다른 스택을 body로 Unwield → 대상을 Wield(인출=`InventoryTransferDuration`) → mealtime 후 1개 제거. 이미 손에 있으면 1·2 생략. ESC=`CharacterActionHost.CancelAll` — **완료된 단계는 유지**(손을 비운 뒤 뒤적이다 취소하면 손은 빈 채). 진행 중 단계는 apply 없이 중단.

| Kind | 조건 | 호스트 | act |
|------|------|--------|-----|
| Eat | FOOD / calories>0 | `IngestFood` (ml+kcal) | `ConsumeDuration.MealtimeSeconds` |
| Drink | DRINK | `IngestDrink` (ml+quench) | 동일 mealtime |
| Use | MED / heal·consume_drug | 대사 + `BodyPartRestoreService` | 0 (손 파이프만) |

캡은 `mlFood+mlWater`와 stored+stomach kcal. 넘치면 버림 + 가슴 `BodyPartEffectIds.Bloated` (`MoodIconId.Discomfort`). 팽만 중 재섭취 → 구토 + `OvereatHit`.

Fun/Healthy/Stim은 호스트가 보관. 틱 감쇠는 없음.

---

## 부패

`ItemInstance.CreatedWorldMinute`. 스캔은 possessed 몸 + 열린 Nearby 컨테이너(중첩 포함).  
`ItemMergeKey.IsRotten`이 다르면 합치지 않음 (`ItemMergePolicy`).

---

## HUD 무드

`PlayerStatusViewModel`이 `PlayerNeedsHost.Active`를 구독한다. Bind 시그니처는 Body/Vitals/Stats 유지.

| 슬롯 | 아이콘 | 조건 (Settings 이름 있는 비율만) |
|------|--------|-----------------------------------|
| 음식 1칸 | Full / Fed / Hungry / VeryHungry | 위장 fill ≥ `MoodOverateRatio` → Full; ≥ `MoodFedRatio` → Fed; stored ≤ `MoodVeryHungryStoredRatio` → VeryHungry; ≤ `MoodHungryStoredRatio` → Hungry; 아니면 Fed |
| 갈증 | ThirstQuenched / Thirsty / VeryThirsty | ≥ `MoodThirstQuenchedRatio` / ≤ `MoodThirstyRatio` / ≤ `MoodVeryThirstyRatio`. 중간 대역은 아이콘 없음 |
| 팽만 | Discomfort | 효과 카탈로그만 (`PlayerStatusMoodEffectCatalog`) |
| Fun | GoodMood / Sad | \|Fun\| ≥ \|`RotFunPenalty`\| (부호로 극성) |
| Healthy | Sick | Healthy ≤ −\|`RotHealthyPenalty`\| |
| Stim | Adrenaline | Stim ≥ \|`RotFunPenalty`\|. 바디 아드레날린이 있으면 슬롯 유지 |

`CollectVitals`는 Stamina Low/Critical만. `MoodIconId.Hunger` / `Thirst` 단일 슬롯은 쓰지 않는다.

---

## 상태창 산문

Survival < 2: 문구. Survival ≥ 2: 저장 kcal · 갈증 cur/max 숫자 (`PlayerStatusVitalDisplay.NumericVitalMinSkillLevel`).

**허기** 남은 일수 `(stored + stomachKcal) / DailyKcalBurn`:

| 밴드 | 임계 |
|------|------|
| Engorged | ≥ `maxDays × MoodOverateRatio` (`maxDays = MaxStoredKcal / DailyKcalBurn`) |
| Sated | ≥ `ProseFullRatio` (일) |
| Hungry | ≥ `ProseOkRatio` (일) |
| Very Hungry | ≥ `ProseLowRatio` (일) |
| Famished | ≥ `MoodStomachEmptyRatio` (일) |
| Starving | 그 아래 |

**갈증** (cur/max, Settings 갈증 비율):

| 밴드 | 임계 |
|------|------|
| Quenched | ≥ `MoodThirstQuenchedRatio` |
| NotThirsty | ≥ `MoodThirstyRatio` |
| Thirsty | ≥ `MoodVeryThirstyRatio` |
| VeryThirsty | cur > 0 |
| Parched | 0 |

Loc: `PlayerStatus.VitalProse.Hunger.{Engorged\|Sated\|Hungry\|VeryHungry\|Famished\|Starving}` · `PlayerStatus.VitalProse.Thirst.{Quenched\|NotThirsty\|Thirsty\|VeryThirsty\|Parched}`.

---

## 메시지

`PlayerNeedsHost.AnyNeedsVomit` / `AnyNeedsFatal` / `AnyNeedsWarning` → `MessageLogNeedsSink`. 키·문구는 [`MESSAGE_LOG.md`](../ui/MESSAGE_LOG.md). UI_ko SSOT.
