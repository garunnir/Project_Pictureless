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
            case WeaponAction.Bashing: return "Bashing";
            case WeaponAction.Cutting: return "Cutting";
            case WeaponAction.Gun: return "Gun";
            default: return "?";
        }
    }

    public static string MaskLabel(WeaponActionMask mask)
    {
        if (mask == WeaponActionMask.None)
            return "(none)";

        var parts = new System.Collections.Generic.List<string>(3);
        if ((mask & WeaponActionMask.Bashing) != 0)
            parts.Add("Bashing");
        if ((mask & WeaponActionMask.Cutting) != 0)
            parts.Add("Cutting");
        if ((mask & WeaponActionMask.Gun) != 0)
            parts.Add("Gun");
        return string.Join("|", parts);
    }
}
