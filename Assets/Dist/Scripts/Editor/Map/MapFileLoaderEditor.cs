#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.Editor
{
    // ============================================================
    // MapFileLoaderEditor — 에디터 맵 로드 인스펙터 UI
    // ============================================================
    [CustomEditor(typeof(MapFileLoader))]
    public sealed class MapFileLoaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var loader = (MapFileLoader)target;
            TileMapManager manager = MapEditorSceneTools.ResolveManager(loader);
            MapFileSaver saver = manager != null ? MapEditorSceneTools.ResolveSaver(manager) : null;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Editor Load", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                MapEditorSceneTools.DrawEditorPathHelpBox(loader.ResolveFullPath());

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Browse JSON…"))
                    {
                        if (MapEditorSceneTools.TryBrowseAndSetMapPath(loader, saver, out _))
                            Repaint();
                    }

                    using (new EditorGUI.DisabledScope(manager == null))
                    {
                        if (GUILayout.Button("Load Map In Scene", GUILayout.Height(24f)))
                            MapEditorSceneTools.TryLoadFromLoader(loader);
                    }
                }

                if (manager == null)
                {
                    EditorGUILayout.HelpBox(
                        "TileMapManager가 연결되어 있지 않습니다. Load Map In Scene을 사용할 수 없습니다.",
                        MessageType.Warning);
                }
            }

            MapEditorSceneTools.DrawPlayModeNotice();
        }
    }
}
#endif
