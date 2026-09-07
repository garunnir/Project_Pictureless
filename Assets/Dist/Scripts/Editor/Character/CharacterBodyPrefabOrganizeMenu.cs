// ============================================================
// CharacterBodyPrefabOrganizeMenu — NpcSample 본체 자식 GO 역할 분리 Patch
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class CharacterBodyPrefabOrganizeMenu
{
    public const string NpcSamplePrefabPath = "Assets/Dist/Visual/Prefabs/3D/NpcSample.prefab";

    static readonly Type[] RootKeepTypes =
    {
        typeof(Transform),
        typeof(Rigidbody),
        typeof(CapsuleCollider),
        typeof(CharacterBodyRoot),
        typeof(CharacterState),
        typeof(CharacterMotor),
        typeof(CharacterDefinitionBinder),
    };

    static readonly Type[] GameplayCoreTypes =
    {
        typeof(CharacterBodyHost),
        typeof(CharacterSkillsHost),
        typeof(CharacterTraitsHost),
        typeof(CharacterFootprintHost),
        typeof(CharacterSessionHub),
        typeof(CharacterActionHost),
        typeof(PlayerInventoryHost),
        typeof(PlayerGearHost),
        typeof(PlayerEncumbranceHost),
        typeof(InventoryTimedMoveHost),
        typeof(NearbyContainerDetector),
        typeof(CharacterAimIntent),
        typeof(CharacterAttacker),
        typeof(BodyEffectTicker),
        typeof(CharacterPainHost),
        typeof(CharacterHitReact),
        typeof(CharacterHitStop),
        typeof(CharacterImbalanceHost),
        typeof(PlayerNeedsHost),
        typeof(CharacterMoodHost),
        typeof(CharacterClimateHost),
    };

    static readonly Type[] SensesTypes =
    {
        typeof(CharacterPresenceHost),
        typeof(CharacterFactionHost),
        typeof(CharacterVision),
        typeof(CharacterHearing),
        typeof(CharacterSenseGizmo),
    };

    static readonly Type[] PresentationTypes =
    {
        typeof(CharacterCombatVfx),
        typeof(CharacterEmoteHost),
        typeof(CharacterMoodEmoteSource),
        typeof(CharacterCombatEmoteBridge),
        typeof(CharacterSightFadeHost),
        typeof(CharacterAppearanceHost),
    };

    [MenuItem(DistMcpMenus.CharacterOrganizeNpcSampleBody)]
    public static void OrganizeNpcSamplePrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(NpcSamplePrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[CharacterBodyPrefabOrganizeMenu] Prefab not found: {NpcSamplePrefabPath}");
            return;
        }

        try
        {
            Undo.RegisterFullObjectHierarchyUndo(prefabRoot, "Organize NpcSample Body");

            Transform gameplayCore = EnsureChild(prefabRoot.transform, "GameplayCore");
            Transform senses = EnsureChild(prefabRoot.transform, "Senses");
            Transform presentation = EnsureChild(prefabRoot.transform, "Presentation");

            if (prefabRoot.GetComponent<CharacterBodyRoot>() == null)
                prefabRoot.AddComponent<CharacterBodyRoot>();

            DeduplicateChildComponents(gameplayCore.gameObject, GameplayCoreTypes);
            DeduplicateChildComponents(senses.gameObject, SensesTypes);
            DeduplicateChildComponents(presentation.gameObject, PresentationTypes);

            MoveTypes(prefabRoot, presentation.gameObject, PresentationTypes);
            MoveTypes(prefabRoot, senses.gameObject, SensesTypes);
            MoveTypes(prefabRoot, gameplayCore.gameObject, GameplayCoreTypes);
            StripForeignComponents(presentation.gameObject, PresentationTypes);
            StripForeignComponents(senses.gameObject, SensesTypes);
            StripForeignComponents(gameplayCore.gameObject, GameplayCoreTypes);
            StripMovedComponentsFromRoot(prefabRoot);

            RemoveLegacyMissingScripts(prefabRoot);
            RemoveMissingScripts(prefabRoot);

            if (!ValidateColliderBodyHostResolution(prefabRoot, out string resolveError))
            {
                Debug.LogError(
                    "[CharacterBodyPrefabOrganizeMenu] Collider → CharacterBodyHost validation failed: "
                    + resolveError,
                    prefabRoot);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, NpcSamplePrefabPath);
            Debug.Log(
                "[CharacterBodyPrefabOrganizeMenu] NpcSample organized: root physics + GameplayCore / Senses / Presentation.",
                prefabRoot);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject child = new(name);
        Undo.RegisterCreatedObjectUndo(child, "Create body child");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        child.layer = parent.gameObject.layer;
        return child.transform;
    }

    static void DeduplicateChildComponents(GameObject child, Type[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            if (type == typeof(Transform))
                continue;

            Component[] matches = child.GetComponents(type);
            for (int j = 1; j < matches.Length; j++)
                Undo.DestroyObjectImmediate(matches[j]);
        }
    }

    static void MoveTypes(GameObject fromRoot, GameObject to, Type[] copyOrder)
    {
        for (int i = 0; i < copyOrder.Length; i++)
        {
            Type type = copyOrder[i];
            if (type == typeof(Transform))
                continue;

            Component source = fromRoot.GetComponent(type);
            if (source == null || source.gameObject == to)
                continue;

            if (to.GetComponent(type) != null)
                continue;

            if (!ComponentUtility.CopyComponent(source))
            {
                Debug.LogWarning($"[CharacterBodyPrefabOrganizeMenu] Copy failed: {type.Name}", fromRoot);
                continue;
            }

            if (!ComponentUtility.PasteComponentAsNew(to))
                Debug.LogWarning($"[CharacterBodyPrefabOrganizeMenu] Paste failed: {type.Name}", to);
        }
    }

    static void StripForeignComponents(GameObject child, Type[] allowedTypes)
    {
        HashSet<Type> allowed = new(allowedTypes.Length + 1) { typeof(Transform) };
        for (int i = 0; i < allowedTypes.Length; i++)
            allowed.Add(allowedTypes[i]);

        Type pinnedAllowedType = null;
        MonoBehaviour[] behaviours = child.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !allowed.Contains(behaviour.GetType()))
                continue;

            if (!PinsForeignOnObject(behaviour.GetType(), child, allowed))
                continue;

            pinnedAllowedType = behaviour.GetType();
            if (!ComponentUtility.CopyComponent(behaviour))
            {
                Debug.LogWarning(
                    $"[CharacterBodyPrefabOrganizeMenu] Could not copy pinned allowed component: {pinnedAllowedType.Name}",
                    child);
                pinnedAllowedType = null;
                break;
            }

            Undo.DestroyObjectImmediate(behaviour);
            break;
        }

        List<MonoBehaviour> foreign = new();
        behaviours = child.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || allowed.Contains(behaviour.GetType()))
                continue;

            foreign.Add(behaviour);
        }

        foreign.Sort((a, b) => CompareDestroyOrder(a.GetType(), b.GetType()));
        for (int i = 0; i < foreign.Count; i++)
        {
            MonoBehaviour behaviour = foreign[i];
            if (behaviour != null)
                Undo.DestroyObjectImmediate(behaviour);
        }

        if (pinnedAllowedType != null && child.GetComponent(pinnedAllowedType) == null)
        {
            if (!ComponentUtility.PasteComponentAsNew(child))
            {
                Debug.LogWarning(
                    $"[CharacterBodyPrefabOrganizeMenu] Could not restore pinned allowed component: {pinnedAllowedType.Name}",
                    child);
            }
        }

        StripForeignComponentsWithoutPin(child, allowed);
    }

    static void StripForeignComponentsWithoutPin(GameObject child, HashSet<Type> allowed)
    {
        for (int pass = 0; pass < 4; pass++)
        {
            List<MonoBehaviour> foreign = new();
            MonoBehaviour[] behaviours = child.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || allowed.Contains(behaviour.GetType()))
                    continue;

                foreign.Add(behaviour);
            }

            if (foreign.Count == 0)
                break;

            foreign.Sort((a, b) => CompareDestroyOrder(a.GetType(), b.GetType()));
            for (int i = 0; i < foreign.Count; i++)
            {
                MonoBehaviour behaviour = foreign[i];
                if (behaviour != null)
                    Undo.DestroyObjectImmediate(behaviour);
            }
        }
    }

    static bool PinsForeignOnObject(Type allowedType, GameObject go, HashSet<Type> allowed)
    {
        MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || allowed.Contains(behaviour.GetType()))
                continue;

            if (TypeRequires(allowedType, behaviour.GetType()))
                return true;
        }

        return false;
    }

    static int CompareDestroyOrder(Type dependent, Type dependency)
    {
        if (TypeRequires(dependent, dependency))
            return -1;

        if (TypeRequires(dependency, dependent))
            return 1;

        return 0;
    }

    static void StripMovedComponentsFromRoot(GameObject fromRoot)
    {
        List<MonoBehaviour> foreign = new();
        MonoBehaviour[] behaviours = fromRoot.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || ShouldKeepOnRoot(behaviour.GetType()))
                continue;

            foreign.Add(behaviour);
        }

        foreign.Sort((a, b) => CompareDestroyOrder(a.GetType(), b.GetType()));
        for (int i = 0; i < foreign.Count; i++)
        {
            MonoBehaviour behaviour = foreign[i];
            if (behaviour != null)
                Undo.DestroyObjectImmediate(behaviour);
        }
    }

    static bool TypeRequires(Type behaviourType, Type requiredType)
    {
        object[] attributes = behaviourType.GetCustomAttributes(typeof(RequireComponent), true);
        for (int j = 0; j < attributes.Length; j++)
        {
            RequireComponent require = (RequireComponent)attributes[j];
            if (require.m_Type0 == requiredType ||
                require.m_Type1 == requiredType ||
                require.m_Type2 == requiredType)
                return true;
        }

        return false;
    }

    static bool ShouldKeepOnRoot(Type type)
    {
        if (type == typeof(CharacterBodyRoot) ||
            type == typeof(CharacterState) ||
            type == typeof(CharacterMotor) ||
            type == typeof(CharacterDefinitionBinder) ||
            type == typeof(CharacterLocomotionAnim) ||
            type == typeof(CharacterFootDustVfx))
            return true;

        for (int i = 0; i < RootKeepTypes.Length; i++)
        {
            if (RootKeepTypes[i] == type)
                return true;
        }

        return false;
    }

    static void RemoveMissingScripts(GameObject root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
    }

    static void RemoveLegacyMissingScripts(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = behaviours.Length - 1; i >= 0; i--)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour.GetType().Name == "CharacterActionCancelConsumer")
                Undo.DestroyObjectImmediate(behaviour);
        }
    }

    static bool ValidateColliderBodyHostResolution(GameObject prefabRoot, out string error)
    {
        error = null;
        if (prefabRoot == null)
        {
            error = "prefab root is null";
            return false;
        }

        if (prefabRoot.GetComponentInChildren<CharacterBodyHost>(true) == null)
        {
            error = "no CharacterBodyHost under prefab root";
            return false;
        }

        Collider[] colliders = prefabRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.isTrigger)
                continue;

            if (CharacterBodyResolve.TryResolveBodyHost(col, out _))
                continue;

            error = $"Collider on '{GetHierarchyPath(col.transform)}' does not resolve to CharacterBodyHost.";
            return false;
        }

        return true;
    }

    static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
#endif
