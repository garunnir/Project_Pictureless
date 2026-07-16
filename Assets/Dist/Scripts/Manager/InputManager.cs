// ============================================================
// InputManager — 입력 정책 SSOT
// ============================================================
// [A] UiMenu 입력  : AcquireUiMenuInput — 건설 등. Player 맵 OFF, UI 맵 ON.
// [B] PlayerAction : SuppressPlayerAction — 인벤 창 위 등. Zoom/Aim만 끔, Move는 유지.
// 소비: Player* / Ui* 이벤트 또는 TryRead* — Actions 직접 접근 금지.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : SceneSingleton<InputManager>
{
    public static Func<bool> click;

    InputActions _actions;

    public bool IsUiMenuInputActive => _uiMenuInputOwners.Count > 0;

    readonly HashSet<object> _uiMenuInputOwners = new();
    readonly Dictionary<PlayerAction, HashSet<object>> _suppressedActions = new();
    bool _uiMenuInputApplied;

    public event Action<InputAction.CallbackContext> PlayerMovePerformed;
    public event Action<InputAction.CallbackContext> PlayerMoveCanceled;
    public event Action<InputAction.CallbackContext> PlayerRunPerformed;
    public event Action<InputAction.CallbackContext> PlayerRunCanceled;
    public event Action<InputAction.CallbackContext> PlayerLookAtStarted;
    public event Action<InputAction.CallbackContext> PlayerLookAtPerformed;
    public event Action<InputAction.CallbackContext> PlayerLookAtCanceled;
    public event Action<InputAction.CallbackContext> PlayerInteractPerformed;
    public event Action<InputAction.CallbackContext> PlayerInventoryTogglePerformed;

    public event Action<InputAction.CallbackContext> UiNavigateStarted;
    public event Action<InputAction.CallbackContext> UiNavigateCanceled;
    public event Action<InputAction.CallbackContext> UiSubmitPerformed;
    public event Action<InputAction.CallbackContext> UiPaginationPerformed;

    protected override void Awake()
    {
        base.Awake();
        click = IsClike;
        _actions = new InputActions();
        WireActionCallbacks();
        _actions.Player.Enable();
    }

    public IDisposable AcquireUiMenuInput(object owner)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));

        _uiMenuInputOwners.Add(owner);
        ApplyActionMaps();
        return new UiMenuInputScope(this, owner);
    }

    public void SuppressPlayerAction(PlayerAction action, object owner, bool suppress)
    {
        if (owner == null)
            return;

        if (!_suppressedActions.TryGetValue(action, out HashSet<object> owners))
        {
            owners = new HashSet<object>();
            _suppressedActions[action] = owners;
        }

        bool changed = suppress ? owners.Add(owner) : owners.Remove(owner);
        if (!changed)
            return;
    }

    public bool IsPlayerActionEnabled(PlayerAction action)
    {
        if (IsUiMenuInputActive)
            return false;

        return !_suppressedActions.TryGetValue(action, out HashSet<object> owners) || owners.Count == 0;
    }

    public bool TryReadZoomScroll(out float scrollY)
    {
        scrollY = 0f;
        if (!IsPlayerActionEnabled(PlayerAction.Zoom))
            return false;

        scrollY = _actions.Player.Zoom.ReadValue<Vector2>().y;
        return true;
    }

    public bool TryReadMove(out Vector2 move)
    {
        move = Vector2.zero;
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return false;

        move = _actions.Player.Move.ReadValue<Vector2>();
        return true;
    }

    public bool TryReadRunHeld(out bool isHeld)
    {
        isHeld = false;
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return false;

        isHeld = _actions.Player.Run.IsPressed();
        return true;
    }

    public bool TryReadPointerScreenPosition(out Vector2 position)
    {
        position = Vector2.zero;

        if (Pointer.current != null)
        {
            position = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    /// <summary>포인터 primary press가 이 프레임에 눌렸는지.</summary>
    public bool TryReadPointerPressedThisFrame(out bool pressed)
    {
        pressed = Pointer.current?.press.wasPressedThisFrame ?? false;
        return Pointer.current != null;
    }

    /// <summary>포인터 primary press가 이 프레임에 떨어졌는지.</summary>
    public bool TryReadPointerReleasedThisFrame(out bool released)
    {
        released = Pointer.current?.press.wasReleasedThisFrame ?? false;
        return Pointer.current != null;
    }

    /// <summary>Cancel(Escape / UI Cancel)이 이 프레임에 수행됐는지.</summary>
    public bool TryReadCancelPerformedThisFrame(out bool canceled)
    {
        canceled = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            canceled = true;

        if (!canceled && IsUiMenuInputActive && _actions != null &&
            _actions.UI.Cancel.WasPerformedThisFrame())
            canceled = true;

        return true;
    }

    void WireActionCallbacks()
    {
        _actions.Player.Move.performed += ForwardPlayerMovePerformed;
        _actions.Player.Move.canceled += ForwardPlayerMoveCanceled;
        _actions.Player.Run.performed += ForwardPlayerRunPerformed;
        _actions.Player.Run.canceled += ForwardPlayerRunCanceled;
        _actions.Player.LookAt.started += ForwardPlayerLookAtStarted;
        _actions.Player.LookAt.performed += ForwardPlayerLookAtPerformed;
        _actions.Player.LookAt.canceled += ForwardPlayerLookAtCanceled;
        _actions.Player.Interaction.performed += ForwardPlayerInteractPerformed;
        _actions.Player.InventoryToggle.performed += ForwardPlayerInventoryTogglePerformed;

        _actions.UI.Navigate.started += ForwardUiNavigateStarted;
        _actions.UI.Navigate.canceled += ForwardUiNavigateCanceled;
        _actions.UI.Submit.performed += ForwardUiSubmitPerformed;
        _actions.UI.Pagination.performed += ForwardUiPaginationPerformed;
    }

    void UnwireActionCallbacks()
    {
        if (_actions == null)
            return;

        _actions.Player.Move.performed -= ForwardPlayerMovePerformed;
        _actions.Player.Move.canceled -= ForwardPlayerMoveCanceled;
        _actions.Player.Run.performed -= ForwardPlayerRunPerformed;
        _actions.Player.Run.canceled -= ForwardPlayerRunCanceled;
        _actions.Player.LookAt.started -= ForwardPlayerLookAtStarted;
        _actions.Player.LookAt.performed -= ForwardPlayerLookAtPerformed;
        _actions.Player.LookAt.canceled -= ForwardPlayerLookAtCanceled;
        _actions.Player.Interaction.performed -= ForwardPlayerInteractPerformed;
        _actions.Player.InventoryToggle.performed -= ForwardPlayerInventoryTogglePerformed;

        _actions.UI.Navigate.started -= ForwardUiNavigateStarted;
        _actions.UI.Navigate.canceled -= ForwardUiNavigateCanceled;
        _actions.UI.Submit.performed -= ForwardUiSubmitPerformed;
        _actions.UI.Pagination.performed -= ForwardUiPaginationPerformed;
    }

    void ForwardPlayerMovePerformed(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return;

        PlayerMovePerformed?.Invoke(ctx);
    }

    void ForwardPlayerMoveCanceled(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return;

        PlayerMoveCanceled?.Invoke(ctx);
    }

    void ForwardPlayerRunPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return;

        PlayerRunPerformed?.Invoke(ctx);
    }

    void ForwardPlayerRunCanceled(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Move))
            return;

        PlayerRunCanceled?.Invoke(ctx);
    }

    void ForwardPlayerLookAtStarted(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Aim))
            return;

        PlayerLookAtStarted?.Invoke(ctx);
    }

    void ForwardPlayerLookAtPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Aim))
            return;

        PlayerLookAtPerformed?.Invoke(ctx);
    }

    void ForwardPlayerLookAtCanceled(InputAction.CallbackContext ctx)
    {
        PlayerLookAtCanceled?.Invoke(ctx);
    }

    void ForwardPlayerInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsPlayerActionEnabled(PlayerAction.Interact))
            return;

        PlayerInteractPerformed?.Invoke(ctx);
    }

    void ForwardPlayerInventoryTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (IsUiMenuInputActive)
            return;

        PlayerInventoryTogglePerformed?.Invoke(ctx);
    }

    void ForwardUiNavigateStarted(InputAction.CallbackContext ctx)
    {
        if (!IsUiMenuInputActive)
            return;

        UiNavigateStarted?.Invoke(ctx);
    }

    void ForwardUiNavigateCanceled(InputAction.CallbackContext ctx)
    {
        if (!IsUiMenuInputActive)
            return;

        UiNavigateCanceled?.Invoke(ctx);
    }

    void ForwardUiSubmitPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUiMenuInputActive)
            return;

        UiSubmitPerformed?.Invoke(ctx);
    }

    void ForwardUiPaginationPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsUiMenuInputActive)
            return;

        UiPaginationPerformed?.Invoke(ctx);
    }

    void ReleaseUiMenuInput(object owner)
    {
        if (owner == null || !_uiMenuInputOwners.Remove(owner))
            return;

        ApplyActionMaps();
    }

    void ApplyActionMaps()
    {
        bool uiMenu = IsUiMenuInputActive;
        if (uiMenu == _uiMenuInputApplied)
            return;

        _uiMenuInputApplied = uiMenu;
        if (uiMenu)
        {
            _actions.Player.Disable();
            _actions.UI.Enable();
        }
        else
        {
            _actions.UI.Disable();
            _actions.Player.Enable();
        }
    }

    protected override void OnDestroy()
    {
        UnwireActionCallbacks();
        _actions?.Dispose();
        _actions = null;
        base.OnDestroy();
    }

    bool IsClike() => Pointer.current?.press.wasPressedThisFrame ?? false;

    public static RaycastHit RayCast() //todo 공통사용가능한 부위로 옮겨야함.
    {
        var screenPos = Pointer.current?.position.ReadValue() ?? Vector2.zero;
        var ray = Camera.main.ScreenPointToRay(screenPos);
        Physics.Raycast(ray, out RaycastHit info);
        if (info.collider != null) Debug.Log(info.collider.name);
        return info;
    }

    sealed class UiMenuInputScope : IDisposable
    {
        readonly InputManager _manager;
        readonly object _owner;
        bool _disposed;

        public UiMenuInputScope(InputManager manager, object owner)
        {
            _manager = manager;
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _manager?.ReleaseUiMenuInput(_owner);
        }
    }
}
