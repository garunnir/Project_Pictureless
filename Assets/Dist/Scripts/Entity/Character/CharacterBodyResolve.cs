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
