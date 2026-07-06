// ============================================================
// GameplayCatalogHost — ItemCatalogSO 씬 배선 (ResourceManager 레거시 대체)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class GameplayCatalogHost : MonoBehaviour
{
    [Required, SerializeField] ItemCatalogSO _itemCatalog;

    public ItemCatalogSO ItemCatalog => _itemCatalog;

    void Awake() => GameplayData.Register(_itemCatalog);

    void OnDestroy() => GameplayData.Unregister(_itemCatalog);
}
