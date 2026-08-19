// ============================================================
// UICraftingIngredientCard — 재료/도구/품질 카드 + 대체재 드롭·메뉴
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CraftingIngredientKind
{
    Consume,
    Keep,
    Fuel,
    Quality
}

public sealed class UICraftingIngredientCard : MonoBehaviour, IDropHandler
{
    [SerializeField] Image _background;
    [SerializeField] Image _icon;
    [SerializeField] Image _kindIcon;
    [SerializeField] TMP_Text _name;
    [SerializeField] TMP_Text _count;
    [SerializeField] Button _iconButton;
    [SerializeField] Button _swapButton;

    readonly List<string> _altItemIds = new(8);

    int _slotIndex;
    CraftingIngredientKind _kind;
    Action<int, CraftingIngredientKind, Vector2> _onOpenAlts;
    Action<int, CraftingIngredientKind, string> _onDropSelect;

    public void Wire(
        Image background,
        Image icon,
        Image kindIcon,
        TMP_Text name,
        TMP_Text count,
        Button iconButton,
        Button swapButton)
    {
        _background = background;
        _icon = icon;
        _kindIcon = kindIcon;
        _name = name;
        _count = count;
        _iconButton = iconButton;
        _swapButton = swapButton;
    }

    void Awake()
    {
        if (_iconButton != null)
            _iconButton.onClick.AddListener(OnIconClicked);

        if (_swapButton != null)
            _swapButton.onClick.AddListener(OnSwapClicked);
    }

    void OnDestroy()
    {
        if (_iconButton != null)
            _iconButton.onClick.RemoveListener(OnIconClicked);

        if (_swapButton != null)
            _swapButton.onClick.RemoveListener(OnSwapClicked);
    }

    public void Bind(
        CraftingIngredientKind kind,
        string itemId,
        string displayName,
        int available,
        int required,
        int qualityLevel,
        int slotIndex,
        IReadOnlyList<string> altItemIds,
        bool showSwap,
        Action<int, CraftingIngredientKind, Vector2> onOpenAlts,
        Action<int, CraftingIngredientKind, string> onDropSelect)
    {
        _slotIndex = slotIndex;
        _kind = kind;
        _onOpenAlts = onOpenAlts;
        _onDropSelect = onDropSelect;

        _altItemIds.Clear();
        if (altItemIds != null)
        {
            for (int i = 0; i < altItemIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(altItemIds[i]))
                    _altItemIds.Add(altItemIds[i]);
            }
        }

        DistUiFont.Apply(_name);
        DistUiFont.Apply(_count);

        if (_icon != null)
        {
            bool showIcon = kind == CraftingIngredientKind.Quality || !string.IsNullOrEmpty(itemId);
            _icon.enabled = showIcon;
            _icon.sprite = showIcon
                ? (string.IsNullOrEmpty(itemId)
                    ? ItemVisualPresenter.GetDefaultIcon()
                    : ItemVisualPresenter.GetDisplayIcon(itemId))
                : null;
            _icon.preserveAspect = true;
            Color iconColor = Color.white;
            iconColor.a = available >= required
                ? 1f
                : CraftingWindowLayout.UnmetIconAlpha;
            _icon.color = iconColor;
        }

        if (_kindIcon != null)
        {
            _kindIcon.gameObject.SetActive(true);
            _kindIcon.enabled = true;
            _kindIcon.color = KindColor(kind);
        }

        if (_count != null)
        {
            _count.gameObject.SetActive(true);
            _count.text = kind == CraftingIngredientKind.Quality
                ? CraftingWindowLabels.FormatQualityCount(available, required)
                : CraftingWindowLabels.FormatCount(available, required);
            _count.color = available >= required
                ? CraftingWindowLayout.SkillMetColor
                : CraftingWindowLayout.SkillUnmetColor;
        }

        if (_name != null)
        {
            bool showName = kind == CraftingIngredientKind.Quality && !string.IsNullOrEmpty(displayName);
            _name.gameObject.SetActive(showName);
            if (showName)
            {
                _name.text = displayName;
                _name.color = available >= required
                    ? CraftingWindowLayout.SkillMetColor
                    : CraftingWindowLayout.SkillUnmetColor;
            }
        }

        if (_swapButton != null)
            _swapButton.gameObject.SetActive(showSwap);
    }

    public void BindOutput(string itemId, string displayName, int count)
    {
        _slotIndex = -1;
        _kind = CraftingIngredientKind.Consume;
        _onOpenAlts = null;
        _onDropSelect = null;
        _altItemIds.Clear();

        DistUiFont.Apply(_name);
        DistUiFont.Apply(_count);

        if (_icon != null)
        {
            bool showIcon = !string.IsNullOrEmpty(itemId);
            _icon.enabled = showIcon;
            _icon.sprite = showIcon ? ItemVisualPresenter.GetDisplayIcon(itemId) : null;
            _icon.preserveAspect = true;
            _icon.color = Color.white;
        }

        if (_kindIcon != null)
        {
            _kindIcon.enabled = false;
            _kindIcon.gameObject.SetActive(false);
        }

        if (_name != null)
            _name.gameObject.SetActive(false);

        if (_count != null)
        {
            _count.gameObject.SetActive(true);
            _count.text = CraftingWindowLabels.FormatOutputCount(count);
            _count.color = Color.white;
        }

        if (_swapButton != null)
            _swapButton.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return;

        if (payload.Kind != InventoryDragKind.Item)
            return;

        if (payload.Stacks == null || payload.Stacks.Count == 0)
            return;

        ItemStack first = payload.Stacks[0];
        string itemId = first?.ItemId;
        if (string.IsNullOrEmpty(itemId))
            return;

        bool matched = false;
        for (int i = 0; i < _altItemIds.Count; i++)
        {
            if (_altItemIds[i] == itemId)
            {
                matched = true;
                break;
            }
        }

        if (!matched)
            return;

        payload.ClearSelection?.Invoke();
        InventoryDragState.MarkConsumed();
        _onDropSelect?.Invoke(_slotIndex, _kind, itemId);
    }

    void OnIconClicked() => OpenAlts();

    void OnSwapClicked() => OpenAlts();

    void OpenAlts()
    {
        if (_altItemIds.Count == 0)
            return;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            null,
            transform.position);
        _onOpenAlts?.Invoke(_slotIndex, _kind, screen);
    }

    static Color KindColor(CraftingIngredientKind kind)
    {
        switch (kind)
        {
            case CraftingIngredientKind.Consume:
                return CraftingWindowLayout.ConsumeIconColor;
            case CraftingIngredientKind.Fuel:
                return CraftingWindowLayout.FuelIconColor;
            case CraftingIngredientKind.Quality:
                return CraftingWindowLayout.QualityIconColor;
            default:
                return CraftingWindowLayout.KeepIconColor;
        }
    }
}
