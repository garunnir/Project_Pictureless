// ============================================================
// CharacterGearSetupMenu — 플레이어에 Gear 호스트·듀얼 드라이버 Ensure (Patch)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterGearSetupMenu
{
    [MenuItem("Dist/Character/Ensure Player Gear Components")]
    static void EnsurePlayerGearComponents()
    {
        PlayerInventoryRuntime runtime = Object.FindAnyObjectByType<PlayerInventoryRuntime>();
        if (runtime == null)
        {
            Debug.LogError("[CharacterGearSetupMenu] PlayerInventoryRuntime not found in scene.");
            return;
        }

        GameObject go = runtime.gameObject;
        Undo.RegisterCompleteObjectUndo(go, "Ensure Player Gear Components");

        if (go.GetComponent<InventoryTimedMoveHost>() == null)
            Undo.AddComponent<InventoryTimedMoveHost>(go);
        if (go.GetComponent<PlayerGearHost>() == null)
            Undo.AddComponent<PlayerGearHost>(go);

        PlayerCombatController combat = go.GetComponent<PlayerCombatController>()
            ?? go.GetComponentInChildren<PlayerCombatController>();
        if (combat != null && combat.GetComponent<DualWieldAttackDriver>() == null)
            Undo.AddComponent<DualWieldAttackDriver>(combat.gameObject);

        EditorUtility.SetDirty(go);
        Debug.Log("[CharacterGearSetupMenu] Player gear components ensured.", go);
    }
}
#endif
