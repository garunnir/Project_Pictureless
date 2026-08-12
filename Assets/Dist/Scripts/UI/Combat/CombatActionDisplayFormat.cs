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

    public static string ActionLabel(WeaponAction action) =>
        CharacterGearLabels.ActionLabel(action);

    public static string MaskLabel(WeaponActionMask mask)
    {
        if (mask == WeaponActionMask.None)
            return "(none)";

        var parts = new System.Collections.Generic.List<string>(WeaponActionUtil.All.Length);
        for (int i = 0; i < WeaponActionUtil.All.Length; i++)
            TryAddMaskPart(parts, mask, WeaponActionUtil.All[i]);
        return string.Join("|", parts);
    }

    static void TryAddMaskPart(
        System.Collections.Generic.List<string> parts,
        WeaponActionMask mask,
        WeaponAction action)
    {
        if ((mask & WeaponActionUtil.ToMask(action)) == 0)
            return;

        string label = CharacterGearLabels.ActionLabel(action);
        if (!parts.Contains(label))
            parts.Add(label);
    }
}
