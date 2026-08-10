// ============================================================
// UICharacterWornRow — 착용 목록 행 (아이콘·이름·covers·호버·벗기)
// ============================================================

using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UICharacterWornRow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    Image _icon;
    TMP_Text _label;
    ItemNameStatusBar _nameBar;
    ItemStack _stack;
    CharacterGearService _gear;
    int _strength;
    Action<string, RectTransform> _onHover;
    Action _onExit;
    Action<ItemStack, bool> _onUnequip;
    UIItemDragGhostService _dragGhost;
    bool _dragging;

    public void EnsureChrome()
    {
        EnsureIcon();
        EnsureLabel();
        if (_nameBar == null)
            _nameBar = ItemNameStatusBar.Ensure(ref _label);
    }

    void EnsureIcon()
    {
        if (_icon != null)
            return;

        Transform t = transform.Find("Icon");
        if (t != null)
            _icon = t.GetComponent<Image>();
        if (_icon != null)
            return;

        GameObject go = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = GearConstants.WornIconSize;
        le.preferredWidth = GearConstants.WornIconSize;
        le.minHeight = GearConstants.WornIconSize;
        le.preferredHeight = GearConstants.WornIconSize;
        _icon = go.GetComponent<Image>();
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;
    }

    void EnsureLabel()
    {
        if (_label != null)
            return;

        Transform labelChild = transform.Find(ItemNameStatusBar.LabelObjectName);
        if (labelChild != null)
            _label = labelChild.GetComponent<TMP_Text>();
        if (_label == null)
            _label = GetComponent<TMP_Text>();
        if (_label == null)
        {
            GameObject labelGo = new(ItemNameStatusBar.LabelObjectName, typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.transform.SetParent(transform, false);
            LayoutElement le = labelGo.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = GearConstants.WornRowHeight;
            _label = labelGo.AddComponent<TextMeshProUGUI>();
        }

        _label.fontSize = GearConstants.UiFontSizeBody;
        _label.raycastTarget = false;
        DistUiFont.Apply(_label);
    }

    public void Bind(
        ItemStack stack,
        CharacterGearService gear,
        int strength,
        Action<string, RectTransform> onHover,
        Action onExit,
        Action<ItemStack, bool> onUnequip)
    {
        EnsureChrome();
        EnsureHorizontalLayout();
        _stack = stack;
        _gear = gear;
        _strength = strength;
        _onHover = onHover;
        _onExit = onExit;
        _onUnequip = onUnequip;

        if (stack?.Item == null)
        {
            if (_label != null)
                _label.text = string.Empty;
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }

            _nameBar?.Clear();
            return;
        }

        if (_icon != null)
        {
            _icon.enabled = true;
            _icon.sprite = ItemVisualPresenter.GetDisplayIcon(stack.ItemId);
            _icon.color = Color.white;
        }

        string covers = stack.Item.armor?.covers != null
            ? string.Join(",", stack.Item.armor.covers)
            : string.Empty;
        _label.text = string.IsNullOrEmpty(covers)
            ? stack.Item.name
            : $"{stack.Item.name} ({covers})";

        RefreshNameBar();
    }

    void EnsureHorizontalLayout()
    {
        HorizontalLayoutGroup h = GetComponent<HorizontalLayoutGroup>();
        if (h == null)
        {
            h = gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 6f;
            h.padding = new RectOffset(4, 4, 2, 2);
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childControlWidth = true;
            h.childControlHeight = true;
        }
    }

    public void RefreshNameBar()
    {
        if (_nameBar == null)
            return;

        ItemTimedNameProgress.Apply(_nameBar, _stack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_stack?.Item == null)
            return;

        int required = GearHandleRules.RequiredStrForWear(_stack.Item);
        bool strain = GearHandleRules.HasLiftStrain(_strength, _stack.Item, false);
        var sb = new StringBuilder(220);
        sb.Append(_stack.Item.name).Append('\n');
        sb.Append(CharacterGearLabels.FormatRequiredStr(required, _strength, strain));
        CharacterGearLabels.AppendItemArmorHover(sb, _stack.Item.armor);
        _onHover?.Invoke(sb.ToString(), transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stack == null || _onUnequip == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            _onUnequip.Invoke(_stack, false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (_stack?.Item == null)
            return;

        _dragging = true;
        EnsureDragGhost()?.Show(
            ItemVisualPresenter.GetDisplayIcon(_stack.ItemId),
            1,
            eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragGhost?.SetScreenPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        _dragging = false;
        _dragGhost?.Hide();

        if (_stack == null || _onUnequip == null)
            return;

        UICharacterWindow window = GetComponentInParent<UICharacterWindow>();
        RectTransform windowRt = window != null ? window.WindowRect : null;
        if (windowRt == null)
            return;

        Canvas canvas = window.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(windowRt, eventData.position, cam))
            _onUnequip.Invoke(_stack, true);
    }

    UIItemDragGhostService EnsureDragGhost()
    {
        if (_dragGhost != null)
            return _dragGhost;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (!UIItemDragGhostService.TryGet(canvas, out _dragGhost) || _dragGhost == null)
        {
            Debug.LogError(
                "[UICharacterWornRow] UIItemDragGhostService missing on UICanvas. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return null;
        }

        _dragGhost.EnsureReady();
        return _dragGhost;
    }
}
