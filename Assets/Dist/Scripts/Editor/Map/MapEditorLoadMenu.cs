#if UNITY_EDITOR
using UnityEditor;

namespace IsoTilemap.Editor
{
    // ============================================================
    // MapEditorLoadMenu — Dist/Map 에디터 메뉴
    // ============================================================
    static class MapEditorLoadMenu
    {
        [MenuItem(MapEditorSceneTools.LoadInOpenSceneMenu)]
        static void LoadMapInOpenScene()
        {
            MapEditorSceneTools.TryLoadInOpenScene();
        }

        [MenuItem(MapEditorSceneTools.SaveFromOpenSceneMenu)]
        static void SaveMapFromOpenScene()
        {
            MapEditorSceneTools.TrySaveFromOpenScene();
        }

        [MenuItem(MapEditorSceneTools.LoadInOpenSceneMenu, true)]
        [MenuItem(MapEditorSceneTools.SaveFromOpenSceneMenu, true)]
        static bool ValidateMapMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   MapEditorSceneTools.FindManagerInOpenScenes() != null;
        }
    }
}
#endif
