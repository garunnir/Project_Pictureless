// ============================================================
// TileMapBfsDebugOverlay — BFS/오클루전·buildingId·실내야외 판정 디버그 (에디터 Scene 뷰)
// ============================================================
#if UNITY_EDITOR
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
        static bool _subscribed;

        static GUIStyle _legendTitleStyle;
        static GUIStyle _legendRowStyle;

        public static void Clear()
        {
            ClearBfsLayers();
            ClearIndoorOutdoorLayers();
        }

        public static void ClearBfsLayers()
        {
            CellLayers.Clear();
            EdgeLayers.Clear();
            TileLabelLayers.Clear();
        }

        public static void ClearIndoorOutdoorLayers() => IndoorOutdoorCellLayers.Clear();

        /// <summary>플레이어 층 바닥 셀의 <see cref="TileMapCacheHub.IsOutdoorEvaluation"/> 결과를 표시합니다.</summary>
        public static void PublishIndoorOutdoorEvaluation(TileMapCacheHub hub, int band)
        {
            ClearIndoorOutdoorLayers();
            if (hub == null)
                return;

            var outdoor = new HashSet<Vector3Int>();
            var indoor = new HashSet<Vector3Int>();
            foreach (var (x, z, b) in hub.Topology.EnumerateOccupiedCells())
            {
                if (b != band)
                    continue;

                if (!hub.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                var cell = new Vector3Int(x, band, z);
                if (hub.IsOutdoorEvaluation(band, x, z))
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
                IndoorOutdoorCellLayers.Count == 0)
                return;

            DrawLegend();
            for (int i = 0; i < CellLayers.Count; i++)
            {
                var layer = CellLayers[i];
                DrawCellOutline(layer.Cells, layer.Offset, layer.Color);
                DrawWorldLabel(layer.Label, layer.Color, ComputeCellCentroid(layer.Cells, layer.Offset));
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
                DrawCellOutline(layer.Cells, layer.Offset, layer.Color);
            }
        }

        static void DrawLegend()
        {
            EnsureLegendStyles();
            const float width = 300f;
            const float rowHeight = 18f;
            int rowCount = CellLayers.Count + EdgeLayers.Count + TileLabelLayers.Count + IndoorOutdoorCellLayers.Count;
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

        static Vector3 ComputeCellCentroid(HashSet<Vector3Int> cells, float offset)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var cell in cells)
            {
                sum += TileHelper.ConvertGridToWorldPos(cell, 1f);
                count++;
            }

            if (count == 0)
                return Vector3.zero;

            Vector3 center = sum / count;
            center.y += 0.35f + offset;
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

        static void DrawCellOutline(HashSet<Vector3Int> occupiedCells, float offset, Color color)
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
                        edgeCenter - perpendicularDir * 0.5f + cellToAdjacentDir * offset, 1f);
                    Vector3 edgeLineEnd = TileHelper.ConvertGridToWorldPos(
                        edgeCenter + perpendicularDir * 0.5f + cellToAdjacentDir * offset, 1f);

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
