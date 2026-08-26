// ============================================================
// MapBloodParticleStampWriter — 논리 바닥 착지 이벤트 → 혈흔 스탬프
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MapParticleFloorLanding))]
[DefaultExecutionOrder(50)]
public sealed class MapBloodParticleStampWriter : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] float _minInterval = MapBloodConsts.ParticleStampMinInterval;
    [SerializeField] int _maxPerBurst = MapBloodConsts.ParticleStampMaxPerBurst;
    [SerializeField] float _stampScale = MapBloodConsts.ParticleStampScale;
    [SerializeField] float _stampAlpha = MapBloodConsts.ParticleStampAlpha;

    MapParticleFloorLanding _landing;
    float _cooldown;
    int _stampedThisBurst;

    void Awake()
    {
        _landing = GetComponent<MapParticleFloorLanding>();
        _landing.Mode = MapParticleLandingMode.NotifyOnly;
    }

    void OnEnable()
    {
        _cooldown = 0f;
        _stampedThisBurst = 0;
        if (_landing == null)
            _landing = GetComponent<MapParticleFloorLanding>();
        if (_landing != null)
            _landing.Landed += OnLanded;
    }

    void OnDisable()
    {
        if (_landing != null)
            _landing.Landed -= OnLanded;
    }

    void Update()
    {
        float dt = TimeScaleService.Delta(_timeChannel);
        if (dt <= 0f)
            return;

        _cooldown -= dt;
    }

    void OnLanded(Vector3 world)
    {
        if (_cooldown > 0f)
            return;

        if (_stampedThisBurst >= _maxPerBurst)
            return;

        MapBloodHost host = MapBloodHost.Runtime;
        if (host == null)
            return;

        host.AddStamp(
            world,
            Random.Range(0f, 360f),
            _stampScale * Random.Range(0.8f, 1.2f),
            _stampAlpha);
        _stampedThisBurst++;
        _cooldown = _minInterval;
    }
}
