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

    [Tooltip("이 시간이 지나면 파티클 생존 여부와 무관하게 풀에 반납한다.")]
    [SerializeField, Min(0.05f)] float _maxLifetimeSeconds = 2f;

    ParticleSystem[] _systems;
    float _elapsed;

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

    void OnEnable()
    {
        _elapsed = 0f;
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

        if (_elapsed < _maxLifetimeSeconds)
            return;

        LeanPool.Despawn(gameObject);
    }
}
