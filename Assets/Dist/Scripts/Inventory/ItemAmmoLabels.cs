// ============================================================
// ItemAmmoLabels — 탄창 보급·장착 탄창 이름 접미사
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class ItemAmmoLabels
{
    const string KeyMagSupply = "ItemAmmo.MagSupplyFormat";
    const string KeyGunMag = "ItemAmmo.GunMagFormat";

    public static string AppendState(string baseName, ItemStack stack)
    {
        if (string.IsNullOrEmpty(baseName) || stack?.Item == null)
            return baseName;

        if (stack.Item.magazine != null)
        {
            int cap = stack.Item.magazine.capacity;
            int have = stack.Instance != null ? stack.Instance.SupplyRounds : 0;
            return Loc.Format(KeyMagSupply, baseName, have, cap);
        }

        if (stack.Item.gun != null && stack.LoadedMagazine != null)
        {
            ItemData magItem = stack.LoadedMagazine.Item;
            string magName = magItem != null
                ? ItemNameTable.Get(
                    magItem.id,
                    LocalizationBundle.Get()?.ActiveLanguage ?? DisplayLanguage.Ko)
                : string.Empty;
            string withMag = Loc.Format(KeyGunMag, baseName, magName);
            if (magItem?.magazine == null)
                return withMag;

            int cap = magItem.magazine.capacity;
            int have = stack.LoadedMagazine.Instance != null
                ? stack.LoadedMagazine.Instance.SupplyRounds
                : 0;
            return Loc.Format(KeyMagSupply, withMag, have, cap);
        }

        if (stack.Item.gun != null && WeaponAmmoFit.IsClipFed(stack.Item))
        {
            int cap = stack.Item.gun.clip_size;
            int have = stack.Instance != null ? stack.Instance.ChamberRounds : 0;
            return Loc.Format(KeyMagSupply, baseName, have, cap);
        }

        return baseName;
    }
}
