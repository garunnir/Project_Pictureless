# BattleSystem — ⚠️ 레거시

**상태**: 레거시 (유지보수·버그픽스만, 신규 기능 금지)  
**판정일**: 2026-07-06  
**사유**: 전투 루프·UI는 **별도 시스템으로 교체 예정**. 인벤·아이템·맵 플레이와 분리하여 취급한다.

---

## 요약

| 항목 | 내용 |
|------|------|
| **하지 말 것** | `BattleSystem`·`BattleActorData` 확장, `GameManager`/`ResourceManager` 재결합 |
| **해도 되는 것** | 기존 씬이 깨지지 않게 하는 최소 수정, `GameplayData.ItemCatalog`로의 점진적 분리 |
| **관련 레거시** | `SKILL_LEGACY.md`, `RESOURCE_MANAGER_LEGACY.md`, `GAME_MANAGER_LEGACY.md` |

---

## 경로

```
Assets/Dist/Legacy/Battle/
├── BATTLE_LEGACY.md
├── BattleSystem.cs
└── WaponSystem.cs
```

---

## 데이터 의존

- 무기 조회: `GameplayActorItems.GetEquippedWeapon(Actor)` (`ItemCatalogSO`)
- 스킬: `SkillCollectionSO` — `SKILL_LEGACY.md` 참고

---

## 마이그레이션 (교체 시)

1. 신규 전투 러너·턴/실시간 규칙 정의
2. `Actor`/`ActorSO` 필드에서 아이템·스탯 읽기 API 단일화
3. `BattleSystem` 씬 오브젝트 비활성 또는 제거
4. 이 문서를 아카이브
