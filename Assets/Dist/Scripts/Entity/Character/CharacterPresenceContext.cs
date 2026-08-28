// ============================================================
// CharacterPresenceContext — 존재감(가시성·소음) 입력 스냅샷
// ============================================================

public struct CharacterPresenceContext
{
    public bool IsStealthActive;
    public float CurrentSpeed;
    public bool IsSprinting;
    public float NoiseReferenceSpeed;
    /// <summary>v2+: 체형/스케일. v1 stub = 1.</summary>
    public float BodyScale01;
    /// <summary>v2+: 투명도. v1 stub = 1.</summary>
    public float Transparency01;
}
