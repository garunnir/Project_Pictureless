#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.Editor
{
    [CustomEditor(typeof(TileView))]
    public class TileViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawSaveReadinessWarnings((TileView)target);
            DrawDefaultInspector();
        }

        static void DrawSaveReadinessWarnings(TileView view)
        {
            if (view == null)
                return;

            foreach (var (messageType, message) in EvaluateSaveReadiness(view))
                EditorGUILayout.HelpBox(message, messageType);
        }

        static List<(MessageType type, string message)> EvaluateSaveReadiness(TileView view)
        {
            var warnings = new List<(MessageType, string)>();

            if (string.IsNullOrEmpty(view.prefabId))
            {
                warnings.Add((
                    MessageType.Warning,
                    "prefabId가 비어 있어 Save Map To JSON 시 이 TileView가 누락됩니다."));
            }

            var slot = ResolveEffectivePlacementSlot(view);
            if (slot == TilePlacementSlot.None)
            {
                warnings.Add((
                    MessageType.Warning,
                    "placementSlot이 None이고 prefabId prefix로도 슬롯을 추론할 수 없어 Save Map To JSON 시 누락됩니다. " +
                    "(예: Furniture/... 는 자동 추론되지 않음 — placementSlot을 직접 지정하세요.)"));
                return warnings;
            }

            if (!string.IsNullOrEmpty(view.prefabId) &&
                (!TilePrefabDB.TryResolveDefinition(view.prefabId, out var def) || def == null))
            {
                warnings.Add((
                    MessageType.Warning,
                    $"prefabId '{view.prefabId}'에 대한 TileDefinition을 찾을 수 없어 Save Map To JSON 시 누락됩니다."));
            }

            return warnings;
        }

        static TilePlacementSlot ResolveEffectivePlacementSlot(TileView view)
        {
            var slot = view.placementSlot;
            if (slot == TilePlacementSlot.None &&
                !string.IsNullOrEmpty(view.prefabId) &&
                view.prefabId.StartsWith("Slope/", StringComparison.Ordinal))
            {
                slot = TilePlacementSlot.OccupiedCell;
            }

            if (slot == TilePlacementSlot.None &&
                !string.IsNullOrEmpty(view.prefabId))
            {
                slot = TileIdentityUtil.InferSlotFromPrefabId(view.prefabId);
            }

            return slot;
        }
    }
}
#endif
