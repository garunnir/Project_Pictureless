// ============================================================
// CameraGroundView — 카메라 지면 가시(현재·최대) AABB / Reach SSOT
// ============================================================

using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 카메라가 지면에서 덮는 범위의 단일 진실원.
/// 히트스캔 사거리·청크 desired·풀 피크 등은 여기만 호출한다.
/// 렌즈 min/max ortho·VisionFactor는 CameraZoomController 담당.
/// </summary>
public static class CameraGroundView
{
    const float FallbackOrtho = 10f;
    const float MinOrtho = 0.01f;

    public static float ResolveOrthographicSize(Camera camera, CinemachineCamera cinemachineCamera)
    {
        if (cinemachineCamera != null)
            return Mathf.Max(MinOrtho, cinemachineCamera.Lens.OrthographicSize);

        if (camera != null && camera.orthographic)
            return Mathf.Max(MinOrtho, camera.orthographicSize);

        return FallbackOrtho;
    }

    public static bool TryGetFootprintBoundsXZ(
        Camera camera,
        CinemachineCamera cinemachineCamera,
        float groundPlaneY,
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        float ortho = ResolveOrthographicSize(camera, cinemachineCamera);
        return TryGetFootprintBoundsXZ(
            camera, ortho, groundPlaneY, out minX, out maxX, out minZ, out maxZ);
    }

    public static bool TryGetFootprintBoundsXZ(
        Camera camera,
        float orthographicSize,
        float groundPlaneY,
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        minX = minZ = float.PositiveInfinity;
        maxX = maxZ = float.NegativeInfinity;
        if (camera == null)
            return false;

        float aspect = Mathf.Max(MinOrtho, camera.aspect);
        float halfW = Mathf.Max(MinOrtho, orthographicSize) * aspect;
        float halfH = Mathf.Max(MinOrtho, orthographicSize);

        Vector3 origin = ResolveViewOriginOnGround(camera, groundPlaneY);
        Vector3 right = FlattenToGround(camera.transform.right);
        Vector3 up = FlattenToGround(camera.transform.up);

        if (right.sqrMagnitude < 1e-8f)
            right = FlattenToGround(camera.transform.forward);
        if (up.sqrMagnitude < 1e-8f)
            up = new Vector3(-right.z, 0f, right.x);

        ExpandBounds(ref minX, ref maxX, ref minZ, ref maxZ, groundPlaneY, origin + right * halfW + up * halfH);
        ExpandBounds(ref minX, ref maxX, ref minZ, ref maxZ, groundPlaneY, origin + right * halfW - up * halfH);
        ExpandBounds(ref minX, ref maxX, ref minZ, ref maxZ, groundPlaneY, origin - right * halfW + up * halfH);
        ExpandBounds(ref minX, ref maxX, ref minZ, ref maxZ, groundPlaneY, origin - right * halfW - up * halfH);

        return minX <= maxX && minZ <= maxZ;
    }

    /// <summary>최대 줌아웃 ortho(VisionFactor 미적용 상한)로 지면 AABB.</summary>
    public static bool TryGetMaxFootprintBoundsXZ(
        Camera camera,
        float maxOrthographicSize,
        float groundPlaneY,
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ) =>
        TryGetFootprintBoundsXZ(
            camera, maxOrthographicSize, groundPlaneY, out minX, out maxX, out minZ, out maxZ);

    public static float ReachFrom(
        Vector3 origin,
        Camera camera,
        CinemachineCamera cinemachineCamera,
        float groundPlaneY = 0f)
    {
        if (!TryGetFootprintBoundsXZ(
                camera, cinemachineCamera, groundPlaneY,
                out float minX, out float maxX, out float minZ, out float maxZ))
            return 0f;
        return ReachFromBounds(origin, minX, maxX, minZ, maxZ);
    }

    public static float MaxReachFrom(
        Vector3 origin,
        Camera camera,
        float maxOrthographicSize,
        float groundPlaneY = 0f)
    {
        if (!TryGetMaxFootprintBoundsXZ(
                camera, maxOrthographicSize, groundPlaneY,
                out float minX, out float maxX, out float minZ, out float maxZ))
            return 0f;
        return ReachFromBounds(origin, minX, maxX, minZ, maxZ);
    }

    public static float ReachFromBounds(
        Vector3 origin,
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        float ox = origin.x;
        float oz = origin.z;
        float d00 = DistanceXZ(ox, oz, minX, minZ);
        float d01 = DistanceXZ(ox, oz, minX, maxZ);
        float d10 = DistanceXZ(ox, oz, maxX, minZ);
        float d11 = DistanceXZ(ox, oz, maxX, maxZ);
        return Mathf.Max(Mathf.Max(d00, d01), Mathf.Max(d10, d11));
    }

    /// <summary>풀 피크 추정용 — ortho half-extent 축 합(보수적).</summary>
    public static float OrthoAxisSpan(float orthographicSize, float aspect)
    {
        float halfW = Mathf.Max(MinOrtho, orthographicSize) * Mathf.Max(1f, aspect);
        float halfH = Mathf.Max(MinOrtho, orthographicSize);
        return halfW + halfH;
    }

    static float DistanceXZ(float ox, float oz, float x, float z)
    {
        float dx = x - ox;
        float dz = z - oz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static void ExpandBounds(
        ref float minX,
        ref float maxX,
        ref float minZ,
        ref float maxZ,
        float groundPlaneY,
        Vector3 point)
    {
        point.y = groundPlaneY;
        minX = Mathf.Min(minX, point.x);
        maxX = Mathf.Max(maxX, point.x);
        minZ = Mathf.Min(minZ, point.z);
        maxZ = Mathf.Max(maxZ, point.z);
    }

    static Vector3 ResolveViewOriginOnGround(Camera camera, float groundPlaneY)
    {
        Vector3 origin = camera.transform.position;
        if (TryIntersectGroundPlane(new Ray(origin, camera.transform.forward), groundPlaneY, out Vector3 hit))
            return hit;

        return new Vector3(origin.x, groundPlaneY, origin.z);
    }

    static Vector3 FlattenToGround(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.zero;
    }

    static bool TryIntersectGroundPlane(Ray ray, float planeY, out Vector3 hit)
    {
        hit = default;
        if (Mathf.Abs(ray.direction.y) < 1e-5f)
            return false;

        float t = (planeY - ray.origin.y) / ray.direction.y;
        if (t < 0f)
            return false;

        hit = ray.origin + ray.direction * t;
        return true;
    }
}
