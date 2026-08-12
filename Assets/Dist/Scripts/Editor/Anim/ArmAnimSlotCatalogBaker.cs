// ============================================================
// ArmAnimSlotCatalogBaker — 라이브러리·thin 슬롯 시드 + catalog (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 라이브러리 Hold|Aim|Attack{Action} 슬롯과 thin 키를 시드하고 catalog를 채운다.
/// </summary>
public static class ArmAnimSlotCatalogBaker
{
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots";
    const string CatalogPath =
        "Assets/Dist/Visual/Anim/CharacterClips/ArmAnimSlotCatalog.asset";

    static readonly WeaponAction[] Actions =
    {
        WeaponAction.Swing,
        WeaponAction.Thrust,
        WeaponAction.Trigger,
        WeaponAction.Raise
    };

    [MenuItem("Dist/MCP/Ensure Arm Anim Slot Catalog")]
    public static void Bake()
    {
        if (!AssetDatabase.IsValidFolder(SlotDir))
        {
            Debug.LogError("[ArmAnimSlotCatalogBaker] Slots folder missing.");
            return;
        }

        EnsureActionHoldSlots();
        EnsureThinSlots();
        EnsureCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ArmAnimSlotCatalogBaker] Ensured library/thin slots + catalog.");
    }

    /// <summary>기존 Hold_* 를 시드로 Hold{Action}_* 라이브러리 슬롯을 만든다 (이미 있으면 유지).</summary>
    static void EnsureActionHoldSlots()
    {
        string[] hands = { "Left", "Right", "TwoHand" };
        for (int a = 0; a < Actions.Length; a++)
        {
            string action = ClipStem(Actions[a]);
            for (int h = 0; h < hands.Length; h++)
                EnsureCopy($"Hold_{hands[h]}_Slot", $"Hold{action}_{hands[h]}_Slot");
        }
    }

    static void EnsureThinSlots()
    {
        EnsureCopy("HoldBashing_Left_Slot", "Hold_Left_Slot");
        EnsureCopy("HoldBashing_Right_Slot", "Hold_Right_Slot");
        EnsureCopy("HoldBashing_TwoHand_Slot", "Hold_TwoHand_Slot");
        EnsureCopy("AimBashing_Left_Slot", "Aim_Left_Slot");
        EnsureCopy("AimBashing_Right_Slot", "Aim_Right_Slot");
        EnsureCopy("AimBashing_TwoHand_Slot", "Aim_TwoHand_Slot");
        EnsureCopy("AttackBashing_Left_Slot", "Attack_Left_Slot");
        EnsureCopy("AttackBashing_Right_Slot", "Attack_Right_Slot");
        EnsureCopy("AttackBashing_TwoHand_Slot", "Attack_TwoHand_Slot");
    }

    static void EnsureCopy(string sourceName, string destName)
    {
        string destPath = $"{SlotDir}/{destName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath) != null)
            return;

        string sourcePath = $"{SlotDir}/{sourceName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath) == null)
            return;

        AssetDatabase.CopyAsset(sourcePath, destPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (clip != null)
        {
            clip.name = destName;
            EditorUtility.SetDirty(clip);
        }
    }

    static void EnsureCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ArmAnimSlotCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ArmAnimSlotCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.SetHoldThin(LoadHandClips("Hold"));
        catalog.SetAimThin(LoadHandClips("Aim"));
        catalog.SetAttackThin(LoadHandClips("Attack"));

        var entries = new ArmAnimSlotCatalog.ActionLibraryEntry[Actions.Length];
        for (int i = 0; i < Actions.Length; i++)
        {
            string name = ClipStem(Actions[i]);
            entries[i] = new ArmAnimSlotCatalog.ActionLibraryEntry
            {
                action = Actions[i],
                hold = LoadHandClips("Hold" + name),
                aim = LoadHandClips("Aim" + name),
                attack = LoadHandClips("Attack" + name)
            };
        }

        catalog.SetActions(entries);
        EditorUtility.SetDirty(catalog);
    }

    /// <summary>기존 슬롯 파일명. 동사 rename과 분리.</summary>
    static string ClipStem(WeaponAction action)
    {
        switch (WeaponActionUtil.Normalize(action))
        {
            case WeaponAction.Trigger:
                return "Gun";
            case WeaponAction.Thrust:
                return "Thrust";
            case WeaponAction.Raise:
                return "Raise";
            default:
                return "Bashing";
        }
    }

    static ArmAnimSlotCatalog.HandClips LoadHandClips(string stem) =>
        new ArmAnimSlotCatalog.HandClips
        {
            leftBase = LoadSlot(stem, "Left"),
            rightBase = LoadSlot(stem, "Right"),
            twoHandBase = LoadSlot(stem, "TwoHand")
        };

    static AnimationClip LoadSlot(string stem, string hand) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{stem}_{hand}_Slot.anim");
}
#endif
