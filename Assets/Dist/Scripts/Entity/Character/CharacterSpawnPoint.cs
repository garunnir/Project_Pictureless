// ============================================================
// CharacterSpawnPoint — IsoLand 배치용 셀 마커 (Play는 셀만 읽음)
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterSpawnPoint : MonoBehaviour
{
    [SerializeField] Vector3Int _cell;
    [SerializeField] CharacterSpawnRole _gizmoRole = CharacterSpawnRole.Npc;
    [SerializeField, Min(0.0001f)] float _gizmoCellSize = 1f;

    public Vector3Int Cell => _cell;
    public CharacterSpawnRole GizmoRole => _gizmoRole;

    void OnValidate()
    {
        _gizmoCellSize = Mathf.Max(0.0001f, _gizmoCellSize);
        _cell = TileHelper.ConvertWorldToGrid(transform.position, _gizmoCellSize);
    }

    void OnDrawGizmos()
    {
        TileHelper.DrawOccupiedCellWire(_cell, _gizmoCellSize, CharacterSpawnGizmoColors.ForRole(_gizmoRole));

        Vector3Int fromTransform = TileHelper.ConvertWorldToGrid(transform.position, _gizmoCellSize);
        if (fromTransform == _cell)
            return;

        Gizmos.color = CharacterSpawnGizmoColors.MarkerMismatch;
        Gizmos.DrawSphere(transform.position, 0.08f);
    }
}
