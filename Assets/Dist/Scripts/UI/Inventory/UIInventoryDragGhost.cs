// ============================================================
// UIInventoryDragGhost — 드래그 중 마우스 따라다니는 고스트
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIInventoryDragGhost : MonoBehaviour
{
    Image _iconImage;
    TMP_Text _countLabel;
    Canvas _rootCanvas;
    RectTransform _rect;

    public void Initialize(Image iconImage, TMP_Text countLabel, Canvas rootCanvas)
    {
        _iconImage = iconImage;
        _countLabel = countLabel;
        _rootCanvas = rootCanvas;
        _rect = transform as RectTransform;
        gameObject.SetActive(false);
    }

    public void EnsureReady(Canvas rootCanvas)
    {
        if (_rect == null)
            _rect = transform as RectTransform;

        if (_iconImage == null)
            TryGetComponent(out _iconImage);

        if (_countLabel == null)
            _countLabel = GetComponentInChildren<TMP_Text>(true);

        if (rootCanvas != null)
            _rootCanvas = rootCanvas;
        else if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Show(Sprite icon, int stackCount, Vector2 screenPosition)
    {
        if (!this)
            return;

        if (_iconImage != null)
        {
            Sprite displayIcon = icon != null ? icon : ItemVisualPresenter.GetDefaultIcon();
            _iconImage.sprite = displayIcon;
            _iconImage.enabled = displayIcon != null;
        }

        if (_countLabel != null)
            _countLabel.text = stackCount > 1 ? $"x{stackCount}" : string.Empty;

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        SetScreenPosition(screenPosition);
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (_rect == null || _rootCanvas == null)
            return;

        Camera camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                screenPosition,
                camera,
                out Vector2 localPoint))
        {
            _rect.anchoredPosition = localPoint;
        }
    }

    public void Hide()
    {
        if (!this)
            return;

        gameObject.SetActive(false);
    }
}
