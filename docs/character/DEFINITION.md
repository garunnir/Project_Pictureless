# Character Definition (Dist)

**SSOT:** `Assets/Dist/Scripts/Gameplay/Definitions/CharacterDefinition.cs`  
**에셋:** `Assets/Dist/SOData/Gameplay/Character/`  
**레거시:** `Assets/Dist/Legacy/Character/ActorSO.cs` — **참조하지 않음** (필드 계약만 이전)

## 편집 허브

Data Definitions (`Tools/Data Definitions`) **Characters** 탭이 편집 진입점이다. BN Reference/Custom JSON과 섞이지 않는다. 리스트는 `Assets/Dist/SOData/Gameplay/Character/`. 상세는 SO 인라인(+ Alignment 위젯). `+` 가 같은 폴더에 에셋을 만든다. 저장은 Unity 에셋(Ctrl+S). 창의 Save Changes는 아이템/레시피 전용.

Inspector CustomEditor는 같은 Alignment 위젯을 쓴다 (`CharacterAlignmentDrawer`).

## 역할

`CharacterDefinition` SO는 캐릭터 **생성 스펙**이다. PC/NPC 구분은 스펙에 없다. Dist는 Dialogue `Actor` / `ActorSO`에 의존하지 않는다.

| 경로 | 책임 |
|------|------|
| `CharacterDefinition` | 스펙 SO (`CreateAssetMenu`: Dist/Character/Definition) |
| `CharacterDefinitionBinder` | 씬/프리팹 GO에 Apply (`DefaultExecutionOrder` -80) |
| `CharacterAppearanceHost` | 성향·초상·체형·이름 오버라이드 **저장만** (소비처 후속) |
| `CharacterFactory.Instantiate` | prefab + Apply. 맵 바인딩은 `MapGameplayBootstrap.BindSpawnedCharacter` |
| `CharacterSessionHub` | 본체 인벤·기어·액션 입구. Possessed만 `BecomePlayer` |
| `CharacterSpawner` | 셀 SSOT 행 → Factory → `SetActive` → `CharacterSpawnGearApplier` → possess / `NpcManager.Register` |

## 스폰

씬 `CharacterSpawner`가 Play 시 행을 소환한다. PC/NPC 구분은 Definition이 아니라 행의 `CharacterSpawnRole`이다.

| 항목 | 계약 |
|------|------|
| 본체 프리팹 | `NpcSample` 하나. `PlayerParity` / `NpcParity` 모두 이 프리팹 |
| 위치 | `Vector3Int` 셀. 런타임 `IWorldGrid.CellToWorld`. 마커는 에디터 뷰 |
| 부모 | `Map/Characters` (`CharacterWorldRoot`, 맵 루트 이름은 `SmallItemSpawner.WorldMapRootName`) |
| possessed | `PlayerManager.Possess` → 입력 리그는 `PlayerPossessedInputHost`, 플레이어 세션은 본체 `CharacterSessionHub.BecomePlayer` (`GameplayData`·인벤 Runtime·Gear/Encumbrance/TimedMove `Active`) |
| NPC | `NpcManager.Register`. 프리팹에 AI MB 없음 |
| 맵 | 스폰 직후 증분 바인드 (Bootstrap Start Find만으로는 이후 스폰이 빠짐) |
| 로드아웃 | Definition `wearItemIds` / `wieldLoadout` / `bodyItemSeeds`. `SetActive` 직후 즉시 Wear/Wield (타이머 없음). 총이면 호환 탄창·탄을 채움 (카탈로그에 없으면 빈 총 + 경고) |
| 셀 기즈모 | `TileHelper.DrawOccupiedCellWire` (TileView Selected와 동일 박스) |

`CharacterMotor`는 `IPlayControllable` 존재만으로 possessed가 되지 않는다. `PlayerManager`가 켠 것만.

플레이어 전용 장치(카메라, 층 가시성, 시야 블렌드, `PlayerSight`)는 시스템. 입력(`PlayerMovement` / Aim / Combat / Pointer)은 `PlayerPossessedInputHost`에 두고 본체 공용 API만 부른다. `PlayerController`를 `NpcSample`에 올리지 않는다.

인벤·기어 호스트는 본체 프리팹에 두고 **인스턴스마다** 컨테이너를 갖는다. 몸 그래프 입구는 `CharacterSessionHub` (NpcSample). NPC는 `BecomePlayer`를 부르지 않고 `Active`를 건드리지 않는다. Possessed만 허브가 `PlayerInventoryRuntime` Bind + Gear/Encumbrance/TimedMove `ClaimActive` + `GameplayData` + 상태 UI rebind를 한 번에 한다. 인벤 이동 게이지는 그 몸의 `CharacterActionHost`를 본다. possessed 몸은 `player-body`, 나머지는 `character-body-*` (레지스트리 충돌 방지). 살아 있는 NPC 몸은 Nearby 루트에 안 뜬다 (`IsAvailableToPlayer`). 쓰러진 NPC 루팅은 그 게이트를 여는 후속.

