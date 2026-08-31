// ============================================================
// ConstructionCatalog — constructions.json 인메모리 인덱스
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class ConstructionCatalog
    {
        readonly List<ConstructionData> _all;
        readonly Dictionary<string, ConstructionData> _byId = new();
        readonly Dictionary<string, List<ConstructionData>> _byCategory = new();

        public IReadOnlyList<ConstructionData> All => _all;

        public ConstructionCatalog(ConstructionsFileRoot root)
        {
            _all = root?.constructions ?? new List<ConstructionData>();
            for (int i = 0; i < _all.Count; i++)
            {
                ConstructionData c = _all[i];
                if (c == null || string.IsNullOrEmpty(c.id))
                    continue;

                _byId[c.id] = c;
                string cat = string.IsNullOrEmpty(c.category) ? "CC_OTHER" : c.category;
                if (!_byCategory.TryGetValue(cat, out List<ConstructionData> list))
                {
                    list = new List<ConstructionData>(4);
                    _byCategory[cat] = list;
                }

                list.Add(c);
            }
        }

        public ConstructionData Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return _byId.TryGetValue(id, out ConstructionData c) ? c : null;
        }

        public List<string> GetCategories()
        {
            var cats = new List<string>(_byCategory.Keys);
            cats.Sort();
            return cats;
        }

        public List<ConstructionData> GetByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return new List<ConstructionData>(_all);

            if (_byCategory.TryGetValue(category, out List<ConstructionData> list))
                return new List<ConstructionData>(list);

            return new List<ConstructionData>(0);
        }

        public static ConstructionCatalog Empty { get; } = new(null);
    }
}
