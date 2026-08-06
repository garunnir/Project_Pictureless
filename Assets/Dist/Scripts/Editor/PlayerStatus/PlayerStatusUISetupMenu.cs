// ============================================================
// PlayerStatusUISetupMenu — 상태창 설치·로컬라이즈 병합·검증 메뉴
// ============================================================

#if UNITY_EDITOR
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class PlayerStatusUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus";
    const string WindowPrefabPath = PrefabFolder + "/Grp_PlayerStatusWindow.prefab";
    const string SummaryPrefabPath = PrefabFolder + "/Grp_PlayerStatusSummary.prefab";
    const string MoodSpriteFolder = "Assets/Dist/Visual/Sprites/UI/PlayerStatus/Mood";
    const string MoodCatalogAssetPath =
        "Assets/Dist/SOData/Gameplay/PlayerStatus/PlayerStatusMoodIconCatalog.asset";
    const string SoGameplayFolder = "Assets/Dist/SOData/Gameplay";
    const string SoPlayerStatusFolder = SoGameplayFolder + "/PlayerStatus";

    [MenuItem("Dist/PlayerStatus/Verify Ownership Cascade (Edit Mode)")]
    static void VerifyOwnershipCascade()
    {
        var body = CharacterBody.CreateHumanDefault(8);
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

    [MenuItem("Dist/PlayerStatus/Verify Vital Display (Edit Mode)")]
    static void VerifyVitalDisplay()
    {
        bool nullStatsProse = !PlayerStatusVitalDisplay.CanShowNumericVitals(null);

        var stats = new DefaultPlayerStats();
        bool level0Prose = !PlayerStatusVitalDisplay.CanShowNumericVitals(stats);
        stats.SetSkillLevel(
            SkillIds.Survival,
            PlayerStatusVitalDisplay.NumericVitalMinSkillLevel);
        bool level2Numeric = PlayerStatusVitalDisplay.CanShowNumericVitals(stats);

        string hungerProse = PlayerStatusLabels.FormatVitalProse(VitalKeys.Hunger, 30, 100);
        bool proseNonEmpty = !string.IsNullOrEmpty(hungerProse);

        string numericLine =
            $"{PlayerStatusLabels.GetVitalName(VitalKeys.Hunger)}  " +
            PlayerStatusLabels.FormatVital(82, 100);
        bool numericHasFraction = numericLine.Contains("82") && numericLine.Contains("100");

        bool ok = nullStatsProse && level0Prose && level2Numeric && proseNonEmpty && numericHasFraction;
        string msg =
            $"[PlayerStatus VitalDisplay] nullProse={nullStatsProse} lv0Prose={level0Prose} " +
            $"lv2Numeric={level2Numeric} prose='{hungerProse}' numeric='{numericLine}' " +
            $"=> {(ok ? "PASS" : "FAIL")}";

        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);
    }

    [MenuItem("Dist/Debug/Verify Debug Input Mode (Play Mode)")]
    static void VerifyDebugInputMode()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[DebugInput] Verification requires Play Mode.");
            return;
        }

        InputManager input = InputManager.Instance;
        if (input == null)
        {
            Debug.LogError("[DebugInput] InputManager.Instance is null.");
            return;
        }

        bool closedMoveEnabled = input.IsPlayerActionEnabled(PlayerAction.Move);
        bool closedDebugInactive = !input.IsDebugInputActive;

        using (input.AcquireDebugInput(typeof(PlayerStatusUISetupMenu)))
        {
            bool openMoveBlocked = !input.IsPlayerActionEnabled(PlayerAction.Move);
            bool openDebugActive = input.IsDebugInputActive;
            bool openGameplayBlocked = openMoveBlocked && openDebugActive;

            IngameDebugConsole.DebugLogManager console = IngameDebugConsole.DebugLogManager.Instance;
            if (console != null)
            {
                console.ShowLogWindow();
                bool shownDebugActive = input.IsDebugInputActive;
                console.HideLogWindow();
                // Bridge may keep or release based on window; force-owned scope still holds.
                bool afterHideStillOwned = input.IsDebugInputActive;
                openGameplayBlocked = openGameplayBlocked && shownDebugActive && afterHideStillOwned;
            }

            bool okWhileHeld = closedMoveEnabled && closedDebugInactive && openGameplayBlocked;
            if (!okWhileHeld)
            {
                Debug.LogError(
                    $"[DebugInput] held FAIL closedMove={closedMoveEnabled} closedDebugOff={closedDebugInactive} " +
                    $"openBlocked={openGameplayBlocked}");
                return;
            }
        }

        bool restoredMove = input.IsPlayerActionEnabled(PlayerAction.Move);
        bool restoredDebugOff = !input.IsDebugInputActive;
        bool ok = restoredMove && restoredDebugOff;
        string msg =
            $"[DebugInput] closedMove={closedMoveEnabled} openBlocked=True restoredMove={restoredMove} " +
            $"restoredDebugOff={restoredDebugOff} => {(ok ? "PASS" : "FAIL")}";

        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);
    }

    [MenuItem("Dist/Debug/Verify Player Commands (Play Mode)")]
    static void VerifyPlayerCommands()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[RuntimeDebugConsole] Verification requires Play Mode.");
            return;
        }

        IPlayerStats stats = GameplayData.Stats;
        IPlayerVitals vitals = GameplayData.Vitals;
        bool consoleInstance = IngameDebugConsole.DebugLogManager.Instance != null;
        int originalSkillLevel = stats.GetSkillLevel(SkillIds.Survival);
        int originalHunger = vitals.GetCurrent(VitalKeys.Hunger);
        int statsChangedCount = 0;
        bool proseGate = false;
        bool numericGate = false;
        bool invalidLevelRejected = false;
        bool invalidPracticeRejected = false;
        bool vitalClamp = false;
        bool vitalSet = false;
        bool invalidVitalRejected = false;

        void OnStatsChanged(string _) => statsChangedCount++;
        stats.Changed += OnStatsChanged;

        try
        {
            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerSkillDebugCommands.SetCommand} {SkillIds.Survival} 0");
            proseGate = !PlayerStatusVitalDisplay.CanShowNumericVitals(stats);

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerSkillDebugCommands.SetCommand} {SkillIds.Survival} " +
                PlayerStatusVitalDisplay.NumericVitalMinSkillLevel);
            numericGate = PlayerStatusVitalDisplay.CanShowNumericVitals(stats);

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerSkillDebugCommands.SetCommand} {SkillIds.Survival} -1");
            invalidLevelRejected =
                stats.GetSkillLevel(SkillIds.Survival) ==
                PlayerStatusVitalDisplay.NumericVitalMinSkillLevel;

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerSkillDebugCommands.PracticeCommand} {SkillIds.Survival} 0");
            invalidPracticeRejected =
                stats.GetSkillLevel(SkillIds.Survival) ==
                PlayerStatusVitalDisplay.NumericVitalMinSkillLevel;

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerSkillDebugCommands.PracticeCommand} {SkillIds.Survival} 100");
            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerVitalDebugCommands.SetCommand} HUNGER {int.MaxValue}");
            vitalClamp =
                vitals.GetCurrent(VitalKeys.Hunger) ==
                vitals.GetMax(VitalKeys.Hunger);

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerVitalDebugCommands.SetCommand} Hunger 30");
            vitalSet = vitals.GetCurrent(VitalKeys.Hunger) == 30;

            IngameDebugConsole.DebugLogConsole.ExecuteCommand(
                $"{PlayerVitalDebugCommands.SetCommand} unknown 20");
            invalidVitalRejected = vitals.GetCurrent(VitalKeys.Hunger) == 30;
        }
        finally
        {
            stats.Changed -= OnStatsChanged;
            stats.SetSkillLevel(SkillIds.Survival, originalSkillLevel);
            vitals.SetCurrent(VitalKeys.Hunger, originalHunger);
        }

        bool notificationsRaised = statsChangedCount >= 3;
        bool ok = consoleInstance && proseGate && numericGate &&
                  invalidLevelRejected && invalidPracticeRejected &&
                  vitalClamp && vitalSet && invalidVitalRejected &&
                  notificationsRaised;
        string msg =
            $"[RuntimeDebugConsole] instance={consoleInstance} proseGate={proseGate} numericGate={numericGate} " +
            $"invalidLevel={invalidLevelRejected} invalidPractice={invalidPracticeRejected} " +
            $"vitalClamp={vitalClamp} vitalSet={vitalSet} invalidVital={invalidVitalRejected} " +
            $"statsChanged={statsChangedCount} => {(ok ? "PASS" : "FAIL")}";

        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);
    }

    [MenuItem("Dist/PlayerStatus/Verify Mood Entries (Edit Mode)")]
    static void VerifyMoodEntries()
    {
        var vitals = new DefaultPlayerVitals();
        var cleanBody = CharacterBody.CreateHumanDefault(8, prototypeSeed: false);

        var normal = new System.Collections.Generic.List<MoodEntry>();
        PlayerStatusMoodEntries.Collect(cleanBody, vitals, normal);
        bool emptyWhenNormal = normal.Count == 0;

        vitals.SetCurrent(VitalKeys.Hunger, 30);
        var lowHunger = new System.Collections.Generic.List<MoodEntry>();
        PlayerStatusMoodEntries.Collect(cleanBody, vitals, lowHunger);
        bool hasHungerLow = lowHunger.Exists(e =>
            e.IconId == MoodIconId.Hunger &&
            e.Polarity == MoodPolarity.Negative &&
            Mathf.Approximately(e.Intensity, PlayerStatusMoodVisuals.VitalLowIntensity));

        vitals.SetCurrent(VitalKeys.Hunger, 10);
        var criticalHunger = new System.Collections.Generic.List<MoodEntry>();
        PlayerStatusMoodEntries.Collect(cleanBody, vitals, criticalHunger);
        bool hasHungerCritical = criticalHunger.Exists(e =>
            e.IconId == MoodIconId.Hunger &&
            Mathf.Approximately(e.Intensity, PlayerStatusMoodVisuals.VitalCriticalIntensity));

        var seededBody = CharacterBody.CreateHumanDefault(8);
        var seeded = new System.Collections.Generic.List<MoodEntry>();
        PlayerStatusMoodEntries.Collect(seededBody, vitals, seeded);
        bool hasBleed = seeded.Exists(e => e.IconId == MoodIconId.Bleed);
        bool hasPositiveCatalog = PlayerStatusMoodEffectCatalog.TryGet(
            BodyPartEffectIds.Regenerating,
            out MoodIconId regenIcon,
            out MoodPolarity regenPolarity) &&
            regenIcon == MoodIconId.Regenerating &&
            regenPolarity == MoodPolarity.Positive;

        Color negativeTint = PlayerStatusMoodVisuals.ResolveFillTint(MoodPolarity.Negative);
        Color positiveTint = PlayerStatusMoodVisuals.ResolveFillTint(MoodPolarity.Positive);
        Color neutralTint = PlayerStatusMoodVisuals.ResolveFillTint(MoodPolarity.Neutral);
        bool tintOk = negativeTint == PlayerStatusMoodVisuals.NegativeRed &&
                      positiveTint == PlayerStatusMoodVisuals.PositiveGreen &&
                      neutralTint == PlayerStatusMoodVisuals.NeutralWhite;
        bool fillAmountContractOk =
            Mathf.Approximately(Mathf.Clamp01(PlayerStatusMoodVisuals.VitalLowIntensity), 0.5f) &&
            Mathf.Approximately(Mathf.Clamp01(PlayerStatusMoodVisuals.VitalCriticalIntensity), 1f);

        bool vmPathOk = false;
        var vmVitals = new DefaultPlayerVitals();
        var vmCheck = new System.Collections.Generic.List<MoodEntry>();
        var viewModel = new PlayerStatusViewModel();
        viewModel.Bind(cleanBody, vmVitals, new DefaultPlayerStats());
        PlayerStatusMoodEntries.Collect(cleanBody, vmVitals, vmCheck);
        vmPathOk = vmCheck.Count == 0 && viewModel.MoodEntries.Count == vmCheck.Count;

        vmVitals.SetCurrent(VitalKeys.Hunger, 10);
        PlayerStatusMoodEntries.Collect(cleanBody, vmVitals, vmCheck);
        bool vmHasCriticalHunger = false;
        for (int i = 0; i < viewModel.MoodEntries.Count; i++)
        {
            if (viewModel.MoodEntries[i].IconId == MoodIconId.Hunger)
                vmHasCriticalHunger = true;
        }

        vmPathOk &= vmCheck.Count == viewModel.MoodEntries.Count && vmHasCriticalHunger;
        viewModel.Unbind();

        bool ok = emptyWhenNormal && hasHungerLow && hasHungerCritical && hasBleed &&
                  hasPositiveCatalog && tintOk && fillAmountContractOk && vmPathOk;
        string msg =
            $"[PlayerStatus Mood] emptyNormal={emptyWhenNormal} hungerLow={hasHungerLow} " +
            $"hungerCritical={hasHungerCritical} bleed={hasBleed} positiveCatalog={hasPositiveCatalog} " +
            $"tintOk={tintOk} fillAmountOk={fillAmountContractOk} vmPathOk={vmPathOk} => {(ok ? "PASS" : "FAIL")}";

        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);
    }

    [MenuItem("Dist/PlayerStatus/Ensure Mood Assets")]
    static void EnsureMoodAssetsMenu()
    {
        EnsureMoodAssets();
        Debug.Log("[PlayerStatusUISetupMenu] Ensure Mood Assets complete.");
    }

    /// <summary>
    /// 구 핸들 자식 제거 후 UIWindowResizeHandles 부착 (레이아웃 유지).
    /// </summary>
    [MenuItem("Dist/PlayerStatus/Patch Window Resize Handlers")]
    static void PatchWindowResizeHandlers()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[PlayerStatusUISetupMenu] Failed to load: {WindowPrefabPath}");
            return;
        }

        try
        {
            UICharacterWindow window = root.GetComponent<UICharacterWindow>();
            if (window == null)
            {
                Debug.LogError(
                    "[PlayerStatusUISetupMenu] UICharacterWindow missing; cannot patch.",
                    root);
                return;
            }

            UIWindowResizeHandlesPrefabPatch.Apply(
                root,
                PlayerStatusUIFactory.ResizeEdgeThickness,
                proximityReveal: false,
                new Vector2(PlayerStatusWindowLayout.MinWidth, PlayerStatusWindowLayout.MinHeight),
                PlayerStatusWindowLayout.GetMaxSize(null));

            PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            Debug.Log(
                $"[PlayerStatusUISetupMenu] Applied UIWindowResizeHandles on {WindowPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// TabBar + GearPanel chrome을 프리팹에 추가·배선 (손수 body diagram 유지, full bake 아님).
    /// </summary>
    [MenuItem("Dist/PlayerStatus/Patch Character Tabs And Gear Panel")]
    static void PatchCharacterTabsAndGearPanel()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[PlayerStatusUISetupMenu] Failed to load: {WindowPrefabPath}");
            return;
        }

        try
        {
            UICharacterWindow window = root.GetComponent<UICharacterWindow>();
            if (window == null)
            {
                Debug.LogError(
                    "[PlayerStatusUISetupMenu] UICharacterWindow missing; cannot patch.",
                    root);
                return;
            }

            TMP_FontAsset font = ResolveWindowFont(root);
            Color headerColor = ResolveHeaderColor(root);
            Color panelColor = new Color(0.14f, 0.14f, 0.14f, 0.92f);
            Color tabColor = new Color(
                Mathf.Min(1f, headerColor.r + 0.06f),
                Mathf.Min(1f, headerColor.g + 0.06f),
                Mathf.Min(1f, headerColor.b + 0.06f),
                1f);

            RectTransform statusContent = FindChildRect(root.transform, "Area_Content");
            RectTransform tabBar = EnsureTabBar(root.transform, tabColor, font);
            RectTransform gearRoot = EnsureGearPanelRoot(root.transform, panelColor);
            UICharacterGearPanel gearPanel = EnsureGearPanelTree(gearRoot, font, tabColor);

            SerializedObject so = new(window);
            so.FindProperty("_statusContentRoot").objectReferenceValue = statusContent;
            so.FindProperty("_tabBarRoot").objectReferenceValue = tabBar;
            so.FindProperty("_gearPanelRoot").objectReferenceValue = gearRoot;
            so.FindProperty("_gearPanel").objectReferenceValue = gearPanel;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            Debug.Log(
                $"[PlayerStatusUISetupMenu] Patched TabBar + GearPanel on {WindowPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static TMP_FontAsset ResolveWindowFont(GameObject root)
    {
        Transform title = root.transform.Find("Header/Title");
        if (title != null)
        {
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            if (titleText != null && titleText.font != null)
                return titleText.font;
        }

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            InventoryUIHierarchyBuilder.DefaultUIFontPath);
    }

    static Color ResolveHeaderColor(GameObject root)
    {
        Transform header = root.transform.Find("Header");
        if (header != null)
        {
            Image img = header.GetComponent<Image>();
            if (img != null)
                return img.color;
        }

        return new Color(0.16f, 0.16f, 0.16f, 1f);
    }

    static RectTransform FindChildRect(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t as RectTransform;
    }

    static RectTransform EnsureTabBar(Transform windowRoot, Color tabColor, TMP_FontAsset font)
    {
        Transform existing = windowRoot.Find("TabBar");
        GameObject barGo;
        RectTransform barRt;
        if (existing != null)
        {
            barGo = existing.gameObject;
            barRt = existing as RectTransform;
        }
        else
        {
            barGo = new GameObject("TabBar", typeof(RectTransform));
            barGo.layer = LayerMask.NameToLayer("UI");
            barGo.transform.SetParent(windowRoot, false);
            barRt = barGo.GetComponent<RectTransform>();
            // Place just under Header; do not rebuild body diagram internals.
            barRt.SetSiblingIndex(1);
        }

        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.sizeDelta = new Vector2(-20f, 28f);
        barRt.anchoredPosition = new Vector2(0f, -40f);

        HorizontalLayoutGroup layout = barGo.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = barGo.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlHeight = true;
        layout.spacing = 4f;
        layout.padding = new RectOffset(4, 4, 2, 2);

        EnsureTabButton(barRt, CharacterWindowTab.Status, CharacterGearLabels.TabStatus, tabColor, font);
        EnsureTabButton(barRt, CharacterWindowTab.Equipment, CharacterGearLabels.TabEquipment, tabColor, font);
        EnsureTabButton(barRt, CharacterWindowTab.Encumbrance, CharacterGearLabels.TabEncumbrance, tabColor, font);
        EnsureTabButton(barRt, CharacterWindowTab.BodyTemp, CharacterGearLabels.TabBodyTemp, tabColor, font);
        return barRt;
    }

    static void EnsureTabButton(
        RectTransform tabBar,
        CharacterWindowTab tab,
        string label,
        Color tabColor,
        TMP_FontAsset font)
    {
        string childName = "Tab_" + tab;
        Transform existing = tabBar.Find(childName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(tabBar, false);
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = 24f;

        Image bg = go.GetComponent<Image>();
        if (bg == null)
            bg = go.AddComponent<Image>();
        bg.color = tabColor;
        bg.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.ColorTint;

        Transform labelTf = go.transform.Find("Label");
        GameObject labelGo;
        if (labelTf != null)
        {
            labelGo = labelTf.gameObject;
        }
        else
        {
            labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.layer = LayerMask.NameToLayer("UI");
            labelGo.transform.SetParent(go.transform, false);
        }

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
    }

    static RectTransform EnsureGearPanelRoot(Transform windowRoot, Color panelColor)
    {
        Transform existing = windowRoot.Find("GearPanelRoot");
        GameObject rootGo;
        RectTransform rt;
        if (existing != null)
        {
            rootGo = existing.gameObject;
            rt = existing as RectTransform;
        }
        else
        {
            rootGo = new GameObject(
                "GearPanelRoot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            rootGo.layer = LayerMask.NameToLayer("UI");
            rootGo.transform.SetParent(windowRoot, false);
            rt = rootGo.GetComponent<RectTransform>();
        }

        rt.anchorMin = new Vector2(0.48f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(4f, 8f);
        rt.offsetMax = new Vector2(-8f, -68f);

        Image bg = rootGo.GetComponent<Image>();
        if (bg == null)
            bg = rootGo.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = false;

        // Prefab default: hidden until Equipment/Encumbrance/BodyTemp.
        rootGo.SetActive(false);
        return rt;
    }

    static UICharacterGearPanel EnsureGearPanelTree(
        RectTransform gearRoot,
        TMP_FontAsset font,
        Color chromeColor)
    {
        UICharacterGearPanel panel = gearRoot.GetComponent<UICharacterGearPanel>();
        if (panel == null)
            panel = gearRoot.gameObject.AddComponent<UICharacterGearPanel>();

        VerticalLayoutGroup rootLayout = gearRoot.GetComponent<VerticalLayoutGroup>();
        if (rootLayout == null)
            rootLayout = gearRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(8, 8, 8, 8);
        rootLayout.spacing = 6f;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;

        RectTransform wieldRoot = EnsureNamedVertical(gearRoot, "WieldRoot", preferredHeight: 80f);
        EnsureWieldSlot(wieldRoot, "Wield_L", WieldSlotId.Left, font, chromeColor);
        EnsureWieldSlot(wieldRoot, "Wield_R", WieldSlotId.Right, font, chromeColor);

        RectTransform wornRoot = EnsureNamedVertical(gearRoot, "WornRoot", preferredHeight: 120f);
        TMP_Text filterLabel = EnsureTmpChild(wornRoot, "FilterLabel", CharacterGearLabels.WornFilterAll, 16f, font);
        LayoutElement filterLe = filterLabel.GetComponent<LayoutElement>();
        if (filterLe == null)
            filterLe = filterLabel.gameObject.AddComponent<LayoutElement>();
        filterLe.minHeight = 22f;
        filterLe.preferredHeight = 22f;

        TMP_Text encTotals = EnsureTmpChild(gearRoot, "EncTotals", string.Empty, 14f, font);
        encTotals.gameObject.SetActive(false);

        TMP_Text hover = EnsureTmpChild(gearRoot, "HoverDetail", string.Empty, 14f, font);
        LayoutElement hoverLe = hover.GetComponent<LayoutElement>();
        if (hoverLe == null)
            hoverLe = hover.gameObject.AddComponent<LayoutElement>();
        hoverLe.minHeight = 40f;
        hoverLe.preferredHeight = 48f;

        Slider progress = EnsureProgressSlider(gearRoot, chromeColor);
        progress.gameObject.SetActive(false);

        SerializedObject panelSo = new(panel);
        panelSo.FindProperty("_wieldRoot").objectReferenceValue = wieldRoot;
        panelSo.FindProperty("_wornRoot").objectReferenceValue = wornRoot;
        panelSo.FindProperty("_hoverText").objectReferenceValue = hover;
        panelSo.FindProperty("_progressBar").objectReferenceValue = progress;
        panelSo.FindProperty("_filterLabel").objectReferenceValue = filterLabel;
        panelSo.FindProperty("_encTotalsText").objectReferenceValue = encTotals;
        panelSo.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    static RectTransform EnsureNamedVertical(Transform parent, string name, float preferredHeight)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        RectTransform rt;
        if (existing != null)
        {
            go = existing.gameObject;
            rt = existing as RectTransform;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
        }

        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = go.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 4f;

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.minHeight = preferredHeight * 0.5f;
        le.preferredHeight = preferredHeight;
        le.flexibleHeight = 1f;
        return rt;
    }

    static void EnsureWieldSlot(
        RectTransform wieldRoot,
        string name,
        WieldSlotId slot,
        TMP_FontAsset font,
        Color chromeColor)
    {
        Transform existing = wieldRoot.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(wieldRoot, false);
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.minHeight = 36f;
        le.preferredHeight = 36f;

        Image bg = go.GetComponent<Image>();
        if (bg == null)
            bg = go.AddComponent<Image>();
        bg.color = new Color(chromeColor.r * 0.85f, chromeColor.g * 0.85f, chromeColor.b * 0.85f, 0.95f);
        bg.raycastTarget = true;

        if (go.GetComponent<UICharacterWieldSlotView>() == null)
            go.AddComponent<UICharacterWieldSlotView>();

        // Optional label child for Prefab Mode readability; runtime EnsureLabel can still use root TMP.
        Transform labelTf = go.transform.Find("Label");
        if (labelTf == null)
        {
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.layer = LayerMask.NameToLayer("UI");
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(6f, 2f);
            labelRt.offsetMax = new Vector2(-6f, -2f);
            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = slot == WieldSlotId.Left
                ? CharacterGearLabels.SlotLeft + ": —"
                : CharacterGearLabels.SlotRight + ": —";
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            if (font != null)
                tmp.font = font;
        }
    }

    static TMP_Text EnsureTmpChild(
        Transform parent,
        string name,
        string text,
        float fontSize,
        TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();
        if (!string.IsNullOrEmpty(text))
            tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        return tmp;
    }

    static Slider EnsureProgressSlider(Transform parent, Color chromeColor)
    {
        Transform existing = parent.Find("Progress");
        GameObject barGo;
        if (existing != null)
        {
            barGo = existing.gameObject;
        }
        else
        {
            barGo = new GameObject(
                "Progress",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            barGo.layer = LayerMask.NameToLayer("UI");
            barGo.transform.SetParent(parent, false);
        }

        LayoutElement le = barGo.GetComponent<LayoutElement>();
        if (le == null)
            le = barGo.AddComponent<LayoutElement>();
        le.minHeight = 16f;
        le.preferredHeight = 16f;

        Image rootImage = barGo.GetComponent<Image>();
        if (rootImage == null)
            rootImage = barGo.AddComponent<Image>();
        rootImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        rootImage.raycastTarget = false;

        Transform bgTf = barGo.transform.Find("Background");
        GameObject bgGo;
        if (bgTf != null)
        {
            bgGo = bgTf.gameObject;
        }
        else
        {
            bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.layer = LayerMask.NameToLayer("UI");
            bgGo.transform.SetParent(barGo.transform, false);
        }

        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImage = bgGo.GetComponent<Image>();
        if (bgImage == null)
            bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(chromeColor.r * 0.7f, chromeColor.g * 0.7f, chromeColor.b * 0.7f, 1f);
        bgImage.raycastTarget = false;

        Transform fillAreaTf = barGo.transform.Find("Fill Area");
        GameObject fillAreaGo;
        if (fillAreaTf != null)
        {
            fillAreaGo = fillAreaTf.gameObject;
        }
        else
        {
            fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.layer = LayerMask.NameToLayer("UI");
            fillAreaGo.transform.SetParent(barGo.transform, false);
        }

        RectTransform fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(2f, 2f);
        fillAreaRt.offsetMax = new Vector2(-2f, -2f);

        Transform fillTf = fillAreaGo.transform.Find("Fill");
        GameObject fillGo;
        if (fillTf != null)
        {
            fillGo = fillTf.gameObject;
        }
        else
        {
            fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.layer = LayerMask.NameToLayer("UI");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
        }

        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImage = fillGo.GetComponent<Image>();
        if (fillImage == null)
            fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.35f, 0.7f, 0.4f, 1f);
        fillImage.raycastTarget = false;

        Slider slider = barGo.GetComponent<Slider>();
        if (slider == null)
            slider = barGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fillRt;
        slider.targetGraphic = bgImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
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

        EnsureBridge(canvas);

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError(
                "[PlayerStatusUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform playerStatusRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.PlayerStatus);

        UICharacterController controller = Object.FindAnyObjectByType<UICharacterController>();
        if (controller == null)
        {
            GameObject go = new("PlayerStatusController");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerStatusController");
            go.transform.SetParent(playerStatusRoot, false);
            controller = Undo.AddComponent<UICharacterController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                playerStatusRoot,
                controller.transform,
                "Move PlayerStatusController Under System/PlayerStatus");
        }

        SerializedObject so = new(controller);
        so.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        so.FindProperty("_layerHost").objectReferenceValue = layerHost;

        UICharacterWindow prefab =
            AssetDatabase.LoadAssetAtPath<UICharacterWindow>(WindowPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[PlayerStatusUISetupMenu] Prefab missing: {WindowPrefabPath}. " +
                "Hand-author or restore the prefab — do not full-bake over layout.");
            return;
        }

        so.FindProperty("_windowPrefab").objectReferenceValue = prefab;
        so.FindProperty("_window").objectReferenceValue = null;

        PlayerStatusWindowLauncher launcher = EnsureHudLauncher(layerHost, controller);
        so.FindProperty("_launcher").objectReferenceValue = launcher;

        UIPlayerStatusSummaryController summaryController =
            EnsureSummaryController(playerStatusRoot);
        so.ApplyModifiedPropertiesWithoutUndo();

        UIPlayerStatusSummaryPanel summaryPrefab =
            AssetDatabase.LoadAssetAtPath<UIPlayerStatusSummaryPanel>(SummaryPrefabPath);
        if (summaryPrefab == null)
        {
            Debug.LogError(
                $"[PlayerStatusUISetupMenu] Prefab missing: {SummaryPrefabPath}. " +
                "Hand-author or restore the prefab — do not full-bake over layout.");
            return;
        }

        UIPlayerStatusSummaryPanel summaryPanel = EnsureHudPrefabInstance(
            layerHost,
            summaryPrefab,
            "Grp_PlayerStatusSummary");
        if (summaryPanel == null)
            return;

        // Prefab root starts inactive (empty mood strip); enable for Edit Mode layout.
        if (!summaryPanel.gameObject.activeSelf)
            Undo.RecordObject(summaryPanel.gameObject, "Activate summary HUD");
        summaryPanel.gameObject.SetActive(true);

        SerializedObject summarySo = new(summaryController);
        summarySo.FindProperty("_panel").objectReferenceValue = summaryPanel;
        summarySo.ApplyModifiedPropertiesWithoutUndo();

        MergeLocalizationKeys();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(
            "[PlayerStatusUISetupMenu] Controller + summary HUD scene instance wired.",
            summaryPanel);
    }

    static PlayerStatusUIBridge EnsureBridge(Canvas canvas)
    {
        PlayerStatusUIBridge bridge = canvas.GetComponent<PlayerStatusUIBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<PlayerStatusUIBridge>(canvas.gameObject);
        return bridge;
    }

    static UIPlayerStatusSummaryController EnsureSummaryController(Transform playerStatusRoot)
    {
        UIPlayerStatusSummaryController controller =
            Object.FindAnyObjectByType<UIPlayerStatusSummaryController>();
        if (controller == null)
        {
            GameObject go = new("PlayerStatusSummaryController");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerStatusSummaryController");
            go.transform.SetParent(playerStatusRoot, false);
            controller = Undo.AddComponent<UIPlayerStatusSummaryController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                playerStatusRoot,
                controller.transform,
                "Move Summary Controller Under System/PlayerStatus");
        }

        return controller;
    }

    static T EnsureHudPrefabInstance<T>(
        UICanvasLayerHost layerHost,
        T prefab,
        string instanceName) where T : Component
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform existing = hud.Find(instanceName);
        if (existing != null)
        {
            T panel = existing.GetComponent<T>();
            if (panel != null)
                return panel;

            Debug.LogError(
                $"[PlayerStatusUISetupMenu] '{instanceName}' under HUD lacks {typeof(T).Name}.",
                existing);
            return null;
        }

        T underHud = hud.GetComponentInChildren<T>(true);
        if (underHud != null)
        {
            underHud.gameObject.name = instanceName;
            return underHud;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, hud);
        if (instance == null)
        {
            Debug.LogError(
                $"[PlayerStatusUISetupMenu] PrefabUtility.InstantiatePrefab failed for {instanceName}.",
                prefab);
            return null;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Place {instanceName}");
        instance.name = instanceName;
        Debug.Log(
            $"[PlayerStatusUISetupMenu] Placed HUD instance '{instanceName}' under {hud.name}.",
            instance);
        return instance.GetComponent<T>();
    }

    // Legacy vitals/body-effect icons (MoodIconId 0–7) — keep order stable for serialization.
    static readonly (MoodIconId Id, string FileName)[] LegacyMoodCatalogEntries =
    {
        (MoodIconId.Hunger, "Mood_Hunger.png"),
        (MoodIconId.Thirst, "Mood_Thirst.png"),
        (MoodIconId.Stamina, "Mood_Stamina.png"),
        (MoodIconId.Bleed, "Mood_Bleed.png"),
        (MoodIconId.Fracture, "Mood_Fracture.png"),
        (MoodIconId.Infected, "Mood_Infected.png"),
        (MoodIconId.Regenerating, "Mood_Regenerating.png"),
        (MoodIconId.Adrenaline, "Mood_Adrenaline.png"),
    };

    // Appended MoodIconId values — file Mood_<Name>.png matches enum member name.
    static readonly MoodIconId[] ExtendedMoodIconIds =
    {
        MoodIconId.GoodMood,
        MoodIconId.Happy,
        MoodIconId.VeryHappy,
        MoodIconId.Stable,
        MoodIconId.SlightlyHappy,
        MoodIconId.Neutral,
        MoodIconId.SlightlySad,
        MoodIconId.Sad,
        MoodIconId.VerySad,
        MoodIconId.Depressed,
        MoodIconId.Stressed,
        MoodIconId.SeverelyStressed,
        MoodIconId.Fear,
        MoodIconId.ExtremeFear,
        MoodIconId.Angry,
        MoodIconId.Furious,
        MoodIconId.Tired,
        MoodIconId.VeryTired,
        MoodIconId.NeedRest,
        MoodIconId.WellRested,
        MoodIconId.Hungry,
        MoodIconId.VeryHungry,
        MoodIconId.Fed,
        MoodIconId.Full,
        MoodIconId.Thirsty,
        MoodIconId.VeryThirsty,
        MoodIconId.ThirstQuenched,
        MoodIconId.Discomfort,
        MoodIconId.Pain,
        MoodIconId.SeverePain,
        MoodIconId.Injured,
        MoodIconId.SeverelyInjured,
        MoodIconId.Sick,
        MoodIconId.SeverelySick,
        MoodIconId.LowImmunity,
        MoodIconId.Recovering,
        MoodIconId.Pale,
        MoodIconId.Overheated,
        MoodIconId.Hypothermia,
        MoodIconId.Comfortable,
        MoodIconId.Dirty,
        MoodIconId.VeryDirty,
        MoodIconId.NeedShower,
        MoodIconId.Attractive,
        MoodIconId.Warm,
        MoodIconId.TooHot,
        MoodIconId.TooCold,
        MoodIconId.Dark,
        MoodIconId.Lonely,
        MoodIconId.Bored,
        MoodIconId.Idle,
        MoodIconId.PleasantConversation,
        MoodIconId.GoodMeal,
        MoodIconId.RestArea,
        MoodIconId.SuitableEnvironment,
        MoodIconId.NatureFriendly,
        MoodIconId.Inspired,
        MoodIconId.Motivated,
        MoodIconId.SkillUp,
        MoodIconId.RelationshipImproved,
        MoodIconId.Loved,
        MoodIconId.MarriedEngaged,
        MoodIconId.Trust,
        MoodIconId.Respect,
        MoodIconId.Overencumbered,
    };

    static void EnsureMoodAssets()
    {
        EnsureSoFolder();
        if (!AssetDatabase.IsValidFolder(MoodSpriteFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Sprites/UI/PlayerStatus"))
                AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites/UI", "PlayerStatus");
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites/UI/PlayerStatus", "Mood");
        }

        EnsureCircleSprite(MoodSpriteFolder + "/Mood_Back.png", 64, Color.white, filled: true);
        for (int i = 0; i < LegacyMoodCatalogEntries.Length; i++)
            EnsureCircleSprite(
                MoodSpriteFolder + "/" + LegacyMoodCatalogEntries[i].FileName,
                48,
                Color.white,
                filled: false);

        for (int i = 0; i < ExtendedMoodIconIds.Length; i++)
        {
            string fileName = "Mood_" + ExtendedMoodIconIds[i] + ".png";
            EnsureCircleSprite(MoodSpriteFolder + "/" + fileName, 48, Color.white, filled: false);
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        PlayerStatusMoodIconCatalog catalog =
            AssetDatabase.LoadAssetAtPath<PlayerStatusMoodIconCatalog>(MoodCatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PlayerStatusMoodIconCatalog>();
            AssetDatabase.CreateAsset(catalog, MoodCatalogAssetPath);
        }

        SerializedObject catalogSo = new(catalog);
        catalogSo.FindProperty("_backPlate").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>(MoodSpriteFolder + "/Mood_Back.png");

        SerializedProperty entries = catalogSo.FindProperty("_entries");
        int totalCount = LegacyMoodCatalogEntries.Length + ExtendedMoodIconIds.Length;
        entries.arraySize = totalCount;

        for (int i = 0; i < LegacyMoodCatalogEntries.Length; i++)
        {
            SetCatalogEntry(
                entries,
                i,
                LegacyMoodCatalogEntries[i].Id,
                LegacyMoodCatalogEntries[i].FileName);
        }

        for (int i = 0; i < ExtendedMoodIconIds.Length; i++)
        {
            MoodIconId iconId = ExtendedMoodIconIds[i];
            SetCatalogEntry(
                entries,
                LegacyMoodCatalogEntries.Length + i,
                iconId,
                "Mood_" + iconId + ".png");
        }

        catalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    static void SetCatalogEntry(SerializedProperty entries, int index, MoodIconId iconId, string fileName)
    {
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("IconId").enumValueIndex = (int)iconId;
        entry.FindPropertyRelative("FrontSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>(MoodSpriteFolder + "/" + fileName);
    }

    static void EnsureCircleSprite(string assetPath, int size, Color color, bool filled)
    {
        bool exists = System.IO.File.Exists(assetPath);
        if (exists)
        {
            EnsureSpriteImportSettings(assetPath);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) != null)
                return;
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float outer = size * 0.45f;
        float inner = size * 0.28f;
        float outerSq = outer * outer;
        float innerSq = inner * inner;

        Color32 c = color;
        Color32 clear = new(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSq = dx * dx + dy * dy;
                bool draw = filled ? distSq <= outerSq : distSq <= outerSq && distSq >= innerSq;
                tex.SetPixel(x, y, draw ? c : clear);
            }
        }

        tex.Apply();
        System.IO.File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        EnsureSpriteImportSettings(assetPath);
    }

    static void EnsureSpriteImportSettings(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    static void EnsureSoFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/SOData"))
            AssetDatabase.CreateFolder("Assets/Dist", "SOData");
        if (!AssetDatabase.IsValidFolder(SoGameplayFolder))
            AssetDatabase.CreateFolder("Assets/Dist/SOData", "Gameplay");
        if (!AssetDatabase.IsValidFolder(SoPlayerStatusFolder))
            AssetDatabase.CreateFolder(SoGameplayFolder, "PlayerStatus");
    }

    [MenuItem("Dist/PlayerStatus/Merge Localization Keys Into UI_ko")]
    static void MergeLocalizationKeysMenu() => MergeLocalizationKeys();

    static PlayerStatusWindowLauncher EnsureHudLauncher(
        UICanvasLayerHost layerHost,
        UICharacterController controller)
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
        Put("PlayerStatus.ConditionFormat", "{0}/{1}");
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

        Put("PlayerStatus.Skill.survival", "생존술");

        Put("PlayerStatus.VitalProse.Hunger.Full", "배가 부르다");
        Put("PlayerStatus.VitalProse.Hunger.Ok", "배가 든든하다");
        Put("PlayerStatus.VitalProse.Hunger.Low", "배가 고프다");
        Put("PlayerStatus.VitalProse.Hunger.Critical", "굶주리고 있다");

        Put("PlayerStatus.VitalProse.Thirst.Full", "목이 충분히 축인다");
        Put("PlayerStatus.VitalProse.Thirst.Ok", "목이 마르지 않았다");
        Put("PlayerStatus.VitalProse.Thirst.Low", "목이 마르다");
        Put("PlayerStatus.VitalProse.Thirst.Critical", "목이 타는 것 같다");

        Put("PlayerStatus.VitalProse.Stamina.Full", "몸이 가볍다");
        Put("PlayerStatus.VitalProse.Stamina.Ok", "아직 버틸 만하다");
        Put("PlayerStatus.VitalProse.Stamina.Low", "몸이 무겁다");
        Put("PlayerStatus.VitalProse.Stamina.Critical", "기진맥진하다");

        Put("PlayerStatus.Effect.bleed", "출혈");
        Put("PlayerStatus.Effect.fracture", "골절");
        Put("PlayerStatus.Effect.infected", "감염");
        Put("PlayerStatus.Effect.regenerating", "재생 중");
        Put("PlayerStatus.Effect.adrenaline", "아드레날린");

        Put("PlayerStatus.Mood.Overencumbered", "과적");
        Put("PlayerStatus.Mood.Overencumbered.Light", "짐이 조금 무겁다");
        Put("PlayerStatus.Mood.Overencumbered.Medium", "짐이 무겁다");
        Put("PlayerStatus.Mood.Overencumbered.Heavy", "짐이 너무 무겁다");
        Put("PlayerStatus.Mood.Overencumbered.Extreme", "움직일 수 없을 만큼 무겁다");

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
