using UnityEngine;

// ============================================================
// LiquidAuthoringView — 에디터 물 저작 마커 (타일 모델·충돌·가려짐 밖)
// ============================================================
// 씬에 놓인 물 프리팹이 갖는 컴포넌트. TileView가 아니므로 TileData·점유·오클루osion에
// 절대 진입하지 않고, 저장 시 liquidAuthoringFaces로만 나가 액체 오버레이의 시드가 된다.
// Play 중에는 스폰되지 않는다 — 수면은 MapLiquidSurfaceRenderer가 그린다.
// 에디터 merged mesh 프리뷰는 MapLiquidAuthoringPreviewRenderer가 담당한다.
namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class LiquidAuthoringView : MapPlacedView
    {
        /// <summary>바닥면 앵커 셀(CellBelow). JSON liquidAuthoringFaces의 x,y,z와 같다.</summary>
        public Vector3Int AnchorCell => gridPos;

        /// <summary>액체가 담기는 walkable 셀 = 앵커의 CellAbove.</summary>
        public Vector3Int LiquidCell => gridPos + Vector3Int.up;

        [Tooltip("켜면 Play 최초 로드 시 이 마커에서 시드된 물이 FlowSolver dirty로 들어가 흌다. 끄면 정지 수면.")]
        [SerializeField] bool _simulateFlowOnLoad;

        public bool SimulateFlowOnLoad
        {
            get => _simulateFlowOnLoad;
            set => _simulateFlowOnLoad = value;
        }

        public bool IsAuthoringTarget()
        {
            if (string.IsNullOrEmpty(prefabId))
                return false;

            return TilePrefabDB.TryResolveDefinition(prefabId, out TileDefinition def)
                && TileFlags.IsLiquidAuthoring(def);
        }

        protected override void Reset()
        {
            TryResolvePrefabIdFromSource();
            ApplyEditorPose();
        }

        protected override void OnValidate()
        {
            ApplyEditorPose();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                MapLiquidAuthoringPreviewRenderer.RequestRefresh();
#endif
        }

        /// <summary>
        /// gridPos는 저장용 CellBelow 앵커 — 기즈모는 액체가 담기는 <see cref="LiquidCell"/>에 맞춘다.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (!drawGizmoGrid)
                return;

            TileHelper.DrawOccupiedCellWire(LiquidCell, SafeCellSize, gizmoGridColor, size);
        }

        /// <summary>물은 바닥 +Y 면에 저작한다 — TileView.HorizontalFace와 동일한 스냅 규칙.</summary>
        protected override void ApplyEditorPose()
        {
            float cs = SafeCellSize;
            if (FloorFacePicker.TryPickNearest(transform.position, cs, out var nearest))
                gridPos = nearest.Anchor;

            var key = new FloorFaceKey(gridPos, FloorFace.PosY);
            FloorFaceKey.GetWorldPose(key, cs, out Vector3 pos, out Quaternion rot);
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
