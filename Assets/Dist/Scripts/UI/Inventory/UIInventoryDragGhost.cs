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

    public void Show(Sprite icon, int stackCount, Vector2 screenPosition)
    {
        if (_iconImage != null)
        {
            _iconImage.enabled = icon != null;
            _iconImage.sprite = icon;
        }

        if (_countLabel != null)
            _countLabel.text = stackCount > 1 ? $"x{stackCount}" : string.Empty;

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

    public void Hide() => gameObject.SetActive(false);
}
