// ============================================================
// ArmAnimSlotCatalogBaker — Pipeline 동사·Impact 시드 + catalog (MCP)
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// WeaponActionUtil.All / ArmImpactKind 기준으로 슬롯·Pipeline 행을 Ensure한다.
/// 동사 추가 시 All(+Mask)만 갱신한 뒤 이 메뉴를 다시 돌리면 된다.
/// </summary>
public static class ArmAnimSlotCatalogBaker
{
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots";
    const string CatalogPath =
        "Assets/Dist/Visual/Anim/CharacterClips/ArmAnimSlotCatalog.asset";
    const string PresentationCatalogPath =
        "Assets/Dist/SOData/Combat/WeaponPresentations/WeaponPresentationCatalog.asset";

    static readonly string[] Hands = { "Left", "Right", "TwoHand" };
    static readonly string[] Phases = { "Hold", "Aim", "Attack" };

    [MenuItem("Dist/MCP/Ensure Arm Anim Pipeline")]
    [MenuItem("Dist/MCP/Ensure Arm Anim Slot Catalog")]
    public static void Bake()
    {
        if (!AssetDatabase.IsValidFolder(SlotDir))
        {
            Debug.LogError("[ArmAnimSlotCatalogBaker]Slots folder missing.");
            return;
        }

        EnsureActionLibrarySlots();
        EnsureImpactLibrarySlots();
        EnsureThinSlots();
        EnsureImpactThinSlots();
        DeleteOrphanHandlessSlots();
        EnsureCatalog();
        WirePresentationCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[ArmAnimSlotCatalogBaker] Ensured Pipeline verbs=" +
            WeaponActionUtil.All.Length +
            " impacts=" +
            Enum.GetValues(typeof(ArmImpactKind)).Length);
    }

    static void EnsureActionLibrarySlots()
    {
        WeaponAction[] actions = WeaponActionUtil.All;
        for (int a = 0; a < actions.Length; a++)
        {
            string action = ClipStem(actions[a]);
            for (int p = 0; p < Phases.Length; p++)
            {
                string phase = Phases[p];
                for (int h = 0; h < Hands.Length; h++)
                {
                    string hand = Hands[h];
                    string dest = phase + action + "_" + hand + "_Slot";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + dest + ".anim") != null)
                        continue;

                    string seed = phase + "Swing_" + hand + "_Slot";
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + seed + ".anim") == null)
                        seed = phase + "_" + hand + "_Slot";
                    EnsureCopy(seed, dest);
                }
            }
        }
    }

    static void EnsureImpactLibrarySlots()
    {
        foreach (ArmImpactKind kindEnum in Enum.GetValues(typeof(ArmImpactKind)))
        {
            string kind = kindEnum.ToString();
            for (int h = 0; h < Hands.Length; h++)
            {
                string hand = Hands[h];
                string dest = "Impact" + kind + "_" + hand + "_Slot";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + dest + ".anim") != null)
                    continue;

                string seed = "AttackSwing_" + hand + "_Slot";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + seed + ".anim") == null)
                    seed = "Attack_" + hand + "_Slot";
                EnsureCopy(seed, dest);
            }
        }
    }

    static void EnsureThinSlots()
    {
        for (int h = 0; h < Hands.Length; h++)
        {
            string hand = Hands[h];
            EnsureCopy("HoldSwing_" + hand + "_Slot", "Hold_" + hand + "_Slot");
            EnsureCopy("AimSwing_" + hand + "_Slot", "Aim_" + hand + "_Slot");
            EnsureCopy("AttackSwing_" + hand + "_Slot", "Attack_" + hand + "_Slot");
        }
    }

    static void EnsureImpactThinSlots()
    {
        foreach (ArmImpactKind kindEnum in Enum.GetValues(typeof(ArmImpactKind)))
        {
            string kind = kindEnum.ToString();
            string thin = "Impact" + kind + "_Slot";
            string seed = "Impact" + kind + "_Right_Slot";
            EnsureCopy(seed, thin);
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + thin + ".anim") == null)
                EnsureCopy("Attack_Right_Slot", thin);
        }
    }

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
            string path = SlotDir + "/" + orphans[i] + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) == null)
                continue;
            AssetDatabase.DeleteAsset(path);
        }
    }

    static void EnsureCopy(string sourceName, string destName)
    {
        string destPath = SlotDir + "/" + destName + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath) != null)
            return;

        string sourcePath = SlotDir + "/" + sourceName + ".anim";
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
        catalog.SetImpactThin(
            LoadFlatSlot("ImpactRecoil_Slot"),
            LoadFlatSlot("ImpactBlocked_Slot"));

        var verbs = new List<ArmAnimSlotCatalog.ActionLibraryEntry>();
        var seen = new HashSet<int>();
        WeaponAction[] all = WeaponActionUtil.All;
        for (int i = 0; i < all.Length; i++)
            verbs.Add(BuildVerbEntry(catalog, all[i], seen));

        if (catalog.Verbs != null)
        {
            for (int i = 0; i < catalog.Verbs.Length; i++)
            {
                ArmAnimSlotCatalog.ActionLibraryEntry orphan = catalog.Verbs[i];
                if (orphan == null)
                    continue;
                int key = (int)WeaponActionUtil.Normalize(orphan.action);
                if (seen.Contains(key))
                    continue;
                verbs.Add(BuildVerbEntry(catalog, orphan.action, seen));
            }
        }

        catalog.SetVerbs(verbs.ToArray());

        var impacts = new List<ArmAnimSlotCatalog.ImpactLibraryEntry>();
        var seenKinds = new HashSet<ArmImpactKind>();
        foreach (ArmImpactKind kind in Enum.GetValues(typeof(ArmImpactKind)))
            impacts.Add(BuildImpactEntry(catalog, kind, seenKinds));

        if (catalog.Impacts != null)
        {
            for (int i = 0; i < catalog.Impacts.Length; i++)
            {
                ArmAnimSlotCatalog.ImpactLibraryEntry orphan = catalog.Impacts[i];
                if (orphan == null || seenKinds.Contains(orphan.kind))
                    continue;
                impacts.Add(BuildImpactEntry(catalog, orphan.kind, seenKinds));
            }
        }

        catalog.SetImpacts(impacts.ToArray());
        EditorUtility.SetDirty(catalog);
    }

    static ArmAnimSlotCatalog.ActionLibraryEntry BuildVerbEntry(
        ArmAnimSlotCatalog catalog,
        WeaponAction action,
        HashSet<int> seen)
    {
        WeaponAction normalized = WeaponActionUtil.Normalize(action);
        seen.Add((int)normalized);
        string name = ClipStem(normalized);

        ArmAnimSlotCatalog.ActionLibraryEntry existing = catalog.FindAction(normalized);
        WeaponActionVfx vfx = existing?.vfx != null && HasAnyVfx(existing.vfx)
            ? existing.vfx
            : new WeaponActionVfx();

        return new ArmAnimSlotCatalog.ActionLibraryEntry
        {
            action = normalized,
            hold = LoadHandClips("Hold" + name),
            aim = LoadHandClips("Aim" + name),
            attack = LoadHandClips("Attack" + name),
            vfx = vfx
        };
    }

    static ArmAnimSlotCatalog.ImpactLibraryEntry BuildImpactEntry(
        ArmAnimSlotCatalog catalog,
        ArmImpactKind kind,
        HashSet<ArmImpactKind> seen)
    {
        seen.Add(kind);
        ArmAnimSlotCatalog.ImpactLibraryEntry existing = catalog.FindImpact(kind);
        AnimationClip thin = existing != null && existing.thin != null
            ? existing.thin
            : LoadFlatSlot("Impact" + kind + "_Slot");
        return new ArmAnimSlotCatalog.ImpactLibraryEntry
        {
            kind = kind,
            clips = LoadHandClips("Impact" + kind),
            thin = thin,
            vfx = existing?.vfx ?? new WeaponActionVfx()
        };
    }

    static bool HasAnyVfx(WeaponActionVfx vfx) =>
        vfx.actionVfx != null ||
        vfx.tracerVfx != null ||
        vfx.hitVfx != null ||
        vfx.missVfx != null;

    static void WirePresentationCatalog()
    {
        var presentation = AssetDatabase.LoadAssetAtPath<WeaponPresentationCatalog>(
            PresentationCatalogPath);
        var pipeline = AssetDatabase.LoadAssetAtPath<ArmAnimSlotCatalog>(CatalogPath);
        if (presentation == null || pipeline == null)
            return;
        presentation.SetAnimPipeline(pipeline);
        EditorUtility.SetDirty(presentation);
        if (presentation.Fallbacks != null)
            EditorUtility.SetDirty(presentation.Fallbacks);
    }

    /// <summary>슬롯 파일 스템 = Normalize 후 enum 이름. 새 동사도 switch 없이 동작.</summary>
    static string ClipStem(WeaponAction action) =>
        WeaponActionUtil.Normalize(action).ToString();

    static ArmAnimSlotCatalog.HandClips LoadHandClips(string stem) =>
        new ArmAnimSlotCatalog.HandClips
        {
            leftBase = LoadSlot(stem, "Left"),
            rightBase = LoadSlot(stem, "Right"),
            twoHandBase = LoadSlot(stem, "TwoHand")
        };

    static AnimationClip LoadSlot(string stem, string hand) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>(
            SlotDir + "/" + stem + "_" + hand + "_Slot.anim");

    static AnimationClip LoadFlatSlot(string fileName) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + fileName + ".anim");
}
#endif
