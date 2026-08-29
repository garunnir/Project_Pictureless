using IsoTilemap;
using System;
using UnityEngine;

public class CharacterState : MonoBehaviour
{
    private IWorldGrid _worldGrid;

    public Vector3 SightDir { get; private set; } = Vector3.zero;
    /// <summary>조준으로 정해진 상호작용 방향. 조준 해제 후에도 유지.</summary>
    public Vector3 InteractionDir { get; private set; } = Vector3.zero;
    /// <summary>조준 시점의 상호작용 SphereCast 최대 거리. 조준 해제 후에도 유지.</summary>
    public float InteractionReach { get; private set; }
    /// <summary>조준 레이의 끝(시야) 월드 위치.</summary>
    public Vector3 AimWorldPoint { get; private set; } = Vector3.zero;
    /// <summary>플레이어 몸의 월드 위치. 조준이 아닐 때 오클루전 등의 기준점.</summary>
    public Vector3 BodyWorldPoint { get; private set; } = Vector3.zero;
    public Vector3 MoveDir { get; private set; } = Vector3.zero;
    public Vector3Int GridPos { get; private set; } = Vector3Int.zero;

    /// <summary>현재 몸 위치 기준 그리드 셀. <see cref="GridPos"/>보다 최신 월드 좌표를 우선합니다.</summary>
    public Vector3Int ResolveCurrentGridCell()
    {
        Vector3 world = BodyWorldPoint.sqrMagnitude > 1e-6f
            ? BodyWorldPoint
            : transform.position;

        return ResolveGridCell(world);
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

    internal void UpdateGridPos(Vector3 worldPos)
    {
        BodyWorldPoint = worldPos;

        Vector3Int gridPos = _worldGrid != null
            ? _worldGrid.WorldToCell(worldPos)
            : TileHelper.ConvertWorldToGrid(worldPos, 1f);

        if (GridPos != gridPos)
        {
            GridPos = gridPos;
            GridPosChanged?.Invoke(gridPos);
            if (Config.DebugMode.PlayerPosUpdate)
                Debug.Log($"Player GridPos Changed: {GridPos}");
        }

        WorldPoseChanged?.Invoke(worldPos);
    }
}
