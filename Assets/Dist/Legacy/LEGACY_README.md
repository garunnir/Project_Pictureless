# Legacy 코드 격리

**루트**: `Assets/Dist/Legacy/`  
**어셈블리**: `DistScript.Legacy` — `DistScript`(메인)만 참조, **메인은 Legacy를 참조하지 않음**

## 포함

| 폴더 | 내용 |
|------|------|
| `Manager/` | GameManager, ResourceManager, Singleton, UIManager, LuaInputManager |
| `Battle/` | BattleSystem, WaponSystem |
| `Skill/` | Skill SO 정의 전체 |
| `PixelCrushers/` | Dialogue System 커스텀 애드온 |
| `Character/` | CharacterManager, ActorSO, ActorSerializer, CharCreater |
| `UI/` | Pixel Crushers·GM 연동 UI |
| `Shim/` | GameplayActorItems (레거시 Actor 필드 shim) |
| `Editor/` | Dialogue/Actor 에디터 확장 |
| `Prefabs/` | GM.prefab 등 |

> `InputManager`는 플레이어 입력용으로 **메인** `Assets/Dist/Scripts/Manager/`에 유지.

## 신규 코드 (메인 `DistScript` 어셈블리)

- `Assets/Dist/Scripts/Manager/GameplayData.cs`, `GameplayCatalogHost.cs`, `SceneSingleton.cs`
- `Assets/Dist/Scripts/Manager/InputManager.cs`

## 어셈블리

| 어셈블리 | 경로 |
|----------|------|
| `DistScript` | `Assets/Dist/DistScript.asmdef` (Legacy·Legacy/Editor 제외) |
| `DistScript.Legacy` | `Assets/Dist/Legacy/` (`autoReferenced: false`) |
| `DistScript.Legacy.Editor` | `Assets/Dist/Legacy/Editor/` |
| `Config` | `Assets/Dist/Scripts/Static/` (ConstDataTable 등) |

**메인 → Legacy 참조 금지** (단방향 격리).

## 프리팹

- `Assets/Dist/Legacy/Prefabs/GM.prefab`
