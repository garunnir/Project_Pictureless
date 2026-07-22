// ============================================================
// UIItemListColumnHeader — 리스트 고정 컬럼 헤더 (클릭 정렬)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIItemListColumnHeader : MonoBehaviour
{
    const string AscendingMark = " ▲";
    const string DescendingMark = " ▼";

    [SerializeField] UIItemListView _listView;
    [SerializeField] TMP_Text _categoryLabel;
    [SerializeField] TMP_Text _nameLabel;
    [SerializeField] TMP_Text _countLabel;
    [SerializeField] TMP_Text _weightValueLabel;
    [SerializeField] TMP_Text _weightUnitLabel;
    [SerializeField] TMP_Text _volumeValueLabel;
    [SerializeField] TMP_Text _volumeUnitLabel;
    [SerializeField] Button _categoryButton;
    [SerializeField] Button _nameButton;
    [SerializeField] Button _countButton;
    [SerializeField] Button _weightButton;
    [SerializeField] Button _volumeButton;

    void Awake()
    {
        WireButton(_categoryButton, ItemListSortKey.Category);
        WireButton(_nameButton, ItemListSortKey.Name);
        WireButton(_countButton, ItemListSortKey.Count);
        WireButton(_weightButton, ItemListSortKey.Weight);
        WireButton(_volumeButton, ItemListSortKey.Volume);
        ApplyBaseLabels();
    }

    void WireButton(Button button, ItemListSortKey key)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnHeaderClicked(key));
    }

    void OnHeaderClicked(ItemListSortKey key)
    {
        if (_listView == null)
        {
            Debug.LogError("[UIItemListColumnHeader] List view reference missing.", this);
            return;
        }

        _listView.SetSort(key);
    }

    public void RefreshSortVisual(ItemListSortKey activeKey, bool ascending)
    {
        ApplyBaseLabels();
        string mark = ascending ? AscendingMark : DescendingMark;

        if (activeKey == ItemListSortKey.Category && _categoryLabel != null)
            _categoryLabel.text = InventoryWindowLabels.ColumnCategory + mark;
        else if (activeKey == ItemListSortKey.Name && _nameLabel != null)
            _nameLabel.text = InventoryWindowLabels.ColumnName + mark;
        else if (activeKey == ItemListSortKey.Count && _countLabel != null)
            _countLabel.text = InventoryWindowLabels.ColumnCount + mark;
        else if (activeKey == ItemListSortKey.Weight && _weightValueLabel != null)
            _weightValueLabel.text = InventoryWindowLabels.ColumnWeight + mark;
        else if (activeKey == ItemListSortKey.Volume && _volumeValueLabel != null)
            _volumeValueLabel.text = InventoryWindowLabels.ColumnVolume + mark;
    }

    void ApplyBaseLabels()
    {
        if (_categoryLabel != null)
            _categoryLabel.text = InventoryWindowLabels.ColumnCategory;
        if (_nameLabel != null)
            _nameLabel.text = InventoryWindowLabels.ColumnName;
        if (_countLabel != null)
            _countLabel.text = InventoryWindowLabels.ColumnCount;
        if (_weightValueLabel != null)
            _weightValueLabel.text = InventoryWindowLabels.ColumnWeight;
        if (_weightUnitLabel != null)
            _weightUnitLabel.text = InventoryWindowLabels.StackWeightUnit;
        if (_volumeValueLabel != null)
            _volumeValueLabel.text = InventoryWindowLabels.ColumnVolume;
        if (_volumeUnitLabel != null)
            _volumeUnitLabel.text = InventoryWindowLabels.StackVolumeUnit;
    }
}
