// ============================================================
// UIOverlayWindowActivate — 포인터 프레스 시 히트 UI의 창을 레이어 내 맨 앞으로
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// EventSystem raycast → GetComponentInParent&lt;UIOverlayWindow&gt; → BringToFront.
/// 자식 IPointerDownHandler가 있어도 창 전체 클릭으로 활성화.
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class UIOverlayWindowActivate : MonoBehaviour
{
    static readonly List<RaycastResult> RaycastBuffer = new(16);

    PointerEventData _pointerEventData;

    void Update()
    {
        // Hot path: press frame only — no alloc beyond reused PointerEventData / buffer.
        if (!TryGetPressScreenPosition(out Vector2 screen))
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        if (_pointerEventData == null)
            _pointerEventData = new PointerEventData(eventSystem);
        else
            _pointerEventData.Reset();

        _pointerEventData.position = screen;
        RaycastBuffer.Clear();
        eventSystem.RaycastAll(_pointerEventData, RaycastBuffer);

        for (int i = 0; i < RaycastBuffer.Count; i++)
        {
            GameObject hit = RaycastBuffer[i].gameObject;
            if (hit == null)
                continue;

            UIOverlayWindow window = hit.GetComponentInParent<UIOverlayWindow>();
            if (window == null)
                continue;

            window.BringToFront();
            return;
        }
    }

    static bool TryGetPressScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = default;
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
            return false;

        screenPosition = mouse.position.ReadValue();
        return true;
    }
}
