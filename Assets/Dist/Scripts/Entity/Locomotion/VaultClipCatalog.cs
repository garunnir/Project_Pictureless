// ============================================================
// VaultClipCatalog — 담/벽 넘기 클립·duration·hybrid progress SSOT
// ============================================================

using IsoTilemap;
using UnityEngine;

[CreateAssetMenu(fileName = "VaultClipCatalog", menuName = "Dist/Locomotion/Vault Clip Catalog")]
public sealed class VaultClipCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Locomotion/VaultClipCatalog.asset";

    [SerializeField] AnimationClip _lowCross;
    [SerializeField] AnimationClip _lowMantle;
    [SerializeField] AnimationClip _highCross;
    [SerializeField] AnimationClip _highMantle;

    [Tooltip("클립 루트 bake (Dist/MCP/Bake Vault Hybrid Progress Curves). 비면 선형 progress.")]
    [SerializeField] AnimationCurve _lowCrossProgress;
    [SerializeField] AnimationCurve _lowMantleProgress;
    [SerializeField] AnimationCurve _highCrossProgress;

    /// <summary>
    /// High Mantle 분리 hybrid의 Y 채널 progress.
    /// Cross·Low Mantle 등 다른 스타일은 <see cref="ResolveProgressCurve"/> arc progress를 사용.
    /// </summary>
    [Tooltip("High Mantle Y 진행률 (분리 hybrid). 비면 선형 progress.")]
    [SerializeField] AnimationCurve _highMantleProgress;

    [Tooltip("High Mantle XZ 진행률. 비면 xzStartT부터 선형 progress.")]
    [SerializeField] AnimationCurve _highMantleXzProgress;

    [Tooltip("High Mantle XZ 선형 폴백 시작 normalized time (XZ 커브 없을 때).")]
    [SerializeField, Range(0f, 1f)] float _highMantleXzStartT = VaultConsts.HighMantleXzStartT;

    [SerializeField, Min(0f)] float _lowCrossDurationSeconds = VaultConsts.LowCrossDurationSeconds;
    [SerializeField, Min(0f)] float _lowMantleDurationSeconds = VaultConsts.LowMantleDurationSeconds;
    [SerializeField, Min(0f)] float _highCrossDurationSeconds = VaultConsts.HighCrossDurationSeconds;
    [SerializeField, Min(0f)] float _highMantleDurationSeconds = VaultConsts.HighMantleDurationSeconds;

    public AnimationClip Resolve(VaultHeightClass height, VaultCrossStyle style)
    {
        if (height == VaultHeightClass.Low)
            return style == VaultCrossStyle.Mantle ? _lowMantle : _lowCross;
        return style == VaultCrossStyle.Mantle ? _highMantle : _highCross;
    }

    public AnimationCurve ResolveProgressCurve(VaultHeightClass height, VaultCrossStyle style)
    {
        if (height == VaultHeightClass.Low)
            return style == VaultCrossStyle.Mantle ? _lowMantleProgress : _lowCrossProgress;
        return style == VaultCrossStyle.Mantle ? _highMantleProgress : _highCrossProgress;
    }

    public AnimationCurve ResolveHighMantleYProgress() => _highMantleProgress;

    public AnimationCurve ResolveHighMantleXzProgress() => _highMantleXzProgress;

    public float ResolveHighMantleXzStartT() => _highMantleXzStartT;

    public float ResolveDuration(VaultHeightClass height, VaultCrossStyle style)
    {
        float configured;
        if (height == VaultHeightClass.Low)
            configured = style == VaultCrossStyle.Mantle
                ? _lowMantleDurationSeconds
                : _lowCrossDurationSeconds;
        else
            configured = style == VaultCrossStyle.Mantle
                ? _highMantleDurationSeconds
                : _highCrossDurationSeconds;

        AnimationClip clip = Resolve(height, style);
        float clipLen = clip != null ? clip.length : 0f;
        return Mathf.Max(configured, clipLen);
    }
}
