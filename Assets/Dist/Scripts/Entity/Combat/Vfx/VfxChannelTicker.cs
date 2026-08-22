// ============================================================
// VfxChannelTicker — 스폰된 VFX를 시간 채널 delta로 수동 재생·반납
// ============================================================
// ParticleSystem 자동 재생은 Time.timeScale을 따르지만 Dist는 timeScale을
// 게임플레이 SSOT로 쓰지 않는다. 자동 틱을 끄고 채널 delta로만 진행시켜야
// World 정지·불릿타임에서 연출만 계속 도는 일이 없다.

using Lean.Pool;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VfxChannelTicker : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    [Tooltip("켜면 수명이 지나도 반납하지 않는다. 발밑 먼지처럼 루핑 연출용.")]
    [SerializeField] bool _persist = false;

    [Tooltip("이 시간이 지나면 파티클 생존 여부와 무관하게 풀에 반납한다. persist가 꺼져 있을 때만.")]
    [SerializeField, Min(0.05f)] float _maxLifetimeSeconds = 2f;

    ParticleSystem[] _systems;
    float _elapsed;
    float _lifetimeOverride;

    void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < _systems.Length; i++)
        {
            ParticleSystem.MainModule main = _systems[i].main;
            main.playOnAwake = false;
        }
    }

    public void SetChannel(TimeScaleChannel channel) => _timeChannel = channel;

    /// <summary>이동 트레이서처럼 기본 수명보다 길어야 할 때 호출. OnEnable에서 리셋.</summary>
    public void EnsureLifetimeAtLeast(float seconds)
    {
        float need = Mathf.Max(0.05f, seconds);
        if (need > _lifetimeOverride)
            _lifetimeOverride = need;
    }

    float ActiveLifetime =>
        _lifetimeOverride > _maxLifetimeSeconds ? _lifetimeOverride : _maxLifetimeSeconds;

    void OnEnable()
    {
        _elapsed = 0f;
        _lifetimeOverride = 0f;
        if (_systems == null)
            return;

        for (int i = 0; i < _systems.Length; i++)
        {
            _systems[i].Clear(false);
            _systems[i].Simulate(0f, false, true, true);
        }
    }

    void Update()
    {
        float delta = TimeScaleService.Delta(_timeChannel);
        if (delta > 0f)
        {
            _elapsed += delta;
            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                    _systems[i].Simulate(delta, false, false, true);
            }
        }

        if (_persist)
            return;

        if (_elapsed < ActiveLifetime)
            return;

        LeanPool.Despawn(gameObject);
    }
}
