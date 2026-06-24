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
