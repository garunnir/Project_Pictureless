// ============================================================
// CharacterSpawnPoint — IsoLand 배치용 셀 마커 (Play는 셀만 읽음)
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterSpawnPoint : MonoBehaviour
{
    [SerializeField] Vector3Int _cell;
    [SerializeField] Vector3Int _gridFootprint = CharacterGridFootprintDefaults.Default;
    [SerializeField] CharacterSpawnRole _gizmoRole = CharacterSpawnRole.Npc;
    [SerializeField, Min(0.0001f)] float _gizmoCellSize = 1f;

    static readonly List<Vector3Int> FootprintCellsScratch = new();

    public Vector3Int Cell => _cell;
    public Vector3Int GridFootprint => CharacterGridFootprintDefaults.Clamp(_gridFootprint);
    public CharacterSpawnRole GizmoRole => _gizmoRole;

    void OnValidate()
    {
        _gizmoCellSize = Mathf.Max(0.0001f, _gizmoCellSize);
        _gridFootprint = CharacterGridFootprintDefaults.Clamp(_gridFootprint);
        _cell = TileHelper.ConvertWorldToGrid(transform.position, _gizmoCellSize);
    }

    void OnDrawGizmos()
    {
        Vector3Int footprint = GridFootprint;
        Color color = CharacterSpawnGizmoColors.ForRole(_gizmoRole);
        FootprintCellsScratch.Clear();
        CharacterOccupiedCellUtil.AppendOccupiedCells(_cell, footprint, FootprintCellsScratch);
        for (int i = 0; i < FootprintCellsScratch.Count; i++)
            TileHelper.DrawOccupiedCellWire(FootprintCellsScratch[i], _gizmoCellSize, color);

        Vector3Int fromTransform = TileHelper.ConvertWorldToGrid(transform.position, _gizmoCellSize);
        if (fromTransform == _cell)
            return;

        Gizmos.color = CharacterSpawnGizmoColors.MarkerMismatch;
        Gizmos.DrawSphere(transform.position, 0.08f);
    }
}
