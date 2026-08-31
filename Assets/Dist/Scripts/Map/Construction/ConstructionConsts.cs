// ============================================================
// ConstructionConsts — 본편 건설·셀 타겟 프리뷰 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class ConstructionConsts
    {
        public const string SlotHorizontalFace = "HorizontalFace";
        public const string SlotOccupiedCell = "OccupiedCell";
        public const string SlotVerticalFace = "VerticalFace";

        public const float TargetPreviewAlpha = 0.55f;
        public static readonly Color TargetPreviewValid =
            new Color(0.28f, 0.82f, 0.32f, TargetPreviewAlpha);
        public static readonly Color TargetPreviewInvalid =
            new Color(0.88f, 0.22f, 0.18f, TargetPreviewAlpha);

        public const string MeshVisualChildName = "MeshVisual";
        public const string SpriteVisualChildName = "SpriteVisual";
        public const string TargetPreviewResourcesName = "Farming/FarmPlantTargetPreview";

        public const float CellArriveStoppingCellFraction = 0.55f;
        public const float DefaultWorkDurationSeconds = 1f;
        public const float MinutesToRealtimeSeconds = 1f;

        public const string OverlayShaderUrpUnlit = "Universal Render Pipeline/Unlit";
        public const string OverlayShaderUnlitColor = "Unlit/Color";
    }
}
