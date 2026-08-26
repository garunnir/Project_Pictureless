// ============================================================
// MapParticleFloorLanding — 파티클 논리 바닥 착지 공용 드라이버
// ============================================================
using System;
using UnityEngine;
using UnityEngine.Events;

namespace IsoTilemap
{
    public enum MapParticleLandingMode
    {
        /// <summary>
        /// Y 스냅 + Manual Sub Emitter 트리거 + remainingLifetime=0.
        /// SetParticles로 kill하면 Death Sub Emitter가 안 뜨므로 Manual을 씁니다.
        /// </summary>
        KillOnLand = 0,
        /// <summary>파티클 유지, OnLanded만 (혈흔 스탬프 등).</summary>
        NotifyOnly = 1,
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(40)]
    public sealed class MapParticleFloorLanding : MonoBehaviour
    {
        [SerializeField] ParticleSystem[] _systems;
        [SerializeField] MapParticleLandingMode _mode = MapParticleLandingMode.KillOnLand;
        [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
        [SerializeField, Min(0f)] float _surfaceYOffset = MapParticleFloorLandingConsts.DefaultSurfaceYOffset;
        [SerializeField, Min(1)] int _maxLandingsPerFrame = MapParticleFloorLandingConsts.DefaultMaxLandingsPerFrame;
        [SerializeField] UnityEvent<Vector3> _onLanded;

        ParticleSystem.Particle[] _buffer;
        readonly MapParticleFloorLandingQueryCache _cache = new();
        TileMapManager _tileMapManager;
        float _cellSize = 1f;
        int _landingsThisFrame;

        public event Action<Vector3> Landed;

        public MapParticleLandingMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public UnityEvent<Vector3> OnLandedEvent => _onLanded;

        void Awake()
        {
            if (_systems == null || _systems.Length == 0)
                _systems = GetComponentsInChildren<ParticleSystem>(true);

            int max = 64;
            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                {
                    if (_systems[i] != null)
                        max = Mathf.Max(max, _systems[i].main.maxParticles);
                }
            }

            _buffer = new ParticleSystem.Particle[max];
        }

        void Update()
        {
            float dt = TimeScaleService.Delta(_timeChannel);
            if (dt <= 0f)
                return;

            if (_systems == null || _systems.Length == 0)
                return;

            TileMapCacheHub hub = TileMapCacheHub.Runtime;
            if (hub == null)
                return;

            EnsureCellSize();
            _cache.Clear();
            _landingsThisFrame = 0;

            for (int s = 0; s < _systems.Length; s++)
            {
                if (_landingsThisFrame >= _maxLandingsPerFrame)
                    return;

                ParticleSystem ps = _systems[s];
                if (ps == null || !ps.isPlaying)
                    continue;

                if (_buffer.Length < ps.main.maxParticles)
                    _buffer = new ParticleSystem.Particle[ps.main.maxParticles];

                int n = ps.GetParticles(_buffer);
                if (n <= 0)
                    continue;

                bool worldSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;
                bool dirty = false;
                int manualSubIndex = _mode == MapParticleLandingMode.KillOnLand
                    ? FindManualSubEmitterIndex(ps)
                    : -1;

                for (int i = 0; i < n; i++)
                {
                    if (_landingsThisFrame >= _maxLandingsPerFrame)
                        break;

                    if (_buffer[i].velocity.y > MapParticleFloorLandingConsts.DownwardVelocityThreshold)
                        continue;

                    Vector3 world = worldSpace
                        ? _buffer[i].position
                        : ps.transform.TransformPoint(_buffer[i].position);

                    float predY = world.y + _buffer[i].velocity.y * dt;
                    Vector3 probeWorld = world;
                    probeWorld.y = world.y
                        + Mathf.Max(0f, -_buffer[i].velocity.y * dt)
                        + MapLogicalFloorCross.Tolerance;

                    if (!MapParticleFloorLandingProbe.TryResolveSurface(
                            hub,
                            probeWorld,
                            _cellSize,
                            out float surfaceY,
                            out _,
                            _cache))
                        continue;

                    if (!MapLogicalFloorCross.StepCrossesOrLands(world.y, predY, surfaceY))
                        continue;

                    Vector3 landWorld = _mode == MapParticleLandingMode.KillOnLand
                        ? new Vector3(world.x, surfaceY + _surfaceYOffset, world.z)
                        : world;

                    RaiseLanded(landWorld);
                    _landingsThisFrame++;

                    if (_mode != MapParticleLandingMode.KillOnLand)
                        continue;

                    if (worldSpace)
                        _buffer[i].position = landWorld;
                    else
                        _buffer[i].position = ps.transform.InverseTransformPoint(landWorld);

                    // SetParticles kill은 Death Sub Emitter를 안 탐 → Manual로 명시 발사.
                    if (manualSubIndex >= 0)
                        ps.TriggerSubEmitter(manualSubIndex, ref _buffer[i]);

                    _buffer[i].remainingLifetime = 0f;
                    dirty = true;
                }

                if (dirty)
                    ps.SetParticles(_buffer, n);
            }
        }

        static int FindManualSubEmitterIndex(ParticleSystem ps)
        {
            ParticleSystem.SubEmittersModule sub = ps.subEmitters;
            if (!sub.enabled)
                return -1;

            for (int i = 0; i < sub.subEmittersCount; i++)
            {
                if (sub.GetSubEmitterType(i) == ParticleSystemSubEmitterType.Manual)
                    return i;
            }

            return -1;
        }

        void RaiseLanded(Vector3 world)
        {
            Landed?.Invoke(world);
            _onLanded?.Invoke(world);
        }

        void EnsureCellSize()
        {
            if (_tileMapManager == null)
                _tileMapManager = FindFirstObjectByType<TileMapManager>();

            IWorldGrid grid = _tileMapManager != null ? _tileMapManager.WorldGrid : null;
            if (grid != null)
                _cellSize = grid.CellSize;
            else
                _cellSize = 1f;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_maxLandingsPerFrame < 1)
                _maxLandingsPerFrame = 1;
        }
#endif
    }
}
