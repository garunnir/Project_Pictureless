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

    static readonly GUIContent SpeedContent = new GUIContent("Speed");
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
            "비우면 Pipeline 폴백·배속 1.\n" +
            "할당한 클립 옆 Speed = 그 클립 재생 배속 (슬롯 속도 아님).",
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

        WeaponAnimClipSpeeds speeds = FindClipSpeeds(ovr);
        bool dirty = false;
        bool speedsDirty = false;
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

                dirty |= DrawClipRow(
                    ovr,
                    PosePrefixes[p] + " / " + Hands[h],
                    original,
                    map,
                    ref speeds,
                    ref speedsDirty);
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Impact thin", EditorStyles.boldLabel);
        dirty |= DrawImpactThin(ovr, map, catalog, ref speeds, ref speedsDirty);

        if (EditorGUI.EndChangeCheck() || dirty || speedsDirty)
        {
            Undo.RecordObject(ovr, "Edit weapon anim override");
            ApplyThinOnly(ovr, map, catalog);
            EditorUtility.SetDirty(ovr);
            if (speeds != null)
            {
                Undo.RecordObject(speeds, "Edit override clip speed");
                PruneClipSpeeds(speeds, map);
                EditorUtility.SetDirty(speeds);
                WirePresentations(ovr, speeds);
            }
        }

        if (GUILayout.Button("Clear non-thin mappings"))
        {
            Undo.RecordObject(ovr, "Clear non-thin override mappings");
            ApplyThinOnly(ovr, map, catalog);
            EditorUtility.SetDirty(ovr);
        }
    }

    static bool DrawImpactThin(
        AnimatorOverrideController ovr,
        Dictionary<AnimationClip, AnimationClip> map,
        ArmAnimSlotCatalog catalog,
        ref WeaponAnimClipSpeeds speeds,
        ref bool speedsDirty)
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

            dirty |= DrawClipRow(ovr, original.name, original, map, ref speeds, ref speedsDirty);
        }

        return dirty;
    }

    static bool DrawClipRow(
        AnimatorOverrideController ovr,
        string label,
        AnimationClip original,
        Dictionary<AnimationClip, AnimationClip> map,
        ref WeaponAnimClipSpeeds speeds,
        ref bool speedsDirty)
    {
        map.TryGetValue(original, out AnimationClip mapped);
        bool hasOverride = mapped != null && mapped != original;
        AnimationClip displayed = hasOverride ? mapped : null;

        Rect total = EditorGUILayout.GetControlRect();
        Rect field = EditorGUI.PrefixLabel(total, new GUIContent(label));
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        AnimationClip next;
        if (hasOverride)
        {
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float speedLabelW = EditorStyles.label.CalcSize(SpeedContent).x;
            float speedFieldW = EditorGUIUtility.fieldWidth;
            float speedBlock = speedLabelW + gap + speedFieldW;
            float minClip = EditorGUIUtility.fieldWidth;
            if (speedBlock + gap + minClip > field.width)
                speedBlock = Mathf.Max(speedFieldW, field.width - minClip - gap);

            Rect speedArea = new Rect(field.xMax - speedBlock, field.y, speedBlock, field.height);
            field.xMax = speedArea.x - gap;

            next = (AnimationClip)EditorGUI.ObjectField(
                field, displayed, typeof(AnimationClip), false);
            if (next == null)
                next = original;

            if (next != original)
            {
                float speed = speeds != null
                    ? speeds.GetSpeed(next)
                    : WeaponAnimClipSpeeds.DefaultSpeed;
                Rect labelRect = new Rect(speedArea.x, speedArea.y, speedLabelW, speedArea.height);
                Rect valueRect = new Rect(
                    labelRect.xMax + gap,
                    speedArea.y,
                    Mathf.Max(0f, speedArea.xMax - labelRect.xMax - gap),
                    speedArea.height);
                EditorGUI.LabelField(labelRect, SpeedContent);
                float nextSpeed = EditorGUI.FloatField(valueRect, speed);
                if (nextSpeed < 0f)
                    nextSpeed = 0f;
                if (!Mathf.Approximately(nextSpeed, speed))
                {
                    if (speeds == null)
                        speeds = GetOrCreateClipSpeeds(ovr);
                    if (speeds != null)
                    {
                        speeds.SetSpeed(next, nextSpeed);
                        speedsDirty = true;
                    }
                }
            }
        }
        else
        {
            next = (AnimationClip)EditorGUI.ObjectField(
                field, displayed, typeof(AnimationClip), false);
            if (next == null)
                next = original;
        }

        EditorGUI.indentLevel = indent;

        bool clipDirty = next != mapped;
        if (clipDirty)
            map[original] = next;
        return clipDirty;
    }

    static void PruneClipSpeeds(
        WeaponAnimClipSpeeds speeds,
        Dictionary<AnimationClip, AnimationClip> map)
    {
        if (speeds == null || map == null)
            return;

        var keep = new List<AnimationClip>(map.Count);
        foreach (var kv in map)
        {
            if (kv.Value == null || kv.Value == kv.Key)
                continue;
            keep.Add(kv.Value);
        }

        speeds.RetainOnly(keep.ToArray());
    }

    static WeaponAnimClipSpeeds FindClipSpeeds(AnimatorOverrideController ovr)
    {
        string path = AssetDatabase.GetAssetPath(ovr);
        if (string.IsNullOrEmpty(path))
            return null;
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is WeaponAnimClipSpeeds speeds)
                return speeds;
        }

        return null;
    }

    static WeaponAnimClipSpeeds GetOrCreateClipSpeeds(AnimatorOverrideController ovr)
    {
        WeaponAnimClipSpeeds existing = FindClipSpeeds(ovr);
        if (existing != null)
        {
            WirePresentations(ovr, existing);
            return existing;
        }

        string path = AssetDatabase.GetAssetPath(ovr);
        if (string.IsNullOrEmpty(path))
            return null;

        var speeds = ScriptableObject.CreateInstance<WeaponAnimClipSpeeds>();
        speeds.name = "ClipSpeeds";
        AssetDatabase.AddObjectToAsset(speeds, ovr);
        Undo.RegisterCreatedObjectUndo(speeds, "Create override clip speeds");
        EditorUtility.SetDirty(ovr);
        EditorUtility.SetDirty(speeds);
        AssetDatabase.SaveAssets();
        WirePresentations(ovr, speeds);
        return speeds;
    }

    static void WirePresentations(AnimatorOverrideController ovr, WeaponAnimClipSpeeds speeds)
    {
        if (ovr == null || speeds == null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:WeaponPresentation");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var presentation = AssetDatabase.LoadAssetAtPath<WeaponPresentation>(path);
            if (presentation == null || presentation.AnimatorOverride != ovr)
                continue;
            if (ReferenceEquals(presentation.AnimClipSpeeds, speeds))
                continue;
            Undo.RecordObject(presentation, "Wire anim clip speeds");
            presentation.SetAnimClipSpeeds(speeds);
            EditorUtility.SetDirty(presentation);
        }
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
