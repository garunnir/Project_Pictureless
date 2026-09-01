// ============================================================
// CharacterTraits — 상시 패시브 특성 보유 집합
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface ICharacterTraits
    {
        bool Has(string traitId);
        void Grant(string traitId);
        IReadOnlyCollection<string> GetKnownIds();
    }

    public sealed class DefaultCharacterTraits : ICharacterTraits
    {
        readonly HashSet<string> _known = new();

        public bool Has(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
                return false;
            return _known.Contains(traitId);
        }

        public void Grant(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
                return;
            _known.Add(traitId);
        }

        public IReadOnlyCollection<string> GetKnownIds() => _known;

        public void ImportFromSave(string[] traitIds)
        {
            _known.Clear();
            if (traitIds == null)
                return;

            for (int i = 0; i < traitIds.Length; i++)
            {
                string id = traitIds[i];
                if (!string.IsNullOrEmpty(id))
                    _known.Add(id);
            }
        }
    }
}
