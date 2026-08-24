// ============================================================
// TillContextAction — DIG 도구로 셀 PLANTABLE 오버레이 설정
// ============================================================

using UnityEngine;

public sealed class TillContextAction : IContextMenuAction
{
    readonly Vector3Int _cell;

    public TillContextAction(Vector3Int cell)
    {
        _cell = cell;
    }

    public string GetDisabledReason() =>
        MapPlantService.GetTillBlockedReason(_cell);

    public void Execute() =>
        MapPlantService.TryTill(_cell);
}
