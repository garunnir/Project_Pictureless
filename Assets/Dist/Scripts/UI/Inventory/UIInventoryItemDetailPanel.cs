// ============================================================
// UIInventoryItemDetailPanel — 인벤 아이템 호버 상세 보조창 (VLG 행)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIInventoryItemDetailPanel : MonoBehaviour
{
    static readonly UIHoverStyle DetailHoverStyle = new(new Vector2(16f, -16f), followMouse: true);

    [SerializeField] UIHoverPanelShell _shell;
    [SerializeField] RectTransform _rect;
    [SerializeField] TMP_Text _nameLine;
    [SerializeField] TMP_Text _descriptionLine;
    [SerializeField] TMP_Text _categoryLine;
    [SerializeField] TMP_Text _typeLine;
    [SerializeField] TMP_Text _countLine;
    [SerializeField] TMP_Text _weightLine;
    [SerializeField] TMP_Text _volumeLine;
    [SerializeField] TMP_Text _durabilityLine;
    [SerializeField] TMP_Text _containerCapacityLine;
    [SerializeField] TMP_Text _materialsLine;

    Canvas _rootCanvas;

    public bool IsVisible => _shell != null && _shell.IsVisible;

    public void Initialize(Canvas rootCanvas)
    {
        _rootCanvas = rootCanvas;
        EnsureShell();
        UIHoverCanvasLayer.EnsureParent(transform, rootCanvas);
        _shell.Initialize(rootCanvas);
    }

    public void Show(ItemStack stack, Vector2 screenPosition)
    {
        if (stack?.Item == null)
        {
            Hide();
            return;
        }

        EnsureShell();
        if (_shell == null)
            return;

        UIHoverCanvasLayer.EnsureParent(transform, _rootCanvas);
        UIHoverCanvasLayer.BringToFront(transform);
        BindRows(stack);
        RebuildLayout();
        _shell.ShowAtScreen(screenPosition, DetailHoverStyle);
    }

    public void Hide() => _shell?.Hide();

    public void SetScreenPosition(Vector2 screenPosition) => _shell?.SetScreenPosition(screenPosition);

    void EnsureShell()
    {
        if (_shell != null)
            return;

        _shell = GetComponent<UIHoverPanelShell>();
        if (_shell == null)
        {
            Debug.LogError(
                "[UIInventoryItemDetailPanel] UIHoverPanelShell missing. Bake onto InventoryItemDetailPanel prefab.",
                this);
        }
    }

    void BindRows(ItemStack stack)
    {
        ItemData item = stack.Item;

        SetRow(_nameLine, InventoryItemDetailLabels.FormatName(stack), alwaysShow: true);
        SetOptionalRow(
            _descriptionLine,
            InventoryItemDetailLabels.TryFormatDescription(item, out string description),
            description);
        SetOptionalRow(_categoryLine, InventoryItemDetailLabels.TryFormatCategory(item, out string category), category);
        SetOptionalRow(_typeLine, InventoryItemDetailLabels.TryFormatType(item, out string type), type);
        SetRow(_countLine, InventoryItemDetailLabels.FormatCount(stack.Count), alwaysShow: true);
        SetRow(_weightLine, InventoryItemDetailLabels.FormatWeight(stack), alwaysShow: true);
        SetRow(_volumeLine, InventoryItemDetailLabels.FormatVolume(stack), alwaysShow: true);
        SetOptionalRow(
            _durabilityLine,
            InventoryItemDetailLabels.TryFormatDurability(stack, out string durability),
            durability);
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
            _rect = transform as RectTransform;
        if (_rect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        Canvas.ForceUpdateCanvases();
    }
}
