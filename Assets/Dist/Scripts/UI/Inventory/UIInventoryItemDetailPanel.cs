// ============================================================
// UIInventoryItemDetailPanel — 인벤 아이템 호버 상세 보조창 (VLG 행)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIInventoryItemDetailPanel : MonoBehaviour
{
    const float ScreenOffsetX = 16f;
    const float ScreenOffsetY = -16f;

    [SerializeField] RectTransform _rect;
    [SerializeField] TMP_Text _nameLine;
    [SerializeField] TMP_Text _categoryLine;
    [SerializeField] TMP_Text _typeLine;
    [SerializeField] TMP_Text _countLine;
    [SerializeField] TMP_Text _weightLine;
    [SerializeField] TMP_Text _volumeLine;
    [SerializeField] TMP_Text _durabilityLine;
    [SerializeField] TMP_Text _containerCapacityLine;
    [SerializeField] TMP_Text _materialsLine;

    Canvas _rootCanvas;
    bool _isVisible;

    public bool IsVisible => _isVisible;

    public void Initialize(Canvas rootCanvas)
    {
        _rootCanvas = rootCanvas;
        if (_rect == null)
            _rect = transform as RectTransform;

        Hide();
    }

    public void Show(ItemStack stack, Vector2 screenPosition)
    {
        if (stack?.Item == null)
        {
            Hide();
            return;
        }

        BindRows(stack);
        gameObject.SetActive(true);
        _isVisible = true;
        RebuildLayout();
        SetScreenPosition(screenPosition);
    }

    public void Hide()
    {
        if (!this)
            return;

        _isVisible = false;
        gameObject.SetActive(false);
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (_rect == null || _rootCanvas == null)
            return;

        RectTransform canvasRect = _rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        Vector2 offsetScreen = screenPosition + new Vector2(ScreenOffsetX, ScreenOffsetY);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                offsetScreen,
                camera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 size = _rect.rect.size;
        Rect canvas = canvasRect.rect;
        float minX = canvas.xMin + size.x * _rect.pivot.x;
        float maxX = canvas.xMax - size.x * (1f - _rect.pivot.x);
        float minY = canvas.yMin + size.y * (1f - _rect.pivot.y);
        float maxY = canvas.yMax - size.y * _rect.pivot.y;

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
        _rect.anchoredPosition = localPoint;
    }

    void BindRows(ItemStack stack)
    {
        ItemData item = stack.Item;

        SetRow(_nameLine, InventoryItemDetailLabels.FormatName(stack), alwaysShow: true);
        SetOptionalRow(_categoryLine, InventoryItemDetailLabels.TryFormatCategory(item, out string category), category);
        SetOptionalRow(_typeLine, InventoryItemDetailLabels.TryFormatType(item, out string type), type);
        SetRow(_countLine, InventoryItemDetailLabels.FormatCount(stack.Count), alwaysShow: true);
        SetRow(_weightLine, InventoryItemDetailLabels.FormatWeight(stack), alwaysShow: true);
        SetRow(_volumeLine, InventoryItemDetailLabels.FormatVolume(stack), alwaysShow: true);
        SetRow(_durabilityLine, InventoryItemDetailLabels.FormatDurability(stack.DamageLevel), alwaysShow: true);
        SetOptionalRow(
            _containerCapacityLine,
            InventoryItemDetailLabels.TryFormatContainerCapacity(item, out string capacity),
            capacity);
        SetOptionalRow(
            _materialsLine,
            InventoryItemDetailLabels.TryFormatMaterials(item, out string materials),
            materials);
    }

    static void SetRow(TMP_Text line, string text, bool alwaysShow)
    {
        if (line == null)
            return;

        bool show = alwaysShow && !string.IsNullOrEmpty(text);
        line.gameObject.SetActive(show);
        if (show)
            line.text = text;
    }

    static void SetOptionalRow(TMP_Text line, bool hasValue, string text)
    {
        if (line == null)
            return;

        line.gameObject.SetActive(hasValue);
        if (hasValue)
            line.text = text;
    }

    void RebuildLayout()
    {
        if (_rect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        Canvas.ForceUpdateCanvases();
    }
}
