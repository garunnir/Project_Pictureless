// ============================================================
// CharacterRecipeMemory — decomp_learn 등으로 영구 습득한 레시피 id
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface ICharacterRecipeMemory
    {
        bool IsKnown(string recipeId);
        void Learn(string recipeId);
    }

    public sealed class DefaultCharacterRecipeMemory : ICharacterRecipeMemory
    {
        readonly HashSet<string> _known = new();

        public bool IsKnown(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return false;
            return _known.Contains(recipeId);
        }

        public void Learn(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return;
            _known.Add(recipeId);
        }
    }
}
