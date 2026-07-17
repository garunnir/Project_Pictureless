// ============================================================
// PlayerStatusUISetupMenu — Bake 프리팹 + 씬에 Controller/Launcher 설치
// ============================================================

#if UNITY_EDITOR
using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class PlayerStatusUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus";
    const string WindowPrefabPath = PrefabFolder + "/Grp_PlayerStatusWindow.prefab";

    [MenuItem("Dist/PlayerStatus/Verify Ownership Cascade (Edit Mode)")]
    static void VerifyOwnershipCascade()
    {
        var body = PlayerBody.CreateHumanDefault(8);
        bool hadHand = body.Has(BodyPartIds.HandL);
        bool hadFinger = body.Has(BodyPartIds.FingerIndexL);
        var effects = new System.Collections.Generic.List<BodyPartEffect>();
        body.CollectEffectsUnder(BodyPartIds.ArmL, effects, includeDescendants: true);
        int effectCountBefore = effects.Count;

        bool removed = body.RemovePart(BodyPartIds.ArmL);
        bool handGone = !body.Has(BodyPartIds.HandL);
        bool fingerGone = !body.Has(BodyPartIds.FingerIndexL);
        effects.Clear();
        body.CollectEffectsUnder(BodyPartIds.ArmL, effects, includeDescendants: true);
        int effectCountAfter = effects.Count;
        bool headRemains = body.Has(BodyPartIds.Head);

        bool ok = hadHand && hadFinger && effectCountBefore > 0 && removed &&
                  handGone && fingerGone && effectCountAfter == 0 && headRemains;

        string msg =
            $"[PlayerStatus Cascade] hadHand={hadHand} hadFinger={hadFinger} effectsBefore={effectCountBefore} " +
            $"removed={removed} handGone={handGone} fingerGone={fingerGone} effectsAfter={effectCountAfter} " +
            $"headRemains={headRemains} => {(ok ? "PASS" : "FAIL")}";

        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);
    }

    [MenuItem("Dist/PlayerStatus/Bake UI Prefab")]
    static void BakePrefab()
    {
        EnsureFolder();
        UIPlayerStatusWindow built = PlayerStatusUIFactory.CreateWindowRoot();
        PrefabUtility.SaveAsPrefabAsset(built.gameObject, WindowPrefabPath, out bool success);
        Object.DestroyImmediate(built.gameObject);
        if (!success)
        {
            Debug.LogError($"[PlayerStatusUISetupMenu] Failed to save {WindowPrefabPath}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlayerStatusUISetupMenu] Saved {WindowPrefabPath}");
    }

    [MenuItem("Dist/PlayerStatus/Setup Canvas In Open Scene")]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PlayerStatusUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        InputManager inputManager = Object.FindAnyObjectByType<InputManager>();
        Transform systemRoot = inputManager != null ? inputManager.transform.parent : null;
        if (systemRoot == null)
        {
            Debug.LogError(
                "[PlayerStatusUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        UIPlayerStatusController controller = Object.FindAnyObjectByType<UIPlayerStatusController>();
        if (controller == null)
        {
            GameObject go = new("PlayerStatusController");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerStatusController");
            go.transform.SetParent(systemRoot, false);
            controller = Undo.AddComponent<UIPlayerStatusController>(go);
        }
        else if (controller.transform.parent != systemRoot)
        {
            Undo.SetTransformParent(
                controller.transform,
                systemRoot,
                "Move PlayerStatusController Under System");
            controller.transform.localPosition = Vector3.zero;
            controller.transform.localRotation = Quaternion.identity;
            controller.transform.localScale = Vector3.one;
        }

        SerializedObject so = new(controller);
        so.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        so.FindProperty("_layerHost").objectReferenceValue = layerHost;

        UIPlayerStatusWindow prefab =
            AssetDatabase.LoadAssetAtPath<UIPlayerStatusWindow>(WindowPrefabPath);
        if (prefab == null)
        {
            BakePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<UIPlayerStatusWindow>(WindowPrefabPath);
        }

        so.FindProperty("_windowPrefab").objectReferenceValue = prefab;
        so.FindProperty("_window").objectReferenceValue = null;

        PlayerStatusWindowLauncher launcher = EnsureHudLauncher(layerHost, controller);
        so.FindProperty("_launcher").objectReferenceValue = launcher;
        so.ApplyModifiedPropertiesWithoutUndo();

        MergeLocalizationKeys();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PlayerStatusUISetupMenu] Controller + HUD launcher wired.", controller);
    }

    [MenuItem("Dist/PlayerStatus/Merge Localization Keys Into UI_ko")]
    static void MergeLocalizationKeysMenu() => MergeLocalizationKeys();

    static PlayerStatusWindowLauncher EnsureHudLauncher(
        UICanvasLayerHost layerHost,
        UIPlayerStatusController controller)
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform existing = hud.Find("Btn_PlayerStatusLauncher");
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject("Btn_PlayerStatusLauncher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create status launcher");
            buttonGo.transform.SetParent(hud, false);
            buttonGo.layer = LayerMask.NameToLayer("UI");

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -52f);
            rect.sizeDelta = new Vector2(40f, 40f);
            buttonGo.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.9f, 0.85f);
        }

        PlayerStatusWindowLauncher launcher = buttonGo.GetComponent<PlayerStatusWindowLauncher>();
        if (launcher == null)
            launcher = Undo.AddComponent<PlayerStatusWindowLauncher>(buttonGo);

        SerializedObject launcherSo = new(launcher);
        launcherSo.FindProperty("_controller").objectReferenceValue = controller;
        launcherSo.FindProperty("_button").objectReferenceValue = buttonGo.GetComponent<Button>();
        launcherSo.FindProperty("_iconImage").objectReferenceValue = buttonGo.GetComponent<Image>();
        launcherSo.ApplyModifiedPropertiesWithoutUndo();
        return launcher;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "PlayerStatus");
    }

    static void MergeLocalizationKeys()
    {
        LocalizationTable table =
            AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            Debug.LogError("[PlayerStatusUISetupMenu] UI_ko table missing. Run Dist/Localization/Select Or Create UI_ko Table.");
            return;
        }

        var map = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        for (int i = 0; i < table.Entries.Count; i++)
        {
            LocalizationTable.Entry e = table.Entries[i];
            if (e != null && !string.IsNullOrEmpty(e.key))
                map[e.key] = e.text ?? string.Empty;
        }

        void Put(string key, string text)
        {
            if (!map.ContainsKey(key))
                map[key] = text;
        }

        Put("PlayerStatus.Title", "상태");
        Put("PlayerStatus.VitalsSection", "생존");
        Put("PlayerStatus.SkillsSection", "스킬");
        Put("PlayerStatus.DetailHeader", "부위 상세");
        Put("PlayerStatus.DetailSubparts", "세부 부위");
        Put("PlayerStatus.DetailEffects", "상태 이상");
        Put("PlayerStatus.NoEffects", "이상 없음");
        Put("PlayerStatus.Lost", "상실");
        Put("PlayerStatus.HpFormat", "{0}/{1}");
        Put("PlayerStatus.VitalFormat", "{0}/{1}");
        Put("PlayerStatus.SkillFormat", "{0}  Lv.{1}");
        Put("PlayerStatus.DebugSeverArmL", "절단(왼팔)");

        Put("PlayerStatus.Part.head", "머리");
        Put("PlayerStatus.Part.torso", "몸통");
        Put("PlayerStatus.Part.arm_l", "왼팔");
        Put("PlayerStatus.Part.arm_r", "오른팔");
        Put("PlayerStatus.Part.leg_l", "왼다리");
        Put("PlayerStatus.Part.leg_r", "오른다리");
        Put("PlayerStatus.Part.eyes", "눈");
        Put("PlayerStatus.Part.mouth", "입");
        Put("PlayerStatus.Part.hand_l", "왼손");
        Put("PlayerStatus.Part.hand_r", "오른손");
        Put("PlayerStatus.Part.foot_l", "왼발");
        Put("PlayerStatus.Part.foot_r", "오른발");
        Put("PlayerStatus.Part.finger_thumb_l", "왼엄지");
        Put("PlayerStatus.Part.finger_index_l", "왼검지");
        Put("PlayerStatus.Part.finger_thumb_r", "오른엄지");
        Put("PlayerStatus.Part.finger_index_r", "오른검지");

        Put("PlayerStatus.Vital.Hunger", "공복");
        Put("PlayerStatus.Vital.Thirst", "갈증");
        Put("PlayerStatus.Vital.Stamina", "스태미나");

        Put("PlayerStatus.Effect.bleed", "출혈");
        Put("PlayerStatus.Effect.fracture", "골절");
        Put("PlayerStatus.Effect.infected", "감염");

        var list = new System.Collections.Generic.List<LocalizationTable.Entry>(map.Count);
        foreach (var kv in map)
            list.Add(new LocalizationTable.Entry { key = kv.Key, text = kv.Value });

        table.EditorSetEntries(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PlayerStatusUISetupMenu] Localization merged ({map.Count} keys).");
    }
}
#endif
