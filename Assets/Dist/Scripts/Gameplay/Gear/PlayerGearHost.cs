// ============================================================
// PlayerGearHost — 플레이어 Gear 런타임 호스트 + Primary/Strain + Env + BodyTemp + Weather/Vision
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventoryHost))]
[RequireComponent(typeof(InventoryTimedMoveHost))]
public sealed class PlayerGearHost : MonoBehaviour
{
    [SerializeField] PlayerInventoryHost _inventoryHost;
    [SerializeField] CharacterSkillsHost _skillsHost;
    [SerializeField] CharacterAttacker _attacker;
    [SerializeField] PlayerMovement _movement;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] WeatherKind _weatherKind = WeatherKind.Clear;

    CharacterGearService _service;
    readonly WearEnvExposure _envExposure = new();
    readonly BodyTemp _bodyTemp = new();
    readonly WeatherExposure _weather = new();
    bool _bound;
    int _lastWetnessPercent = -1;
    int _lastBodyTempTenths = int.MinValue;
    int _lastVisionPercent = -1;
    bool _hasLastWeatherKind;
    WeatherKind _lastWeatherKind;

    public static PlayerGearHost Active { get; private set; }

    public CharacterGearService Service => _service;
    public EquipmentWearState Wear => _service?.Wear;
    public WieldSlots Wield => _service?.Wield;
    public GearTimedAction Timed => _service?.Timed;
    public WearEnvExposure EnvExposure => _envExposure;
    public BodyTemp BodyTemperature => _bodyTemp;
    public WeatherExposure Weather => _weather;
    public float VisionFactor { get; private set; } = HelmetVision.FullVisionFactor;
    public bool HasHeadVisionPenalty => VisionFactor < HelmetVision.FullVisionFactor;
    public bool HasLiftStrain => _service != null && _service.HasLiftStrain;

    public event Action Changed;

    void Awake()
    {
        EnsureReferences();
        _service = new CharacterGearService();
        ApplyWeatherKind(_weatherKind);
    }

    void OnEnable()
    {
        Active = this;
        EnsureBound();
        ApplyLiftStrainMovement();
        ApplyEnvMovement();
        ApplyVisionToCamera();
        RefreshPrimaryWield();
        ApplyWeatherKind(_weatherKind);
    }

    void OnDisable()
    {
        if (_service != null)
        {
            _service.LiftStrainChanged -= ApplyLiftStrainMovement;
            _service.Changed -= OnServiceChanged;
            _service.Unbind();
            _bound = false;
        }

        if (_movement != null)
        {
            _movement.SetLiftStrainMovement(1f);
            _movement.SetEnvMovement(1f);
        }

        CameraZoomController zoom = CameraZoomController.Active;
        if (zoom != null)
            zoom.SetVisionFactor(HelmetVision.FullVisionFactor);

        if (Active == this)
            Active = null;
    }

    void OnValidate()
    {
        EnsureReferences();
        ApplyWeatherKind(_weatherKind);
    }

    void Reset() => EnsureReferences();

    void Update()
    {
        if (_service == null)
            return;
        float dt = TimeScaleService.Delta(_timeChannel);
        _service.Tick(dt);
        TickEnvBodyTempAndVision(dt);
    }

    /// <summary>Inspector/디버그용 날씨 설정. Resolve 후 다음 tick에 반영.</summary>
    public void SetWeatherKind(WeatherKind kind)
    {
        _weatherKind = kind;
        ApplyWeatherKind(kind);
        Changed?.Invoke();
    }

    void ApplyWeatherKind(WeatherKind kind)
    {
        _weather.SetKind(kind);
    }

    void TickEnvBodyTempAndVision(float dt)
    {
        WearStatsAggregator.WearArmorTotals totals = WearStatsAggregator.Aggregate(_service.Wear);
        _weather.Resolve();
        _envExposure.Tick(dt, totals.TotalEnvironmentalProtection, _weather.AmbientWetnessGainPerSecond);
        _bodyTemp.Tick(dt, totals.TotalWarmth, _envExposure.Wetness01, _weather.AmbientTempC);
        VisionFactor = HelmetVision.ComputeVisionFactor(_service.Wear);
        ApplyEnvMovement();
        ApplyVisionToCamera();

        int wetPercent = _envExposure.WetnessPercent;
        int tempTenths = _bodyTemp.BodyTempTenths;
        int visionPct = HelmetVision.VisionPercent(VisionFactor);
        WeatherKind weatherKind = _weather.Kind;
        if (wetPercent == _lastWetnessPercent
            && tempTenths == _lastBodyTempTenths
            && visionPct == _lastVisionPercent
            && _hasLastWeatherKind
            && weatherKind == _lastWeatherKind)
            return;

        _lastWetnessPercent = wetPercent;
        _lastBodyTempTenths = tempTenths;
        _lastVisionPercent = visionPct;
        _lastWeatherKind = weatherKind;
        _hasLastWeatherKind = true;
        Changed?.Invoke();
    }

    void EnsureReferences()
    {
        if (_inventoryHost == null)
            TryGetComponent(out _inventoryHost);
        if (_skillsHost == null)
            TryGetComponent(out _skillsHost);
        if (_attacker == null)
            TryGetComponent(out _attacker);
        if (_movement == null)
            TryGetComponent(out _movement);
    }

    void EnsureBound()
    {
        if (_bound || _service == null)
            return;

        EnsureReferences();
        _service.Bind(
            Strength,
            Skills,
            BodyContainer,
            FloorContainer,
            RefreshPrimaryWield);
        _service.SetPresentationCatalog(_attacker != null ? _attacker.Catalog : null);
        _service.LiftStrainChanged += ApplyLiftStrainMovement;
        _service.Changed += OnServiceChanged;
        _bound = true;
    }

    int Strength()
    {
        ICharacterSkills skills = Skills();
        return skills != null ? skills.Level(AttributeIds.Str) : 0;
    }

    ICharacterSkills Skills() =>
        _skillsHost != null ? _skillsHost.Skills : GameplayData.CharacterSkills;

    InventoryContainer BodyContainer() =>
        _inventoryHost != null ? _inventoryHost.Container : null;

    InventoryContainer FloorContainer()
    {
        InventorySession session = PlayerInventoryRuntime.Active?.Session;
        if (session == null)
            return null;

        IReadOnlyList<InventoryContainer> sidebar = session.GetSidebarContainers();
        for (int i = 0; i < sidebar.Count; i++)
        {
            InventoryContainer c = sidebar[i];
            if (c != null
                && string.Equals(
                    c.InstanceId,
                    FloorLootHost.DefaultInstanceId,
                    System.StringComparison.Ordinal))
                return c;
        }

        return null;
    }

    void OnServiceChanged()
    {
        if (_service != null)
            VisionFactor = HelmetVision.ComputeVisionFactor(_service.Wear);
        ApplyVisionToCamera();
        Changed?.Invoke();
        PlayerInventoryRuntime.Active?.Session?.NotifySidebarLayoutChanged();
    }

    void ApplyLiftStrainMovement()
    {
        if (_movement == null)
            return;
        float factor = HasLiftStrain ? GearConstants.LiftStrainMoveFactor : 1f;
        _movement.SetLiftStrainMovement(factor);
    }

    void ApplyEnvMovement()
    {
        if (_movement == null)
            return;
        float factor = GearEnvPenalties.MoveSpeedFactor(
            _bodyTemp.Feeling,
            _envExposure.Wetness01);
        _movement.SetEnvMovement(factor);
    }

    void ApplyVisionToCamera()
    {
        CameraZoomController zoom = CameraZoomController.Active;
        if (zoom == null)
            return;
        zoom.SetVisionFactor(VisionFactor);
    }

    public void RefreshPrimaryWield()
    {
        if (_attacker == null || _service == null)
            return;

        if (!PrimaryWieldResolver.TryResolvePrimary(
                _service.Wield,
                _attacker.Catalog,
                Skills(),
                out PrimaryWieldResolver.HandScore primary,
                out _))
        {
            _attacker.SetWieldedItem((ItemStack)null);
            _attacker.SetActiveWieldHand(WieldHand.Right);
            return;
        }

        _attacker.SetWieldedItem(primary.Stack);
        _attacker.SetActiveWieldHand(
            CharacterAttacker.AnimHandFrom(_service.Wield, primary.Slot));
    }
}
