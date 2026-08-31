// ============================================================
// UIItemListRow — 아이템 리스트 한 행 (바인딩 + 선택 + 드래그 + 이름 겹침 바)
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
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    public static event Action<ItemStack, InventoryContainer, Vector2> RightClicked;
    public static event Action<ItemStack, Vector2> Hovered;
    public static event Action HoverEnded;
    public static event Action<ItemStack, InventoryContainer, UIItemListView> DoubleClicked;

    static readonly Color NormalColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SelectedColor = new(0.28f, 0.38f, 0.48f, 1f);

    [SerializeField] TMP_Text _categoryText;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _countText;
    [SerializeField] TMP_Text _weightValueText;
    [SerializeField] TMP_Text _weightUnitText;
    [SerializeField] TMP_Text _volumeValueText;
    [SerializeField] TMP_Text _volumeUnitText;
    [SerializeField] Image _iconImage;
    [SerializeField] Image _backgroundImage;
    [SerializeField] Image _nameStatusFill;

    ItemStack _stack;
    InventoryListDisplayGroup _displayGroup;
    InventoryContainer _ownerContainer;
    InventoryListSelection _selection;
    IInventoryItemDragHost _dragHost;
    UIItemDragGhostService _dragGhost;
    UIItemListView _listView;
    ItemNameStatusBar _nameBar;
    InventoryTimedMoveHost _timedHost;
    CharacterGearService _gearService;
    bool _hovered;
    bool _subscribedTimed;
    bool _subscribedGear;

    public ItemStack Stack => _stack;
    public InventoryListDisplayGroup DisplayGroup => _displayGroup;
    public RectTransform RectTransform => transform as RectTransform;

    void OnEnable() => SubscribeProgressSources();

    void OnDisable()
    {
        UnsubscribeProgressSources();
        ClearHoverIfNeeded();
    }

    public void Bind(
        ItemStack stack,
        InventoryContainer ownerContainer,
        InventoryListSelection selection,
        IInventoryItemDragHost dragHost,
        UIItemDragGhostService dragGhost,
        UIItemListView listView)
    {
        ClearHoverIfNeeded();
        EnsureNameBar();

        _displayGroup = null;
        _stack = stack;
        _ownerContainer = ownerContainer;
        _selection = selection;
        _dragHost = dragHost;
        _dragGhost = dragGhost;
        _listView = listView;

        if (_backgroundImage == null)
            TryGetComponent(out _backgroundImage);

        if (stack?.Item == null)
        {
            _nameBar?.Clear();
            return;
        }

        ItemData item = stack.Item;

        if (_categoryText != null)
            _categoryText.text = InventoryWindowLabels.GetItemCategory(item.category);

        if (_nameText != null)
        {
            _nameText.text = ItemAmmoLabels.AppendState(
                ItemDamageLabels.FormatName(
                    UITextPresenter.GetItemName(item),
                    stack.DamageLevel),
                stack);
        }

        if (_countText != null)
            _countText.text = InventoryWindowLabels.FormatStackCount(stack.Count);

        if (_weightValueText != null)
            _weightValueText.text = InventoryWindowLabels.FormatStackWeightValue(stack.TotalWeight);

        if (_weightUnitText != null)
            _weightUnitText.text = InventoryWindowLabels.StackWeightUnit;

        if (_volumeValueText != null)
            _volumeValueText.text = InventoryWindowLabels.FormatStackVolumeValue(stack.TotalVolume);

        if (_volumeUnitText != null)
            _volumeUnitText.text = InventoryWindowLabels.StackVolumeUnit;

        if (_iconImage != null)
        {
            Sprite icon = ItemVisualPresenter.GetDisplayIcon(item.id);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            if (icon != null)
                _iconImage.color = Color.white;
        }

        RefreshSelectionVisual();
        RefreshNameBar();
        SubscribeProgressSources();
    }

    public void BindDisplayGroup(
        InventoryListDisplayGroup group,
        InventoryContainer ownerContainer,
        InventoryListSelection selection,
        IInventoryItemDragHost dragHost,
        UIItemDragGhostService dragGhost,
        UIItemListView listView)
    {
        ClearHoverIfNeeded();
        EnsureNameBar();

        _displayGroup = group;
        _stack = group?.RepresentativeStack;
        _ownerContainer = ownerContainer;
        _selection = selection;
        _dragHost = dragHost;
        _dragGhost = dragGhost;
        _listView = listView;

        if (_backgroundImage == null)
            TryGetComponent(out _backgroundImage);

        if (_stack?.Item == null)
        {
            _nameBar?.Clear();
            return;
        }

        ItemData item = _stack.Item;

        if (_categoryText != null)
            _categoryText.text = InventoryWindowLabels.GetItemCategory(item.category);

        if (_nameText != null)
        {
            _nameText.text = ItemAmmoLabels.AppendState(
                ItemDamageLabels.FormatName(
                    UITextPresenter.GetItemName(item),
                    _stack.DamageLevel),
                _stack);
        }

        if (_countText != null)
            _countText.text = InventoryWindowLabels.FormatStackCount(group.DisplayCount);

        if (_weightValueText != null)
            _weightValueText.text = InventoryWindowLabels.FormatStackWeightValue(group.DisplayWeight);

        if (_weightUnitText != null)
            _weightUnitText.text = InventoryWindowLabels.StackWeightUnit;

        if (_volumeValueText != null)
            _volumeValueText.text = InventoryWindowLabels.FormatStackVolumeValue(group.DisplayVolume);

        if (_volumeUnitText != null)
            _volumeUnitText.text = InventoryWindowLabels.StackVolumeUnit;

        if (_iconImage != null)
        {
            Sprite icon = ItemVisualPresenter.GetDisplayIcon(item.id);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            if (icon != null)
                _iconImage.color = Color.white;
        }

        RefreshSelectionVisual();
        RefreshNameBar();
        SubscribeProgressSources();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stack?.Item == null || _ownerContainer == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 2 && !InventoryDragState.IsDragging && _listView != null)
                DoubleClicked?.Invoke(_stack, _ownerContainer, _listView);
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        RightClicked?.Invoke(_stack, _ownerContainer, eventData.position);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryDragState.IsDragging || _stack?.Item == null)
            return;

        _hovered = true;
        Hovered?.Invoke(_stack, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_hovered)
            return;

        _hovered = false;
        HoverEnded?.Invoke();
    }

    void ClearHoverIfNeeded()
    {
        if (!_hovered)
            return;

        _hovered = false;
        HoverEnded?.Invoke();
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

        if (_displayGroup != null)
        {
            BeginAggregateDisplayGroupDrag();
        }
        else if (!LootAggregateHost.IsAggregateContainer(_ownerContainer))
        {
            if (!_selection.IsSelected(_stack))
                _selection.SetSingle(_stack);

            IReadOnlyList<ItemStack> stacks = _selection.GetSelectedStacks();
            InventoryDragState.Begin(_ownerContainer, stacks, () => _selection?.Clear());
        }
        else
        {
            return;
        }

        if (!InventoryDragState.IsDragging)
            return;

        IReadOnlyList<ItemStack> ghostStacks = InventoryDragState.TryGetActive(out InventoryDragPayload payload)
            ? payload.Stacks
            : null;

        _dragHost.OnItemDragStarted();
        _dragGhost?.Show(ResolveDragIcon(ghostStacks), ghostStacks?.Count ?? 1, eventData.position);

        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!InventoryDragState.IsDragging)
            return;

        _dragGhost?.SetScreenPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragHost == null)
            return;

        _dragHost.OnItemDragEnded();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (LootAggregateHost.IsAggregateContainer(_ownerContainer))
            return;

        InventorySession session = _listView != null
            ? _listView.Session
            : PlayerInventoryRuntime.Active?.Session;

        if (WeaponAmmoDrop.TryApplyTo(_stack, session))
            return;

        if (session != null && _ownerContainer != null)
            InventoryDragDrop.TryApplyTo(session, _ownerContainer);
    }

    void BeginAggregateDisplayGroupDrag()
    {
        if (_displayGroup == null || _ownerContainer == null || _selection == null)
            return;

        if (!_selection.IsSelected(_stack))
            _selection.SetSingle(_stack);

        IReadOnlyList<ItemStack> selected = _selection.GetSelectedStacks();
        var sources = new List<(InventoryContainer owner, ItemStack stack)>();
        var selectedKeys = new HashSet<ItemMergeKey>();
        ItemMergeKeyDisplayEquivalence equivalence = ItemMergeKeyDisplayEquivalence.Instance;

        for (int i = 0; i < selected.Count; i++)
        {
            ItemStack representative = selected[i];
            if (representative != null)
                selectedKeys.Add(equivalence.GetDisplayKey(representative));
        }

        if (selectedKeys.Count == 0)
            return;

        IReadOnlyList<ItemStack> containerStacks = _ownerContainer.Stacks;
        for (int i = 0; i < containerStacks.Count; i++)
        {
            ItemStack stack = containerStacks[i];
            if (stack == null || !selectedKeys.Contains(equivalence.GetDisplayKey(stack)))
                continue;

            InventoryContainer owner = ResolveAggregateStackOwner(stack);
            if (owner == null || LootAggregateHost.IsAggregateContainer(owner))
                continue;

            sources.Add((owner, stack));
        }

        InventoryDragState.BeginAggregateDisplayGroup(
            _ownerContainer,
            sources,
            () => _selection?.Clear());
    }

    InventoryContainer ResolveAggregateStackOwner(ItemStack stack) =>
        _listView != null ? _listView.ResolveAggregateStackOwner(stack) : _ownerContainer;

    static Sprite ResolveDragIcon(IReadOnlyList<ItemStack> stacks)
    {
        if (stacks != null &&
            stacks.Count > 0 &&
            stacks[0]?.Item != null)
        {
            return ItemVisualPresenter.GetDisplayIcon(stacks[0].ItemId);
        }

        return ItemVisualPresenter.GetDefaultIcon();
    }

    void EnsureNameBar()
    {
        if (_nameBar != null)
            return;

        if (_nameStatusFill == null)
        {
            Debug.LogError("[UIItemListRow] Name status fill missing on row prefab.", this);
            return;
        }

        _nameBar = ItemNameStatusBar.Wrap(_nameStatusFill);
    }

    void RefreshNameBar()
    {
        if (_nameBar == null)
            return;

        ItemTimedNameProgress.Apply(_nameBar, _stack);
    }

    void SubscribeProgressSources()
    {
        SubscribeTimed();
        SubscribeGear();
    }

    void UnsubscribeProgressSources()
    {
        UnsubscribeTimed();
        UnsubscribeGear();
    }

    void SubscribeTimed()
    {
        InventoryTimedMoveHost timed = InventoryTimedMoveHost.Active;
        if (timed == _timedHost && _subscribedTimed)
            return;

        UnsubscribeTimed();
        _timedHost = timed;
        if (_timedHost == null)
            return;

        _timedHost.Changed += OnProgressChanged;
        _subscribedTimed = true;
    }

    void UnsubscribeTimed()
    {
        if (!_subscribedTimed || _timedHost == null)
            return;

        _timedHost.Changed -= OnProgressChanged;
        _timedHost = null;
        _subscribedTimed = false;
    }

    void SubscribeGear()
    {
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == _gearService && _subscribedGear)
            return;

        UnsubscribeGear();
        _gearService = gear;
        if (_gearService == null)
            return;

        _gearService.Changed += OnProgressChanged;
        _subscribedGear = true;
    }

    void UnsubscribeGear()
    {
        if (!_subscribedGear || _gearService == null)
            return;

        _gearService.Changed -= OnProgressChanged;
        _gearService = null;
        _subscribedGear = false;
    }

    void OnProgressChanged() => RefreshNameBar();
}
