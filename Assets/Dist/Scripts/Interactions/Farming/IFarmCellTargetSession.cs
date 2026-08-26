// ============================================================
// IFarmCellTargetSession — GridCursor 농사 타겟팅 콜백
// ============================================================

using UnityEngine;

public interface IFarmCellTargetSession
{
    bool CanApply(Vector3Int cell);
    void OnCellHover(Vector3Int cell, bool canApply);
    bool TryConfirm(Vector3Int cell);
    void OnCancel();
}
