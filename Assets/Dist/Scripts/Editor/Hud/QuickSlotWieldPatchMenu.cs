// ============================================================
// QuickSlotWieldPatchMenu — Dist/MCP HUD·장비창 L/R 들기 슬롯 크롬 Patch
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class QuickSlotWieldPatchMenu
{
    const string QuickSlotPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/HUD/Grp_QuickSlot.prefab";
    const string CharacterWindowPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus/Grp_PlayerStatusWindow.prefab";

    [MenuItem(DistMcpMenus.HudPatchQuickSlotWield)]
    static void PatchAll()
    {
        PatchQuickSlot();
        PatchCharacterWindow();
        MergeLocKeys();
        AssetDatabase.SaveAssets();
        Debug.Log("[QuickSlotWieldPatchMenu] HUD QuickSlot + Character Wield chrome patched.");
    }

    static void PatchQuickSlot()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(QuickSlotPath);
        if (root == null)
        {
            Debug.LogWarning($"[QuickSlotWieldPatchMenu] Missing prefab: {QuickSlotPath}");
            return;
        }

        try
        {
            if (root.GetComponent<UIOverlayWindow>() == null)
                root.AddComponent<UIOverlayWindow>();

            UIHudQuickSlotController controller = root.GetComponent<UIHudQuickSlotController>();
            if (controller == null)
                controller = root.AddComponent<UIHudQuickSlotController>();

            Transform slotL = FindDeep(root.transform, "Slot_L");
            Transform slotR = FindDeep(root.transform, "Slot_R");
            UICharacterWieldSlotView leftView = PatchWieldSlot(slotL, WieldSlotId.Left);
            UICharacterWieldSlotView rightView = PatchWieldSlot(slotR, WieldSlotId.Right);

            Transform legacyDetail = root.transform.Find("DetailPanel");
            if (legacyDetail != null)
                Object.DestroyImmediate(legacyDetail.gameObject);

            var so = new SerializedObject(controller);
            so.FindProperty("_leftSlot").objectReferenceValue = leftView;
            so.FindProperty("_rightSlot").objectReferenceValue = rightView;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, QuickSlotPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PatchCharacterWindow()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CharacterWindowPath);
        if (root == null)
        {
            Debug.LogWarning($"[QuickSlotWieldPatchMenu] Missing prefab: {CharacterWindowPath}");
            return;
        }

        try
        {
            Transform left = FindDeep(root.transform, "Wield_L");
            Transform right = FindDeep(root.transform, "Wield_R");
            PatchWieldSlot(left, WieldSlotId.Left);
            PatchWieldSlot(right, WieldSlotId.Right);
            PrefabUtility.SaveAsPrefabAsset(root, CharacterWindowPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static UICharacterWieldSlotView PatchWieldSlot(Transform slotTf, WieldSlotId slot)
    {
        if (slotTf == null)
        {
            Debug.LogWarning($"[QuickSlotWieldPatchMenu] Missing slot for {slot}.");
            return null;
        }

        GameObject go = slotTf.gameObject;
        Image bg = go.GetComponent<Image>();
        if (bg == null)
            bg = go.AddComponent<Image>();
        bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, bg.color.a < 0.01f ? 0f : bg.color.a);
        if (bg.color.a < 0.01f)
            bg.color = new Color(0.18f, 0.18f, 0.18f, 0.01f);
        bg.raycastTarget = true;

        Transform iconTf = slotTf.Find("Icon");
        if (iconTf != null)
        {
            Image icon = iconTf.GetComponent<Image>();
            if (icon != null)
                icon.raycastTarget = false;
        }

        Image actionIcon = EnsureActionIconTopLeft(slotTf);
        TMP_Text actionLabel = actionIcon != null
            ? actionIcon.transform.Find("Label")?.GetComponent<TMP_Text>()
            : null;
        TMP_Text ammo = EnsureAmmoTopRight(slotTf);
        TMP_Text nameLabel = EnsureHiddenNameLabel(slotTf);

        UICharacterWieldSlotView view = go.GetComponent<UICharacterWieldSlotView>();
        if (view == null)
            view = go.AddComponent<UICharacterWieldSlotView>();

        var so = new SerializedObject(view);
        if (iconTf != null)
            so.FindProperty("_itemIcon").objectReferenceValue = iconTf.GetComponent<Image>();
        so.FindProperty("_actionIcon").objectReferenceValue = actionIcon;
        so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
        so.FindProperty("_ammoLabel").objectReferenceValue = ammo;
        so.FindProperty("_label").objectReferenceValue = nameLabel;
        Transform cooldownTf = slotTf.Find(UICharacterWieldSlotView.CooldownFillObjectName);
        if (cooldownTf != null)
            so.FindProperty("_cooldownFill").objectReferenceValue = cooldownTf.GetComponent<Image>();
        so.ApplyModifiedPropertiesWithoutUndo();

        view.EnsureChrome();
        return view;
    }

    static Image EnsureActionIconTopLeft(Transform slotTf)
    {
        Transform t = slotTf.Find("ActionIcon");
        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
        }
        else
        {
            go = new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(slotTf, false);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(2f, -2f);
        rt.sizeDelta = new Vector2(GearConstants.WieldActionIconSize, GearConstants.WieldActionIconSize);

        Image img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        img.raycastTarget = false;

        Transform labelTf = go.transform.Find("Label");
        if (labelTf == null)
        {
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.layer = LayerMask.NameToLayer("UI");
            labelGo.transform.SetParent(go.transform, false);
            labelTf = labelGo.transform;
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = GearConstants.UiFontSizeActionIcon;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.text = "—";
            DistUiFont.Apply(tmp);
        }
        else
        {
            TMP_Text existing = labelTf.GetComponent<TMP_Text>();
            if (existing != null)
            {
                existing.raycastTarget = false;
                DistUiFont.Apply(existing);
            }
        }

        return img;
    }

    static TMP_Text EnsureAmmoTopRight(Transform slotTf)
    {
        Transform t = slotTf.Find("Ammo");
        if (t == null)
            t = slotTf.Find("tmp");

        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
            go.name = "Ammo";
        }
        else
        {
            go = new GameObject("Ammo", typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(slotTf, false);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        if (rt.sizeDelta.x < 1f || rt.sizeDelta.y < 1f)
            rt.sizeDelta = new Vector2(48f, 14f);
        if (Mathf.Approximately(rt.anchoredPosition.x, 0f) && Mathf.Approximately(rt.anchoredPosition.y, 0f))
            rt.anchoredPosition = new Vector2(-2f, -2f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = GearConstants.UiFontSizeActionIcon;
        tmp.alignment = TextAlignmentOptions.TopRight;
        tmp.raycastTarget = false;
        tmp.text = string.Empty;
        DistUiFont.Apply(tmp);
        return tmp;
    }

    static TMP_Text EnsureHiddenNameLabel(Transform slotTf)
    {
        Transform t = slotTf.Find(ItemNameStatusBar.LabelObjectName);
        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
        }
        else
        {
            go = new GameObject(ItemNameStatusBar.LabelObjectName, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(slotTf, false);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = 1f;
        tmp.color = new Color(1f, 1f, 1f, 0f);
        tmp.raycastTarget = false;
        DistUiFont.Apply(tmp);
        return tmp;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    static void MergeLocKeys()
    {
        LocalizationTable table =
            AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
            return;

        var map = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        for (int i = 0; i < table.Entries.Count; i++)
        {
            LocalizationTable.Entry e = table.Entries[i];
            if (e != null && !string.IsNullOrEmpty(e.key))
                map[e.key] = e.text ?? string.Empty;
        }

        if (!map.ContainsKey("ItemAmmo.WieldGunRounds"))
            map["ItemAmmo.WieldGunRounds"] = "{0}/{1}+{2}";
        if (!map.ContainsKey("ItemAmmo.WieldClipRounds"))
            map["ItemAmmo.WieldClipRounds"] = "{0}/{1}";

        var list = new System.Collections.Generic.List<LocalizationTable.Entry>(map.Count);
        foreach (var kv in map)
            list.Add(new LocalizationTable.Entry { key = kv.Key, text = kv.Value });
        list.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        table.EditorSetEntries(list);
        EditorUtility.SetDirty(table);
    }
}
#endif
