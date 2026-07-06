// ============================================================
// ItemCatalogSO — 아이템 카탈로그 (EquipmentCollectionSO 대체)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Item
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "GameDataAsset/Item/Item Catalog")]
    public sealed class ItemCatalogSO : UnityEngine.ScriptableObject
    {
        [SerializeField] ItemDefinitionSO[] _items = Array.Empty<ItemDefinitionSO>();

        public IReadOnlyList<ItemDefinitionSO> Items => _items;

        public ItemDefinitionSO GetByIndex(int index)
        {
            if (index < 0 || index >= _items.Length)
                return null;
            return _items[index];
        }

        public int IndexOf(ItemDefinitionSO item)
        {
            if (item == null)
                return -1;

            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == item)
                    return i;
            }

            return -1;
        }
    }
}
