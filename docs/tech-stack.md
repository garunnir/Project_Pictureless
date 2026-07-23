# Tech Stack (Canonical)

## Engine & Pipeline

* Unity **6000.3.10f1 (LTS)**, **URP**

## C# 9.0

* **PROHIBITED:** C# 10+ (global using, file-scoped types, required members, etc.)
* **PROHIBITED:** `record` unless `IsExternalInit` is validated in the project
* **CONDITIONAL:** `init`-only setters only with validated `IsExternalInit`; else `private set` or constructor immutability
* **ALLOWED:** pattern matching

## Packages & Input

* **UniTask** — mandatory for new async logic
* **Odin Inspector** — UI/Inspector attributes only; no `OdinSerializer`
* **Input System** (`com.unity.inputsystem`)

## Assembly & Namespace

* `.asmdef` present → assembly name = root namespace
* No `.asmdef` → infer `ProjectName.FeatureName` from context; one conservative default + confirm if conflict
* New namespace or assembly requires prior approval

## Game Time (Dist)

* **SSOT:** `TimeScaleService` (채널 배율) + `WorldClock` (하루·시각). Detail: [`docs/time/TIME.md`](time/TIME.md)
* **PROHIBITED as Dist gameplay SSOT:** Unity `Time.timeScale` (불릿타임·채널 분리와 충돌)
* **Channels:** `Realtime` | `World` | `Player` — 소비는 `GetDelta(channel)`. 배속은 Push/Pop 곱셈 스택
* **Pending:** 불릿타임 콘텐츠·저장·낮밤 라이팅. Dist Scripts 시뮬 `Time.deltaTime` 경로는 채널 연동됨 — `docs/time/TIME.md` Consumer 표
