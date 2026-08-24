// ============================================================
// CharacterVision — 캐릭터 공통 탐지/놓침 시야 반경
// ============================================================

using UnityEngine;

public static class CharacterVisionDefaults
{
    public const float DetectRadius = 10f;
    public const float LoseRadius = 14f;
}

[DisallowMultipleComponent]
public sealed class CharacterVision : MonoBehaviour
{
    [SerializeField, Min(0f)] float _detectRadius = CharacterVisionDefaults.DetectRadius;
    [SerializeField, Min(0f)] float _loseRadius = CharacterVisionDefaults.LoseRadius;

    PlayerGearHost _gearHost;

    public float DetectRadius => _detectRadius;
    public float LoseRadius => _loseRadius;

    public float EffectiveDetectRadius
    {
        get
        {
            float factor = _gearHost != null ? _gearHost.VisionFactor : 1f;
            return _detectRadius * Mathf.Clamp01(factor);
        }
    }

    void Awake()
    {
        if (_gearHost == null)
            TryGetComponent(out _gearHost);
    }
}
