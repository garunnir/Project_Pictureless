// ============================================================
// UIItemListRow — 아이템 리스트 한 행 (바인딩 + 선택 + 드래그)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIItemListRow : MonoBehaviour,
    IPointerDownHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public static event Action<ItemStack, InventoryContainer, Vector2> RightClicked;

    static readonly Color NormalColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SelectedColor = new(0.28f, 0.38f, 0.48f, 1f);

    [SerializeField] TMP_Text _categoryText;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _detailText;
    [SerializeField] Image _iconImage;
    [SerializeField] Image _backgroundImage;

    ItemStack _stack;
    InventoryContainer _ownerContainer;
    InventoryListSelection _selection;
    IInventoryItemDragHost _dragHost;

    public ItemStack Stack => _stack;
    public RectTransform RectTransform => transform as RectTransform;

    public void Bind(
        ItemStack stack,
        InventoryContainer ownerContainer,
        InventoryListSelection selection,
        IInventoryItemDragHost dragHost)
    {
        _stack = stack;
        _ownerContainer = ownerContainer;
        _selection = selection;
        _dragHost = dragHost;

        if (_backgroundImage == null)
            TryGetComponent(out _backgroundImage);

        if (stack?.Item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ItemData item = stack.Item;

        if (_categoryText != null)
        {
            _categoryText.overflowMode = TextOverflowModes.Ellipsis;
            _categoryText.text = item.category ?? "";
        }

        if (_nameText != null)
        {
            _nameText.overflowMode = TextOverflowModes.Ellipsis;
            _nameText.text = UITextPresenter.GetItemName(item);
        }

        if (_detailText != null)
        {
            _detailText.overflowMode = TextOverflowModes.Ellipsis;
            _detailText.text = $"x{stack.Count}  {stack.TotalWeight:0.##}kg  {stack.TotalVolume:0.##}L";
        }

        if (_iconImage != null)
        {
            Sprite icon = ItemVisualPresenter.GetDisplayIcon(item.id);
            _iconImage.enabled = icon != null;
            _iconImage.sprite = icon;
        }

        RefreshSelectionVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (_stack?.Item == null || _ownerContainer == null)
            return;

        RightClicked?.Invoke(_stack, _ownerContainer, eventData.position);
    }

    public void RefreshSelectionVisual()
    {
        if (_backgroundImage == null)
            return;

        bool selected = _selection != null && _stack != null && _selection.IsSelected(_stack);
        _backgroundImage.color = selected ? SelectedColor : NormalColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_stack == null || _ownerContainer == null || _selection == null || _dragHost == null)
            return;

        if (!_selection.IsSelected(_stack))
            _selection.SetSingle(_stack);

        IReadOnlyList<ItemStack> stacks = _selection.GetSelectedStacks();
        InventoryDragState.Begin(_ownerContainer, _selection, stacks);

        _dragHost.OnItemDragStarted();
        _dragHost.BeginDragGhost(eventData.position, stacks.Count);

        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!InventoryDragState.IsDragging)
            return;

        if (_dragHost == null)
            return;

        _dragHost.UpdateDragGhostPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragHost == null)
            return;

        _dragHost.OnItemDragEnded();
    }
}
