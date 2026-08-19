// ============================================================
// InventoryItemDetailLabels — 호버 보조창 행별 문구 SSOT
// ============================================================

using System.Text;
using Garunnir.Runtime.Gameplay.Data;

public static class InventoryItemDetailLabels
{
    const string KeyCategory = "Inventory.ItemDetail.Category";
    const string KeyType = "Inventory.ItemDetail.Type";
    const string KeyCount = "Inventory.ItemDetail.Count";
    const string KeyWeight = "Inventory.ItemDetail.Weight";
    const string KeyVolume = "Inventory.ItemDetail.Volume";
    const string KeyDurability = "Inventory.ItemDetail.Durability";
    const string KeyDurabilityPristine = "Inventory.ItemDetail.DurabilityPristine";
    const string KeyContainerCapacity = "Inventory.ItemDetail.ContainerCapacity";
    const string KeyMaterials = "Inventory.ItemDetail.Materials";
    const string KeyDamagePrefix = "ItemDamage.";
    const int MaxDamageLevel = 4;

    public static string FormatName(ItemStack stack)
    {
        if (stack?.Item == null)
            return string.Empty;

        return ItemAmmoLabels.AppendState(
            ItemDamageLabels.FormatName(
                UITextPresenter.GetItemName(stack.Item),
                stack.DamageLevel),
            stack);
    }

    public static bool TryFormatCategory(ItemData item, out string text)
    {
        text = null;
        if (item == null || string.IsNullOrEmpty(item.category))
            return false;

        text = Loc.Format(KeyCategory, InventoryWindowLabels.GetItemCategory(item.category));
        return true;
    }

    public static bool TryFormatType(ItemData item, out string text)
    {
        text = null;
        if (item == null || string.IsNullOrEmpty(item.type))
            return false;

        text = Loc.Format(KeyType, item.type);
        return true;
    }

    public static string FormatCount(int count) => Loc.Format(KeyCount, count);

    public static string FormatWeight(ItemStack stack)
    {
        if (stack?.Item == null)
            return string.Empty;

        float unitWeight = stack.Item.Weight;
        return Loc.Format(KeyWeight, unitWeight, stack.TotalWeight);
    }

    public static string FormatVolume(ItemStack stack)
    {
        if (stack?.Item == null)
            return string.Empty;

        return Loc.Format(KeyVolume, stack.TotalVolume);
    }

    public static bool TryFormatDescription(ItemData item, out string text)
    {
        text = null;
        if (item == null || string.IsNullOrEmpty(item.id))
            return false;

        string description = UITextPresenter.GetItemDescription(item);
        if (string.IsNullOrWhiteSpace(description))
            return false;

        text = description.Trim();
        return true;
    }

    public static bool TryFormatDurability(ItemStack stack, out string text)
    {
        text = null;
        if (stack?.Item == null)
            return false;

        if (!ItemDurabilityRules.ShouldShowDurability(stack.Item, stack.DamageLevel))
            return false;

        text = FormatDurability(stack.DamageLevel);
        return true;
    }

    public static string FormatDurability(int damageLevel)
    {
        string status = damageLevel <= 0
            ? Loc.Get(KeyDurabilityPristine)
            : Loc.Get(KeyDamagePrefix + (damageLevel <= MaxDamageLevel ? damageLevel : MaxDamageLevel));

        return Loc.Format(KeyDurability, status);
    }

    public static bool TryFormatContainerCapacity(ItemData item, out string text)
    {
        text = null;
        if (item == null || !item.is_container || string.IsNullOrEmpty(item.container_id))
            return false;

        ContainerData containerDef = GameplayData.GetContainer(item.container_id);
        if (containerDef == null)
            return false;

        text = Loc.Format(
            KeyContainerCapacity,
            containerDef.MaxWeight,
            containerDef.MaxVolume);
        return true;
    }

    public static bool TryFormatMaterials(ItemData item, out string text)
    {
        text = null;
        if (item?.materials == null || item.materials.Count == 0)
            return false;

        var builder = new StringBuilder(64);
        for (int i = 0; i < item.materials.Count; i++)
        {
            string materialId = item.materials[i];
            if (string.IsNullOrEmpty(materialId))
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            MaterialData material = GameplayData.GetMaterial(materialId);
            builder.Append(material != null && !string.IsNullOrEmpty(material.name)
                ? material.name
                : materialId);
        }

        if (builder.Length == 0)
            return false;

        text = Loc.Format(KeyMaterials, builder.ToString());
        return true;
    }
}
