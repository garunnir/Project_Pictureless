#if UNITY_EDITOR
using System.Collections.Generic;
using IsoTilemap;
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.Editor
{
    // ============================================================
    // MapPlacedViewSceneSnap — 씬 Transform 드래그 종료 후 그리드 스냅
    // ============================================================
    [InitializeOnLoad]
    static class MapPlacedViewSceneSnap
    {
        static readonly HashSet<MapPlacedView> Pending = new();
        static bool _isSnapping;
        static bool _wasDragging;
        static bool _wasEditingTextField;

        static MapPlacedViewSceneSnap()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGui;
        }

        static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (_isSnapping || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                    continue;

                stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var evt);
                MapPlacedView view = ResolveMapPlacedView(evt.instanceId);
                if (view != null)
                    Pending.Add(view);
            }
        }

        static MapPlacedView ResolveMapPlacedView(int instanceId)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceId);
            return obj switch
            {
                MapPlacedView placed => placed,
                Transform t => t.GetComponent<MapPlacedView>(),
                GameObject go => go.GetComponent<MapPlacedView>(),
                _ => null
            };
        }

        static void OnSceneGui(SceneView _)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0)
                TryFlushPending();
        }

        static void OnEditorUpdate()
        {
            bool dragging = GUIUtility.hotControl != 0;
            bool editing = EditorGUIUtility.editingTextField;

            if ((_wasDragging && !dragging) || (_wasEditingTextField && !editing))
                TryFlushPending();

            _wasDragging = dragging;
            _wasEditingTextField = editing;
        }

        static void TryFlushPending()
        {
            if (Pending.Count == 0 || _isSnapping)
                return;

            var views = new MapPlacedView[Pending.Count];
            Pending.CopyTo(views);
            Pending.Clear();

            _isSnapping = true;
            try
            {
                foreach (MapPlacedView view in views)
                {
                    if (view == null)
                        continue;

                    Undo.RecordObject(view, "Snap Map Placed View");
                    Undo.RecordObject(view.transform, "Snap Map Placed View");
                    view.SnapEditorPoseToGrid();
                    EditorUtility.SetDirty(view);
                    EditorUtility.SetDirty(view.transform);
                }
            }
            finally
            {
                _isSnapping = false;
            }
        }
    }
}
#endif
