// ============================================================
// ComponentUnionFind — bake scratch component union (buildingId 할당 전)
// ============================================================
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class ComponentUnionFind
    {
        readonly Dictionary<int, int> _parent = new();
        int _nextId = 1;

        public void Clear()
        {
            _parent.Clear();
            _nextId = 1;
        }

        public int MakeSet()
        {
            int id = _nextId++;
            _parent[id] = id;
            return id;
        }

        public int Find(int id)
        {
            if (!_parent.TryGetValue(id, out int parent))
                return id;

            if (parent != id)
            {
                parent = Find(parent);
                _parent[id] = parent;
            }

            return parent;
        }

        public void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA == rootB)
                return;

            _parent[rootB] = rootA;
        }

        public int SetCount => _parent.Count;
    }
}
