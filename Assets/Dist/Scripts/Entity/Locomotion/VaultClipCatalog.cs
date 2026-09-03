// ============================================================
// VaultClipCatalog — 담/벽 넘기 클립·duration SSOT
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
