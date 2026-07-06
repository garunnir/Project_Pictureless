// ============================================================
// ItemDefinitionSO — 아이템 정의 (EquipAsset 대체)
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Item
{
    [CreateAssetMenu(fileName = "Item", menuName = "GameDataAsset/Item/Item Definition")]
    public sealed class ItemDefinitionSO : UnityEngine.ScriptableObject
    {
        [SerializeField] string _locKey;
        [SerializeField] Sprite _icon;
        [SerializeField] ItemCategory _category = ItemCategory.Misc;
        [SerializeField, Min(0f)] float _weight = 0.1f;
        [SerializeField, Min(0f)] float _volume = 0.1f;
        [SerializeField, Min(1)] int _maxStack = 99;
        [SerializeField] bool _isContainer;
        [SerializeField] ContainerDefinitionSO _nestedContainerDefinition;

        public string LocKey => _locKey;
        public Sprite Icon => _icon;
        public ItemCategory Category => _category;
        public float Weight => _weight;
        public float Volume => _volume;
        public int MaxStack => _maxStack;
        public bool IsContainer => _isContainer;
        public ContainerDefinitionSO NestedContainerDefinition => _nestedContainerDefinition;
    }
}