## 필드

| 필드 | ActorSO 대응 | Dist |
|------|--------------|------|
| `id` | `Name` (Loc 키) | `Loc.Get(id)` |
| `displayName` | Display Name | 비어 있으면 `id` Loc |
| `portraitSprite` | spritePortrait | 저장만 |
| `alignment` | Status.Alignment | Vector2, 전용 에디터, 소비처 없음 |
| `attributes` | Status.Str..Cha | 기본 8 (`SkillGrowth.DefaultAttributeLevel`) |
| `skillOverrides` | Skill.Active* | BN `skillId` — 레거시 인덱스 아님 |
| `bodyMassKg`, 쓰리사이즈 | Body.Personalized | 저장만; 과적 미연동 |
| `partMasses` | (없음) | `BodyPartIds` 트리 노드 키. 절단 시 `CharacterAppearanceHost.RemainingMassKg`가 없는 부위 kg을 차감. **과적(encumbrance)은 미연동** — [`docs/body/BODY.md`](../body/BODY.md) |
| `prototypeSeed` | — | `CharacterBody.CreateHumanDefault` |
| `prefab` | — | Factory용 |
| `wearItemIds` | — | 스폰 즉시 Wear. 겹치면 이후 항목 스킵 |
| `wieldLoadout` | — | `itemId` + `WieldHand`. 양손 무기는 TwoHand. 실패 시 몸통 폴백 |
| `bodyItemSeeds` | — | 몸통 `AddItem`. 테스터 컨테이너 시드 대체 |

**미이관:** HP 풀, Passive 슬롯, Equipment 인덱스, Dialogue int id, Bark 테이블.

## Apply 경계

```text
definition == null  → BodyHost/SkillsHost 기존 시드 (CreateHumanDefault(8), CreateSeededSkills)
UseGameplayData* 또는 호스트 없음 → GameplayData.Stats / Body
그 외 → CharacterBodyHost.BindBody + CharacterSkillsHost.BindSkills
```

## 본체 vs 매니저

PC와 NPC는 **같은 본체 프리팹** (`NpcSample`: 모터·몸·Binder·공격·애니)을 쓴다. SO는 정보만 채운다. NPC 인스턴스에 `NpcCombatBehavior` / `NpcSenses` / `NpcSteerToPoint`를 Add하지 않는다.

| 경로 | 책임 |
|------|------|
| `CharacterMotor` | 공용 물리. possessed면 Player 채널, 아니면 World |
| `PlayerMovement` | 시스템(`PlayerPossessedInputHost`) 입력 드라이버. possessed 모터에 `ICharacterMotorDrive` 바인드. NPC에는 입력을 켜지 않음 |
| `NpcManager` | 비possessed 유닛을 행 단위 FSM으로 원격 틱 (2대 이상) |
| `NpcSteer` | 조향 헬퍼 (MB 아님) |
| `CharacterActionHost` | 행위자 1줄 행동 큐·게이지·CancelAll. [`ACTION.md`](ACTION.md) |
| `CharacterSessionHub` | 본체 세션 입구. possessed만 `BecomePlayer` |

`CharacterKind`로 PC/NPC를 나누지 않는다. 조종 여부는 `CharacterMotor.IsPossessed` (`PlayerManager.Possess`).

## 샘플

| 에셋 | 용도 |
|------|------|
| `CharacterDefinition.NpcParity.asset` | NpcSample 배선용 패리티 (능력치 8, undershirt + bat) |
| `CharacterDefinition.PlayerParity.asset` | 동일 패리티, prefab=`NpcSample`, plain + pistol(클립 탄) + 몸통 bag/egg |

## 검증

- Unity 컴파일 에러 없음
- Data Definitions → Characters: NpcParity / PlayerParity 목록, 상세 인라인, Alignment 위젯
- NpcSample + NpcParity SO: Play 시 STR 8·기존 전투 시드 유지. 프리팹에 AI MB 없음
- IsoLand `CharacterSpawner`: Possessed 1 + Npc 2, 셀 서로 다름. 씬에 박힌 `>PlayerCharacter` / `NpcSample` 인스턴스는 비활성
- `NpcManager`: 스폰된 NPC만 틱. possessed는 건너뜀
- Binder/definition null: 기존과 동일 시드
