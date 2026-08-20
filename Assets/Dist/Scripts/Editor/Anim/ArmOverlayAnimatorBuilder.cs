// ============================================================
// ArmOverlayAnimatorBuilder — Hold/Aim/Attack thin 팔 레이어 재구성 (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ArmOverlayAnimatorBuilder
{
    const string ControllerPath =
        "Assets/Dist/Visual/Anim/CharacterAnimator/CharacterAnimController.controller";
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterAnimator/Slots";
    const string RightMaskPath = "Assets/Dist/Visual/Anim/CharacterAnimator/RightArm.mask";
    const string LeftMaskPath = "Assets/Dist/Visual/Anim/CharacterAnimator/LeftArm.mask";
    const string UpperMaskPath = "Assets/Dist/Visual/Anim/CharacterAnimator/UpperBody.mask";

    static readonly string[] Hands = { "Left", "Right", "TwoHand" };

    [MenuItem("Dist/MCP/Rebuild Arm Overlay Animator")]
    public static void Rebuild()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[ArmOverlayAnimatorBuilder] Controller missing.");
            return;
        }

        EnsureThinSlotClips();
        EnsureImpactThinClips();
        EnsureHurtThinClips();
        EnsureParameters(controller);
        RebuildLayers(controller);
        RemoveLibraryKeyLayer(controller);
        if (!AssertControllerHasNoAnimVerb(controller))
        {
            Debug.LogError(
                "[ArmOverlayAnimatorBuilder] FAIL: controller still encodes AnimVerb " +
                "(Swing/Thrust/Trigger/Raise or LibraryKeys). Fix before shipping.");
            return;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArmOverlayAnimatorBuilder] Rebuilt arm + Impact + Hurt (no AnimVerb on controller).");
    }

    static void EnsureThinSlotClips()
    {
        for (int h = 0; h < Hands.Length; h++)
        {
            string hand = Hands[h];
            EnsureClipCopy($"HoldSwing_{hand}_Slot", $"Hold_{hand}_Slot");
            EnsureClipCopy($"AimSwing_{hand}_Slot", $"Aim_{hand}_Slot");
            EnsureClipCopy($"AttackSwing_{hand}_Slot", $"Attack_{hand}_Slot");
        }
    }

    static void EnsureImpactThinClips()
    {
        foreach (ArmImpactKind kindEnum in System.Enum.GetValues(typeof(ArmImpactKind)))
        {
            string kind = kindEnum.ToString();
            string thin = "Impact" + kind + "_Slot";
            EnsureClipCopy("Impact" + kind + "_Right_Slot", thin);
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{thin}.anim") == null)
                EnsureClipCopy("Attack_Right_Slot", thin);
        }
    }

    static void EnsureClipCopy(string sourceName, string destName)
    {
        string destPath = $"{SlotDir}/{destName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath) != null)
            return;

        string sourcePath = $"{SlotDir}/{sourceName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath) == null)
        {
            Debug.LogWarning($"[ArmOverlayAnimatorBuilder] Missing source clip {sourcePath}");
            return;
        }

        if (!AssetDatabase.CopyAsset(sourcePath, destPath))
        {
            Debug.LogError($"[ArmOverlayAnimatorBuilder] Failed to create {destPath}");
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (clip != null)
        {
            clip.name = destName.Replace(".anim", string.Empty);
            EditorUtility.SetDirty(clip);
        }
    }

    static void EnsureParameters(AnimatorController controller)
    {
        RemoveParam(controller, "Action");
        RemoveParam(controller, "WieldHand");
        RemoveParam(controller, "Attack");
        RemoveParam(controller, "MirrorR");
        RemoveParam(controller, "MirrorL");
        RemoveParam(controller, "ActionR");
        RemoveParam(controller, "ActionL");
        RemoveParam(controller, "Action2H");

        EnsureParam(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsAiming", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "AttackR", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, "AttackL", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, "Attack2H", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, "ImpactRecoil", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, "ImpactBlocked", AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, CharacterHitReact.ParamFlinch, AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, CharacterHitReact.ParamStagger, AnimatorControllerParameterType.Trigger);
        EnsureParam(controller, CharacterHitReact.ParamPainShocked, AnimatorControllerParameterType.Bool);
        EnsureFloatParam(controller, WeaponAnimClipSpeeds.ParamRight, WeaponAnimClipSpeeds.DefaultSpeed);
        EnsureFloatParam(controller, WeaponAnimClipSpeeds.ParamLeft, WeaponAnimClipSpeeds.DefaultSpeed);
        EnsureFloatParam(controller, WeaponAnimClipSpeeds.ParamTwoHand, WeaponAnimClipSpeeds.DefaultSpeed);
        EnsureFloatParam(controller, WeaponAnimClipSpeeds.ParamImpact, WeaponAnimClipSpeeds.DefaultSpeed);
    }

    static void RemoveParam(AnimatorController controller, string name)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name != name)
                continue;
            controller.RemoveParameter(i);
            return;
        }
    }

    static void EnsureParam(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name == name)
                return;
        }

        controller.AddParameter(name, type);
    }

    static void EnsureFloatParam(AnimatorController controller, string name, float defaultValue)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name != name)
                continue;
            if (parameters[i].type == AnimatorControllerParameterType.Float)
            {
                parameters[i].defaultFloat = defaultValue;
                controller.parameters = parameters;
            }

            return;
        }

        var parameter = new AnimatorControllerParameter
        {
            name = name,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = defaultValue
        };
        controller.AddParameter(parameter);
    }

    static void RebuildLayers(AnimatorController controller)
    {
        while (controller.layers.Length > 1)
            controller.RemoveLayer(1);

        var rightMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(RightMaskPath);
        var leftMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(LeftMaskPath);
        var upperMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath);

        AddArmLayer(controller, "RightArm Layer", rightMask, "Right", "AttackR");
        AddArmLayer(controller, "LeftArm Layer", leftMask, "Left", "AttackL");
        AddTwoHandLayer(controller, upperMask);
        AddImpactLayer(controller);
        AddHurtLayer(controller);
    }

    /// <summary>
    /// 컨트롤러는 thin만 안다. AnimVerb(LibraryKeys) 레이어가 있으면 제거.
    /// </summary>
    static void RemoveLibraryKeyLayer(AnimatorController controller)
    {
        const string layerName = "LibraryKeys";
        AnimatorControllerLayer[] layers = controller.layers;
        int index = -1;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == layerName)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return;

        var next = new AnimatorControllerLayer[layers.Length - 1];
        for (int i = 0, w = 0; i < layers.Length; i++)
        {
            if (i == index)
                continue;
            next[w++] = layers[i];
        }

        controller.layers = next;
    }

    /// <summary>
    /// 컨트롤러에 동작 이름·LibraryKeys가 남아 있으면 false.
    /// </summary>
    static bool AssertControllerHasNoAnimVerb(AnimatorController controller)
    {
        bool ok = true;
        AnimatorControllerLayer[] layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            string layerName = layers[i].name;
            if (NameEncodesAnimVerb(layerName))
            {
                Debug.LogError(
                    "[ArmOverlayAnimatorBuilder] Forbidden layer name: " + layerName);
                ok = false;
            }

            AnimatorStateMachine sm = layers[i].stateMachine;
            if (sm != null && !AssertStateMachineHasNoAnimVerb(sm, layerName))
                ok = false;
        }

        return ok;
    }

    static bool AssertStateMachineHasNoAnimVerb(AnimatorStateMachine sm, string path)
    {
        bool ok = true;
        ChildAnimatorState[] states = sm.states;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            if (state == null)
                continue;
            if (NameEncodesAnimVerb(state.name))
            {
                Debug.LogError(
                    "[ArmOverlayAnimatorBuilder] Forbidden state: " + path + "/" + state.name);
                ok = false;
            }

            Motion motion = state.motion;
            if (motion != null && NameEncodesAnimVerb(motion.name))
            {
                Debug.LogError(
                    "[ArmOverlayAnimatorBuilder] Forbidden motion on " +
                    path + "/" + state.name + ": " + motion.name);
                ok = false;
            }
        }

        ChildAnimatorStateMachine[] children = sm.stateMachines;
        for (int i = 0; i < children.Length; i++)
        {
            AnimatorStateMachine child = children[i].stateMachine;
            if (child == null)
                continue;
            string childPath = path + "/" + child.name;
            if (NameEncodesAnimVerb(child.name))
            {
                Debug.LogError(
                    "[ArmOverlayAnimatorBuilder] Forbidden state machine: " + childPath);
                ok = false;
            }

            if (!AssertStateMachineHasNoAnimVerb(child, childPath))
                ok = false;
        }

        return ok;
    }

    static bool NameEncodesAnimVerb(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (name == "LibraryKeys" || name.Contains("LibraryKeys"))
            return true;
        // Thin ImpactRecoil / ImpactBlocked 허용. AttackTrigger_ 등 AnimVerb 슬롯 금지.
        if (name.Contains("Swing") ||
            name.Contains("Thrust") ||
            name.Contains("Trigger") ||
            name.Contains("Raise") ||
            name.Contains("Semi") ||
            name.Contains("Burst") ||
            name.Contains("Auto"))
            return true;
        return false;
    }

    static void EnsureHurtThinClips()
    {
        EnsureHurtFromSource(
            CharacterHitReact.ClipFlinch,
            "Assets/Dist/Visual/Anim/SourceRef/ActMotion/Reaction.anim",
            false);
        EnsureHurtFromSource(
            CharacterHitReact.ClipStagger,
            "Assets/Dist/Visual/Anim/SourceRef/ActMotion/Stunned.anim",
            false);
        EnsureHurtFromSource(
            CharacterHitReact.ClipPainDown,
            "Assets/Dist/Visual/Anim/SourceRef/ActMotion/Pistol Kneeling Idle.anim",
            true);
    }

    static void EnsureHurtFromSource(string destName, string sourcePath, bool loop)
    {
        string destPath = $"{SlotDir}/{destName}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (existing != null && !IsHurtPlaceholder(existing))
        {
            if (loop)
                SetClipLoop(existing, true);
            return;
        }

        if (existing != null)
            AssetDatabase.DeleteAsset(destPath);

        if (!AssetDatabase.CopyAsset(sourcePath, destPath))
        {
            Debug.LogWarning($"[ArmOverlayAnimatorBuilder] Hurt source missing: {sourcePath}");
            EnsurePlaceholderClip(destName, loop ? 1f : 0.25f, loop);
            return;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (clip == null)
            return;
        clip.name = destName;
        if (loop)
            SetClipLoop(clip, true);
        EditorUtility.SetDirty(clip);
    }

    static bool IsHurtPlaceholder(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i].propertyName == "HurtPlaceholder")
                return true;
        }

        return bindings.Length == 0 && clip.length <= 1.01f;
    }

    static void SetClipLoop(AnimationClip clip, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static void EnsurePlaceholderClip(string destName, float seconds, bool loop)
    {
        string destPath = $"{SlotDir}/{destName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath) != null)
            return;

        var clip = new AnimationClip
        {
            name = destName,
            frameRate = 60f
        };
        var binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(Animator),
            propertyName = "HurtPlaceholder"
        };
        AnimationUtility.SetEditorCurve(
            clip,
            binding,
            AnimationCurve.Constant(0f, Mathf.Max(0.05f, seconds), 0f));
        if (loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        AssetDatabase.CreateAsset(clip, destPath);
    }

    static void AddHurtLayer(AnimatorController controller)
    {
        controller.AddLayer(CharacterHitReact.HurtLayerName);
        AnimatorControllerLayer[] layers = controller.layers;
        int index = layers.Length - 1;
        AnimatorControllerLayer layer = layers[index];
        layer.defaultWeight = 0f;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.avatarMask = null;
        layer.syncedLayerIndex = -1;
        layer.iKPass = false;
        layer.syncedLayerAffectsTiming = false;
        layers[index] = layer;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[index].stateMachine;
        ClearStateMachine(sm);

        AnimatorState empty = AddState(sm, CharacterHitReact.StateEmpty, null, new Vector3(200, 0, 0));
        AnimatorState flinch = AddState(
            sm,
            CharacterHitReact.StateFlinch,
            FlatClip(CharacterHitReact.ClipFlinch),
            new Vector3(420, 0, 0),
            writeDefaults: false);
        AnimatorState stagger = AddState(
            sm,
            CharacterHitReact.StateStagger,
            FlatClip(CharacterHitReact.ClipStagger),
            new Vector3(420, 120, 0));
        AnimatorState painDown = AddState(
            sm,
            CharacterHitReact.StatePainDown,
            FlatClip(CharacterHitReact.ClipPainDown),
            new Vector3(200, 180, 0));
        sm.defaultState = empty;

        AddHurtTrigger(empty, flinch, CharacterHitReact.ParamFlinch);
        AddHurtTrigger(empty, stagger, CharacterHitReact.ParamStagger);
        AddHurtTrigger(flinch, stagger, CharacterHitReact.ParamStagger);
        AddExitToEmpty(flinch, empty);
        AddExitToEmpty(stagger, empty);
        AddBoolTransition(empty, painDown, CharacterHitReact.ParamPainShocked, true);
        AddBoolTransition(flinch, painDown, CharacterHitReact.ParamPainShocked, true);
        AddBoolTransition(stagger, painDown, CharacterHitReact.ParamPainShocked, true);
        AddBoolTransition(painDown, empty, CharacterHitReact.ParamPainShocked, false);
    }

    static void AddHurtTrigger(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, CharacterHitReact.ParamPainShocked);
    }

    static void AddImpactLayer(AnimatorController controller)
    {
        controller.AddLayer("Impact Layer");
        AnimatorControllerLayer[] layers = controller.layers;
        int index = layers.Length - 1;
        AnimatorControllerLayer layer = layers[index];
        layer.defaultWeight = 0f;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.avatarMask = null;
        layer.syncedLayerIndex = -1;
        layer.iKPass = false;
        layer.syncedLayerAffectsTiming = false;
        layers[index] = layer;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[index].stateMachine;
        ClearStateMachine(sm);

        AnimationClip emptyMotion = null;
        AnimatorState empty = AddState(sm, "Empty", emptyMotion, new Vector3(200, 0, 0));
        AnimatorState recoil = AddState(
            sm,
            "Recoil",
            FlatClip("ImpactRecoil_Slot"),
            new Vector3(420, 0, 0),
            WeaponAnimClipSpeeds.ParamImpact);
        AnimatorState blocked = AddState(
            sm,
            "Blocked",
            FlatClip("ImpactBlocked_Slot"),
            new Vector3(420, 120, 0),
            WeaponAnimClipSpeeds.ParamImpact);
        sm.defaultState = empty;

        AddAttackFrom(empty, recoil, "ImpactRecoil");
        AddAttackFrom(empty, blocked, "ImpactBlocked");
        AddAttackFrom(recoil, blocked, "ImpactBlocked");
        AddAttackFrom(blocked, recoil, "ImpactRecoil");
        AddExitToEmpty(recoil, empty);
        AddExitToEmpty(blocked, empty);
    }

    static void AddExitToEmpty(AnimatorState from, AnimatorState empty)
    {
        var t = from.AddTransition(empty);
        t.hasExitTime = true;
        t.exitTime = 0.85f;
        t.duration = 0.05f;
    }

    static AnimationClip FlatClip(string fileName) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{fileName}.anim");

    static void AddArmLayer(
        AnimatorController controller,
        string layerName,
        AvatarMask mask,
        string ownHand,
        string attackParam)
    {
        controller.AddLayer(layerName);
        AnimatorControllerLayer[] layers = controller.layers;
        int index = layers.Length - 1;
        AnimatorControllerLayer layer = layers[index];
        layer.defaultWeight = 0f;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.avatarMask = mask;
        layer.syncedLayerIndex = -1;
        layer.iKPass = false;
        layer.syncedLayerAffectsTiming = false;
        layers[index] = layer;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[index].stateMachine;
        ClearStateMachine(sm);
        BuildThinStateMachine(sm, ownHand, attackParam);
    }

    static void AddTwoHandLayer(AnimatorController controller, AvatarMask mask)
    {
        controller.AddLayer("TwoHand Layer");
        AnimatorControllerLayer[] layers = controller.layers;
        int index = layers.Length - 1;
        AnimatorControllerLayer layer = layers[index];
        layer.defaultWeight = 0f;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.avatarMask = mask;
        layer.syncedLayerIndex = -1;
        layer.iKPass = false;
        layer.syncedLayerAffectsTiming = false;
        layers[index] = layer;
        controller.layers = layers;

        AnimatorStateMachine sm = controller.layers[index].stateMachine;
        ClearStateMachine(sm);
        BuildThinStateMachine(sm, "TwoHand", "Attack2H");
    }

    static void ClearStateMachine(AnimatorStateMachine sm)
    {
        ChildAnimatorStateMachine[] machines = sm.stateMachines;
        for (int i = machines.Length - 1; i >= 0; i--)
            sm.RemoveStateMachine(machines[i].stateMachine);

        ChildAnimatorState[] states = sm.states;
        for (int i = states.Length - 1; i >= 0; i--)
            sm.RemoveState(states[i].state);
    }

    static AnimationClip Clip(string stem, string hand) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SlotDir}/{stem}_{hand}_Slot.anim");

    static void BuildThinStateMachine(AnimatorStateMachine sm, string ownHand, string attackParam)
    {
        string speedParam = SpeedParamForHand(ownHand);
        AnimatorState hold = AddState(
            sm, "Hold", Clip("Hold", ownHand), new Vector3(200, 0, 0), speedParam);
        AnimatorState aim = AddState(
            sm, "Aim", Clip("Aim", ownHand), new Vector3(420, 0, 0), speedParam);
        AnimatorState atk = AddState(
            sm, "Attack", Clip("Attack", ownHand), new Vector3(700, 0, 0), speedParam);
        sm.defaultState = hold;

        AddBoolTransition(hold, aim, "IsAiming", true);
        AddBoolTransition(aim, hold, "IsAiming", false);

        AddAttackFrom(hold, atk, attackParam);
        AddAttackFrom(aim, atk, attackParam);
        AddAttackFrom(atk, atk, attackParam);
        AddExitAttack(atk, aim, hold);
    }

    static string SpeedParamForHand(string ownHand)
    {
        if (ownHand == "Left")
            return WeaponAnimClipSpeeds.ParamLeft;
        if (ownHand == "TwoHand")
            return WeaponAnimClipSpeeds.ParamTwoHand;
        return WeaponAnimClipSpeeds.ParamRight;
    }

    static AnimatorState AddState(
        AnimatorStateMachine sm,
        string name,
        AnimationClip clip,
        Vector3 pos,
        string speedParam = null,
        bool writeDefaults = true)
    {
        AnimatorState state = sm.AddState(name, pos);
        state.motion = clip;
        state.mirror = false;
        state.writeDefaultValues = writeDefaults;
        if (!string.IsNullOrEmpty(speedParam))
        {
            state.speed = WeaponAnimClipSpeeds.DefaultSpeed;
            state.speedParameter = speedParam;
            state.speedParameterActive = true;
        }

        return state;
    }

    static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void AddAttackFrom(AnimatorState from, AnimatorState to, string attackParam)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = true;
        t.AddCondition(AnimatorConditionMode.If, 0, attackParam);
    }

    static void AddExitAttack(AnimatorState atk, AnimatorState aim, AnimatorState hold)
    {
        var toAim = atk.AddTransition(aim);
        toAim.hasExitTime = true;
        toAim.exitTime = 0.85f;
        toAim.duration = 0.05f;
        toAim.AddCondition(AnimatorConditionMode.If, 0, "IsAiming");

        var toHold = atk.AddTransition(hold);
        toHold.hasExitTime = true;
        toHold.exitTime = 0.85f;
        toHold.duration = 0.05f;
        toHold.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAiming");
    }
}
#endif
