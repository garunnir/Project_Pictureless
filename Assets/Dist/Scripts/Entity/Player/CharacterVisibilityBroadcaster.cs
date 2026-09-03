// ============================================================

// CharacterVisibilityBroadcaster — 플레이어 위치 기준 벽 캐릭터 오클루전 갱신

// ============================================================

using IsoTilemap;

using UnityEngine;



[DefaultExecutionOrder(-99)]
public class CharacterVisibilityBroadcaster : MonoBehaviour

{

    private CharacterState _characterState;



    [SerializeField] private TileMapManager _tileMapManager;



    [Tooltip("Play 전 Inspector. 끄면 플레이어 주변 마스크 추가 숨김만 비활성(BFS·거리 오클루전은 유지).")]

    [SerializeField] private bool _playerProximityMaskEnabled = true;



    [SerializeField] private OcclusionProximitySettings _occlusionSettings =

        OcclusionProximitySettings.DefaultUnity;



    private Vector3 _lastOcclusionWorld = new Vector3(float.NaN, float.NaN, float.NaN);

    private bool _hasLastOcclusionWorld;

    private bool _hasLastVisibilityCtx;

    private bool _lastIsPlayerOutdoor;

    private int _lastPlayerBuildingId;

    private int _lastPlayerFloorCellY;



    public void BindPlayerState(CharacterState state)
    {
        _characterState = state;
        if (isActiveAndEnabled)
            SyncSettingsCellFromMapGrid();
    }

    private void Awake()

    {

        if (_characterState == null)

            _characterState = GetComponent<CharacterState>();

        if (_tileMapManager == null)

            _tileMapManager = FindFirstObjectByType<TileMapManager>();

        if (_tileMapManager == null)

            Debug.LogWarning("CharacterVisibilityBroadcaster: TileMapManager 참조 없음.");

    }



    private void OnEnable()

    {

        if (_characterState != null)

            SyncSettingsCellFromMapGrid();

    }



    private void OnDisable()

    {

        _hasLastOcclusionWorld = false;

        _hasLastVisibilityCtx = false;

    }



#if UNITY_EDITOR

    private void OnValidate()

    {

        if (_characterState == null)

            _characterState = GetComponent<CharacterState>();



        SyncSettingsCellFromMapGrid();

    }

#endif



    private void SyncSettingsCellFromMapGrid()

    {

        if (_tileMapManager?.WorldGrid == null) return;



        OcclusionProximitySettings settings = _occlusionSettings;

        settings.CellSize = _tileMapManager.WorldGrid.CellSize;

        _occlusionSettings = settings;

    }



    private void LateUpdate() => ApplyNow();



    /// <summary>틱 오케스트레이션·진단용. LateUpdate와 동일 규칙.</summary>

    public void ApplyNow()

    {

        if (_characterState == null)

            return;



        PlayerVisibilityWorldResolve.ResolveEvaluation(
            _characterState,
            _tileMapManager?.FloorVisibilityPolicy,
            bodyHeightOffsetWorld: 0f,
            out Vector3 visibilityWorld,
            out Vector3Int evaluationCell,
            out Vector3Int footprint);

        bool visibilityCtxChanged = HasVisibilityContextChanged(
            visibilityWorld, evaluationCell, footprint);

        bool positionUnchanged = _hasLastOcclusionWorld &&
                                 (visibilityWorld - _lastOcclusionWorld).sqrMagnitude <= 1e-8f;

        if (!visibilityCtxChanged && positionUnchanged)
            return;

        _lastOcclusionWorld = visibilityWorld;
        _hasLastOcclusionWorld = true;
        BroadcastOcclusion(evaluationCell, footprint);

    }



    private Vector3Int _lastEvaluationCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private Vector3Int _lastFootprint = CharacterGridFootprintDefaults.Default;

    bool HasVisibilityContextChanged(
        Vector3 playerWorld,
        Vector3Int evaluationCell,
        Vector3Int footprint)
    {
        if (_tileMapManager == null ||
            !_tileMapManager.TryResolveFloorVisibilityContext(
                playerWorld,
                evaluationCell,
                footprint,
                out FloorVisibilityContext ctx))
        {
            return !_hasLastVisibilityCtx;
        }

        bool changed = !_hasLastVisibilityCtx ||
                       ctx.IsPlayerOutdoor != _lastIsPlayerOutdoor ||
                       ctx.PlayerBuildingId != _lastPlayerBuildingId ||
                       ctx.PlayerFloorCellY != _lastPlayerFloorCellY ||
                       evaluationCell != _lastEvaluationCell ||
                       footprint != _lastFootprint;

        _hasLastVisibilityCtx = true;
        _lastIsPlayerOutdoor = ctx.IsPlayerOutdoor;
        _lastPlayerBuildingId = ctx.PlayerBuildingId;
        _lastPlayerFloorCellY = ctx.PlayerFloorCellY;
        _lastEvaluationCell = evaluationCell;
        _lastFootprint = footprint;

        return changed;
    }

    /// <summary>
    /// <see cref="PlayerVisibilityWorldResolve"/> 기준점으로 BFS·거리 오클루전 갱신.
    /// </summary>
    private void BroadcastOcclusion(Vector3Int evaluationCell, Vector3Int footprint)

    {

        if (_characterState == null) return;



        SyncSettingsCellFromMapGrid();



        OcclusionProximitySettings settings = _occlusionSettings;



        if (_tileMapManager?.WorldGrid != null)

            settings.CellSize = Mathf.Max(1e-4f, _tileMapManager.WorldGrid.CellSize);



        settings.PlayerProximityMaskEnabled = _playerProximityMaskEnabled;

        NormalizeSettings(ref settings);

        _occlusionSettings = settings;



        _tileMapManager?.UpdateWallOcclusionFromPlayer(
            _lastOcclusionWorld,
            evaluationCell,
            settings,
            footprint);

    }



    private static void NormalizeSettings(ref OcclusionProximitySettings s)

    {

        if (s.OcclusionFullWithinDistance > s.OcclusionNoneBeyondDistance)

        {

            float t = s.OcclusionFullWithinDistance;

            s.OcclusionFullWithinDistance = s.OcclusionNoneBeyondDistance;

            s.OcclusionNoneBeyondDistance = t;

        }



        if (Mathf.Abs(s.OcclusionNoneBeyondDistance - s.OcclusionFullWithinDistance) < 1e-4f)

            s.OcclusionNoneBeyondDistance = s.OcclusionFullWithinDistance + 1e-3f;

    }

}


