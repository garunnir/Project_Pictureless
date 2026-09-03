#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.Editor
{
    // ============================================================
    // MapFileSaverEditor — 에디터 맵 저장 인스펙터 UI
    // ============================================================
    [CustomEditor(typeof(MapFileSaver))]
    public sealed class MapFileSaverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var saver = (MapFileSaver)target;
            TileMapManager manager = MapEditorSceneTools.ResolveManager(saver);
            MapFileLoader loader = manager != null ? MapEditorSceneTools.ResolveLoader(manager) : null;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Editor Save", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                MapEditorSceneTools.DrawEditorPathHelpBox(saver.ResolveFullPath());

                if (GUILayout.Button("Save Map To JSON", GUILayout.Height(24f)))
                    MapEditorSceneTools.TrySaveFromSaver(saver);

                if (loader != null &&
                    loader.FileName != saver.FileName)
                {
                    EditorGUILayout.HelpBox(
                        "MapFileLoader.fileName과 다릅니다. Browse는 Loader 인스펙터에서 하면 둘 다 동기화됩니다.",
                        MessageType.Warning);
                }
            }

            MapEditorSceneTools.DrawPlayModeNotice();
        }
    }
}
#endif
