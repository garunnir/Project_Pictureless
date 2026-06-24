---
name: document-convergence-competition
description: Cursor-only persuasion convergence for project docs via parallel Task subagents (propose, rebut, conviction vote). Judge+JIS only on persuasion failure. Use when the user @-mentions this skill, runs /document-convergence-competition, or asks for 경합형/설득 수렴.
disable-model-invocation: true
---

# Document Convergence Competition (Cursor)

Cursor **수동 스킬** — `disable-model-invocation: true` (alwaysApply 아님).  
사용자가 `@document-convergence-competition` 또는 슬래시/명시 요청 시에만 실행.

**목표:** A/B/C가 **설득으로 전원 accept**할 때까지 경합.  
Persuaded ≠ 객관적 정답 — 실행 전 사용자가 최종안을 검토한다.

## Cursor 실행 전제

| 항목 | 규칙 |
|------|------|
| **병렬** | 각 라운드 A/B/C는 **Task 3회 동시 호출**. 단일 응답 3역할 금지 |
| **모드** | **Ask** — 분석·반영안만, 파일 편집 금지 |
| | **Plan** — 계획·반영안만, 파일 편집 금지 |
| | **Agent** — `본문 직접 수정: 허용` + 사용자 승인 시에만 적용 |
| **프롬프트** | `prompts.md` Read → Task에 **§Output Contract → 해당 §N** 순 주입 (`OUTPUT FORMAT` 블록 최상단) |
| **응답 검증** | 필수 `##` 헤더 누락·요약만 반환 → 동일 agent **resume 1회**. 2회 연속 무효 시 §6 검토 |
| **Context** | 반박 이후 **Context Pack**만 사용 ([prompts.md §Context Pack](prompts.md)) |

## 입력

- 대상 문서 경로
- Cursor 모드: `Ask` | `Agent` | `Plan` (미지정 → Ask)
- 본문 직접 수정: `허용` | `금지(반영안만)`
- (선택) `doc_type`: `rule` | `skill` | `code` | `policy` (미지정 → `policy`)
- (선택) 사용자 메모

슬롯·개수 제한 없음. A/B/C **역할 고정:** Runtime / Context / Gate — [prompts.md §Personas](prompts.md).

## 워크플로

1. **1차 제안** — Task×3 병렬, §Personas red line, confidence, 상호 참조 금지
2. **반박** — Task×3, Context Pack + 요약만 입력
3. **(선택) 재반박** — 미해결 `issue_id`만; RA-CR lite: 라운드당 1 agent 침묵 가능
4. **납득 투표** — Task×3, accept|hold + conviction + red_line_check
5. **분기**
   - **3/3 accept** (형식 검증 통과) → §7 Persuaded 최종안, **심판·JIS 없음**
   - **2/3 accept + hold** → hold 대상 §5 마지막 반박 → 4) 재투표
   - **hold ≥2** 또는 재투표 실패 → **설득 실패** → §6 심판+JIS
6. **상한** — 반박·재반박·재투표 합 **3회** → 초과 시 §6 강제

## 절대 규칙

- 골고루 채택 금지 — accept = 설득됨, 타협 합산 아님
- Winner-takes-all(A|B|C 단독) 금지 — 성공 시 `Persuaded`
- **Conformity guard:** `persuaded_by`·인용·red_line_check 없는 accept **무효**
- **Factual attrition:** DelibTrace lite `must` 항목 유실 시 3/3 accept 인정 금지
- Pending 금지 — Persuaded 또는 JudgeForced로 닫힌 1안
- 교집합/평균점수 수렴과 혼용 금지
- `Multi-Agent Execution Check`에 Task 병렬 No → Persuaded 불가

## 최종 출력 (스켈레톤)

**Persuaded:** Check · Outcome(Persuaded, 3/3) · Source Trace · Final Document · Next Action  
**JudgeForced:** Check · Outcome · Hold Summary · Final Document · JIS · Next Action

상세: accept/hold, JIS 채점, 서브프롬프트 → [prompts.md](prompts.md)

## 재경합

이전 hold `unresolved` + red line만 시드. Persuaded로 고정된 문장은 재논쟁 금지(사용자 메모 예외).
