// ============================================================
// UICraftingCategoryRow — 제작 창 왼쪽 카테고리 한 줄
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICraftingCategoryRow : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] Image _background;
    [SerializeField] TMP_Text _label;

    string _categoryId;
    Action<string> _onSelected;

    public string CategoryId => _categoryId;

    public void Wire(Button button, Image background, TMP_Text label)
    {
        _button = button;
        _background = background;
        _label = label;
    }

    void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnClicked);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    public void Bind(string categoryId, string label, bool selected, Action<string> onSelected)
    {
        _categoryId = categoryId;
        _onSelected = onSelected;

        DistUiFont.Apply(_label);
        if (_label != null)
            _label.text = label ?? string.Empty;

        if (_background != null)
            _background.color = selected
                ? CraftingWindowLayout.SelectedColor
                : CraftingWindowLayout.RowColor;
    }

    void OnClicked() => _onSelected?.Invoke(_categoryId);
}
