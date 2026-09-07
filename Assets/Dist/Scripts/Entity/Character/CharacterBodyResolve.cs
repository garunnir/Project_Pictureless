// ============================================================
// CharacterBodyResolve — 본체 루트·자식 간 GetComponent SSOT
// ============================================================

using UnityEngine;

public static class CharacterBodyResolve
{
    public static GameObject GetBodyRoot(GameObject go)
    {
        if (go == null)
            return null;

        if (go.TryGetComponent(out CharacterBodyRoot _))
            return go;

        CharacterBodyRoot marker = go.GetComponentInParent<CharacterBodyRoot>();
        return marker != null ? marker.gameObject : go;
    }

    public static T GetInBody<T>(Component component) where T : Component
    {
        if (component == null)
            return null;

        if (component.TryGetComponent(out T self))
            return self;

        GameObject root = GetBodyRoot(component.gameObject);
        return root != null ? root.GetComponentInChildren<T>(true) : null;
    }

    public static bool TryGetInBody<T>(Component component, out T result) where T : Component
    {
        result = GetInBody<T>(component);
        return result != null;
    }

    /// <summary>전투·히트스캔 SSOT. Collider는 루트, Host는 자식일 수 있다.</summary>
    public static bool TryResolveBodyHost(Collider collider, out CharacterBodyHost host)
    {
        host = GetInBody<CharacterBodyHost>(collider);
        if (host == null || host.Body == null || host.Body.IsDeadState)
        {
            host = null;
            return false;
        }

        return true;
    }

    public static bool IsSameBodyRoot(Component a, Component b)
    {
        if (a == null || b == null)
            return false;

        GameObject rootA = GetBodyRoot(a.gameObject);
        GameObject rootB = GetBodyRoot(b.gameObject);
        return rootA != null && rootA == rootB;
    }

    /// <summary>콜라이더가 본체 루트 트리(루트·자식)에 속하는지.</summary>
    public static bool IsColliderOnBody(Component bodyMember, Collider collider)
    {
        if (bodyMember == null || collider == null)
            return false;

        GameObject root = GetBodyRoot(bodyMember.gameObject);
        if (root == null)
            return false;

        Transform colliderTransform = collider.transform;
        return colliderTransform == root.transform || colliderTransform.IsChildOf(root.transform);
    }
}

public static class CharacterBodyGameObjectExtensions
{
    public static bool TryGetBodyComponent<T>(this GameObject body, out T component) where T : Component
    {
        component = null;
        if (body == null)
            return false;

        if (body.TryGetComponent(out component))
            return true;

        GameObject root = CharacterBodyResolve.GetBodyRoot(body);
        if (root == null)
            return false;

        component = root.GetComponentInChildren<T>(true);
        return component != null;
    }

    public static T GetBodyComponent<T>(this GameObject body) where T : Component
    {
        body.TryGetBodyComponent(out T component);
        return component;
    }
}
