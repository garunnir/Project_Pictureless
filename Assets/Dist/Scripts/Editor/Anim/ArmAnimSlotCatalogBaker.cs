// ============================================================
// ArmAnimSlotCatalogBaker — Leaf마다 폴백 행·슬롯 Ensure (MCP)
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// WeaponActionUtil.All(Leaf) / ArmImpactKind 기준으로 슬롯·Catalog 행을 Ensure한다.
/// Semi/Burst/Auto도 각자 폴백 줄이 있어야 한다. 표시는 DropdownPath(Melee/Trigger).
/// </summary>
public static class ArmAnimSlotCatalogBaker
{
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterAnimator/Slots";
    const string CatalogPath = ArmAnimSlotCatalog.DefaultAssetPath;
    const string PresentationCatalogPath = WeaponPresentationCatalog.DefaultAssetPath;

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
            "[ArmAnimSlotCatalogBaker] Ensured Leaf fallback verbs=" +
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

                    string seed = PickSeedClip(phase, hand, actions[a]);
                    EnsureCopy(seed, dest);
                }
            }
        }
    }

    static string PickSeedClip(string phase, string hand, WeaponAction leaf)
    {
        if (WeaponActionUtil.IsRanged(leaf))
        {
            string trigger = phase + "Trigger_" + hand + "_Slot";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + trigger + ".anim") != null)
                return trigger;
        }

        string swing = phase + "Swing_" + hand + "_Slot";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + swing + ".anim") != null)
            return swing;

        return phase + "_" + hand + "_Slot";
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
        ArmAnimSlotCatalog catalog =
            DistScriptableObjectEnsure.LoadOrCreate<ArmAnimSlotCatalog>(CatalogPath);

        catalog.SetHoldThin(LoadHandClips("Hold"));
        catalog.SetAimThin(LoadHandClips("Aim"));
        catalog.SetAttackThin(LoadHandClips("Attack"));
        catalog.SetImpactThin(
            LoadFlatSlot("ImpactRecoil_Slot"),
            LoadFlatSlot("ImpactBlocked_Slot"));

        WeaponActionVfx rangedVfxTemplate = FindRangedVfxTemplate(catalog);
        WeaponActionVfx meleeVfxTemplate = FindMeleeVfxTemplate(catalog);

        var verbs = new List<ArmAnimSlotCatalog.ActionLibraryEntry>();
        var seen = new HashSet<int>();
        WeaponAction[] all = WeaponActionUtil.All;
        for (int i = 0; i < all.Length; i++)
            verbs.Add(BuildVerbEntry(catalog, all[i], seen, rangedVfxTemplate, meleeVfxTemplate));

        if (catalog.Verbs != null)
        {
            for (int i = 0; i < catalog.Verbs.Length; i++)
            {
                ArmAnimSlotCatalog.ActionLibraryEntry orphan = catalog.Verbs[i];
                if (orphan == null)
                    continue;
                WeaponAction leaf = WeaponActionUtil.Normalize(orphan.action);
                if (leaf == WeaponAction.Trigger)
                    leaf = WeaponAction.Semi;
                if (seen.Contains((int)leaf))
                    continue;
                verbs.Add(BuildVerbEntry(catalog, leaf, seen, rangedVfxTemplate, meleeVfxTemplate));
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

    static WeaponActionVfx FindRangedVfxTemplate(ArmAnimSlotCatalog catalog)
    {
        if (catalog.Verbs == null)
            return null;
        for (int i = 0; i < catalog.Verbs.Length; i++)
        {
            ArmAnimSlotCatalog.ActionLibraryEntry e = catalog.Verbs[i];
            if (e == null || e.vfx == null || !HasAnyVfx(e.vfx))
                continue;
            if (WeaponActionUtil.IsRanged(e.action) || e.action == WeaponAction.Trigger)
                return CloneVfx(e.vfx);
        }

        return null;
    }

    static WeaponActionVfx FindMeleeVfxTemplate(ArmAnimSlotCatalog catalog)
    {
        if (catalog.Verbs == null)
            return null;
        for (int i = 0; i < catalog.Verbs.Length; i++)
        {
            ArmAnimSlotCatalog.ActionLibraryEntry e = catalog.Verbs[i];
            if (e == null || e.vfx == null || !HasAnyVfx(e.vfx))
                continue;
            WeaponAction leaf = WeaponActionUtil.Normalize(e.action);
            if (leaf == WeaponAction.Swing || leaf == WeaponAction.Thrust)
                return CloneVfx(e.vfx);
        }

        return null;
    }

    static ArmAnimSlotCatalog.ActionLibraryEntry BuildVerbEntry(
        ArmAnimSlotCatalog catalog,
        WeaponAction action,
        HashSet<int> seen,
        WeaponActionVfx rangedVfxTemplate,
        WeaponActionVfx meleeVfxTemplate)
    {
        WeaponAction leaf = WeaponActionUtil.Normalize(action);
        seen.Add((int)leaf);
        string name = ClipStem(leaf);

        ArmAnimSlotCatalog.ActionLibraryEntry existing = FindExact(catalog, leaf);
        WeaponActionVfx vfx = existing?.vfx != null && HasAnyVfx(existing.vfx)
            ? CloneVfx(existing.vfx)
            : new WeaponActionVfx();

        if (!HasAnyVfx(vfx) && WeaponActionUtil.IsRanged(leaf) && rangedVfxTemplate != null)
            vfx = CloneVfx(rangedVfxTemplate);
        if (!HasAnyVfx(vfx) &&
            (leaf == WeaponAction.Raise || leaf == WeaponAction.Swing || leaf == WeaponAction.Thrust) &&
            meleeVfxTemplate != null)
            vfx = CloneVfx(meleeVfxTemplate);

        return new ArmAnimSlotCatalog.ActionLibraryEntry
        {
            action = leaf,
            hold = LoadHandClips("Hold" + name),
            aim = LoadHandClips("Aim" + name),
            attack = LoadHandClips("Attack" + name),
            vfx = vfx
        };
    }

    static ArmAnimSlotCatalog.ActionLibraryEntry FindExact(
        ArmAnimSlotCatalog catalog,
        WeaponAction leaf)
    {
        if (catalog.Verbs == null)
            return null;
        for (int i = 0; i < catalog.Verbs.Length; i++)
        {
            ArmAnimSlotCatalog.ActionLibraryEntry e = catalog.Verbs[i];
            if (e == null)
                continue;
            if (WeaponActionUtil.Normalize(e.action) == leaf)
                return e;
            if (leaf == WeaponAction.Semi && e.action == WeaponAction.Trigger)
                return e;
        }

        return null;
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
            vfx = existing?.vfx != null ? CloneVfx(existing.vfx) : new WeaponActionVfx()
        };
    }

    static WeaponActionVfx CloneVfx(WeaponActionVfx src)
    {
        if (src == null)
            return new WeaponActionVfx();
        return new WeaponActionVfx
        {
            actionVfx = src.actionVfx,
            tracerVfx = src.tracerVfx,
            hitVfx = src.hitVfx,
            missVfx = src.missVfx
        };
    }

    static bool HasAnyVfx(WeaponActionVfx vfx) =>
        vfx != null &&
        (vfx.actionVfx != null ||
         vfx.tracerVfx != null ||
         vfx.hitVfx != null ||
         vfx.missVfx != null);

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

    /// <summary>슬롯 파일 스템 = Normalize(Leaf) 이름.</summary>
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
