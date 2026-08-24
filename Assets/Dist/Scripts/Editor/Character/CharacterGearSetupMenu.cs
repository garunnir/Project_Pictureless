// ============================================================
// CharacterGearSetupMenu — Dist/MCP Gear 컴포넌트 Ensure (에이전트용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterGearSetupMenu
{
    [MenuItem(DistMcpMenus.CharacterEnsurePlayerGearComponents)]
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

        EditorUtility.SetDirty(go);
        Debug.Log("[CharacterGearSetupMenu] Player gear components ensured.", go);
    }
}
#endif
