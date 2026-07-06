// ============================================================
// UIItemListView — 컨테이너 스택 목록 (LeanPool)
// ============================================================

using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public sealed class UIItemListView : MonoBehaviour
{
    [SerializeField] RectTransform _contentRoot;
    [SerializeField] UIItemListRow _rowPrefab;

    readonly List<UIItemListRow> _activeRows = new();

    public void Bind(InventoryContainer container)
    {
        ClearRows();

        if (container == null || _rowPrefab == null || _contentRoot == null)
            return;

        for (int i = 0; i < container.Stacks.Count; i++)
        {
            UIItemListRow row = LeanPool.Spawn(_rowPrefab, _contentRoot);
            row.Bind(container.Stacks[i]);
            _activeRows.Add(row);
        }
    }

    public void ClearRows()
    {
        for (int i = _activeRows.Count - 1; i >= 0; i--)
        {
            if (_activeRows[i] != null)
                LeanPool.Despawn(_activeRows[i].gameObject);
        }

        _activeRows.Clear();
    }
}
