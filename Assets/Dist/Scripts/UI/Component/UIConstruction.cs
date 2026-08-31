// ============================================================
// UIConstruction — 런타임 편집기 (PrefabDB 즉시 배치). 본편 건설과 별 트랙.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using IsoTilemap;

[RequireComponent(typeof(UiMenuInputBehaviour))]
public class UIConstruction : MonoBehaviour
{
    public static bool IsOpen
    {
        get
        {
            UIConstruction ui = FindFirstObjectByType<UIConstruction>(FindObjectsInactive.Include);
            return ui != null && ui.isActiveAndEnabled && ui._isOpen;
        }
    }

    [SerializeField] Button prevBtn;
    [SerializeField] Button nextBtn;
    [SerializeField] Button closeBtn;
    [SerializeField] LayoutGroup content;
    [SerializeField] TileMapManager _tileManager;
    [SerializeField] TilePlacementState _placementState;
    [SerializeField] GridCursor _gridCursor;

    List<string> _categories = new();
    Dictionary<string, List<TileDefinition>> _grouped = new();
    int _categoryIndex;
    Button _selectedButton;
    bool _isOpen;
    bool _closing;

    void Start()
    {
        if (prevBtn == null) Debug.LogError("[UIConstruction] prevBtn is null");
        if (nextBtn == null) Debug.LogError("[UIConstruction] nextBtn is null");
        if (closeBtn == null) Debug.LogError("[UIConstruction] closeBtn is null");
        if (content == null) Debug.LogError("[UIConstruction] content is null");
        if (_tileManager == null) Debug.LogError("[UIConstruction] tileManager is null");
        if (_placementState == null) Debug.LogError("[UIConstruction] placementState is null");
        if (_gridCursor == null) Debug.LogError("[UIConstruction] gridCursor is null");

        if (prevBtn != null) prevBtn.onClick.AddListener(Prev);
        if (nextBtn != null) nextBtn.onClick.AddListener(Next);
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);

        if (InputManager.Instance != null)
            InputManager.Instance.UiPaginationPerformed += OnPagination;

        BuildGroups();
        ShowCategory(_categoryIndex);
    }

    void BuildGroups()
    {
        _grouped.Clear();
        _categories.Clear();

        if (_tileManager?.PrefabDB?.entries == null)
            return;

        foreach (var entry in _tileManager.PrefabDB.entries)
        {
            if (entry == null) continue;
            string cat = string.IsNullOrEmpty(entry.category)
                ? ConstDataTable.Tile.UncategorizedCategory
                : entry.category;
            if (!_grouped.ContainsKey(cat))
            {
                _grouped[cat] = new List<TileDefinition>();
                _categories.Add(cat);
            }
            _grouped[cat].Add(entry);
        }
    }

    void ShowCategory(int index)
    {
        if (content == null)
            return;

        foreach (Transform child in content.transform)
            Destroy(child.gameObject);

        _selectedButton = null;

        if (_categories.Count == 0) return;

        string cat = _categories[index];
        foreach (var def in _grouped[cat])
            CreateButton(def);

        if (prevBtn != null) prevBtn.interactable = index > 0;
        if (nextBtn != null) nextBtn.interactable = index < _categories.Count - 1;
    }

    void CreateButton(TileDefinition def)
    {
        var go = new GameObject(def.prefabId, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(content.transform, false);

        var img = go.GetComponent<Image>();
        if (def.thumbnail != null)
            img.sprite = def.thumbnail;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => OnTileButtonClicked(def, btn));
    }

    void OnTileButtonClicked(TileDefinition def, Button btn)
    {
        if (_selectedButton != null)
            _selectedButton.image.color = Color.white;

        _selectedButton = btn;
        btn.image.color = Color.yellow;

        _placementState?.Select(def);
        if (_gridCursor != null)
        {
            _gridCursor.SetActive(true);
            _gridCursor.SyncFromPointer();
        }
    }

    void OnPagination(InputAction.CallbackContext context)
    {
        float dir = context.ReadValue<float>();
        if (dir < 0) Prev();
        else if (dir > 0) Next();
    }

    void Prev()
    {
        if (_categoryIndex <= 0) return;
        _categoryIndex--;
        ShowCategory(_categoryIndex);
    }

    void Next()
    {
        if (_categoryIndex >= _categories.Count - 1) return;
        _categoryIndex++;
        ShowCategory(_categoryIndex);
    }

    public void Close()
    {
        if (_closing)
            return;

        _closing = true;
        _isOpen = false;
        _placementState?.Clear();
        if (_gridCursor != null &&
            !FarmCellTargetSession.IsActive &&
            !FishCellTargetSession.IsActive &&
            !ConstructionCellTargetSession.IsActive)
            _gridCursor.SetActive(false);

        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        _closing = false;
    }

    public void Open()
    {
        if (FarmCellTargetSession.IsActive ||
            FishCellTargetSession.IsActive ||
            ConstructionCellTargetSession.IsActive ||
            UIConstructionController.IsGameplayOpen)
            return;

        _isOpen = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _gridCursor?.SetActive(true);
    }

    void OnEnable()
    {
        // Do not call Open() here — avoids Open↔SetActive re-entrancy.
        _isOpen = true;
    }

    void OnDisable()
    {
        if (_closing)
            return;

        _isOpen = false;
        _placementState?.Clear();
    }

    void OnDestroy()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.UiPaginationPerformed -= OnPagination;
    }
}
