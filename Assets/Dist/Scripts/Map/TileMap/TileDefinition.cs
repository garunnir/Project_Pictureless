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

#if UNITY_EDITOR
        [FoldoutGroup("충돌·오클루전", expanded: true)]
        [PropertyOrder(-10)]
        [OnInspectorGUI]
        void DrawUnsavedChangesBanner()
        {
            if (!HasUnsavedChanges)
                return;

            UnityEditor.EditorGUILayout.HelpBox(
                "저장되지 않은 변경사항이 있습니다.",
                UnityEditor.MessageType.Warning);
        }

        [FoldoutGroup("충돌·오클루전")]
        [PropertyOrder(-9)]
        [Button("저장"), HorizontalGroup("충돌·오클루전/SaveBar", Width = 72)]
        [EnableIf(nameof(HasUnsavedChanges))]
        void SaveCollisionSettings() => SaveAssetToDisk();
#endif

        [FoldoutGroup("충돌·오클루전")]
        [PropertyOrder(0)]
        [InlineProperty, HideLabel]
        [OnValueChanged(nameof(MarkDirty))]
        public TileOccupiedCellCollision occupied;

        [FoldoutGroup("충돌·오클루전")]
        [PropertyOrder(1)]
        [InlineProperty, HideLabel]
        [OnValueChanged(nameof(MarkDirty))]
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
            MarkDirty();
        }

        void MarkDirty()
        {
#if UNITY_EDITOR
            if (this == null)
                return;

            UnityEditor.Undo.RecordObject(this, "TileDefinition 변경");
            UnityEditor.EditorUtility.SetDirty(this);
            RequestInspectorRepaint();
#endif
        }

#if UNITY_EDITOR
        bool HasUnsavedChanges => UnityEditor.EditorUtility.IsDirty(this);

        static void RequestInspectorRepaint()
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
        }

        void SaveAssetToDisk()
        {
            if (this == null)
                return;

            UnityEditor.Undo.FlushUndoRecordObjects();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            RequestInspectorRepaint();
        }
#endif
    }
}
