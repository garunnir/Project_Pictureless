# Task Plan

## Goal
Elona식 단일 숙련 테이블 (능력치=스킬 행) + Base/Buffed Refresh + 신체 효과 합산 소스.

## Design Decisions (locked 2026-07-25)
- 단일 테이블, 능력치 행 상시 시드 (`AttributeIds`)
- Base/Buffed + Refresh (Reset→가산→클램프)
- JSON 프로토 DTO (`SkillDef`) — 로더 연결은 후속; 런타임은 코드 시드
- 바이탈 분리; 스탯에 cur/max 없음
- 연습: Elona (1000 XP/레벨, 잠재력 공식)
- Buffed≤0 능력치 → Collapsed (스톤수프)
- 레벨 다운: `ModifyBaseLevel`
- NPC도 동일 `ICharacterSkills` 인스턴스 모델
- UI 통지: `Refreshed` 일괄 (`IPlayerStats.Changed`는 어댑터 브리지)
- 부위 효과 → `BodySkillModifierAggregator` 1회 합산 → Refresh 소스

## Steps
- [x] BuffableStat / SkillEntry / SkillGrowth / AttributeIds / SkillDef
- [x] ICharacterSkills + DefaultCharacterSkills + modifier/body aggregate
- [x] DefaultPlayerStats 어댑터 + GameplayData/ViewModel 배선
- [x] SkillCatalog(JSON 로더) — DefaultPlayerStats/CharacterSkillsHost 시드
- [x] 소비처 이주 (GameplayData Str, CraftingService Int) + StatKeys 삭제 (VitalKeys.cs 분리)
- [x] CharacterSkillsHost 컴포넌트 (NPC/플레이어 프리팹 부착용 — 배선은 프리팹 작업 시)
- [x] Defeat 레이어: ICharacterDefeat + DefaultCharacterDefeat (Body∨Skills OR, 래치+Revive)
      GameplayData.Defeat / CharacterSkillsHost.Defeat 노출

## Out of Scope (this slice)
- 장비 modifier 소스
- 세이브 직렬화
- 부위 컨디션 max↔STR 재계산 정책
- Defeat → 실제 게임오버/AI 정지 소비 (판정 레이어만; 소비처 배선 후속)
- NPC 바디 모델 (Defeat는 현재 NPC에서 StatCollapse만)
