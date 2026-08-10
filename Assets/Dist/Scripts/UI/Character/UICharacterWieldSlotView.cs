// ============================================================
// UICharacterWieldSlotView — L/R 들기 슬롯 (아이콘·액션 아이콘·호버·해제)
// ============================================================

using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UICharacterWieldSlotView :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    Image _itemIcon;
    Image _actionIcon;
    TMP_Text _actionLabel;
    TMP_Text _label;
    ItemNameStatusBar _nameBar;
    CharacterGearService _gear;
    WieldSlotId _slot;
    int _strength;
    Action<string, RectTransform> _onHover;
    Action _onExit;
    Action<WieldSlotId, bool> _onUnequip;
    UIItemDragGhostService _dragGhost;
    bool _dragging;

    public void EnsureChrome()
    {
        EnsureDropRaycast();
        EnsureItemIcon();
        EnsureActionIcon();
        EnsureLabelForProgressBar();
    }

    void EnsureDropRaycast()
    {
        if (!TryGetComponent(out Image bg))
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
        }

        bg.raycastTarget = true;
    }

    void EnsureItemIcon()
    {
        if (_itemIcon != null)
            return;

        Transform t = transform.Find("Icon");
        if (t != null)
            _itemIcon = t.GetComponent<Image>();
        if (_itemIcon != null)
            return;

        GameObject go = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(GearConstants.WieldIconSize, GearConstants.WieldIconSize);
        _itemIcon = go.GetComponent<Image>();
        _itemIcon.preserveAspect = true;
        _itemIcon.raycastTarget = false;
    }

    void EnsureActionIcon()
    {
        if (_actionIcon != null)
            return;

        Transform t = transform.Find("ActionIcon");
        if (t != null)
        {
            _actionIcon = t.GetComponent<Image>();
            Transform labelTf = t.Find("Label");
            if (labelTf != null)
                _actionLabel = labelTf.GetComponent<TMP_Text>();
        }

        if (_actionIcon != null)
        {
            DistUiFont.Apply(_actionLabel);
            return;
        }

        GameObject go = new("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-2f, -2f);
        rt.sizeDelta = new Vector2(GearConstants.WieldActionIconSize, GearConstants.WieldActionIconSize);
        _actionIcon = go.GetComponent<Image>();
        _actionIcon.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        _actionIcon.raycastTarget = false;

        GameObject labelGo = new("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        _actionLabel = labelGo.AddComponent<TextMeshProUGUI>();
        _actionLabel.fontSize = GearConstants.UiFontSizeActionIcon;
        _actionLabel.alignment = TextAlignmentOptions.Center;
        _actionLabel.raycastTarget = false;
        DistUiFont.Apply(_actionLabel);
    }

    void EnsureLabelForProgressBar()
    {
        if (_label != null)
            return;

        Transform labelChild = transform.Find(ItemNameStatusBar.LabelObjectName);
        if (labelChild != null)
            _label = labelChild.GetComponent<TMP_Text>();
        if (_label == null)
        {
            GameObject labelGo = new(ItemNameStatusBar.LabelObjectName, typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.transform.SetParent(transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            _label = labelGo.AddComponent<TextMeshProUGUI>();
        }

        // Plan: no always-on name on wield — keep for name-overlay bar only.
        _label.text = string.Empty;
        _label.fontSize = 1f;
        _label.color = new Color(1f, 1f, 1f, 0f);
        _label.raycastTarget = false;
        DistUiFont.Apply(_label);
        _nameBar = ItemNameStatusBar.Ensure(ref _label);
    }

    public void Bind(
        CharacterGearService gear,
        WieldSlotId slot,
        int strength,
        Action<string, RectTransform> onHover,
        Action onExit,
        Action<WieldSlotId, bool> onUnequip)
    {
        EnsureChrome();
        _gear = gear;
        _slot = slot;
        _strength = strength;
        _onHover = onHover;
        _onExit = onExit;
        _onUnequip = onUnequip;

        ItemStack stack = gear?.Wield?.Get(slot);
        if (stack?.Item == null)
        {
            if (_itemIcon != null)
            {
                _itemIcon.enabled = true;
                _itemIcon.sprite = ItemVisualPresenter.GetDefaultIcon();
                _itemIcon.color = new Color(1f, 1f, 1f, 0.25f);
            }

            SetActionVisual(null);
            _nameBar?.Clear();
            return;
        }

        if (_itemIcon != null)
        {
            _itemIcon.enabled = true;
            _itemIcon.sprite = ItemVisualPresenter.GetDisplayIcon(stack.ItemId);
            _itemIcon.color = Color.white;
        }

        WeaponAction? action = null;
        gear.HandActions.TryGet(stack.ItemId, out action);
        SetActionVisual(action);
        RefreshNameBar();
    }

    void SetActionVisual(WeaponAction? action)
    {
        if (_actionLabel == null)
            return;

        if (action == null)
            _actionLabel.text = "—";
        else
        {
            switch (action.Value)
            {
                case WeaponAction.Bashing:
                    _actionLabel.text = "B";
                    break;
                case WeaponAction.Cutting:
                    _actionLabel.text = "C";
                    break;
                case WeaponAction.Gun:
                    _actionLabel.text = "G";
                    break;
                default:
                    _actionLabel.text = "—";
                    break;
            }
        }
    }

    public void RefreshNameBar()
    {
        if (_nameBar == null)
            return;

        ItemStack stack = _gear?.Wield?.Get(_slot);
        ItemTimedNameProgress.Apply(_nameBar, stack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemStack stack = _gear?.Wield?.Get(_slot);
        if (stack?.Item == null)
            return;

        bool twoHand = _gear.Wield.IsTwoHand;
        int required = GearHandleRules.RequiredStr(stack.Item, twoHand);
        bool strain = GearHandleRules.HasLiftStrain(_strength, stack.Item, twoHand);
        var sb = new StringBuilder(160);
        sb.Append(stack.Item.name).Append('\n');
        sb.Append(CharacterGearLabels.FormatRequiredStr(required, _strength, strain));
        _onHover?.Invoke(sb.ToString(), transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_gear == null)
            return;

        ItemStack stack = _gear.Wield.Get(_slot);
        if (stack?.Item == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            UICharacterHandActionMenu.Show(
                _gear,
                stack.ItemId,
                _slot,
                eventData.position,
                canvas,
                () => Bind(_gear, _slot, _strength, _onHover, _onExit, _onUnequip));
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            _onUnequip?.Invoke(_slot, false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GearInventoryDrop.TryWieldFromActiveDrag(_slot);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        ItemStack stack = _gear?.Wield?.Get(_slot);
        if (stack?.Item == null)
            return;
        _dragging = true;
        EnsureDragGhost()?.Show(
            ItemVisualPresenter.GetDisplayIcon(stack.ItemId),
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

        ItemStack stack = _gear?.Wield?.Get(_slot);
        if (stack?.Item == null || _onUnequip == null)
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
            _onUnequip.Invoke(_slot, true);
    }

    UIItemDragGhostService EnsureDragGhost()
    {
        if (_dragGhost != null)
            return _dragGhost;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (!UIItemDragGhostService.TryGet(canvas, out _dragGhost) || _dragGhost == null)
        {
            Debug.LogError(
                "[UICharacterWieldSlotView] UIItemDragGhostService missing on UICanvas. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return null;
        }

        _dragGhost.EnsureReady();
        return _dragGhost;
    }
}
