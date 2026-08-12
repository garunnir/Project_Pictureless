// ============================================================
// WeaponAction — 동작 동사 (SWING/THRUST/TRIGGER/RAISE). 데미지 속성 아님
// ============================================================

using System;

[Flags]
public enum WeaponActionMask
{
    None = 0,
    Swing = 1,
    Trigger = 4,
    Thrust = 8,
    Raise = 16
}

/// <summary>
/// 값 고정: 0=Swing(구 Bashing), 2=Trigger(구 Gun), 3=Thrust, 4=Raise.
/// 1은 구 Cutting — Normalize가 Swing으로 접음.
/// </summary>
public enum WeaponAction
{
    Swing = 0,
    Trigger = 2,
    Thrust = 3,
    Raise = 4
}
