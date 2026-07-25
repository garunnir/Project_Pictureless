# Context

숙련 시스템을 OpenNefia `Stat` Base/Buffed + Refresh 의미론으로 가져오되 ECS 이벤트 버스는 이식하지 않음.
능력치·스킬은 같은 `SkillEntry` / 성장 공식. 레거시 `Status.*`는 어댑터 매핑만.
신체 부위 효과는 UI 무드와 동일 effectId를 쓰되, 수치는 카탈로그→바디 합산→숙련 Refresh 소스.
