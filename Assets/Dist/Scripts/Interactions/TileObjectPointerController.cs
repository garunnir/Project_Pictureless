// ============================================================
// TileObjectPointerController — 타일 오브젝트 호버 하이라이트 + RMB 클릭 메뉴
// ============================================================

using System;
using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TileObjectPointerController : MonoBehaviour
{
    const int PhysicsHitBufferSize = 16;

    [SerializeField] Camera _refCam;
    [SerializeField] LayerMask _hitMask = ~0;
    [SerializeField] float _maxRayDistance = 200f;

    readonly List<RaycastResult> _uiRaycastResults = new();
    readonly RaycastHit[] _physicsHits = new RaycastHit[PhysicsHitBufferSize];

    TileObjectInteractionTarget _hovered;
    bool _hoverSelectionOwned;
    bool _connected;
    bool _inputEnabled = true;

    void OnEnable()
    {
        if (_inputEnabled)
            Connect();
    }

    void Start()
    {
        if (_inputEnabled)
            Connect();
    }

    void OnDisable()
    {
        Disconnect();
        ClearHover();
    }

    public void SetEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (enabled)
            Connect();
        else
        {
            Disconnect();
            ClearHover();
            ContextMenuHostEvents.RequestHide();
        }
    }

    void Connect()
    {
        InputManager input = InputManager.Instance;
        if (input == null || _connected)
            return;

        input.PlayerLookAtTapPerformed += OnLookAtTapPerformed;
        _connected = true;
    }

    void Disconnect()
    {
        InputManager input = InputManager.Instance;
        if (input != null && _connected)
            input.PlayerLookAtTapPerformed -= OnLookAtTapPerformed;

        _connected = false;
    }

    void LateUpdate()
    {
        if (!_inputEnabled)
            return;

        InputManager input = InputManager.Instance;
        if (input == null || !input.TryReadPointerScreenPosition(out Vector2 screenPos))
        {
            ClearHover();
            return;
        }

        if (IsPointerBlockedByUiAt(screenPos))
        {
            ClearHover();
            return;
        }

        if (!TryRaycastTarget(screenPos, out TileObjectInteractionTarget target))
        {
            ClearHover();
            return;
        }

        if (target == _hovered)
            return;

        ClearHover();
        _hovered = target;
        ApplyHoverSelection(true);
    }

    void OnLookAtTapPerformed(InputAction.CallbackContext context)
    {
        InputManager input = InputManager.Instance;
        if (input == null || !input.TryReadPointerScreenPosition(out Vector2 screenPos))
            return;

        ProcessClick(screenPos);
    }

    void ProcessClick(Vector2 screenPosition)
    {
        if (IsPointerBlockedByUiAt(screenPosition))
            return;

        TileObjectInteractionTarget target = _hovered;
        if (target == null && !TryRaycastTarget(screenPosition, out target))
            return;

        ContextMenuModel model = target.BuildContextMenuModel();
        if (model.IsEmpty)
            return;

        if (!UIContextMenuHost.TryShow(model, screenPosition))
        {
            Debug.LogError(
                "[TileObjectPointerController] UIContextMenuHost failed to show.",
                this);
        }
    }

    bool TryRaycastTarget(Vector2 screenPos, out TileObjectInteractionTarget target)
    {
        target = null;
        Camera cam = _refCam != null ? _refCam : Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            _physicsHits,
            _maxRayDistance,
            _hitMask,
            QueryTriggerInteraction.Collide);

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _physicsHits[i];
            if (hit.collider == null)
                continue;

            TileObjectInteractionTarget candidate =
                hit.collider.GetComponentInParent<TileObjectInteractionTarget>();
            if (candidate == null)
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            target = candidate;
        }

        return target != null;
    }

    void ApplyHoverSelection(bool selected)
    {
        if (_hovered == null)
            return;

        if (selected)
        {
            if (ShouldSkipHoverClearForLoot(_hovered))
            {
                _hoverSelectionOwned = false;
                return;
            }

            _hovered.SetHoverSelected(true);
            _hoverSelectionOwned = true;
            return;
        }

        if (!_hoverSelectionOwned)
            return;

        if (ShouldSkipHoverClearForLoot(_hovered))
        {
            _hoverSelectionOwned = false;
            return;
        }

        _hovered.SetHoverSelected(false);
        _hoverSelectionOwned = false;
    }

    void ClearHover()
    {
        if (_hovered == null)
            return;

        ApplyHoverSelection(false);
        _hovered = null;
        _hoverSelectionOwned = false;
    }

    static bool ShouldSkipHoverClearForLoot(TileObjectInteractionTarget target)
    {
        if (target == null)
            return false;

        Guid tileId = target.ResolvePresentationTileIdForLootGuard();
        if (tileId == Guid.Empty)
            return false;

        ITileLootHighlightSink sink = TilePresentationSystem.Instance;
        return sink != null && sink.IsLootHighlightActive(tileId);
    }

    /// <summary>
    /// GraphicRaycaster 히트만 UI로 본다. PhysicsRaycaster 월드 히트는 포인터 차단으로 치지 않는다.
    /// </summary>
    bool IsPointerBlockedByUiAt(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
        _uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, _uiRaycastResults);

        for (int i = 0; i < _uiRaycastResults.Count; i++)
        {
            if (_uiRaycastResults[i].module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}
