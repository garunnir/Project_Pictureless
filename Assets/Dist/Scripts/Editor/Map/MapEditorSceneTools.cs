#if UNITY_EDITOR
using System.IO;
using IsoTilemap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IsoTilemap.Editor
{
    // ============================================================
    // MapEditorSceneTools — 에디터 맵 JSON ↔ 씬 로드/저장 SSOT
    // ============================================================
    internal static class MapEditorSceneTools
    {
        const string LastBrowseKey = "Dist.Map.EditorLoad.LastBrowseDir";

        public const string LoadInOpenSceneMenu = "Dist/Map/Load Map In Open Scene";
        public const string SaveFromOpenSceneMenu = "Dist/Map/Save Map From Open Scene";

        public static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static TileMapManager FindManagerInOpenScenes() =>
            Object.FindFirstObjectByType<TileMapManager>(FindObjectsInactive.Include);

        public static MapFileLoader ResolveLoader(TileMapManager manager) =>
            manager != null ? manager.EditorLoader : null;

        public static MapFileSaver ResolveSaver(TileMapManager manager) =>
            manager != null ? manager.EditorSaver : null;

        public static TileMapManager ResolveManager(MapFileLoader loader)
        {
            if (loader == null)
                return null;

            if (loader.TryGetComponent(out TileMapManager onSelf))
                return onSelf;

            TileMapManager inParent = loader.GetComponentInParent<TileMapManager>();
            if (inParent != null)
                return inParent;

            TileMapManager[] managers = Object.FindObjectsByType<TileMapManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].EditorLoader == loader)
                    return managers[i];
            }

            return null;
        }

        public static TileMapManager ResolveManager(MapFileSaver saver)
        {
            if (saver == null)
                return null;

            if (saver.TryGetComponent(out TileMapManager onSelf))
                return onSelf;

            TileMapManager inParent = saver.GetComponentInParent<TileMapManager>();
            if (inParent != null)
                return inParent;

            TileMapManager[] managers = Object.FindObjectsByType<TileMapManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].EditorSaver == saver)
                    return managers[i];
            }

            return null;
        }

        public static bool TryLoadFromLoader(MapFileLoader loader, string absoluteJsonPath = null)
        {
            TileMapManager manager = ResolveManager(loader);
            if (manager == null)
            {
                Debug.LogError("[MapEditorSceneTools] TileMapManager를 찾을 수 없습니다.", loader);
                return false;
            }

            return TryLoadInOpenScene(manager, absoluteJsonPath);
        }

        public static bool TrySaveFromSaver(MapFileSaver saver)
        {
            if (saver == null)
            {
                Debug.LogError("[MapEditorSceneTools] MapFileSaver가 없습니다.");
                return false;
            }

            saver.SaveFromSceneInEditor();

            TileMapManager manager = ResolveManager(saver);
            if (manager != null)
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            return true;
        }

        public static bool TryLoadInOpenScene(TileMapManager manager = null, string absoluteJsonPath = null)
        {
            manager ??= FindManagerInOpenScenes();
            if (manager == null)
            {
                Debug.LogError("[MapEditorSceneTools] 열린 씬에서 TileMapManager를 찾을 수 없습니다.");
                return false;
            }

            manager.LoadInEditor(absoluteJsonPath);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            return manager.Model != null;
        }

        public static bool TrySaveFromOpenScene(TileMapManager manager = null)
        {
            manager ??= FindManagerInOpenScenes();
            MapFileSaver saver = manager != null ? ResolveSaver(manager) : null;
            return TrySaveFromSaver(saver);
        }

        public static bool TryBrowseAndSetMapPath(MapFileLoader loader, out string selectedPath)
        {
            selectedPath = null;
            TileMapManager manager = ResolveManager(loader);
            MapFileSaver saver = manager != null ? ResolveSaver(manager) : null;
            return TryBrowseAndSetMapPath(loader, saver, out selectedPath);
        }

        public static bool TryBrowseAndSetMapPath(
            MapFileLoader loader,
            MapFileSaver saver,
            out string selectedPath)
        {
            selectedPath = null;
            if (loader == null)
                return false;

            string initialDir = EditorPrefs.GetString(LastBrowseKey, ProjectRoot);
            if (!Directory.Exists(initialDir))
                initialDir = ProjectRoot;

            string picked = EditorUtility.OpenFilePanel("맵 JSON 선택", initialDir, "json");
            if (string.IsNullOrEmpty(picked))
                return false;

            EditorPrefs.SetString(LastBrowseKey, Path.GetDirectoryName(picked));
            SyncMapPath(loader, saver, MakeProjectRelativeFileName(picked));
            selectedPath = picked;
            return true;
        }

        static void SyncMapPath(MapFileLoader loader, MapFileSaver saver, string fileName)
        {
            if (loader != null)
            {
                Undo.RecordObject(loader, "Set Map JSON Path");
                loader.FileName = fileName;
                loader.UsePersistentPath = false;
                EditorUtility.SetDirty(loader);
            }

            if (saver != null)
            {
                Undo.RecordObject(saver, "Set Map JSON Path");
                saver.FileName = fileName;
                saver.UsePersistentPath = false;
                EditorUtility.SetDirty(saver);
            }
        }

        static string MakeProjectRelativeFileName(string absolutePath)
        {
            string full = Path.GetFullPath(absolutePath);
            string root = ProjectRoot;
            if (full.StartsWith(root))
            {
                string relative = full.Substring(root.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return relative.Replace('\\', '/');
            }

            return Path.GetFileName(full);
        }

        public static void DrawEditorPathHelpBox(string resolvedPath)
        {
            EditorGUILayout.HelpBox(
                $"경로:\n{resolvedPath}",
                !string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath)
                    ? MessageType.None
                    : MessageType.Warning);
        }

        public static void DrawPlayModeNotice()
        {
            if (!Application.isPlaying)
                return;

            EditorGUILayout.HelpBox(
                "플레이 모드에서는 런타임 로드/저장이 사용됩니다. 에디터 작업은 플레이 종료 후 사용하세요.",
                MessageType.Info);
        }
    }
}
#endif
