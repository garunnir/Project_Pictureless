using UnityEngine;

/// <summary>캡슐 콜라이더 기준 발 월드 위치·오프셋. topology 충돌·지지 호출부에서 공통 사용.</summary>
public static class CharacterFeetPose
{
    public static Vector3 GetFeetWorld(Transform transform)
    {
        if (TryGetFeetOffset(transform, out float feetOffset))
            return transform.position - Vector3.up * feetOffset;

        return transform.position;
    }

    public static float GetFeetOffset(Transform transform)
    {
        TryGetFeetOffset(transform, out float feetOffset);
        return feetOffset;
    }

    public static bool TryGetFeetOffset(Transform transform, out float feetOffset)
    {
        feetOffset = 0f;
        if (!transform.TryGetComponent<CapsuleCollider>(out var capsule))
            return false;

        float halfHeight = Mathf.Max(0f, (capsule.height * 0.5f) - capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(capsule.center);
        float feetY = worldCenter.y - halfHeight;
        feetOffset = transform.position.y - feetY;
        return true;
    }
}
