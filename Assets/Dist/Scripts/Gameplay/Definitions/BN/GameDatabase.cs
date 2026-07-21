// ============================================================
// GameDatabase — JSON 데이터의 인메모리 인덱스 (아이템·레시피·컨테이너 조회)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class GameDatabase
    {
        public IReadOnlyList<ItemData> Items => _items;
        public IReadOnlyList<RecipeData> Recipes => _recipes;
        public IReadOnlyList<RecipeData> Uncrafts => _uncrafts;
        public IReadOnlyList<MaterialData> Materials => _materials;
        public IReadOnlyList<QualityData> Qualities => _qualities;
        public IReadOnlyList<ContainerData> Containers => _containers;
        public IReadOnlyList<SkillData> Skills => _skills;

        readonly List<ItemData> _items;
        readonly List<RecipeData> _recipes;
        readonly List<RecipeData> _uncrafts;
        readonly List<MaterialData> _materials;
        readonly List<QualityData> _qualities;
        readonly List<ContainerData> _containers;
        readonly List<SkillData> _skills;

        readonly Dictionary<string, ItemData> _itemById = new();
        readonly Dictionary<string, ContainerData> _containerById = new();
        readonly Dictionary<string, SkillData> _skillById = new();
        readonly Dictionary<string, MaterialData> _materialById = new();
        readonly Dictionary<string, List<RecipeData>> _recipesByResult = new();
        readonly Dictionary<string, List<RecipeData>> _recipesByCategory = new();
        readonly Dictionary<string, List<RecipeData>> _recipesByIngredient = new();
        readonly Dictionary<string, List<RecipeData>> _uncraftsByResult = new();

        public GameDatabase(ItemsFileRoot itemsRoot, RecipesFileRoot recipesRoot)
        {
            _items = itemsRoot?.items ?? new List<ItemData>();
            _materials = itemsRoot?.materials ?? new List<MaterialData>();
            _qualities = itemsRoot?.qualities ?? new List<QualityData>();
            _containers = itemsRoot?.containers ?? new List<ContainerData>();
            _skills = itemsRoot?.skills ?? new List<SkillData>();
            _recipes = recipesRoot?.recipes ?? new List<RecipeData>();
            _uncrafts = recipesRoot?.uncraft ?? new List<RecipeData>();

            BuildIndices();
        }

        void BuildIndices()
        {
            foreach (ItemData item in _items)
            {
                if (!string.IsNullOrEmpty(item.id))
                    _itemById[item.id] = item;
            }

            foreach (SkillData skill in _skills)
            {
                if (!string.IsNullOrEmpty(skill?.id))
                    _skillById[skill.id] = skill;
            }

            foreach (MaterialData material in _materials)
            {
                if (!string.IsNullOrEmpty(material?.id))
                    _materialById[material.id] = material;
            }

            foreach (ContainerData container in _containers)
            {
                if (!string.IsNullOrEmpty(container.id))
                    _containerById[container.id] = container;
            }

            foreach (RecipeData recipe in _recipes)
                IndexRecipe(recipe);

            foreach (RecipeData recipe in _uncrafts)
                IndexUncraft(recipe);
        }

        void IndexRecipe(RecipeData recipe)
        {
            if (!string.IsNullOrEmpty(recipe.result))
            {
                if (!_recipesByResult.TryGetValue(recipe.result, out var list))
                {
                    list = new List<RecipeData>(1);
                    _recipesByResult[recipe.result] = list;
                }
                list.Add(recipe);
            }

            if (!string.IsNullOrEmpty(recipe.category))
            {
                if (!_recipesByCategory.TryGetValue(recipe.category, out var list))
                {
                    list = new List<RecipeData>(4);
                    _recipesByCategory[recipe.category] = list;
                }
                list.Add(recipe);
            }

            if (recipe.components == null) return;
            foreach (ComponentSlot slot in recipe.components)
            {
                if (slot.alternatives == null) continue;
                foreach (ComponentAlt alt in slot.alternatives)
                {
                    if (string.IsNullOrEmpty(alt.item)) continue;
                    if (!_recipesByIngredient.TryGetValue(alt.item, out var list))
                    {
                        list = new List<RecipeData>(1);
                        _recipesByIngredient[alt.item] = list;
                    }
                    if (!list.Contains(recipe))
                        list.Add(recipe);
                }
            }
        }

        void IndexUncraft(RecipeData recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.result)) return;
            if (!_uncraftsByResult.TryGetValue(recipe.result, out var list))
                _uncraftsByResult[recipe.result] = list = new List<RecipeData>(1);
            if (!list.Contains(recipe))
                list.Add(recipe);
        }

        public ItemData GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _itemById.TryGetValue(id, out var item);
            return item;
        }

        public List<RecipeData> GetRecipesForResult(string resultId)
        {
            if (string.IsNullOrEmpty(resultId)) return _emptyList;
            return _recipesByResult.TryGetValue(resultId, out var list) ? list : _emptyList;
        }

        public List<RecipeData> GetRecipesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return _emptyList;
            return _recipesByCategory.TryGetValue(category, out var list) ? list : _emptyList;
        }

        public List<RecipeData> GetRecipesUsingIngredient(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return _emptyList;
            return _recipesByIngredient.TryGetValue(itemId, out var list) ? list : _emptyList;
        }

        public ContainerData GetContainer(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _containerById.TryGetValue(id, out var container);
            return container;
        }

        public SkillData GetSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _skillById.TryGetValue(id, out var skill);
            return skill;
        }

        public MaterialData GetMaterial(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _materialById.TryGetValue(id, out var material);
            return material;
        }

        public List<RecipeData> GetUncraftForResult(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return _emptyList;

            if (_uncraftsByResult.TryGetValue(itemId, out var list) && list.Count > 0)
                return list;

            // reversible assembly fallback: if BN had no explicit uncraft entry,
            // treat reversible recipe's components as disassembly output.
            if (_recipesByResult.TryGetValue(itemId, out var assemblyCandidates))
            {
                var reversible = assemblyCandidates.FindAll(r => r != null && r.reversible);
                return reversible.Count > 0 ? reversible : _emptyList;
            }

            return _emptyList;
        }

        static readonly List<RecipeData> _emptyList = new(0);
    }
}
