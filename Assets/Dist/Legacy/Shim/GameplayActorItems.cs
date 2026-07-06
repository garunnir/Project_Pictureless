// ============================================================
// GameplayActorItems — 레거시 Actor 필드 → ItemDefinitionSO shim
// ============================================================

using Garunnir;
using Garunnir.Runtime.Gameplay.Item;
using PixelCrushers.DialogueSystem;

public static class GameplayActorItems
{
    public static ItemDefinitionSO GetEquippedWeapon(Actor actor)
    {
        if (actor == null)
            return null;

        ItemCatalogSO catalog = GameplayData.ItemCatalog;
        if (catalog == null)
            return null;

        int itemIndex = Field.LookupInt(actor.fields, ConstDataTable.Item.Weapon);
        return itemIndex != -1 ? catalog.GetByIndex(itemIndex) : null;
    }
}
