// ============================================================
// ItemAmmoLabels — 탄창 보급·장착 탄창 이름 접미사 · 들기 슬롯 탄약 표기
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class ItemAmmoLabels
{
    const string KeyMagSupply = "ItemAmmo.MagSupplyFormat";
    const string KeyGunMag = "ItemAmmo.GunMagFormat";
    const string KeyWieldGunRounds = "ItemAmmo.WieldGunRounds";
    const string KeyWieldClipRounds = "ItemAmmo.WieldClipRounds";

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

    /// <summary>
    /// 들기 슬롯 우상단. 탄창 총=`현/최대+약실`, 클립=`약실/클립`. 비총기·빈 칸은 빈 문자열.
    /// </summary>
    public static string FormatWieldGunRounds(ItemStack stack)
    {
        if (stack?.Item?.gun == null)
            return string.Empty;

        int chamber = stack.Instance != null ? stack.Instance.ChamberRounds : 0;

        if (WeaponAmmoFit.IsClipFed(stack.Item))
        {
            int clipCap = stack.Item.gun.clip_size;
            return Loc.Format(KeyWieldClipRounds, chamber, clipCap);
        }

        if (WeaponAmmoFit.HasMagazineWell(stack.Item))
        {
            int have = 0;
            int cap = 0;
            ItemStack mag = stack.LoadedMagazine;
            if (mag?.Item?.magazine != null)
            {
                cap = mag.Item.magazine.capacity;
                have = mag.Instance != null ? mag.Instance.SupplyRounds : 0;
            }

            return Loc.Format(KeyWieldGunRounds, have, cap, chamber);
        }

        return string.Empty;
    }
}
