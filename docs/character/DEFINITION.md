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
| `CharacterFactory.Instantiate` | prefab + Apply (맵 스폰·Play 중 충돌 바인딩은 후속) |

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
| `partMasses` | (없음) | `BodyPartIds` 트리 노드 키; 절단 차감 후속 |
| `prototypeSeed` | — | `CharacterBody.CreateHumanDefault` |
| `prefab` | — | Factory용 |

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
| `PlayerMovement` | 씬 플레이어 인스턴스 입력 드라이버. NPC에는 없음 |
| `NpcManager` | 비possessed 유닛을 행 단위 FSM으로 원격 틱 (2대 이상) |
| `NpcSteer` | 조향 헬퍼 (MB 아님) |

`CharacterKind`로 PC/NPC를 나누지 않는다. 조종 여부는 `CharacterMotor.IsPossessed` (`IPlayControllable` 있으면 기본 possessed).

## 샘플

| 에셋 | 용도 |
|------|------|
| `CharacterDefinition.NpcParity.asset` | NpcSample 배선용 패리티 (능력치 8, override 없음) |
| `CharacterDefinition.PlayerParity.asset` | 동일 패리티 템플릿 (prefab 미할당) |

## 검증

- Unity 컴파일 에러 없음
- Data Definitions → Characters: NpcParity / PlayerParity 목록, 상세 인라인, Alignment 위젯
- NpcSample + NpcParity SO: Play 시 STR 8·기존 전투 시드 유지. 프리팹에 AI MB 없음
- IsoLand `NpcManager`: NPC 2대 이상 각자 웨이포인트/FSM. 플레이어는 틱하지 않음
- Binder/definition null: 기존과 동일 시드
