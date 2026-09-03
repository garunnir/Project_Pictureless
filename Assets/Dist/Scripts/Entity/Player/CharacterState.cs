using IsoTilemap;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterState : MonoBehaviour
{
    private IWorldGrid _worldGrid;
    private CharacterFootprintHost _footprintHost;

    public Vector3 SightDir { get; private set; } = Vector3.zero;
    /// <summary>조준으로 정해진 상호작용 방향. 조준 해제 후에도 유지.</summary>
    public Vector3 InteractionDir { get; private set; } = Vector3.zero;
    /// <summary>조준 시점의 상호작용 SphereCast 최대 거리. 조준 해제 후에도 유지.</summary>
    public float InteractionReach { get; private set; }
    /// <summary>조준 레이의 끝(시야) 월드 위치.</summary>
    public Vector3 AimWorldPoint { get; private set; } = Vector3.zero;
    /// <summary>플레이어 몸의 월드 위치.</summary>
    public Vector3 BodyWorldPoint { get; private set; } = Vector3.zero;

    /// <summary>
    /// 타일 가시성·오클루전 evaluate 월드 기준점. 조준 중이면 <see cref="AimWorldPoint"/>, 아니면 <see cref="BodyWorldPoint"/>.
    /// 발높이 보정은 <see cref="PlayerVisibilityWorldResolve"/>에서 비조준 시만 적용합니다.
    /// </summary>
    public Vector3 ResolveVisibilityWorldPoint() =>
        IsAiming ? AimWorldPoint : BodyWorldPoint;
    public Vector3 MoveDir { get; private set; } = Vector3.zero;
    /// <summary>발밑 그리드 셀 (<see cref="CharacterFeetPose"/> + <see cref="MapCollisionGrid"/>).</summary>
    public Vector3Int GridPos { get; private set; } = Vector3Int.zero;

    /// <summary>현재 발밑 그리드 셀. <see cref="GridPos"/>와 동일 계약.</summary>
    public Vector3Int ResolveCurrentGridCell()
    {
        Vector3 world = BodyWorldPoint.sqrMagnitude > 1e-6f
            ? BodyWorldPoint
            : transform.position;

        return ResolveFeetGridCell(world);
    }

    public Vector3Int ResolveGridCell(Vector3 worldPos) =>
        _worldGrid != null
            ? _worldGrid.WorldToCell(worldPos)
            : TileHelper.ConvertWorldToGrid(worldPos, 1f);

    public bool IsAiming { get; private set; }
    public bool IsStealth { get; private set; }
    public bool IsWading { get; private set; }
    public bool IsSwimming { get; private set; }
    public bool IsDiving { get; private set; }
    /// <summary>Dive 수직 입력 (-1..1). CharacterSwimHost가 넣는다.</summary>
    public float SwimVerticalInput { get; private set; }
    public event Action<Vector3Int> GridPosChanged;
    public event Action<bool> StealthChanged;
    public event Action SwimModeChanged;
    /// <summary>매 <see cref="UpdateGridPos"/> 호출 때마다(셀 변경 없이 포함) 발생.</summary>
    public event Action<Vector3> WorldPoseChanged;
    public event Action<Vector3> AimWorldPointChanged;

    /// <summary><see cref="TileMapManager"/>가 맵 로드 후 바인딩합니다.</summary>
    public void BindWorldGrid(IWorldGrid worldGrid) => _worldGrid = worldGrid;

    /// <summary><see cref="CharacterDefinitionBinder"/>가 footprint를 적용한 뒤 바인딩합니다.</summary>
    public void BindFootprint(CharacterFootprintHost footprintHost) =>
        _footprintHost = footprintHost;

    public Vector3Int GridFootprint =>
        _footprintHost != null
            ? _footprintHost.GridFootprint
            : CharacterGridFootprintDefaults.Default;

    public void AppendOccupiedCells(ICollection<Vector3Int> cells) =>
        CharacterOccupiedCellUtil.AppendOccupiedCells(GridPos, GridFootprint, cells);

    internal void SetMoveDir(Vector3 desiredMove)
    {
        if (desiredMove == Vector3.zero) return;
        MoveDir = desiredMove;
    }

    internal void ClearMoveDir() => MoveDir = Vector3.zero;

    internal void SetAimDir(Vector3 dir, Vector3 aimWorldPoint, float interactionReach)
    {
        if (dir == Vector3.zero) return;
        SightDir = dir;
        InteractionDir = dir;
        InteractionReach = interactionReach;
        AimWorldPoint = aimWorldPoint;
        IsAiming = true;
        AimWorldPointChanged?.Invoke(aimWorldPoint);
    }

    public bool HasInteractionFocus =>
        InteractionDir.sqrMagnitude > 1e-4f && InteractionReach > 1e-4f;

    internal Vector3 GetFacingDir()
    {
        if (IsAiming)
            return SightDir;
        return MoveDir;
    }

    internal void ClearAim()
    {
        IsAiming = false;
        InteractionDir = Vector3.zero;
        InteractionReach = 0f;
        AimWorldPoint = Vector3.zero;
        AimWorldPointChanged?.Invoke(Vector3.zero);
    }

    internal void SetStealth(bool value)
    {
        if (IsStealth == value)
            return;

        IsStealth = value;
        StealthChanged?.Invoke(value);
    }

    internal void SetSwimMode(bool wading, bool swimming, bool diving)
    {
        if (IsWading == wading && IsSwimming == swimming && IsDiving == diving)
            return;

        IsWading = wading;
        IsSwimming = swimming;
        IsDiving = diving;
        SwimModeChanged?.Invoke();
    }

    internal void SetSwimVerticalInput(float vertical01) =>
        SwimVerticalInput = Mathf.Clamp(vertical01, -1f, 1f);

    /// <summary>저장 스냅샷 등 — 월드 좌표를 즉시 반영하고 발밑 그리드 셀을 갱신합니다.</summary>
    public void SnapWorldPosition(Vector3 worldPos) => UpdateGridPos(worldPos);

    internal void UpdateGridPos(Vector3 worldPos)
    {
        BodyWorldPoint = worldPos;

        Vector3Int gridPos = ResolveFeetGridCell(worldPos);

        if (GridPos != gridPos)
        {
            GridPos = gridPos;
            GridPosChanged?.Invoke(gridPos);
            if (Config.DebugMode.PlayerPosUpdate)
                Debug.Log($"Player GridPos Changed: {GridPos}");
        }

        WorldPoseChanged?.Invoke(worldPos);
    }

    Vector3Int ResolveFeetGridCell(Vector3 bodyWorld)
    {
        float cellSize = _worldGrid != null ? _worldGrid.CellSize : 1f;
        float feetOffset = CharacterFeetPose.GetFeetOffset(transform);
        MapCollisionGrid.FeetCell feetCell =
            MapCollisionGrid.ResolveFeetCell(bodyWorld, feetOffset, cellSize);
        return MapCollisionGrid.ToGrid(feetCell);
    }
}
