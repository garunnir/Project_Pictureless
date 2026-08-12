// ============================================================
// ArmAnimSlotCatalogBaker — 라이브러리·thin 슬롯 시드 + catalog (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 라이브러리 Hold|Aim|Attack{Swing|Thrust|Trigger|Raise} 슬롯과 thin 키를 시드하고 catalog를 채운다.
/// </summary>
public static class ArmAnimSlotCatalogBaker
{
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots";
    const string CatalogPath =
        "Assets/Dist/Visual/Anim/CharacterClips/ArmAnimSlotCatalog.asset";

    static readonly string[] Hands = { "Left", "Right", "TwoHand" };
    static readonly string[] Phases = { "Hold", "Aim", "Attack" };

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

        EnsureActionLibrarySlots();
        EnsureThinSlots();
        DeleteOrphanHandlessSlots();
        EnsureCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ArmAnimSlotCatalogBaker] Ensured library/thin slots + catalog.");
    }

    /// <summary>
    /// 동사별 Hold/Aim/Attack × 손. 없으면 Swing 라이브러리(또는 thin Hold)에서 복사.
    /// </summary>
    static void EnsureActionLibrarySlots()
    {
        for (int a = 0; a < Actions.Length; a++)
        {
            string action = ClipStem(Actions[a]);
            for (int p = 0; p < Phases.Length; p++)
            {
                string phase = Phases[p];
                for (int h = 0; h < Hands.Length; h++)
                {
                    string hand = Hands[h];
                    string dest = $"{phase}{action}_{hand}_Slot";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{dest}.anim") != null)
                        continue;

                    string seed = $"{phase}Swing_{hand}_Slot";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{seed}.anim") == null)
                        seed = $"{phase}_{hand}_Slot";
                    EnsureCopy(seed, dest);
                }
            }
        }
    }

    static void EnsureThinSlots()
    {
        for (int h = 0; h < Hands.Length; h++)
        {
            string hand = Hands[h];
            EnsureCopy($"HoldSwing_{hand}_Slot", $"Hold_{hand}_Slot");
            EnsureCopy($"AimSwing_{hand}_Slot", $"Aim_{hand}_Slot");
            EnsureCopy($"AttackSwing_{hand}_Slot", $"Attack_{hand}_Slot");
        }
    }

    /// <summary>손 접미사 없는 레거시 Aim*_Slot (AimBashing_Slot 등) 제거.</summary>
    static void DeleteOrphanHandlessSlots()
    {
        string[] orphans =
        {
            "AimSwing_Slot",
            "AimTrigger_Slot",
            "AimBashing_Slot",
            "AimGun_Slot",
            "AimCutting_Slot"
        };
        for (int i = 0; i < orphans.Length; i++)
        {
            string path = $"{SlotDir}/{orphans[i]}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) == null)
                continue;
            AssetDatabase.DeleteAsset(path);
        }
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

    /// <summary>라이브러리 슬롯 스템 = WeaponAction 동사명.</summary>
    static string ClipStem(WeaponAction action)
    {
        switch (WeaponActionUtil.Normalize(action))
        {
            case WeaponAction.Trigger:
                return "Trigger";
            case WeaponAction.Thrust:
                return "Thrust";
            case WeaponAction.Raise:
                return "Raise";
            default:
                return "Swing";
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
