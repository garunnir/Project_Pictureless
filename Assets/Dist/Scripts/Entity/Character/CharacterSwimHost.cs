// ============================================================
// CharacterSwimHost — 발밑 immersion → Swim 모드·이동 배율·Dive 입력
// ============================================================

using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterState))]
[DefaultExecutionOrder(5)]
public sealed class CharacterSwimHost : MonoBehaviour
{
    CharacterState _state;
    CharacterMotor _motor;
    PlayerMovement _movement;
    CharacterBreathHost _breath;
    TileMapManager _tileMapManager;
    float _cellSize = 1f;
    bool _diveHeld;
    bool _inputConnected;
    MapSwimImmersion _last;

    public MapSwimImmersion LastImmersion => _last;
    public bool DiveHeld => _diveHeld;

    void Awake()
    {
        _state = GetComponent<CharacterState>();
        TryGetComponent(out _motor);
        TryGetComponent(out _movement);
        TryGetComponent(out _breath);
        if (_breath == null)
            _breath = gameObject.AddComponent<CharacterBreathHost>();
    }

    void OnEnable()
    {
        if (_motor != null && _motor.IsPossessed)
            ConnectDiveInput();
    }

    void OnDisable() => DisconnectDiveInput();

    void Update()
    {
        float dt = TimeScaleService.Delta(
            _motor != null && _motor.IsPossessed
                ? TimeScaleChannel.Player
                : TimeScaleChannel.World);
        if (dt <= 0f)
            return;

        SyncPossessedInput();
        RefreshImmersion();
        ApplyMovementGate();
        ApplyVerticalInput();
        _breath?.TickSwim(dt, _last);
    }

    public void NotifyPossessedChanged(bool possessed)
    {
        if (possessed)
            ConnectDiveInput();
        else
        {
            DisconnectDiveInput();
            _diveHeld = false;
        }
    }

    void SyncPossessedInput()
    {
        bool possessed = _motor != null && _motor.IsPossessed;
        if (possessed && !_inputConnected)
            ConnectDiveInput();
        else if (!possessed && _inputConnected)
        {
            DisconnectDiveInput();
            _diveHeld = false;
        }
    }

    void RefreshImmersion()
    {
        Vector3 feet = CharacterFeetPose.GetFeetWorld(transform.position, CharacterFeetPose.GetFeetOffset(transform));
        ResolveCellSize();
        bool diveWanted = _diveHeld && (_motor == null || _motor.IsPossessed);
        if (_breath != null && _breath.IsAsphyxiaDowned)
            diveWanted = false;

        _last = MapSwimQuery.Resolve(feet, _cellSize, diveWanted);

        // 익사 다운 중에는 머리 잠김으로 Dive에 머물지 않고 수면으로 올린다.
        if (_breath != null
            && _breath.IsAsphyxiaDowned
            && _last.CanSwim
            && _last.Mode == MapSwimMode.Dive)
        {
            _last = new MapSwimImmersion(
                MapSwimMode.Swim,
                _last.FeetCell,
                _last.Fill01,
                _last.ColumnMl,
                _last.SurfaceFeetY,
                _last.ColumnBottomFeetY,
                _last.CanSwim,
                _last.HeadSubmerged);
        }

        bool wading = _last.Mode == MapSwimMode.Wade;
        bool swimming = _last.Mode == MapSwimMode.Swim;
        bool diving = _last.Mode == MapSwimMode.Dive;
        _state.SetSwimMode(wading, swimming, diving);
    }

    void ApplyMovementGate()
    {
        float factor = 1f;
        bool blockSprint = false;
        float sprintFactor = 1f;

        switch (_last.Mode)
        {
            case MapSwimMode.Wade:
                factor = MapSwimConsts.WadeSpeedFactor;
                sprintFactor = MapSwimConsts.WadeSprintFactor;
                break;
            case MapSwimMode.Swim:
                factor = MapSwimConsts.SwimSpeedFactor;
                blockSprint = true;
                break;
            case MapSwimMode.Dive:
                factor = MapSwimConsts.DiveSpeedFactor;
                blockSprint = true;
                break;
        }

        _motor?.SetSwimMovement(factor);
        if (_movement != null)
        {
            bool active = _last.Mode != MapSwimMode.Dry;
            _movement.SetSwimMovement(active, factor, blockSprint, sprintFactor);
        }
    }

    void ApplyVerticalInput()
    {
        if (_last.Mode != MapSwimMode.Dive)
        {
            _state.SetSwimVerticalInput(0f);
            return;
        }

        // Dive 홀드 = 하강. 릴리즈 후 Swim으로 부상. 상향은 헤드 잠김이 풀릴 때까지 자동.
        float vertical = _diveHeld ? -1f : 0f;
        if (_last.HeadSubmerged && !_diveHeld)
            vertical = 1f;
        _state.SetSwimVerticalInput(vertical);
    }

    void ResolveCellSize()
    {
        if (_tileMapManager == null)
            _tileMapManager = FindFirstObjectByType<TileMapManager>();

        IWorldGrid grid = _tileMapManager != null ? _tileMapManager.WorldGrid : null;
        if (grid != null && grid.CellSize > 0f)
            _cellSize = grid.CellSize;
    }

    void ConnectDiveInput()
    {
        InputManager input = InputManager.Instance;
        if (input == null || _inputConnected)
            return;

        input.PlayerDivePerformed += OnDive;
        input.PlayerDiveCanceled += OnDive;
        _inputConnected = true;
    }

    void DisconnectDiveInput()
    {
        InputManager input = InputManager.Instance;
        if (input == null || !_inputConnected)
            return;

        input.PlayerDivePerformed -= OnDive;
        input.PlayerDiveCanceled -= OnDive;
        _inputConnected = false;
        _diveHeld = false;
    }

    void OnDive(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            _diveHeld = false;
            return;
        }

        if (context.performed || context.started)
            _diveHeld = true;
    }
}
