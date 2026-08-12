// ============================================================
// WeaponActionRows — Presentation 행 → available / default / instance select
// ============================================================

public static class WeaponActionRows
{
    public static WeaponActionMask Available(WeaponPresentation presentation)
    {
        if (presentation == null)
            return WeaponActionMask.Swing;

        presentation.RebuildSupportedActions();
        WeaponActionMask mask = presentation.SupportedActions;
        return mask == WeaponActionMask.None
            ? WeaponActionMask.Swing
            : mask;
    }

    public static WeaponAction Default(WeaponPresentation presentation)
    {
        if (presentation == null)
            return WeaponAction.Swing;

        WeaponPresentation.Entry[] entries = presentation.Entries;
        if (entries == null || entries.Length == 0)
            return WeaponAction.Swing;

        int index = presentation.DefaultEntryIndex;
        if (index < 0 || index >= entries.Length)
            index = 0;

        WeaponPresentation.Entry entry = entries[index];
        if (entry != null)
            return WeaponActionUtil.Normalize(entry.action);

        for (int i = 0; i < entries.Length; i++)
        {
            WeaponPresentation.Entry candidate = entries[i];
            if (candidate == null)
                continue;
            return WeaponActionUtil.Normalize(candidate.action);
        }

        return WeaponAction.Swing;
    }

    public static WeaponAction ResolveSelected(
        ItemInstance instance,
        WeaponPresentation presentation)
    {
        WeaponActionMask available = Available(presentation);
        WeaponAction? stored = instance != null ? instance.SelectedAction : null;
        if (stored.HasValue &&
            (available & WeaponActionUtil.ToMask(stored.Value)) != 0)
            return WeaponActionUtil.Normalize(stored.Value);

        return Default(presentation);
    }

    public static WeaponPresentation Resolve(
        WeaponPresentationCatalog catalog,
        ItemStack stack)
    {
        if (catalog == null)
            return null;
        return catalog.Resolve(stack?.ItemId, stack?.Item);
    }
}
