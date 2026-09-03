#if UNITY_EDITOR
using System.IO;
using System.Text;
using IsoTilemap;
using UnityEditor;
using UnityEngine;

namespace Dist.Editor.Map
{
    static class MapVaultProbeDiagnosticMenu
    {
        const string MapFileName = "map01.json";

        [MenuItem("Dist/Map/Diagnose Vault Probe (map01)")]
        static void DiagnoseFromFile()
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MapFileName));
                if (!File.Exists(path))
                {
                    Debug.LogError($"[VaultDiag] Map not found: {path}");
                    return;
                }

                var pipeline = new MapLoadPipeline(
                    new TileMapSerializer(),
                    new TileMapModelBuilder(),
                    new TileMapDtoMapper());
                MapLoadResult loaded = pipeline.Load(path);
                if (loaded.Model is not TileMapModel model)
                {
                    Debug.LogError("[VaultDiag] Model build failed.");
                    return;
                }

                var index = FloorMapIndex.FromModel(model);
                var sb = new StringBuilder();
                sb.AppendLine($"[VaultDiag] schema={loaded.Dto.schemaVersion} path={path}");

                LogFloorCell(sb, index, -5, 0, -4, "approach");
                LogFloorCell(sb, index, -4, 0, -4, "aheadGround");
                LogFloorCell(sb, index, -4, 1, -4, "aheadTop");

                float cellSize = loaded.Dto.gridCellSize > 0f ? loaded.Dto.gridCellSize : 1f;
                var hub = TileMapCacheHub.Create(model, new BuildingGroupRegistry());
                var query = MapCollisionServices.Create(hub, cellSize).Query;

                Vector3 feet = TileHelper.ConvertGridToWorldPos(new Vector3Int(-5, 0, -4), cellSize);
                bool okApproach = MapVaultQuery.TryFindCandidate(
                    query,
                    feet,
                    new Vector3Int(1, 2, 1),
                    Vector3.right,
                    out VaultCandidate candidate);
                sb.AppendLine(
                    $"[VaultDiag] TryFindCandidate(-5,0,-4)+X => {okApproach} landing={candidate.LandingFeetCell} style={candidate.Style}");

                Vector3 pressedFeet = TileHelper.ConvertGridToWorldPos(new Vector3Int(-4, 0, -4), cellSize);
                bool okPressed = MapVaultQuery.TryFindCandidate(
                    query,
                    pressedFeet,
                    new Vector3Int(1, 2, 1),
                    Vector3.right,
                    out VaultCandidate pressedCand);
                sb.AppendLine(
                    $"[VaultDiag] TryFindCandidate(-4,0,-4)+X pressed => {okPressed} landing={pressedCand.LandingFeetCell}");

                string report = sb.ToString();
                string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "VaultDiag.txt"));
                File.WriteAllText(outPath, report);
                Debug.Log(report);
            }
            catch (System.Exception ex)
            {
                string err = "[VaultDiag] FAILED: " + ex;
                Debug.LogError(err);
                File.WriteAllText(
                    Path.GetFullPath(Path.Combine(Application.dataPath, "..", "VaultDiag.txt")),
                    err);
            }
        }

        static void LogFloorCell(StringBuilder sb, FloorMapIndex index, int x, int y, int z, string label)
        {
            bool floor = index.CellHasFloor(x, y, z);
            bool face = index.TryGetFloorFaceForWalkableCell(x, y, z, out TileData tile);
            sb.AppendLine(
                $"[VaultDiag] {label} ({x},{y},{z}) CellHasFloor={floor}" +
                (face ? $" face={tile.identity.PrefabId} gridPos={tile.identity.GridPos}" : " face=MISSING"));
        }
    }
}
#endif
