// ============================================================
// CharacterPresenceResolved — 존재감(가시성·소음) 복합 출력
// ============================================================

using UnityEngine;

public struct CharacterPresenceResolved
{
    /// <summary>타인 시력 판정 반경 배율 (1 = 완전 노출).</summary>
    public float Visibility01;

    /// <summary>이동 소음 강도 (0 = 무음, 1 = 기준 소음).</summary>
    public float Noise01;

    public static CharacterPresenceResolved Identity => new CharacterPresenceResolved
    {
        Visibility01 = 1f,
        Noise01 = 1f,
    };

    public static CharacterPresenceResolved Evaluate(
        in CharacterPresenceContext ctx,
        in CharacterPresenceSettings settings)
    {
        float visibility = ctx.IsStealthActive
            ? settings.StealthVisibilityMultiplier
            : 1f;

        float refSpeed = Mathf.Max(0.01f, ctx.NoiseReferenceSpeed);
        float speedNorm = Mathf.Clamp01(ctx.CurrentSpeed / refSpeed);
        float sprintFactor = ctx.IsSprinting
            ? Mathf.Max(0f, settings.SprintNoiseMultiplier)
            : 1f;
        float stealthNoise = ctx.IsStealthActive
            ? settings.StealthNoiseMultiplier
            : 1f;

        float noise = speedNorm * sprintFactor * stealthNoise;
        noise *= Mathf.Clamp01(ctx.BodyScale01);
        noise *= Mathf.Clamp01(ctx.Transparency01);

        return new CharacterPresenceResolved
        {
            Visibility01 = Mathf.Clamp01(visibility),
            Noise01 = Mathf.Clamp01(noise),
        };
    }
}
