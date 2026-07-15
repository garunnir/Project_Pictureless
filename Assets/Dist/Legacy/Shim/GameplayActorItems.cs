// ============================================================
// GameplayActorItems — 레거시 Actor 필드 → ItemData shim
// ============================================================

using Garunnir;
using Garunnir.Runtime.Gameplay.Data;
using PixelCrushers.DialogueSystem;

public static class GameplayActorItems
{
    public static ItemData GetEquippedWeapon(Actor actor)
    {
        if (actor == null)
            return null;

        GameDatabase db = GameplayData.GameItems;
        if (db == null || db.Items.Count == 0)
            return null;

        int itemIndex = Field.LookupInt(actor.fields, ConstDataTable.Item.Weapon);
        return itemIndex >= 0 && itemIndex < db.Items.Count ? db.Items[itemIndex] : null;
    }
}
