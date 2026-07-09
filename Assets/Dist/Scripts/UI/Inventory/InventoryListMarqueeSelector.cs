// ============================================================
// InventoryListMarqueeSelector — 박스 드래그로 Row 다중 선택
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class InventoryListMarqueeSelector : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [SerializeField] RectTransform _selectionRect;
    [SerializeField] Canvas _rootCanvas;

    UIItemListView _listView;
    RectTransform _hostRect;
    Camera _uiCamera;
    Vector2 _startLocal;
    bool _isSelecting;

    public void Bind(UIItemListView listView, Canvas rootCanvas, RectTransform selectionRect)
    {
        _listView = listView;
        _rootCanvas = rootCanvas;
        _selectionRect = selectionRect;
        _hostRect = transform as RectTransform;
        _uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        Image image = GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_listView == null || _hostRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _hostRect, eventData.position, _uiCamera, out _startLocal))
            return;

        _isSelecting = true;
        _listView.Selection.Clear();
        UpdateSelectionRect(_startLocal, _startLocal);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isSelecting || _hostRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _hostRect, eventData.position, _uiCamera, out Vector2 currentLocal))
            return;

        UpdateSelectionRect(_startLocal, currentLocal);
        _listView.SelectRowsInRect(GetScreenRect(_startLocal, currentLocal));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isSelecting)
            return;

        _isSelecting = false;
        if (_selectionRect != null)
            _selectionRect.gameObject.SetActive(false);
    }

    void UpdateSelectionRect(Vector2 startLocal, Vector2 endLocal)
    {
        if (_selectionRect == null)
            return;

        Vector2 min = Vector2.Min(startLocal, endLocal);
        Vector2 max = Vector2.Max(startLocal, endLocal);
        _selectionRect.gameObject.SetActive(true);
        _selectionRect.anchoredPosition = min;
        _selectionRect.sizeDelta = max - min;
    }

    Rect GetScreenRect(Vector2 startLocal, Vector2 endLocal)
    {
        Vector3 worldMin = _hostRect.TransformPoint(Vector2.Min(startLocal, endLocal));
        Vector3 worldMax = _hostRect.TransformPoint(Vector2.Max(startLocal, endLocal));
        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldMin);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldMax);
        return Rect.MinMaxRect(
            Mathf.Min(screenMin.x, screenMax.x),
            Mathf.Min(screenMin.y, screenMax.y),
            Mathf.Max(screenMin.x, screenMax.x),
            Mathf.Max(screenMin.y, screenMax.y));
    }
}
