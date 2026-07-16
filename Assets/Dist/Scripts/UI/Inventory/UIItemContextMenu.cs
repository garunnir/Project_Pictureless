// ============================================================
// UIItemContextMenu — 아이템 우클릭 컨텍스트 메뉴 (합성 레시피 표시)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIItemContextMenu : MonoBehaviour
{
    [SerializeField] RectTransform _panel;
    [SerializeField] Transform _buttonContainer;
    [SerializeField] Button _buttonPrefab;

    readonly List<Button> _activeButtons = new();

    InventoryContainer _sourceContainer;
    InventorySession _session;
    Canvas _rootCanvas;
    bool _isOpen;

    void OnEnable()
    {
        UIItemListRow.RightClicked += OnItemRightClicked;
    }

    void OnDisable()
    {
        UIItemListRow.RightClicked -= OnItemRightClicked;
    }

    void Update()
    {
        if (!_isOpen) return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    _panel, Input.mousePosition, UIPopupPositioner.ResolveCamera(_rootCanvas)))
            {
                Hide();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public void Initialize(InventorySession session, Canvas rootCanvas)
    {
        _session = session;
        _rootCanvas = rootCanvas;
    }

    void OnItemRightClicked(ItemStack stack, InventoryContainer container, Vector2 screenPosition)
    {
        if (stack?.Item == null)
            return;

        List<RecipeData> recipes = GameplayData.GetRecipesUsingIngredient(stack.ItemId);
        if (recipes.Count == 0)
        {
            Hide();
            return;
        }

        _sourceContainer = container;
        Show(recipes, screenPosition);
    }

    void Show(List<RecipeData> recipes, Vector2 screenPosition)
    {
        ClearButtons();

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (string.IsNullOrEmpty(recipe?.result)) continue;

            Button btn = Instantiate(_buttonPrefab, _buttonContainer);
            btn.transform.localScale = Vector3.one;
            btn.gameObject.SetActive(true);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = FormatRecipeLabel(recipe);

            bool canCraft = CraftingService.CanCraft(recipe, _sourceContainer);
            btn.interactable = canCraft;

            RecipeData capturedRecipe = recipe;
            btn.onClick.AddListener(() => OnRecipeClicked(capturedRecipe));

            _activeButtons.Add(btn);
        }

        PositionAtScreenPoint(screenPosition);
        _panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
        _isOpen = true;
    }

    public void Hide()
    {
        if (!_isOpen) return;

        ClearButtons();
        _panel.gameObject.SetActive(false);
        _sourceContainer = null;
        _isOpen = false;
    }

    void OnRecipeClicked(RecipeData recipe)
    {
        if (_sourceContainer == null || _session == null)
            return;

        CraftingService.TryCraft(recipe, _sourceContainer, _session);
        Hide();
    }

    void PositionAtScreenPoint(Vector2 screenPosition)
    {
        UIPopupPositioner.PlaceAtScreenPoint(_panel, screenPosition, _rootCanvas);
    }

    void ClearButtons()
    {
        for (int i = _activeButtons.Count - 1; i >= 0; i--)
        {
            if (_activeButtons[i] != null)
                Destroy(_activeButtons[i].gameObject);
        }

        _activeButtons.Clear();
    }

    static string FormatRecipeLabel(RecipeData recipe)
    {
        ItemData resultItem = GameplayData.GetItem(recipe.result);
        string resultName = resultItem?.name ?? recipe.result;

        int count = recipe.result_count > 0 ? recipe.result_count : 1;
        if (count > 1)
            return $"{resultName} x{count}";

        return resultName;
    }
}
