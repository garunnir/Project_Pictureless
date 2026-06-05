#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IsoTilemap.EditorTools
{
    public static class MapObstacleMigrationEditor
    {
        const string LegacyPattern = "\"tileType\": 3";
        const string WallPattern = "\"tileType\": 2";

        [MenuItem("Tools/Map/Rewrite legacy tileType 3 → 2 in JSON maps")]
        static void RewriteLegacyObstacleTileType()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[MapObstacleMigration] project root를 찾을 수 없습니다.");
                return;
            }

            string[] files = Directory.GetFiles(projectRoot, "*.json", SearchOption.AllDirectories);
            int changedFiles = 0;
            int changedTiles = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                if (path.Contains($"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}") ||
                    path.Contains($"{Path.DirectorySeparatorChar}Packages{Path.DirectorySeparatorChar}"))
                    continue;

                string text = File.ReadAllText(path);
                if (!text.Contains(LegacyPattern))
                    continue;

                int count = 0;
                int index = 0;
                while ((index = text.IndexOf(LegacyPattern, index, System.StringComparison.Ordinal)) >= 0)
                {
                    count++;
                    index += LegacyPattern.Length;
                }

                string rewritten = text.Replace(LegacyPattern, WallPattern);
                File.WriteAllText(path, rewritten);
                changedFiles++;
                changedTiles += count;
                Debug.Log($"[MapObstacleMigration] {path} — {count}건 rewrite");
            }

            Debug.Log($"[MapObstacleMigration] 완료: 파일 {changedFiles}개, tileType 3→2 {changedTiles}건");
        }
    }
}
#endif
