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
        "Assets/Dist/Visual/Anim/CharacterClips/CharacterAnimController.controller";
    const string SlotDir = "Assets/Dist/Visual/Anim/CharacterClips/Slots";
    const string RightMaskPath = "Assets/Dist/Visual/Anim/CharacterClips/RightArm.mask";
    const string LeftMaskPath = "Assets/Dist/Visual/Anim/CharacterClips/LeftArm.mask";
    const string UpperMaskPath = "Assets/Dist/Visual/Anim/CharacterClips/UpperBody.mask";

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
        EnsureParameters(controller);
        RebuildLayers(controller);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArmOverlayAnimatorBuilder] Rebuilt arm + Impact layers.");
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
            new Vector3(420, 0, 0));
        AnimatorState blocked = AddState(
            sm,
            "Blocked",
            FlatClip("ImpactBlocked_Slot"),
            new Vector3(420, 120, 0));
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
        AnimatorState hold = AddState(sm, "Hold", Clip("Hold", ownHand), new Vector3(200, 0, 0));
        AnimatorState aim = AddState(sm, "Aim", Clip("Aim", ownHand), new Vector3(420, 0, 0));
        AnimatorState atk = AddState(sm, "Attack", Clip("Attack", ownHand), new Vector3(700, 0, 0));
        sm.defaultState = hold;

        AddBoolTransition(hold, aim, "IsAiming", true);
        AddBoolTransition(aim, hold, "IsAiming", false);

        AddAttackFrom(hold, atk, attackParam);
        AddAttackFrom(aim, atk, attackParam);
        AddExitAttack(atk, aim, hold);
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 pos)
    {
        AnimatorState state = sm.AddState(name, pos);
        state.motion = clip;
        state.mirror = false;
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
