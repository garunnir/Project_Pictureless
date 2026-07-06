# GameManager / GM.prefab — ⚠️ 레거시

**상태**: 레거시 (유지보수·버그픽스만, 신규 기능 금지)  
**판정일**: 2026-07-06  

---

## 요약

| 항목 | 내용 |
|------|------|
| **레거시에 포함** | `GameManager`, `GM.prefab`, Pixel Crushers, `ResourceManager` 연동 |
| **신규 코드 기본** | `GameplayData.ItemCatalog`, `GameplayCatalogHost` |
| **프리팹** | `Assets/Dist/Legacy/Prefabs/GM.prefab` |
| **하지 말 것** | `GameManager.Instance` / `GetResourceManager()`로 신규 시스템 부트스트랩 |

---

## 대체 진입점

```csharp
GameplayData.ItemCatalog
```

씬에 `GameplayCatalogHost` + `ItemCatalogSO`만 배선하면 인벤·IsoLand 테스트 가능.

---

## 마이그레이션

1. 씬에 `GameplayCatalogHost` 배치
2. `GetResourceManager().GetItemCatalog()` → `GameplayData.ItemCatalog`
3. `Form`/`Form0` 직렬화 키는 `GameManager.GetFormDic` 유지 (캐릭터 레거시)
4. GM.prefab 제거는 모든 참조 제거 후

**관련**: `RESOURCE_MANAGER_LEGACY.md`, `BATTLE_LEGACY.md`, `SKILL_LEGACY.md`
