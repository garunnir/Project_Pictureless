// ============================================================
// UIHoverPanelShell — 호버 정보창 Show/Hide·배치 셸 (콘텐츠 비소유, keep-in-bounds 고정)
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public sealed class UIHoverPanelShell : MonoBehaviour
{
    [SerializeField] RectTransform _rect;

    Canvas _rootCanvas;
    UIHoverStyle _style;
    bool _isVisible;
    bool _initialized;

    public bool IsVisible => _isVisible;

    public void Initialize(Canvas rootCanvas)
    {
        _rootCanvas = rootCanvas;
        if (_rect == null)
            _rect = transform as RectTransform;

        if (_initialized)
            return;

        DisableRaycasts();
        _initialized = true;
        Hide();
    }

    public void ShowAtScreen(Vector2 screenPosition, UIHoverStyle style)
    {
        if (_rect == null || _rootCanvas == null)
            return;

        _style = style;
        gameObject.SetActive(true);
        _isVisible = true;
        Place(screenPosition);
    }

    public void ShowNearAnchor(RectTransform anchor, UIHoverStyle style)
    {
        if (_rect == null || _rootCanvas == null || anchor == null)
            return;

        Camera camera = UIPopupPositioner.ResolveCamera(_rootCanvas);
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, anchor.position);
        ShowAtScreen(screenPosition, style);
    }

    public void Hide()
    {
        if (!this)
            return;

        _isVisible = false;
        gameObject.SetActive(false);
    }

    /// <summary>호출측이 마우스 추적할 때 사용. FollowMouse 스타일이 아니면 no-op.</summary>
    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (!_isVisible || !_style.FollowMouse)
            return;

        Place(screenPosition);
    }

    void Place(Vector2 screenPosition)
    {
        UIPopupPositioner.PlaceAtScreenPoint(
            _rect,
            screenPosition,
            _rootCanvas,
            _style.ScreenOffset,
            clampToCanvas: true);
    }

    void DisableRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
