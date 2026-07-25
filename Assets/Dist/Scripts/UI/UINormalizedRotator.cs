// ============================================================
// UINormalizedRotator — 0~1 정규화 값으로 RectTransform Z 회전
// ============================================================

using UnityEngine;

public sealed class UINormalizedRotator : MonoBehaviour
{
    const float DefaultAngleAtZero = 0f;
    const float DefaultAngleAtOne = -360f;

    [SerializeField] RectTransform _target;
    [SerializeField] float _angleAtZero = DefaultAngleAtZero;
    [SerializeField] float _angleAtOne = DefaultAngleAtOne;
    [SerializeField, Range(0f, 1f)] float _normalized;

    public float Normalized => _normalized;
    public float AngleAtZero => _angleAtZero;
    public float AngleAtOne => _angleAtOne;

    void Awake()
    {
        ResolveTarget();
        Apply();
    }

    public void Wire(RectTransform target)
    {
        _target = target;
        Apply();
    }

    public void SetAngleRange(float angleAtZero, float angleAtOne)
    {
        _angleAtZero = angleAtZero;
        _angleAtOne = angleAtOne;
        Apply();
    }

    public void SetNormalized(float value)
    {
        _normalized = Mathf.Clamp01(value);
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _normalized = Mathf.Clamp01(_normalized);
        ResolveTarget();
        Apply();
    }
#endif

    void ResolveTarget()
    {
        if (_target == null)
            _target = transform as RectTransform;
    }

    void Apply()
    {
        RectTransform target = _target != null ? _target : transform as RectTransform;
        if (target == null)
            return;

        float angle = Mathf.Lerp(_angleAtZero, _angleAtOne, _normalized);
        Vector3 euler = target.localEulerAngles;
        euler.z = angle;
        target.localEulerAngles = euler;
    }
}
