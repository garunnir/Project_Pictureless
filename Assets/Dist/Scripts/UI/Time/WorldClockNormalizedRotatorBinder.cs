// ============================================================
// WorldClockNormalizedRotatorBinder — DayNormalized → UINormalizedRotator
// ============================================================

using UnityEngine;

public sealed class WorldClockNormalizedRotatorBinder : MonoBehaviour
{
    [SerializeField] UINormalizedRotator _rotator;

    void Awake()
    {
        if (_rotator == null)
            _rotator = GetComponent<UINormalizedRotator>();
    }

    void LateUpdate()
    {
        if (_rotator == null)
            return;

        WorldClock clock = WorldClock.Instance;
        if (clock == null)
            return;

        // Hot path: Instance lookup + float write only (no alloc).
        float normalized = clock.DayNormalized;
        if (Mathf.Approximately(normalized, _rotator.Normalized))
            return;

        _rotator.SetNormalized(normalized);
    }
}
