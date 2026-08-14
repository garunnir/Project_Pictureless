// ============================================================
// VfxTracerLine — 충격점까지 이동하는 탄 줄 + 도착 시 착탄 VFX
// ============================================================

using Lean.Pool;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class VfxTracerLine : MonoBehaviour
{
    public const float DefaultTravelSpeed = 28f;
    public const float DefaultStreakLength = 0.55f;
    public const float ArrivalHoldSeconds = 0.05f;

    [SerializeField, Min(0.1f)] float _travelSpeed = DefaultTravelSpeed;
    [SerializeField, Min(0.01f)] float _streakLength = DefaultStreakLength;

    LineRenderer _line;
    VfxChannelTicker _ticker;
    Vector3 _start;
    Vector3 _end;
    Vector3 _direction;
    float _distance;
    float _travelled;
    bool _armed;
    bool _arrived;
    GameObject _impactPrefab;
    TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    void Awake()
    {
        EnsureLine();
        TryGetComponent(out _ticker);
    }

    void OnEnable()
    {
        _armed = false;
        _arrived = false;
        _travelled = 0f;
        _impactPrefab = null;
    }

    public void Play(
        Vector3 start,
        Vector3 end,
        GameObject impactPrefab,
        TimeScaleChannel timeChannel)
    {
        EnsureLine();
        _start = start;
        _end = end;
        _impactPrefab = impactPrefab;
        _timeChannel = timeChannel;
        _travelled = 0f;
        _arrived = false;

        Vector3 offset = end - start;
        _distance = offset.magnitude;
        if (_distance <= CharacterAttacker.MinRayDistance)
        {
            _direction = Vector3.forward;
            _armed = true;
            Arrive();
            return;
        }

        _direction = offset / _distance;
        _armed = true;
        ApplyLineAt(0f);

        float travelSeconds = _distance / Mathf.Max(0.1f, _travelSpeed);
        if (_ticker != null)
        {
            _ticker.SetChannel(timeChannel);
            _ticker.EnsureLifetimeAtLeast(travelSeconds + ArrivalHoldSeconds);
        }
    }

    void Update()
    {
        if (!_armed || _arrived)
            return;

        float delta = TimeScaleService.Delta(_timeChannel);
        if (delta <= 0f)
            return;

        _travelled += _travelSpeed * delta;
        if (_travelled >= _distance)
        {
            ApplyLineAt(_distance);
            Arrive();
            return;
        }

        ApplyLineAt(_travelled);
    }

    void Arrive()
    {
        if (_arrived)
            return;
        _arrived = true;
        ApplyLineAt(_distance);

        if (_impactPrefab != null)
        {
            Quaternion rotation = _direction.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(-_direction, Vector3.up)
                : Quaternion.identity;
            GameObject impact = LeanPool.Spawn(_impactPrefab, _end, rotation);
            if (impact != null)
            {
                VfxChannelTicker ticker = impact.GetComponent<VfxChannelTicker>();
                if (ticker != null)
                    ticker.SetChannel(_timeChannel);
            }
        }
    }

    void ApplyLineAt(float headDistance)
    {
        float head = Mathf.Clamp(headDistance, 0f, Mathf.Max(_distance, 0f));
        Vector3 headPos = _start + _direction * head;
        float streak = Mathf.Min(_streakLength, head);
        Vector3 tailPos = headPos - _direction * streak;
        if (head <= CharacterAttacker.MinRayDistance)
            tailPos = _start;

        _line.SetPosition(0, tailPos);
        _line.SetPosition(1, headPos);
    }

    void EnsureLine()
    {
        if (_line == null)
            _line = GetComponent<LineRenderer>();

        _line.useWorldSpace = true;
        _line.positionCount = 2;
    }
}
