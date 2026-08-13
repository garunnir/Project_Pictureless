// ============================================================
// WeaponAction — Leaf 선택 단위 (Family 묶음은 Util). AnimVerb로 접힘.
// ============================================================

using System;

/// <summary>UI 묶음. null Family = 평면 Leaf (예: Raise).</summary>
public enum WeaponActionFamily
{
    Melee = 0,
    Trigger = 1
}

[Flags]
public enum WeaponActionMask
{
    None = 0,
    Swing = 1,
    /// <summary>구 Trigger 비트. Normalize 후 Semi로 접힘.</summary>
    Trigger = 4,
    Thrust = 8,
    Raise = 16,
    Semi = 32,
    Burst = 64,
    Auto = 128
}

/// <summary>
/// Leaf: 선택·영속·시전. 값 고정 —
/// 0=Swing, 2=Trigger(구·Normalize→Semi), 3=Thrust, 4=Raise, 5=Semi, 6=Burst, 7=Auto.
/// 1은 구 Cutting — Normalize가 Swing으로 접음.
/// AnimVerb(Pipeline)는 <see cref="WeaponActionUtil.ToAnimVerb"/>.
/// </summary>
public enum WeaponAction
{
    Swing = 0,
    Trigger = 2,
    Thrust = 3,
    Raise = 4,
    Semi = 5,
    Burst = 6,
    Auto = 7
}
