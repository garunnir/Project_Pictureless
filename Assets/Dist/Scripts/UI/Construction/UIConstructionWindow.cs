// ============================================================
// UIConstructionWindow — 본편 건설 레시피 목록·상세·건설 시작
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIConstructionWindow : MonoBehaviour
{
    [SerializeField] TMP_Text _title;
    [SerializeField] TMP_Text _detail;
    [SerializeField] Button _buildButton;
    [SerializeField] Button _closeButton;
    [SerializeField] Transform _listContent;
    [SerializeField] UIConstructionRecipeRow _rowPrefab;

    ConstructionData _selected;
    readonly List<UIConstructionRecipeRow> _rows = new();

    public event System.Action CloseRequested;
    public event System.Action<ConstructionData> BuildRequested;

    void Awake()
    {
        if (_buildButton != null)
            _buildButton.onClick.AddListener(OnBuildClicked);
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
    }

    void OnDestroy()
    {
        if (_buildButton != null)
            _buildButton.onClick.RemoveListener(OnBuildClicked);
    }

    public void Refresh()
    {
        ClearRows();
        IReadOnlyList<ConstructionData> all = GameplayData.GetAllConstructions();
        for (int i = 0; i < all.Count; i++)
        {
            ConstructionData data = all[i];
            if (data == null)
                continue;

            UIConstructionRecipeRow row = CreateRow(data);
            if (row != null)
                _rows.Add(row);
        }

        if (_selected == null && _rows.Count > 0)
            Select(_rows[0].Data);
        else
            RefreshDetail();
    }

    public void Select(ConstructionData data)
    {
        _selected = data;
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].SetSelected(_rows[i].Data == data);

        RefreshDetail();
    }

    void RefreshDetail()
    {
        if (_title != null)
            _title.text = "건설";

        if (_detail == null)
            return;

        if (_selected == null)
        {
            _detail.text = string.Empty;
            if (_buildButton != null)
                _buildButton.interactable = false;
            return;
        }

        string name = string.IsNullOrEmpty(_selected.display_name)
            ? _selected.id
            : _selected.display_name;
        _detail.text =
            $"{name}\n" +
            $"결과: {_selected.post_prefab_id}\n" +
            $"시간: {_selected.time_minutes:0.##}분\n" +
            $"슬롯: {_selected.post_slot}\n" +
            "R: 프리뷰 회전";

        CraftingMaterialPool pool = ConstructionService.CreatePoolFromActivePlayer();
        RecipeData recipe = ConstructionService.ToMaterialRecipe(_selected);
        bool can = CraftingService.CanCraft(recipe, pool);

        if (_buildButton != null)
            _buildButton.interactable = can;
    }

    void OnBuildClicked()
    {
        if (_selected == null)
            return;

        BuildRequested?.Invoke(_selected);
    }

    UIConstructionRecipeRow CreateRow(ConstructionData data)
    {
        if (_listContent == null)
            return null;

        UIConstructionRecipeRow row;
        if (_rowPrefab != null)
        {
            row = Instantiate(_rowPrefab, _listContent);
            row.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject(data.id, typeof(RectTransform), typeof(UIConstructionRecipeRow));
            go.transform.SetParent(_listContent, false);
            row = go.GetComponent<UIConstructionRecipeRow>();
            row.EnsureRuntimeChrome();
        }

        row.Bind(data, Select);
        return row;
    }

    void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i] != null)
                Destroy(_rows[i].gameObject);
        }

        _rows.Clear();
    }
}
