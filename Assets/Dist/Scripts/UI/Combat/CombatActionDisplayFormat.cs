// ============================================================
// CombatActionDisplayFormat — 전투 액션 HUD 텍스트 SSOT
// ============================================================

public static class CombatActionDisplayFormat
{
    public static string Format(
        WeaponAction selected,
        WeaponActionMask available,
        string weaponName)
    {
        string weapon = string.IsNullOrEmpty(weaponName) ? "-" : weaponName;
        return $"{weapon}  [{ActionLabel(selected)}]  {MaskLabel(available)}";
    }

    public static string ActionLabel(WeaponAction action)
    {
        switch (action)
        {
            case WeaponAction.Swing: return "Swing";
            case WeaponAction.Stab: return "Stab";
            case WeaponAction.Trigger: return "Trigger";
            default: return "?";
        }
    }

    public static string MaskLabel(WeaponActionMask mask)
    {
        if (mask == WeaponActionMask.None)
            return "(none)";

        var parts = new System.Collections.Generic.List<string>(3);
        if ((mask & WeaponActionMask.Swing) != 0)
            parts.Add("Swing");
        if ((mask & WeaponActionMask.Stab) != 0)
            parts.Add("Stab");
        if ((mask & WeaponActionMask.Trigger) != 0)
            parts.Add("Trigger");
        return string.Join("|", parts);
    }
}
