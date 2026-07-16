using UnityEngine;

public static class ScreenRaycaster
{
    public static bool TryGetMouseWorldPosition(Camera cam, float yLevel, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (cam == null)
            return false;

        InputManager input = InputManager.Instance;
        if (input == null || !input.TryReadPointerScreenPosition(out Vector2 screenPos))
            return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Mathf.Abs(ray.direction.y) < 1e-6f)
            return false;

        float t = (yLevel - ray.origin.y) / ray.direction.y;
        if (t < 0f)
            return false;

        worldPos = ray.origin + ray.direction * t;
        return true;
    }
}
