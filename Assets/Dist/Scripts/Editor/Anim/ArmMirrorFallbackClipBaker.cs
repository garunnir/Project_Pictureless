// ============================================================
// ArmMirrorFallbackClipBaker — 라이브러리·Hold 미러 폴백 + catalog (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 라이브러리 Hold|Aim|Attack{Action} 및 thin Hold에 대해 반대손 미러 Fallback을 베이크하고 catalog를 채운다.
/// </summary>
public static class ArmMirrorFallbackClipBaker
{
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots";
    const string FallbackDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots/Fallback";
    const string CatalogPath =
        "Assets/Dist/Visual/Anim/CharacterClips/ArmAnimSlotCatalog.asset";

    static readonly WeaponAction[] Actions =
    {
        WeaponAction.Bashing,
        WeaponAction.Cutting,
        WeaponAction.Gun
    };

    static readonly string[] LibraryPoseStems =
    {
        "HoldBashing", "HoldCutting", "HoldGun",
        "AimBashing", "AimCutting", "AimGun",
        "AttackBashing", "AttackCutting", "AttackGun"
    };

    [MenuItem("Dist/MCP/Bake Arm Mirror Fallback Clips")]
    public static void Bake()
    {
        if (!AssetDatabase.IsValidFolder(FallbackDir))
        {
            if (!AssetDatabase.IsValidFolder(SlotDir))
            {
                Debug.LogError("[ArmMirrorFallbackClipBaker] Slots folder missing.");
                return;
            }

            AssetDatabase.CreateFolder(SlotDir, "Fallback");
        }

        EnsureActionHoldSlots();
        EnsureThinSlots();

        int ok = 0;
        for (int i = 0; i < LibraryPoseStems.Length; i++)
        {
            if (BakePair(LibraryPoseStems[i], "Left", "Right"))
                ok++;
            if (BakePair(LibraryPoseStems[i], "Right", "Left"))
                ok++;
        }

        EnsureCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ArmMirrorFallbackClipBaker] Baked/updated {ok} fallback clips + catalog.");
    }

    /// <summary>기존 Hold_* 를 시드로 Hold{Action}_* 라이브러리 슬롯을 만든다 (이미 있으면 유지).</summary>
    static void EnsureActionHoldSlots()
    {
        string[] hands = { "Left", "Right", "TwoHand" };
        for (int a = 0; a < Actions.Length; a++)
        {
            string action = Actions[a].ToString();
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

        catalog.SetHoldThin(LoadThinHandClips("Hold"));
        catalog.SetAimThin(LoadThinHandClips("Aim"));
        catalog.SetAttackThin(LoadThinHandClips("Attack"));

        var entries = new ArmAnimSlotCatalog.ActionLibraryEntry[Actions.Length];
        for (int i = 0; i < Actions.Length; i++)
        {
            string name = Actions[i].ToString();
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

    static ArmAnimSlotCatalog.HandClips LoadHandClips(string stem) =>
        new ArmAnimSlotCatalog.HandClips
        {
            leftBase = LoadSlot(stem, "Left"),
            rightBase = LoadSlot(stem, "Right"),
            twoHandBase = LoadSlot(stem, "TwoHand"),
            leftFallback = LoadFallback(stem, "Left"),
            rightFallback = LoadFallback(stem, "Right")
        };

    static ArmAnimSlotCatalog.HandClips LoadThinHandClips(string stem) =>
        new ArmAnimSlotCatalog.HandClips
        {
            leftBase = LoadSlot(stem, "Left"),
            rightBase = LoadSlot(stem, "Right"),
            twoHandBase = LoadSlot(stem, "TwoHand"),
            leftFallback = null,
            rightFallback = null
        };

    static AnimationClip LoadSlot(string stem, string hand) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{stem}_{hand}_Slot.anim");

    static bool BakePair(string stem, string ownHand, string otherHand)
    {
        string otherPath = $"{SlotDir}/{stem}_{otherHand}_Slot.anim";
        string fallbackPath = $"{FallbackDir}/{stem}_{ownHand}_Fallback.anim";
        var other = AssetDatabase.LoadAssetAtPath<AnimationClip>(otherPath);
        if (other == null)
            return false;

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(fallbackPath);
        if (existing == null)
        {
            if (!AssetDatabase.CopyAsset(otherPath, fallbackPath))
                return false;
            existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(fallbackPath);
        }

        if (existing == null)
            return false;

        WriteMirroredCurves(other, existing);
        existing.name = $"{stem}_{ownHand}_Fallback";
        EditorUtility.SetDirty(existing);
        return true;
    }

    static void WriteMirroredCurves(AnimationClip source, AnimationClip dest)
    {
        EditorCurveBinding[] oldBindings = AnimationUtility.GetCurveBindings(dest);
        for (int i = 0; i < oldBindings.Length; i++)
            AnimationUtility.SetEditorCurve(dest, oldBindings[i], null);

        EditorCurveBinding[] srcBindings = AnimationUtility.GetCurveBindings(source);
        var written = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < srcBindings.Length; i++)
        {
            EditorCurveBinding src = srcBindings[i];
            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, src);
            if (curve == null)
                continue;

            string mirroredName = MirrorPropertyName(src.propertyName);
            AnimationCurve outCurve = ShouldNegate(src.propertyName)
                ? NegateCurve(curve)
                : new AnimationCurve(curve.keys);

            var dst = src;
            dst.propertyName = mirroredName;
            string key = dst.path + "|" + dst.type.FullName + "|" + dst.propertyName;
            if (!written.Add(key))
                continue;

            AnimationUtility.SetEditorCurve(dest, dst, outCurve);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        AnimationUtility.SetAnimationClipSettings(dest, settings);
        dest.frameRate = source.frameRate;
        dest.wrapMode = source.wrapMode;
        dest.legacy = source.legacy;
    }

    public static string MirrorPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        if (IsLateralMuscleName(propertyName))
            return propertyName;

        if (propertyName.StartsWith("LeftHand.", System.StringComparison.Ordinal))
            return "RightHand." + propertyName.Substring("LeftHand.".Length);
        if (propertyName.StartsWith("RightHand.", System.StringComparison.Ordinal))
            return "LeftHand." + propertyName.Substring("RightHand.".Length);

        if (propertyName.StartsWith("Left", System.StringComparison.Ordinal))
            return "Right" + propertyName.Substring(4);
        if (propertyName.StartsWith("Right", System.StringComparison.Ordinal))
            return "Left" + propertyName.Substring(5);

        return propertyName;
    }

    static bool IsLateralMuscleName(string propertyName) =>
        propertyName.Contains("Left-Right");

    public static bool ShouldNegate(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;

        if (IsLateralMuscleName(propertyName))
            return true;

        if (propertyName == "RootT.x" ||
            propertyName == "RootQ.y" ||
            propertyName == "RootQ.z")
            return true;

        if (propertyName.EndsWith("T.x", System.StringComparison.Ordinal) &&
            (propertyName.StartsWith("Left", System.StringComparison.Ordinal) ||
             propertyName.StartsWith("Right", System.StringComparison.Ordinal)))
            return true;

        if ((propertyName.EndsWith("Q.y", System.StringComparison.Ordinal) ||
             propertyName.EndsWith("Q.z", System.StringComparison.Ordinal)) &&
            (propertyName.StartsWith("Left", System.StringComparison.Ordinal) ||
             propertyName.StartsWith("Right", System.StringComparison.Ordinal)))
            return true;

        return false;
    }

    static AnimationCurve NegateCurve(AnimationCurve source)
    {
        var keys = source.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].value = -keys[i].value;
            keys[i].inTangent = -keys[i].inTangent;
            keys[i].outTangent = -keys[i].outTangent;
        }

        return new AnimationCurve(keys);
    }

    public static AnimationClip LoadFallback(string stem, string ownHand) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{FallbackDir}/{stem}_{ownHand}_Fallback.anim");
}
#endif
