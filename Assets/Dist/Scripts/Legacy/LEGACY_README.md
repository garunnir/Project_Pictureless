# Legacy 코드 격리

**어셈블리**: `DistScript.Legacy` — `DistScript`(메인)만 참조, **메인은 Legacy를 참조하지 않음**

## 포함

| 폴더 | 내용 |
|------|------|
| `Manager/` | GameManager, ResourceManager, Singleton, UIManager, InputManager, LuaInputManager |
| `Battle/` | BattleSystem, WaponSystem |
| `Skill/` | Skill SO 정의 전체 |
| `PixelCrushers/` | Dialogue System 커스텀 애드온 |
| `Character/` | CharacterManager, ActorSO, ActorSerializer, CharCreater |
| `UI/` | Pixel Crushers·GM 연동 UI |
| `Shim/` | GameplayActorItems (레거시 Actor 필드 shim) |
| `Editor/` | Dialogue/Actor 에디터 확장 |

## 신규 코드 (메인 `DistScript` 어셈블리)

- `Assets/Dist/Scripts/Manager/GameplayData.cs`, `GameplayCatalogHost.cs`, `SceneSingleton.cs`
- `Assets/Dist/Scripts/Manager/InputManager.cs` — 플레이어 입력 (레거시 GM과 분리, 메인에 유지)

## 어셈블리

| 어셈블리 | 경로 |
|----------|------|
| `DistScript` | `Assets/Dist/DistScript.asmdef` (Legacy·Editor 제외) |
| `DistScript.Legacy` | `Assets/Dist/Scripts/Legacy/` (`autoReferenced: false`) |
| `DistScript.Legacy.Editor` | `Assets/Dist/Scripts/Legacy/Editor/` |
| `Config` | `Assets/Dist/Scripts/Static/` (ConstDataTable 등) |

**메인 → Legacy 참조 금지** (단방향 격리).

## 프리팹

- `Assets/Dist/Legacy/Prefabs/GM.prefab`
