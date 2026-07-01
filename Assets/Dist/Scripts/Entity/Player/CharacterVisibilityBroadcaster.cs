// ============================================================
// CharacterVisibilityBroadcaster — 플레이어 위치 기준 벽 캐릭터 오클루전 갱신
// ============================================================
using IsoTilemap;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
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

    private void Awake()
    {
        _characterState = GetComponent<CharacterState>();
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

    private void LateUpdate()
    {
        if (_characterState == null)
            return;

        Vector3 occlusionWorld = _characterState.IsAiming
            ? _characterState.AimWorldPoint
            : _characterState.BodyWorldPoint;

        bool visibilityCtxChanged = HasVisibilityContextChanged(occlusionWorld);
        bool positionUnchanged = _hasLastOcclusionWorld &&
                                 (occlusionWorld - _lastOcclusionWorld).sqrMagnitude <= 1e-8f;

        if (!visibilityCtxChanged && positionUnchanged)
            return;

        _lastOcclusionWorld = occlusionWorld;
        _hasLastOcclusionWorld = true;
        BroadcastOcclusion();
    }

    bool HasVisibilityContextChanged(Vector3 playerWorld)
    {
        if (_tileMapManager == null ||
            !_tileMapManager.TryResolveFloorVisibilityContext(playerWorld, out FloorVisibilityContext ctx))
        {
            return !_hasLastVisibilityCtx;
        }

        bool changed = !_hasLastVisibilityCtx ||
                       ctx.IsPlayerOutdoor != _lastIsPlayerOutdoor ||
                       ctx.PlayerBuildingId != _lastPlayerBuildingId ||
                       ctx.PlayerFloorCellY != _lastPlayerFloorCellY;

        _hasLastVisibilityCtx = true;
        _lastIsPlayerOutdoor = ctx.IsPlayerOutdoor;
        _lastPlayerBuildingId = ctx.PlayerBuildingId;
        _lastPlayerFloorCellY = ctx.PlayerFloorCellY;
        return changed;
    }

    /// <summary>조준 중에는 <see cref="CharacterState.AimWorldPoint"/>, 아니면 <see cref="CharacterState.BodyWorldPoint"/>로 BFS·거리 오클루전 갱신.</summary>
    private void BroadcastOcclusion()
    {
        if (_characterState == null) return;

        SyncSettingsCellFromMapGrid();

        OcclusionProximitySettings settings = _occlusionSettings;

        if (_tileMapManager?.WorldGrid != null)
            settings.CellSize = Mathf.Max(1e-4f, _tileMapManager.WorldGrid.CellSize);

        settings.PlayerProximityMaskEnabled = _playerProximityMaskEnabled;
        NormalizeSettings(ref settings);
        _occlusionSettings = settings;

        int playerFloorCellY = _tileMapManager != null
            ? _tileMapManager.ResolvePlayerFloorCellY(_characterState.BodyWorldPoint)
            : TileHelper.ConvertWorldToGrid(_characterState.BodyWorldPoint, settings.CellSize).y;

        _tileMapManager.Model?.UpdateOcclusionFromPlayerWorld(
            _lastOcclusionWorld, playerFloorCellY, settings);
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
