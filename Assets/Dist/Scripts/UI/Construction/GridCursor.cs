using System;
using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridCursor : MonoBehaviour
{
    [SerializeField] TileMapController _controller;
    [SerializeField] TileMapManager _tileMapManager;
    [SerializeField] TilePlacementState _placementState;
    [SerializeField] GameObject _cursorVisual;
    [SerializeField] Camera _camera;

    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    private Vector3Int _cursorGridPos;

    private Vector2 _heldDir;
    private float _holdTimer;
    private float _repeatTimer;

    const float HOLD_THRESHOLD = 1f;
    const float REPEAT_INTERVAL = 0.15f;

    void Start()
    {
        if (_camera == null) _camera = Camera.main;

        InputManager input = InputManager.Instance;
        input.UiNavigateStarted += OnNavigateStarted;
        input.UiNavigateCanceled += OnNavigateCanceled;
        input.UiSubmitPerformed += OnSubmit;
    }

    void Update()
    {
        UpdateFromPointer();
        UpdateHoldRepeat();

        InputManager input = InputManager.Instance;
        if (input != null &&
            input.TryReadPointerPressedThisFrame(out bool pressed) &&
            pressed)
            TryPlace();
    }

    // 포인터(마우스)가 이 프레임에 움직였을 때만 커서 위치를 절대 좌표로 갱신한다.
    // 움직이지 않으면 키보드 Navigate 입력이 우선된다.
    void UpdateFromPointer()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        if (!input.TryReadPointerDelta(out Vector2 delta) || delta == Vector2.zero)
            return;

        if (!input.TryReadPointerScreenPosition(out Vector2 screenPos))
            return;

        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (!GroundPlane.Raycast(ray, out float dist)) return;

        float cellSize = ResolveCellSize();
        Vector3Int newGrid = TileHelper.ConvertWorldToGrid(ray.GetPoint(dist), cellSize);
        if (newGrid == _cursorGridPos) return;

        _cursorGridPos = newGrid;
        UpdateVisual();
    }

    void UpdateHoldRepeat()
    {
        if (_heldDir == Vector2.zero) return;

        float dt = TimeScaleService.Delta(TimeScaleChannel.Realtime);
        _holdTimer += dt;
        if (_holdTimer < HOLD_THRESHOLD) return;

        _repeatTimer += dt;
        if (_repeatTimer >= REPEAT_INTERVAL)
        {
            MoveCursor(_heldDir);
            _repeatTimer = 0f;
        }
    }

    void OnNavigateStarted(InputAction.CallbackContext ctx)
    {
        Vector2 dir = ctx.ReadValue<Vector2>();
        _heldDir = dir;
        _holdTimer = 0f;
        _repeatTimer = 0f;
        MoveCursor(dir);
    }

    void OnNavigateCanceled(InputAction.CallbackContext ctx)
    {
        _heldDir = Vector2.zero;
        _holdTimer = 0f;
        _repeatTimer = 0f;
    }

    void MoveCursor(Vector2 dir)
    {
        // 아이소메트릭 기준: 입력 x → grid x, 입력 y → grid z
        _cursorGridPos += new Vector3Int(
            Mathf.RoundToInt(dir.x),
            0,
            Mathf.RoundToInt(dir.y)
        );
        UpdateVisual();
    }

    void OnSubmit(InputAction.CallbackContext ctx) => TryPlace();

    void TryPlace()
    {
        if (_placementState.Selected == null) return;

        var def = _placementState.Selected;
        var slot = TileIdentityUtil.ResolvePlacementSlot(def, def.prefabId);
        var sizeUnit = new Vector3Int(
            Mathf.Max(1, def.size.x),
            Mathf.Max(1, def.size.y),
            Mathf.Max(1, def.size.z));
        Vector3Int gridPos = slot == TilePlacementSlot.HorizontalFace
            ? _cursorGridPos + Vector3Int.down
            : _cursorGridPos;

        var identity = new TileIdentity
        {
            PrefabId = def.prefabId,
            GridPos = gridPos,
            sizeUnit = sizeUnit,
            placementSlot = (byte)slot,
            wallFace = slot == TilePlacementSlot.VerticalFace ? (byte)0 : (byte)0,
            floorFace = slot == TilePlacementSlot.HorizontalFace
                ? (byte)FloorFace.PosY
                : (byte)0,
            collisionFlags = TileCollisionProfile.FromDefinitionForSlot(slot, def),
        };

        var tileData = new TileData
        {
            tileDefId = Guid.NewGuid(),
            state = new TileState(),
            identity = identity,
        };
        _controller.AddAndFlush(tileData);
    }

    void UpdateVisual()
    {
        UpdateVisual(TileHelper.ConvertGridToWorldPos(_cursorGridPos, ResolveCellSize()));
    }
    void UpdateVisual(Vector3 worldPos)
    {
        if (_cursorVisual == null) return;
        _cursorVisual.transform.position = worldPos;
    }

    public void SetActive(bool active)
    {
        enabled = active;
        if (_cursorVisual != null)
            _cursorVisual.SetActive(active);
    }

    float ResolveCellSize() =>
        _tileMapManager?.WorldGrid != null ? _tileMapManager.WorldGrid.CellSize : 1f;

    void OnDestroy()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.UiNavigateStarted -= OnNavigateStarted;
        input.UiNavigateCanceled -= OnNavigateCanceled;
        input.UiSubmitPerformed -= OnSubmit;
    }
}
