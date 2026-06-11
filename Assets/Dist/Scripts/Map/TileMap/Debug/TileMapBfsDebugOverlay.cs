// ============================================================
// TileMapBfsDebugOverlay — BFS/오클루전·buildingId·실내야외 판정 디버그 (에디터 Scene 뷰)
// ============================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IsoTilemap
{
    public static class TileMapBfsDebugOverlay
    {
        struct CellLayer
        {
            public string Label;
            public Color Color;
            public float Offset;
            public HashSet<Vector3Int> Cells;
        }

        struct EdgeLayer
        {
            public string Label;
            public Color Color;
            public float Offset;
            public List<TileData> Edges;
        }

        struct TileLabelLayer
        {
            public string Label;
            public Color Color;
            public float Offset;
            public List<TileData> Tiles;
        }

        static readonly Vector3Int[] CardinalNeighbors =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        static readonly List<CellLayer> CellLayers = new();
        static readonly List<EdgeLayer> EdgeLayers = new();
        static readonly List<TileLabelLayer> TileLabelLayers = new();
        static readonly List<CellLayer> IndoorOutdoorCellLayers = new();
        static readonly List<CellLayer> SightLineCellLayers = new();
        static Vector3 _sightLineCameraWorld;
        static Vector3 _sightLinePlayerWorld;
        static bool _hasSightLineSegment;
        static string _sightLineSummary;
        static float _sightLineCellSize = 1f;
        static bool _subscribed;

        static GUIStyle _legendTitleStyle;
        static GUIStyle _legendRowStyle;

        public static void Clear()
        {
            ClearBfsLayers();
            ClearIndoorOutdoorLayers();
            ClearSightLineLayers();
            ClearBuildingIdLabelLayers();
        }

        public static void ClearBfsLayers()
        {
            CellLayers.Clear();
            EdgeLayers.Clear();
        }

        public static void ClearIndoorOutdoorLayers() => IndoorOutdoorCellLayers.Clear();

        public static void ClearBuildingIdLabelLayers() => TileLabelLayers.Clear();

        public static void ClearSightLineLayers()
        {
            SightLineCellLayers.Clear();
            _hasSightLineSegment = false;
            _sightLineSummary = null;
        }

        /// <summary>시선 차단 <see cref="BuildingPlayerOcclusionResolver"/> 샘플 셀을 표시합니다.</summary>
        public static void PublishSightLineBuilding(
            in SightLineBuildingDebugSnapshot snapshot,
            float cellSize,
            bool isPlayerOutdoor,
            IReadOnlyCollection<int> appliedBlockingBuildingIds,
            int playerBuildingId)
        {
            ClearSightLineLayers();
            if (!snapshot.IsValid)
                return;

            _sightLineCellSize = cellSize > 0f ? cellSize : 1f;
            _sightLineCameraWorld = snapshot.CameraWorld;
            _sightLinePlayerWorld = snapshot.PlayerWorld;
            _hasSightLineSegment = true;

            int appliedCount = appliedBlockingBuildingIds?.Count ?? 0;
            int resolvedCount = 0;
            if (snapshot.BlockingBuildingIds != null)
            {
                foreach (int _ in snapshot.BlockingBuildingIds)
                    resolvedCount++;
            }

            string outdoorTag = isPlayerOutdoor ? "야외" : "실내";
            string playerBuildingTag = playerBuildingId > 0 ? $"player B:{playerBuildingId} 제외" : "player B:없음";
            _sightLineSummary =
                $"{outdoorTag} | {playerBuildingTag} | 샘플 {CountCells(snapshot.SampledCells)} | 차단기여 {CountCells(snapshot.BlockingCells)} | resolver B:{resolvedCount} applied B:{appliedCount}";

            AddSightLineCellLayer("회색 — 시선 샘플 셀", new Color(0.75f, 0.75f, 0.75f),
                ToCellHashSet(snapshot.SampledCells), 0.04f);
            AddSightLineCellLayer("빨강 — 차단 buildingId 기여 셀", Color.red,
                ToCellHashSet(snapshot.BlockingCells), 0.06f);
            EnsureSubscribed();
        }

        static int CountCells(IReadOnlyCollection<Vector3Int> cells)
        {
            if (cells == null)
                return 0;
            int count = 0;
            foreach (Vector3Int _ in cells)
                count++;
            return count;
        }

        static HashSet<Vector3Int> ToCellHashSet(IReadOnlyCollection<Vector3Int> cells)
        {
            var set = new HashSet<Vector3Int>();
            if (cells == null)
                return set;
            foreach (var cell in cells)
                set.Add(cell);
            return set;
        }

        static void AddSightLineCellLayer(string label, Color color, HashSet<Vector3Int> cells, float offset)
        {
            if (cells == null || cells.Count == 0)
                return;

            SightLineCellLayers.Add(new CellLayer
            {
                Label = label,
                Color = color,
                Offset = offset,
                Cells = cells
            });
        }

        /// <summary>플레이어 층 바닥 셀의 <see cref="TileMapCacheHub.IsOutdoorEvaluation"/> 결과를 표시합니다.</summary>
        public static void PublishIndoorOutdoorEvaluation(TileMapCacheHub hub, int cellY)
        {
            ClearIndoorOutdoorLayers();
            if (hub == null)
                return;

            var outdoor = new HashSet<Vector3Int>();
            var indoor = new HashSet<Vector3Int>();
            foreach (var (x, z, b) in hub.Topology.EnumerateOccupiedCells())
            {
                if (b != cellY)
                    continue;

                if (!hub.TryGetCellTiles(x, z, cellY, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                var cell = new Vector3Int(x, cellY, z);
                if (hub.IsOutdoorEvaluation(cellY, x, z))
                    outdoor.Add(cell);
                else
                    indoor.Add(cell);
            }

            AddIndoorOutdoorCellLayer("청록 — 야외 판정 바닥", new Color(0.2f, 0.85f, 0.95f), outdoor, 0.04f);
            AddIndoorOutdoorCellLayer("주황 — 실내 판정 바닥", new Color(1f, 0.55f, 0.15f), indoor, 0.06f);
            EnsureSubscribed();
        }

        static void AddIndoorOutdoorCellLayer(string label, Color color, HashSet<Vector3Int> cells, float offset)
        {
            if (cells == null || cells.Count == 0)
                return;

            IndoorOutdoorCellLayers.Add(new CellLayer
            {
                Label = label,
                Color = color,
                Offset = offset,
                Cells = cells
            });
        }

        public static void AddCellLayer(string label, Color color, HashSet<Vector3Int> cells, float offset = 0f)
        {
            if (cells == null || cells.Count == 0)
                return;

            CellLayers.Add(new CellLayer
            {
                Label = label,
                Color = color,
                Offset = offset,
                Cells = cells
            });
        }

        public static void AddEdgeLayer(string label, Color color, List<TileData> edges, float offset = 0f)
        {
            if (edges == null || edges.Count == 0)
                return;

            EdgeLayers.Add(new EdgeLayer
            {
                Label = label,
                Color = color,
                Offset = offset,
                Edges = edges
            });
        }

        public static void AddTileBuildingIdLabelLayer(string label, Color color, List<TileData> tiles, float offset = 0f)
        {
            if (tiles == null || tiles.Count == 0)
                return;

            TileLabelLayers.Add(new TileLabelLayer
            {
                Label = label,
                Color = color,
                Offset = offset,
                Tiles = tiles
            });
        }

        /// <summary>맵 구조 타일(Floor/Wall/EdgeWall)에 buildingId 라벨을 표시합니다.</summary>
        public static void PublishBuildingIdLabels(TileMapCacheHub hub)
        {
            ClearBuildingIdLabelLayers();
            if (hub == null)
                return;

            List<TileData> structural = CollectStructuralTiles(hub);
            AddTileBuildingIdLabelLayer("하양 — 구조 타일 buildingId", Color.white, structural, 0.08f);
            EnsureSubscribed();
        }

        static List<TileData> CollectStructuralTiles(TileMapCacheHub hub)
        {
            var result = new List<TileData>();
            var seen = new HashSet<Guid>();

            foreach (var (x, z, y) in hub.EnumerateOccupiedCells())
            {
                if (!hub.TryGetCellTiles(x, z, y, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    TileData tile = list[i];
                    if (!IsStructuralCellTile((TileView.TileType)tile.identity.tileType))
                        continue;

                    if (!seen.Add(tile.tileDefId))
                        continue;

                    result.Add(tile);
                }
            }

            foreach (TileData edgeTile in hub.Topology.Index.EnumerateEdgeTiles())
            {
                if ((TileView.TileType)edgeTile.identity.tileType != TileView.TileType.EdgeWall)
                    continue;

                if (!seen.Add(edgeTile.tileDefId))
                    continue;

                result.Add(edgeTile);
            }

            return result;
        }

        static bool IsStructuralCellTile(TileView.TileType type) =>
            type == TileView.TileType.Floor || type == TileView.TileType.Wall;

        public static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            SceneView.duringSceneGui += OnSceneGui;
            _subscribed = true;
        }

        static void OnSceneGui(SceneView view)
        {
            if (CellLayers.Count == 0 && EdgeLayers.Count == 0 && TileLabelLayers.Count == 0 &&
                IndoorOutdoorCellLayers.Count == 0 && SightLineCellLayers.Count == 0 && !_hasSightLineSegment)
                return;

            DrawLegend();
            if (_hasSightLineSegment)
            {
                Handles.color = Color.white;
                Handles.DrawLine(_sightLineCameraWorld, _sightLinePlayerWorld);
                if (!string.IsNullOrEmpty(_sightLineSummary))
                {
                    Vector3 mid = (_sightLineCameraWorld + _sightLinePlayerWorld) * 0.5f;
                    DrawWorldLabel(_sightLineSummary, Color.white, mid);
                }
            }
            for (int i = 0; i < CellLayers.Count; i++)
            {
                var layer = CellLayers[i];
                DrawCellOutline(layer.Cells, layer.Offset, layer.Color, 1f);
                DrawWorldLabel(layer.Label, layer.Color, ComputeCellCentroid(layer.Cells, layer.Offset, 1f));
            }

            for (int i = 0; i < EdgeLayers.Count; i++)
            {
                var layer = EdgeLayers[i];
                DrawEdgeOutline(layer.Edges, layer.Offset, layer.Color);
                DrawWorldLabel(layer.Label, layer.Color, ComputeEdgeCentroid(layer.Edges, layer.Offset));
            }

            for (int i = 0; i < TileLabelLayers.Count; i++)
            {
                var layer = TileLabelLayers[i];
                DrawTileBuildingIdLabels(layer.Tiles, layer.Offset, layer.Color);
            }

            for (int i = 0; i < IndoorOutdoorCellLayers.Count; i++)
            {
                var layer = IndoorOutdoorCellLayers[i];
                DrawCellOutline(layer.Cells, layer.Offset, layer.Color, 1f);
            }

            for (int i = 0; i < SightLineCellLayers.Count; i++)
            {
                var layer = SightLineCellLayers[i];
                DrawCellOutline(layer.Cells, layer.Offset, layer.Color, _sightLineCellSize);
                DrawWorldLabel(layer.Label, layer.Color, ComputeCellCentroid(layer.Cells, layer.Offset, _sightLineCellSize));
            }
        }

        static void DrawLegend()
        {
            EnsureLegendStyles();
            const float width = 300f;
            const float rowHeight = 18f;
            int rowCount = CellLayers.Count + EdgeLayers.Count + TileLabelLayers.Count +
                           IndoorOutdoorCellLayers.Count + SightLineCellLayers.Count +
                           (_hasSightLineSegment ? 1 : 0);
            float height = 28f + rowCount * rowHeight;
            var area = new Rect(10f, 10f, width, height);

            Handles.BeginGUI();
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(area);
            GUILayout.Label("타일맵 디버그", _legendTitleStyle);
            for (int i = 0; i < CellLayers.Count; i++)
                DrawLegendRow(CellLayers[i].Color, CellLayers[i].Label);
            for (int i = 0; i < EdgeLayers.Count; i++)
                DrawLegendRow(EdgeLayers[i].Color, EdgeLayers[i].Label);
            for (int i = 0; i < TileLabelLayers.Count; i++)
                DrawLegendRow(TileLabelLayers[i].Color, TileLabelLayers[i].Label);
            for (int i = 0; i < IndoorOutdoorCellLayers.Count; i++)
                DrawLegendRow(IndoorOutdoorCellLayers[i].Color, IndoorOutdoorCellLayers[i].Label);
            for (int i = 0; i < SightLineCellLayers.Count; i++)
                DrawLegendRow(SightLineCellLayers[i].Color, SightLineCellLayers[i].Label);
            if (_hasSightLineSegment)
                DrawLegendRow(Color.white, "흰선 — 카메라↔플레이어 시선");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        static void DrawLegendRow(Color color, string label)
        {
            GUILayout.BeginHorizontal();
            var swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
            EditorGUI.DrawRect(swatch, color);
            GUILayout.Space(4f);
            GUILayout.Label(label, _legendRowStyle);
            GUILayout.EndHorizontal();
        }

        static void EnsureLegendStyles()
        {
            if (_legendTitleStyle != null)
                return;

            _legendTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            _legendRowStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, wordWrap = false };
        }

        static void DrawWorldLabel(string text, Color color, Vector3 worldPos)
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                fontSize = 11
            };
            Handles.Label(worldPos, text, style);
        }

        static Vector3 ComputeCellCentroid(HashSet<Vector3Int> cells, float offset, float cellSize)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var cell in cells)
            {
                sum += TileHelper.ConvertGridToWorldPos(cell, cellSize);
                count++;
            }

            if (count == 0)
                return Vector3.zero;

            Vector3 center = sum / count;
            center.y += cellSize * 0.35f + offset;
            return center;
        }

        static Vector3 ComputeEdgeCentroid(List<TileData> edges, float offset)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < edges.Count; i++)
            {
                var key = WallEdgeKey.FromEdgeTileIdentity(edges[i].identity);
                sum += TileHelper.ConvertGridToWorldPos(key.CellA, 1f);
                sum += TileHelper.ConvertGridToWorldPos(key.CellB, 1f);
            }

            Vector3 center = sum / (edges.Count * 2);
            center.y += 0.35f + offset;
            return center;
        }

        static void DrawCellOutline(HashSet<Vector3Int> occupiedCells, float offset, Color color, float cellSize)
        {
            Handles.color = color;
            foreach (var cell in occupiedCells)
            {
                foreach (var direction in CardinalNeighbors)
                {
                    var adjacentCell = new Vector3Int(cell.x + direction.x, cell.y, cell.z + direction.z);
                    if (occupiedCells.Contains(adjacentCell))
                        continue;

                    Vector3 cellToAdjacentDir = adjacentCell - cell;
                    Vector3 perpendicularDir = new Vector3(-cellToAdjacentDir.z, 0, cellToAdjacentDir.x).normalized;
                    Vector3 edgeCenter = new Vector3(
                        (cell.x + adjacentCell.x) * 0.5f,
                        cell.y,
                        (cell.z + adjacentCell.z) * 0.5f);

                    Vector3 edgeLineStart = TileHelper.ConvertGridToWorldPos(
                        edgeCenter - perpendicularDir * 0.5f + cellToAdjacentDir * offset, cellSize);
                    Vector3 edgeLineEnd = TileHelper.ConvertGridToWorldPos(
                        edgeCenter + perpendicularDir * 0.5f + cellToAdjacentDir * offset, cellSize);

                    Handles.DrawLine(edgeLineStart, edgeLineEnd);
                }
            }
        }

        static void DrawEdgeOutline(List<TileData> edgeTiles, float offset, Color color)
        {
            Handles.color = color;
            for (int i = 0; i < edgeTiles.Count; i++)
            {
                var key = WallEdgeKey.FromEdgeTileIdentity(edgeTiles[i].identity);
                Vector3Int neighbor = key.CellB;
                Vector3 cellToNeighbor = neighbor - key.CellA;
                Vector3 perpendicularDir = new Vector3(-cellToNeighbor.z, 0, cellToNeighbor.x).normalized;
                Vector3 edgeCenter = new Vector3(
                    (key.CellA.x + neighbor.x) * 0.5f,
                    key.CellA.y,
                    (key.CellA.z + neighbor.z) * 0.5f);

                Vector3 edgeLineStart = TileHelper.ConvertGridToWorldPos(
                    edgeCenter - perpendicularDir * 0.5f + cellToNeighbor * offset, 1f);
                Vector3 edgeLineEnd = TileHelper.ConvertGridToWorldPos(
                    edgeCenter + perpendicularDir * 0.5f + cellToNeighbor * offset, 1f);

                Handles.DrawLine(edgeLineStart, edgeLineEnd);
            }
        }

        static void DrawTileBuildingIdLabels(List<TileData> tiles, float offset, Color color)
        {
            if (tiles == null || tiles.Count == 0)
                return;

            var seen = new HashSet<System.Guid>();
            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = color },
                fontSize = 10
            };

            for (int i = 0; i < tiles.Count; i++)
            {
                TileData tile = tiles[i];
                if (!seen.Add(tile.tileDefId))
                    continue;

                Vector3 worldPos = ResolveBuildingIdLabelWorldPos(tile);
                worldPos.y += 0.25f + offset;
                Handles.Label(worldPos, $"B:{tile.identity.buildingId}", style);
            }
        }

        static Vector3 ResolveBuildingIdLabelWorldPos(TileData tile)
        {
            if ((TileView.TileType)tile.identity.tileType == TileView.TileType.EdgeWall)
            {
                var key = WallEdgeKey.FromEdgeTileIdentity(tile.identity);
                WallEdgeKey.GetWorldPose(key, 1f, out Vector3 edgePos, out _);
                return edgePos;
            }

            return TileHelper.ConvertGridToWorldPos(tile.identity.GridPos, 1f);
        }
    }
}
#endif
