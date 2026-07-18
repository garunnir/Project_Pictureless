// ============================================================
// UIContainerSlot — 사이드바 컨테이너 슬롯 (선택·드래그)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ContainerSlotVisualState
{
    Normal,
    Selected,
    Dragging,
}

[RequireComponent(typeof(UIContainerSlotDropZone))]
public sealed class UIContainerSlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    static readonly Color NormalColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SelectedColor = new(0.28f, 0.38f, 0.48f, 1f);
    static readonly Color DropHoverColor = new(0.32f, 0.42f, 0.32f, 1f);
    static readonly Color DraggingColor = new(0.18f, 0.18f, 0.18f, 0.45f);

    [SerializeField] Button _button;
    [SerializeField] TMP_Text _label;
    [SerializeField] Image _iconImage;
    [SerializeField] Image _highlight;
    [SerializeField] Image _backgroundImage;

    InventoryContainer _container;
    InventoryContainer _dragParentContainer;
    ItemStack _dragContainerStack;
    Action<InventoryContainer> _onSelected;
    IInventoryItemDragHost _dragHost;
    UIInventoryListWindow _window;
    InventorySession _session;
    bool _canMoveContainerAsStack;
    bool _isDropHover;
    ContainerSlotVisualState _visualState = ContainerSlotVisualState.Normal;
    bool _isSelected;

    public InventoryContainer Container => _container;
    public string ContainerInstanceId => _container != null ? _container.InstanceId : string.Empty;

    void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClick);

        if (_backgroundImage == null)
            _backgroundImage = GetComponent<Image>();
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    void OnDisable()
    {
        // Sidebar Sync / ApplyModeLayout may deactivate or destroy this slot mid-drag.
        if (_visualState != ContainerSlotVisualState.Dragging)
            return;

        _visualState = _isSelected
            ? ContainerSlotVisualState.Selected
            : ContainerSlotVisualState.Normal;

        if (_dragHost == null)
            return;

        _dragHost.OnItemDragEnded();
    }

    public void Bind(
        InventoryContainer container,
        bool selected,
        Action<InventoryContainer> onSelected,
        IInventoryItemDragHost dragHost,
        UIInventoryListWindow window,
        InventorySession session)
    {
        _container = container;
        _onSelected = onSelected;
        _dragHost = dragHost;
        _window = window;
        _session = session;
        _canMoveContainerAsStack = false;
        _dragParentContainer = null;
        _dragContainerStack = null;

        UIContainerSlotDropZone dropZone = GetComponent<UIContainerSlotDropZone>();
        dropZone?.Bind(window, this);

        if (container?.Definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ContainerData def = container.Definition;

        if (_label != null)
            _label.text = UITextPresenter.GetContainerName(def);

        if (_iconImage != null)
        {
            Sprite icon = ContainerVisualPresenter.GetDisplayIcon(container, _session);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            if (icon != null)
                _iconImage.color = Color.white;
        }

        if (_session != null &&
            _session.TryGetContainerItemStack(container, out InventoryContainer parent, out ItemStack stack))
        {
            _canMoveContainerAsStack = true;
            _dragParentContainer = parent;
            _dragContainerStack = stack;
        }

        _isDropHover = false;
        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (_highlight != null)
        {
            _highlight.gameObject.SetActive(selected);
            _highlight.enabled = selected;
        }

        ApplyIdleBackgroundColor();
    }

    public void SetDropHover(bool hover)
    {
        _isDropHover = hover;
        ApplyIdleBackgroundColor();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_dragHost == null || _container == null)
            return;

        if (_canMoveContainerAsStack &&
            _dragParentContainer != null &&
            _dragContainerStack != null)
        {
            InventoryDragState.BeginContainerTab(_dragParentContainer, _dragContainerStack);
            SetVisualState(ContainerSlotVisualState.Dragging);
            _dragHost.OnItemDragStarted();
            _dragHost.BeginDragGhost(eventData.position, 1);
            eventData.Use();
            return;
        }

        IReadOnlyList<ItemStack> contents = _container.Stacks;
        if (contents == null || contents.Count == 0)
            return;

        InventoryDragState.BeginContainerContents(_container);
        if (!InventoryDragState.IsDragging)
            return;

        int ghostCount = 0;
        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] != null)
                ghostCount++;
        }

        SetVisualState(ContainerSlotVisualState.Dragging);
        _dragHost.OnItemDragStarted();
        _dragHost.BeginDragGhost(eventData.position, ghostCount);
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!InventoryDragState.IsDragging || _dragHost == null)
            return;

        _dragHost.UpdateDragGhostPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_visualState == ContainerSlotVisualState.Dragging)
            SetVisualState(_isSelected ? ContainerSlotVisualState.Selected : ContainerSlotVisualState.Normal);

        if (_dragHost == null)
            return;

        _dragHost.OnItemDragEnded();
    }

    void OnClick() => _onSelected?.Invoke(_container);

    void SetVisualState(ContainerSlotVisualState state)
    {
        _visualState = state;

        switch (state)
        {
            case ContainerSlotVisualState.Dragging:
                ApplyBackgroundColor(DraggingColor);
                break;
            default:
                ApplyIdleBackgroundColor();
                break;
        }
    }

    void ApplyIdleBackgroundColor()
    {
        if (_visualState == ContainerSlotVisualState.Dragging)
            return;

        if (_isDropHover)
            ApplyBackgroundColor(DropHoverColor);
        else
            ApplyBackgroundColor(_isSelected ? SelectedColor : NormalColor);
    }

    void ApplyBackgroundColor(Color color)
    {
        if (_backgroundImage != null)
            _backgroundImage.color = color;
    }
}
