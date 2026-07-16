// ============================================================
// UIItemContextMenu — 아이템 우클릭 컨텍스트 메뉴 (합성·분해 레시피 표시)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIItemContextMenu : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] RectTransform _panel;
    [SerializeField] Transform _buttonContainer;
    [SerializeField] Button _buttonPrefab;

    readonly List<Button> _activeButtons = new();

    InventoryContainer _sourceContainer;
    InventorySession _session;
    Canvas _rootCanvas;
    ItemStack _clickedStack;
    Image _rootRaycastImage;
    bool _isOpen;

    void Awake()
    {
        TryGetComponent(out _rootRaycastImage);
        SetRootRaycastEnabled(false);
    }

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
        if (!_isOpen)
            return;

        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        if (input.TryReadCancelPerformedThisFrame(out bool canceled) && canceled)
            Hide();
    }

    public void Initialize(InventorySession session, Canvas rootCanvas)
    {
        _session = session;
        _rootCanvas = rootCanvas;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isOpen)
            return;

        // 루트(전체 화면) 히트 = 패널 바깥 클릭 → 닫기.
        // 패널/버튼은 자신 Graphic이 먼저 소비하므로 여기로 오지 않는다.
        Hide();
    }

    void OnItemRightClicked(ItemStack stack, InventoryContainer container, Vector2 screenPosition)
    {
        if (stack?.Item == null)
            return;

        List<RecipeData> craftRecipes = GameplayData.GetRecipesUsingIngredient(stack.ItemId);
        List<RecipeData> uncraftRecipes = GameplayData.GetUncraftForResult(stack.ItemId);

        bool hasCraft = craftRecipes != null && craftRecipes.Count > 0;
        bool hasUncraft = uncraftRecipes != null && uncraftRecipes.Count > 0;
        if (!hasCraft && !hasUncraft)
        {
            Hide();
            return;
        }

        _sourceContainer = container;
        _clickedStack = stack;
        Show(craftRecipes, uncraftRecipes, screenPosition);
    }

    void Show(List<RecipeData> craftRecipes, List<RecipeData> uncraftRecipes, Vector2 screenPosition)
    {
        ClearButtons();

        if (craftRecipes != null)
        {
            for (int i = 0; i < craftRecipes.Count; i++)
            {
                RecipeData recipe = craftRecipes[i];
                if (string.IsNullOrEmpty(recipe?.result)) continue;

                Button btn = Instantiate(_buttonPrefab, _buttonContainer);
                btn.transform.localScale = Vector3.one;
                btn.gameObject.SetActive(true);

                string knowledgeFailure = RecipeKnowledge.GetFailureReason(recipe, _sourceContainer);
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = FormatRecipeLabel(recipe);
                    if (!string.IsNullOrEmpty(knowledgeFailure))
                        label.text = $"{label.text}\n{knowledgeFailure}";
                }

                bool canCraft = CraftingService.CanCraft(recipe, _sourceContainer);
                btn.interactable = canCraft && string.IsNullOrEmpty(knowledgeFailure);

                RecipeData capturedRecipe = recipe;
                btn.onClick.AddListener(() => OnRecipeClicked(capturedRecipe, isUncraft: false));

                _activeButtons.Add(btn);
            }
        }

        if (uncraftRecipes != null)
        {
            for (int i = 0; i < uncraftRecipes.Count; i++)
            {
                RecipeData recipe = uncraftRecipes[i];
                if (string.IsNullOrEmpty(recipe?.result)) continue;

                Button btn = Instantiate(_buttonPrefab, _buttonContainer);
                btn.transform.localScale = Vector3.one;
                btn.gameObject.SetActive(true);

                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"분해: {FormatRecipeLabel(recipe)}";

                btn.interactable = CraftingService.CanUncraft(recipe, _sourceContainer);

                RecipeData capturedRecipe = recipe;
                btn.onClick.AddListener(() => OnRecipeClicked(capturedRecipe, isUncraft: true));

                _activeButtons.Add(btn);
            }
        }

        PositionAtScreenPoint(screenPosition);
        _panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
        SetRootRaycastEnabled(true);
        _isOpen = true;
    }

    public void Hide()
    {
        if (!_isOpen) return;

        ClearButtons();
        _panel.gameObject.SetActive(false);
        SetRootRaycastEnabled(false);
        _sourceContainer = null;
        _clickedStack = null;
        _isOpen = false;
    }

    void OnRecipeClicked(RecipeData recipe, bool isUncraft)
    {
        if (_sourceContainer == null || _session == null)
            return;

        if (isUncraft)
        {
            if (_clickedStack == null)
                return;
            CraftingService.TryUncraft(recipe, _clickedStack, _sourceContainer, _session);
        }
        else
        {
            CraftingService.TryCraft(recipe, _sourceContainer, _session);
        }

        Hide();
    }

    void PositionAtScreenPoint(Vector2 screenPosition)
    {
        UIPopupPositioner.PlaceAtScreenPoint(_panel, screenPosition, _rootCanvas);
    }

    void SetRootRaycastEnabled(bool enabled)
    {
        if (_rootRaycastImage == null)
            TryGetComponent(out _rootRaycastImage);

        if (_rootRaycastImage != null)
            _rootRaycastImage.raycastTarget = enabled;
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
