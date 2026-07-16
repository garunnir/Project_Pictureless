// ============================================================
// ContainerVisualPresenter — 사이드바 컨테이너 표시 아이콘 SSOT
// ============================================================
// 우선순위: 월드 타일 thumbnail → provider SpriteRenderer → 중첩(아이템 아이콘) → null

using IsoTilemap;
using UnityEngine;

public static class ContainerVisualPresenter
{
    /// <summary>
    /// 컨테이너의 표시 스프라이트. floor-loot 가상 컨테이너는 null(아이콘 숨김).
    /// </summary>
    public static Sprite GetDisplayIcon(InventoryContainer container, InventorySession session = null)
    {
        if (container == null)
            return null;

        // 가상 바닥 컨테이너는 월드 오브젝트가 없다 — 아이콘 숨김.
        if (container.InstanceId == FloorLootHost.DefaultInstanceId)
            return null;

        if (ContainerTileViewRegistry.Instance.TryGetViewByContainerInstanceId(
                container.InstanceId,
                out TileView tileView))
        {
            Sprite tileSprite = ResolveTileViewSprite(tileView);
            if (tileSprite != null)
                return tileSprite;
        }

        if (InventoryContainerRegistry.TryGetProviderByInstanceId(
                container.InstanceId,
                out IInventoryContainerProvider provider))
        {
            Sprite providerSprite = ResolveProviderSprite(provider);
            if (providerSprite != null)
                return providerSprite;
        }

        // 인벤 안 가방(중첩 컨테이너)은 담고 있는 아이템의 아이콘을 사용.
        if (session != null &&
            session.TryGetContainerItemStack(container, out _, out ItemStack stack) &&
            stack?.Item != null)
        {
            return ItemVisualPresenter.GetDisplayIcon(stack.Item.id);
        }

        return null;
    }

    static Sprite ResolveTileViewSprite(TileView tileView)
    {
        if (tileView == null)
            return null;

        if (!string.IsNullOrEmpty(tileView.prefabId) &&
            TilePrefabDB.TryResolveDefinition(tileView.prefabId, out TileDefinition def) &&
            def != null &&
            def.thumbnail != null)
        {
            return def.thumbnail;
        }

        SpriteRenderer renderer = tileView.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.sprite : null;
    }

    static Sprite ResolveProviderSprite(IInventoryContainerProvider provider)
    {
        if (provider is ContainerInteractable interactable)
        {
            Sprite tileSprite = ResolveTileViewSprite(interactable.TileView);
            if (tileSprite != null)
                return tileSprite;
        }

        if (provider is Component component)
        {
            SpriteRenderer renderer = component.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null && renderer.sprite != null)
                return renderer.sprite;
        }

        return null;
    }
}
