// ============================================================
// WeaponAnimOverrideEditor — Override=thin 클립 덮어쓰기(컨트롤러는 동작 모름)
// ============================================================

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimatorOverrideController))]
public sealed class WeaponAnimOverrideEditor : Editor
{
    const string CatalogPath =
        "Assets/Dist/SOData/Combat/Fallbacks/ArmAnimSlotCatalog.asset";
    const string CharacterControllerGuid = "340c7b66e1595a44db858530dc6283b7";
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterAnimator/Slots";

    static readonly string[] PosePrefixes = { "Hold", "Aim", "Attack" };
    static readonly string[] Hands = { "Left", "Right", "TwoHand" };

    public override void OnInspectorGUI()
    {
        var ovr = (AnimatorOverrideController)target;
        if (ovr == null || !IsCharacterArmOverride(ovr))
        {
            DrawDefaultInspector();
            return;
        }

        EditorGUILayout.HelpBox(
            "Override = thin 클립 덮어쓰기(분류 아님).\n" +
            "컨트롤러는 Hold/Aim/Attack만 압니다. AnimVerb는 Pipeline → resolve.\n" +
            "비우면 Pipeline 폴백.",
            MessageType.Info);

        var catalog = AssetDatabase.LoadAssetAtPath<ArmAnimSlotCatalog>(CatalogPath);
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        ovr.GetOverrides(pairs);
        var map = new Dictionary<AnimationClip, AnimationClip>(pairs.Count);
        for (int i = 0; i < pairs.Count; i++)
        {
            if (pairs[i].Key == null)
                continue;
            map[pairs[i].Key] = pairs[i].Value;
        }

        bool dirty = false;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Action thin", EditorStyles.boldLabel);
        for (int p = 0; p < PosePrefixes.Length; p++)
        {
            for (int h = 0; h < Hands.Length; h++)
            {
                string clipName = PosePrefixes[p] + "_" + Hands[h] + "_Slot";
                AnimationClip original = ThinClip(catalog, PosePrefixes[p], Hands[h])
                    ?? LoadSlot(clipName);
                if (original == null)
                    continue;

                map.TryGetValue(original, out AnimationClip mapped);
                AnimationClip next = (AnimationClip)EditorGUILayout.ObjectField(
                    PosePrefixes[p] + " / " + Hands[h],
                    mapped == null || mapped == original ? null : mapped,
                    typeof(AnimationClip),
                    false);
                if (next == null)
                    next = original;
                if (next != mapped)
                {
                    map[original] = next;
                    dirty = true;
                }
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Impact thin", EditorStyles.boldLabel);
        dirty |= DrawImpactThin(map, catalog);

        if (EditorGUI.EndChangeCheck() || dirty)
        {
            Undo.RecordObject(ovr, "Edit weapon anim override");
            ApplyThinOnly(ovr, map, catalog);
            EditorUtility.SetDirty(ovr);
        }

        if (GUILayout.Button("Clear non-thin mappings"))
        {
            Undo.RecordObject(ovr, "Clear non-thin override mappings");
            ApplyThinOnly(ovr, map, catalog);
            EditorUtility.SetDirty(ovr);
        }
    }

    static bool DrawImpactThin(
        Dictionary<AnimationClip, AnimationClip> map,
        ArmAnimSlotCatalog catalog)
    {
        bool dirty = false;
        AnimationClip[] impacts =
        {
            catalog != null ? catalog.ImpactRecoilThin : null,
            catalog != null ? catalog.ImpactBlockedThin : null,
            LoadSlot("ImpactRecoil_Slot"),
            LoadSlot("ImpactBlocked_Slot")
        };

        var seen = new HashSet<AnimationClip>();
        for (int i = 0; i < impacts.Length; i++)
        {
            AnimationClip original = impacts[i];
            if (original == null || !seen.Add(original))
                continue;

            map.TryGetValue(original, out AnimationClip mapped);
            AnimationClip next = (AnimationClip)EditorGUILayout.ObjectField(
                original.name,
                mapped == null || mapped == original ? null : mapped,
                typeof(AnimationClip),
                false);
            if (next == null)
                next = original;
            if (next != mapped)
            {
                map[original] = next;
                dirty = true;
            }
        }

        return dirty;
    }

    static void ApplyThinOnly(
        AnimatorOverrideController ovr,
        Dictionary<AnimationClip, AnimationClip> map,
        ArmAnimSlotCatalog catalog)
    {
        HashSet<AnimationClip> thin = CollectThin(catalog);
        var kept = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var kv in map)
        {
            if (kv.Key == null || !thin.Contains(kv.Key))
                continue;
            if (kv.Value == null || kv.Value == kv.Key)
                continue;
            kept.Add(new KeyValuePair<AnimationClip, AnimationClip>(kv.Key, kv.Value));
        }

        ovr.ApplyOverrides(kept);
    }

    static HashSet<AnimationClip> CollectThin(ArmAnimSlotCatalog catalog)
    {
        var set = new HashSet<AnimationClip>();
        if (catalog != null)
        {
            AddHand(set, catalog.HoldThin);
            AddHand(set, catalog.AimThin);
            AddHand(set, catalog.AttackThin);
            if (catalog.ImpactRecoilThin != null)
                set.Add(catalog.ImpactRecoilThin);
            if (catalog.ImpactBlockedThin != null)
                set.Add(catalog.ImpactBlockedThin);
        }

        for (int p = 0; p < PosePrefixes.Length; p++)
        {
            for (int h = 0; h < Hands.Length; h++)
            {
                AnimationClip clip = LoadSlot(PosePrefixes[p] + "_" + Hands[h] + "_Slot");
                if (clip != null)
                    set.Add(clip);
            }
        }

        AnimationClip recoil = LoadSlot("ImpactRecoil_Slot");
        AnimationClip blocked = LoadSlot("ImpactBlocked_Slot");
        if (recoil != null)
            set.Add(recoil);
        if (blocked != null)
            set.Add(blocked);
        return set;
    }

    static AnimationClip ThinClip(ArmAnimSlotCatalog catalog, string pose, string hand)
    {
        if (catalog == null)
            return null;
        ArmAnimSlotCatalog.HandClips row =
            pose == "Hold" ? catalog.HoldThin :
            pose == "Aim" ? catalog.AimThin :
            catalog.AttackThin;
        if (row == null)
            return null;
        if (hand == "Left")
            return row.leftBase;
        if (hand == "Right")
            return row.rightBase;
        return row.twoHandBase;
    }

    static void AddHand(HashSet<AnimationClip> set, ArmAnimSlotCatalog.HandClips hand)
    {
        if (hand == null)
            return;
        if (hand.leftBase != null)
            set.Add(hand.leftBase);
        if (hand.rightBase != null)
            set.Add(hand.rightBase);
        if (hand.twoHandBase != null)
            set.Add(hand.twoHandBase);
    }

    static AnimationClip LoadSlot(string clipName) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>(SlotDir + "/" + clipName + ".anim");

    static bool IsCharacterArmOverride(AnimatorOverrideController ovr)
    {
        RuntimeAnimatorController root = ovr.runtimeAnimatorController;
        if (root == null)
            return false;
        string path = AssetDatabase.GetAssetPath(root);
        if (string.IsNullOrEmpty(path))
            return false;
        return AssetDatabase.AssetPathToGUID(path) == CharacterControllerGuid;
    }
}
