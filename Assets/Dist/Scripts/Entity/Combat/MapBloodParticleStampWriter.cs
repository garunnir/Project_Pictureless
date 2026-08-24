// ============================================================
// MapBloodParticleStampWriter — Simulate 파티클 위치 샘플 → 바닥 스탬프
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class MapBloodParticleStampWriter : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] float _nearGroundY = MapBloodConsts.ParticleNearGroundY;
    [SerializeField] float _minInterval = MapBloodConsts.ParticleStampMinInterval;
    [SerializeField] int _maxPerBurst = MapBloodConsts.ParticleStampMaxPerBurst;
    [SerializeField] float _stampScale = MapBloodConsts.ParticleStampScale;
    [SerializeField] float _stampAlpha = MapBloodConsts.ParticleStampAlpha;

    ParticleSystem[] _systems;
    ParticleSystem.Particle[] _buffer;
    float _cooldown;
    int _stampedThisBurst;

    void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
        int max = 64;
        for (int i = 0; i < _systems.Length; i++)
            max = Mathf.Max(max, _systems[i].main.maxParticles);
        _buffer = new ParticleSystem.Particle[max];
    }

    void OnEnable()
    {
        _cooldown = 0f;
        _stampedThisBurst = 0;
    }

    void Update()
    {
        float dt = TimeScaleService.Delta(_timeChannel);
        if (dt <= 0f)
            return;

        _cooldown -= dt;
        if (_cooldown > 0f)
            return;

        MapBloodHost host = MapBloodHost.Runtime;
        if (host == null || _systems == null || _systems.Length == 0)
            return;

        if (_stampedThisBurst >= _maxPerBurst)
            return;

        TileMapCacheHub hub = TileMapCacheHub.Runtime;
        float cellSize = host.CellSize;

        for (int s = 0; s < _systems.Length; s++)
        {
            ParticleSystem ps = _systems[s];
            if (ps == null)
                continue;

            if (_buffer.Length < ps.main.maxParticles)
                _buffer = new ParticleSystem.Particle[ps.main.maxParticles];

            int n = ps.GetParticles(_buffer);
            for (int i = 0; i < n; i++)
            {
                if (_stampedThisBurst >= _maxPerBurst)
                    return;

                Vector3 world = ps.main.simulationSpace == ParticleSystemSimulationSpace.World
                    ? _buffer[i].position
                    : ps.transform.TransformPoint(_buffer[i].position);

                if (_buffer[i].velocity.y > 0.05f)
                    continue;

                float floorY = ResolveFloorY(hub, world, cellSize);
                if (world.y > floorY + _nearGroundY)
                    continue;

                host.AddStamp(
                    world,
                    Random.Range(0f, 360f),
                    _stampScale * Random.Range(0.8f, 1.2f),
                    _stampAlpha);
                _stampedThisBurst++;
                _cooldown = _minInterval;
                if (_cooldown > 0f)
                    return;
            }
        }
    }

    static float ResolveFloorY(TileMapCacheHub hub, Vector3 world, float cellSize)
    {
        if (hub == null)
            return world.y;

        Vector3Int cell = OccupiedCellCoord.ResolveFromWorld(hub, world, cellSize, world.y);
        return cell.y * cellSize;
    }
}
