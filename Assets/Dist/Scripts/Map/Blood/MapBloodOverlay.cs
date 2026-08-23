// ============================================================
// MapBloodOverlay — 월드 혈흔 스탬프 버퍼 (셀 소속·청소·세이브)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct BloodStamp
    {
        public readonly Vector3 WorldPos;
        public readonly float Yaw;
        public readonly float Scale;
        public readonly float Alpha;
        public readonly Vector3Int OwnerCell;

        public BloodStamp(Vector3 worldPos, float yaw, float scale, float alpha, Vector3Int ownerCell)
        {
            WorldPos = worldPos;
            Yaw = yaw;
            Scale = scale;
            Alpha = alpha;
            OwnerCell = ownerCell;
        }
    }

    /// <summary>맵 혈흔 SSOT. 스탬프당 GO 없음 — 뷰는 DrawMeshInstanced.</summary>
    public sealed class MapBloodOverlay
    {
        readonly List<BloodStamp> _stamps = new(256);
        readonly Dictionary<Vector3Int, List<int>> _byCell = new();

        public IReadOnlyList<BloodStamp> Stamps => _stamps;
        public int Count => _stamps.Count;
        public event Action Changed;

        public void Clear()
        {
            if (_stamps.Count == 0)
                return;
            _stamps.Clear();
            _byCell.Clear();
            Changed?.Invoke();
        }

        public void LoadFromDto(IReadOnlyList<BloodStampSaveData> dto)
        {
            _stamps.Clear();
            _byCell.Clear();
            if (dto == null)
            {
                Changed?.Invoke();
                return;
            }

            for (int i = 0; i < dto.Count; i++)
            {
                BloodStampSaveData s = dto[i];
                if (s == null)
                    continue;
                _stamps.Add(new BloodStamp(
                    new Vector3(s.wx, s.wy, s.wz),
                    s.yaw,
                    Mathf.Clamp(s.scale, MapBloodConsts.MinScale, MapBloodConsts.MaxScale),
                    Mathf.Clamp01(s.alpha),
                    new Vector3Int(s.cx, s.cy, s.cz)));
            }

            RebuildCellIndex();
            Changed?.Invoke();
        }

        public void WriteToDto(List<BloodStampSaveData> dto)
        {
            if (dto == null)
                return;
            dto.Clear();
            for (int i = 0; i < _stamps.Count; i++)
            {
                BloodStamp s = _stamps[i];
                dto.Add(new BloodStampSaveData
                {
                    wx = s.WorldPos.x,
                    wy = s.WorldPos.y,
                    wz = s.WorldPos.z,
                    yaw = s.Yaw,
                    scale = s.Scale,
                    alpha = s.Alpha,
                    cx = s.OwnerCell.x,
                    cy = s.OwnerCell.y,
                    cz = s.OwnerCell.z,
                });
            }
        }

        public void AddStamp(
            Vector3 worldPos,
            float yaw,
            float scale,
            float alpha,
            TileMapCacheHub hub,
            float cellSize)
        {
            Vector3Int cell = ResolveOwnerCell(worldPos, hub, cellSize);
            AddInternal(worldPos, yaw, scale, alpha, cell, cellSize, rebuildIndex: true);
            TrimToMax();
            Changed?.Invoke();
        }

        public void Spray(
            Vector3 origin,
            Vector3 direction,
            int count,
            float coneHalfRad,
            float minDist,
            float maxDist,
            float groundBiasY,
            float scale,
            float alpha,
            TileMapCacheHub hub,
            float cellSize)
        {
            if (count <= 0)
                return;

            Vector3 dir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            if (right.sqrMagnitude < 1e-6f)
                right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(dir, right);

            for (int i = 0; i < count; i++)
            {
                float yawJitter = (UnityEngine.Random.value * 2f - 1f) * coneHalfRad;
                float pitchJitter = (UnityEngine.Random.value * 2f - 1f) * coneHalfRad * 0.35f;
                Vector3 sprayDir = (dir + right * Mathf.Sin(yawJitter) + up * Mathf.Sin(pitchJitter)).normalized;
                float dist = Mathf.Lerp(minDist, maxDist, UnityEngine.Random.value);
                Vector3 pos = origin + sprayDir * dist;
                pos.y -= groundBiasY * UnityEngine.Random.value;
                float yaw = UnityEngine.Random.Range(0f, 360f);
                float s = scale * UnityEngine.Random.Range(0.7f, 1.15f);
                float a = alpha * UnityEngine.Random.Range(0.75f, 1f);
                Vector3Int cell = ResolveOwnerCell(pos, hub, cellSize);
                AddInternal(pos, yaw, s, a, cell, cellSize, rebuildIndex: true);
            }

            TrimToMax();
            Changed?.Invoke();
        }

        public int ClearCell(Vector3Int cell)
        {
            if (!_byCell.TryGetValue(cell, out List<int> indices) || indices.Count == 0)
                return 0;

            var remove = new HashSet<int>(indices);
            var kept = new List<BloodStamp>(_stamps.Count - remove.Count);
            for (int i = 0; i < _stamps.Count; i++)
            {
                if (!remove.Contains(i))
                    kept.Add(_stamps[i]);
            }

            int removed = _stamps.Count - kept.Count;
            _stamps.Clear();
            _stamps.AddRange(kept);
            RebuildCellIndex();
            if (removed > 0)
                Changed?.Invoke();
            return removed;
        }

        static Vector3Int ResolveOwnerCell(Vector3 worldPos, TileMapCacheHub hub, float cellSize)
        {
            float size = Mathf.Max(1e-4f, cellSize);
            if (hub == null)
                return TileHelper.ConvertWorldToGrid(worldPos, size);

            return OccupiedCellCoord.ResolveFromWorld(hub, worldPos, size, worldPos.y);
        }

        void AddInternal(
            Vector3 worldPos,
            float yaw,
            float scale,
            float alpha,
            Vector3Int ownerCell,
            float cellSize,
            bool rebuildIndex)
        {
            float s = Mathf.Clamp(scale, MapBloodConsts.MinScale, MapBloodConsts.MaxScale);
            float a = Mathf.Clamp01(alpha);
            float size = Mathf.Max(1e-4f, cellSize);
            worldPos.y = ownerCell.y * size;
            int index = _stamps.Count;
            _stamps.Add(new BloodStamp(worldPos, yaw, s, a, ownerCell));
            if (rebuildIndex)
            {
                if (!_byCell.TryGetValue(ownerCell, out List<int> list))
                {
                    list = new List<int>(4);
                    _byCell[ownerCell] = list;
                }

                list.Add(index);
            }
        }

        void RebuildCellIndex()
        {
            _byCell.Clear();
            for (int i = 0; i < _stamps.Count; i++)
            {
                Vector3Int cell = _stamps[i].OwnerCell;
                if (!_byCell.TryGetValue(cell, out List<int> list))
                {
                    list = new List<int>(4);
                    _byCell[cell] = list;
                }

                list.Add(i);
            }
        }

        void TrimToMax()
        {
            int max = MapBloodConsts.MaxStamps;
            if (_stamps.Count <= max)
                return;

            // 가장 옅은 것부터 제거
            while (_stamps.Count > max)
            {
                int weakest = 0;
                float weakestAlpha = _stamps[0].Alpha;
                for (int i = 1; i < _stamps.Count; i++)
                {
                    if (_stamps[i].Alpha < weakestAlpha)
                    {
                        weakestAlpha = _stamps[i].Alpha;
                        weakest = i;
                    }
                }

                _stamps.RemoveAt(weakest);
            }

            RebuildCellIndex();
        }
    }
}
