# ResourceManager — ⚠️ 레거시

**경로**: `Assets/Dist/Legacy/Manager/ResourceManager.cs`  
**상태**: 레거시 (유지보수·버그픽스만, 신규 기능 금지)  
**판정일**: 2026-07-06  

---

## 요약

| 항목 | 내용 |
|------|------|
| **레거시 역할** | 갤러리/파일 이미지 로드, 맵 컨테이너, 스킬 컬렉션, 구 Actor 무기 인덱스 조회 |
| **신규 대체** | `GameplayCatalogHost` + `GameplayData.ItemCatalog` (`ItemCatalogSO`) |
| **하지 말 것** | 신규 시스템에서 `ResourceManager`/`GetResourceManager()` 의존 |

---

## 마이그레이션

| 레거시 | 대체 |
|--------|------|
| `GetItemCatalog()` | `GameplayData.ItemCatalog` |
| `GetWeapon(Actor)` | `GameplayActorItems.GetEquippedWeapon(actor)` (레거시 Actor 필드용 shim) |
| `GetSkillData()` | `SKILL_LEGACY.md` — 교체 전까지 ResourceManager 유지 |
| `GetImg` / `GetBG` / `LoadAllImg` | 레거시 UI 전용, 신규 미사용 |
