using UnityEngine;
using Sirenix.OdinInspector;

namespace IsoTilemap
{
    [CreateAssetMenu(fileName = "TileDefinition", menuName = "Iso/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        [HorizontalGroup("Row", Width = 90)]
        [PreviewField(90, ObjectFieldAlignment.Left), HideLabel]
        public Sprite thumbnail;

        [HorizontalGroup("Row"), VerticalGroup("Row/Info"), LabelWidth(70)]
        public string prefabId;

        [VerticalGroup("Row/Info"), LabelWidth(70)]
        public GameObject prefab;

        [VerticalGroup("Row/Info"), LabelWidth(70)]
        public string category;

        [VerticalGroup("Row/Info"), LabelWidth(70)]
        public Vector3Int size = Vector3Int.one;

        [FoldoutGroup("충돌·오클루전", expanded: true)]
        [InlineProperty, HideLabel]
        [OnValueChanged(nameof(PersistEditorChanges))]
        public TileOccupiedCellCollision occupied;

        [FoldoutGroup("충돌·오클루전")]
        [InlineProperty, HideLabel]
        [OnValueChanged(nameof(PersistEditorChanges))]
        public TileEdgeCollision edge;

        [FoldoutGroup("충돌·오클루전")]
        [Button("Floor"), HorizontalGroup("충돌·오클루전/Presets")]
        void ApplyFloorPreset() => ApplyPreset(
            occupied: new TileOccupiedCellCollision { providesLogicalFloor = true },
            edge: default);

        [FoldoutGroup("충돌·오클루전")]
        [Button("Wall"), HorizontalGroup("충돌·오클루전/Presets")]
        void ApplyWallPreset() => ApplyPreset(
            occupied: new TileOccupiedCellCollision { blocksPassageAndOcclusion = true },
            edge: default);

        [FoldoutGroup("충돌·오클루전")]
        [Button("EdgeWall"), HorizontalGroup("충돌·오클루전/Presets")]
        void ApplyEdgeWallPreset() => ApplyPreset(
            occupied: default,
            edge: new TileEdgeCollision { blocksPassageAndOcclusion = true });

        [FoldoutGroup("충돌·오클루전")]
        [Button("Slope/Physics"), HorizontalGroup("충돌·오클루전/Presets")]
        void ApplyPhysicsPreset() => ApplyPreset(
            occupied: new TileOccupiedCellCollision { usePhysicsCollider = true },
            edge: default);

        [FoldoutGroup("충돌·오클루전")]
        [Button("None"), HorizontalGroup("충돌·오클루전/Presets")]
        void ApplyNonePreset() => ApplyPreset(default, default);

        void ApplyPreset(TileOccupiedCellCollision occupied, TileEdgeCollision edge)
        {
            this.occupied = occupied;
            this.edge = edge;
            PersistEditorChanges();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (UnityEditor.BuildPipeline.isBuildingPlayer ||
                UnityEditor.EditorApplication.isUpdating)
                return;

            PersistEditorChanges();
        }

        void PersistEditorChanges()
        {
            if (this == null)
                return;

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}
