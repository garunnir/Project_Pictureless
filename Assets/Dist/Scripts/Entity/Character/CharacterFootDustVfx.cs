// ============================================================
// CharacterFootDustVfx — 넉백 발끌림 루핑 먼지 + 걸음 거리 버스트
// ============================================================

using Lean.Pool;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterMotor))]
public sealed class CharacterFootDustVfx : MonoBehaviour
{
    [SerializeField] GameObject _dustPrefab;

    [Header("Knockback loop")]
    [SerializeField, Min(0f)] float _knockbackMinSpeed = 0.8f;
    [SerializeField, Min(0.01f)] float _knockbackFullSpeed = 4f;
    [SerializeField, Min(0f)] float _knockbackRateMin = 8f;
    [SerializeField, Min(0f)] float _knockbackRateMax = 36f;

    [Header("Run steps")]
    [SerializeField, Min(0.05f)] float _strideMeters = 0.7f;
    [SerializeField, Min(0f)] float _stepMinSpeed = 1.5f;
    [SerializeField, Min(1)] int _stepBurstCount = 3;

    [Header("Emit")]
    [SerializeField, Min(0f)] float _emitUpBias = 0.35f;

    CharacterMotor _motor;
    CapsuleCollider _capsule;
    Transform _dust;
    ParticleSystem[] _systems;
    VfxChannelTicker _ticker;
    float _strideAccum;
    bool _loggedMissingPrefab;
    bool _spawnPending;

    void Awake()
    {
        _motor = GetComponent<CharacterMotor>();
        _capsule = _motor != null ? _motor.Capsule : GetComponent<CapsuleCollider>();
    }

    void OnEnable()
    {
        if (_dust == null)
            _spawnPending = true;
    }

    void OnDisable()
    {
        DespawnDust();
        _strideAccum = 0f;
        _spawnPending = false;
    }

    void LateUpdate()
    {
        if (_spawnPending)
        {
            _spawnPending = false;
            SpawnDust();
        }
    }

    void FixedUpdate()
    {
        // 할당 없음. Spawn/GetComponents는 OnEnable. Emission 모듈은 구조체 쓰기.
        if (_motor == null || _dust == null || _systems == null)
            return;

        _dust.position = ResolveFeetWorld();

        TimeScaleChannel channel = _motor.IsPossessed
            ? TimeScaleChannel.Player
            : TimeScaleChannel.World;
        float dt = TimeScaleService.FixedDelta(channel);
        if (dt <= 0f)
            return;

        if (_ticker != null)
            _ticker.SetChannel(channel);

        Vector3 kb = _motor.KnockbackVelocity;
        kb.y = 0f;
        float kbSpeed = kb.magnitude;
        if (kbSpeed >= _knockbackMinSpeed)
        {
            _strideAccum = 0f;
            float t = Mathf.InverseLerp(_knockbackMinSpeed, _knockbackFullSpeed, kbSpeed);
            SetRate(Mathf.Lerp(_knockbackRateMin, _knockbackRateMax, t));
            FaceAlong(-kb);
            return;
        }

        SetRate(0f);

        Vector3 applied = _motor.LastAppliedDelta;
        applied.y = 0f;
        float dist = applied.magnitude;
        if (dist < 1e-6f)
            return;

        float speed = dist / dt;
        if (speed < _stepMinSpeed)
        {
            _strideAccum = 0f;
            return;
        }

        FaceAlong(-applied);
        _strideAccum += dist;
        if (_strideAccum < _strideMeters)
            return;

        _strideAccum %= _strideMeters;
        EmitBurst(_stepBurstCount);
    }

    void SpawnDust()
    {
        if (_dust != null)
            return;
        if (_dustPrefab == null)
        {
            if (!_loggedMissingPrefab)
            {
                Debug.LogError($"[CharacterFootDustVfx] '{name}' dust prefab is missing.", this);
                _loggedMissingPrefab = true;
            }
            return;
        }

        Vector3 feet = ResolveFeetWorld();
        GameObject instance = LeanPool.Spawn(_dustPrefab, feet, Quaternion.identity, null);
        if (instance == null)
            return;

        _dust = instance.transform;
        _systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        instance.TryGetComponent(out _ticker);
        SetRate(0f);
    }

    void DespawnDust()
    {
        if (_dust == null)
            return;

        LeanPool.Despawn(_dust.gameObject);
        _dust = null;
        _systems = null;
        _ticker = null;
    }

    void SetRate(float rate)
    {
        if (_systems == null)
            return;
        for (int i = 0; i < _systems.Length; i++)
        {
            ParticleSystem.EmissionModule emission = _systems[i].emission;
            emission.rateOverTime = rate;
        }
    }

    void EmitBurst(int count)
    {
        if (_systems == null || count <= 0)
            return;
        for (int i = 0; i < _systems.Length; i++)
            _systems[i].Emit(count);
    }

    void FaceAlong(Vector3 dirXz)
    {
        dirXz.y = 0f;
        if (dirXz.sqrMagnitude < 1e-6f || _dust == null)
            return;

        Vector3 look = dirXz.normalized + Vector3.up * _emitUpBias;
        if (look.sqrMagnitude < 1e-6f)
            return;
        _dust.rotation = Quaternion.LookRotation(look, Vector3.up);
    }

    Vector3 ResolveFeetLocal()
    {
        if (_capsule == null)
            return Vector3.zero;
        Vector3 local = _capsule.center;
        local.y -= _capsule.height * 0.5f;
        return local;
    }

    Vector3 ResolveFeetWorld() => transform.TransformPoint(ResolveFeetLocal());
}
