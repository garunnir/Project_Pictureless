// ============================================================
// UICraftingRecipeCell — 제작 창 그리드 레시피 셀
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICraftingRecipeCell : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] Image _background;
    [SerializeField] Image _icon;
    [SerializeField] TMP_Text _name;

    RecipeData _recipe;
    Action<RecipeData> _onSelected;

    public RecipeData Recipe => _recipe;

    public void Wire(Button button, Image background, Image icon, TMP_Text name)
    {
        _button = button;
        _background = background;
        _icon = icon;
        _name = name;
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

    public void Bind(
        RecipeData recipe,
        bool selected,
        bool craftable,
        Action<RecipeData> onSelected)
    {
        _recipe = recipe;
        _onSelected = onSelected;

        DistUiFont.Apply(_name);

        if (recipe == null)
        {
            if (_icon != null)
            {
                _icon.enabled = false;
                _icon.sprite = null;
            }

            if (_name != null)
                _name.text = string.Empty;
            return;
        }

        if (_icon != null)
        {
            _icon.enabled = true;
            _icon.sprite = ItemVisualPresenter.GetDisplayIcon(recipe.result);
            _icon.preserveAspect = true;
        }

        if (_name != null)
        {
            _name.text = UITextPresenter.GetItemName(recipe.result);
            _name.color = craftable
                ? CraftingWindowLayout.SkillMetColor
                : Color.white;
        }

        if (_background != null)
            _background.color = selected
                ? CraftingWindowLayout.SelectedColor
                : CraftingWindowLayout.RowColor;
    }

    void OnClicked()
    {
        if (_recipe != null)
            _onSelected?.Invoke(_recipe);
    }
}
