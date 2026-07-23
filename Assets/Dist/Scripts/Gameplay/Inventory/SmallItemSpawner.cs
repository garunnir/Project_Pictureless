// ============================================================
// SmallItemSpawner — 소형 아이템 월드 스폰 공용 API (테스트·드롭)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

public static class SmallItemSpawner
{
    public static SmallItemObject Spawn(
        SmallItemObject prefab,
        ItemData definition,
        int count,
        Vector3 worldPosition,
        IWorldGrid worldGrid = null,
        Quaternion? rotation = null,
        Transform parent = null)
    {
        if (prefab == null || definition == null || count < 1)
            return null;

        Quaternion spawnRotation = rotation ?? Quaternion.identity;
        SmallItemObject instance = InstantiateInactive(prefab, worldPosition, spawnRotation, parent);
        PrepareInstance(instance, definition, count, worldGrid);
        return instance;
    }

    public static SmallItemObject Spawn(
        SmallItemObject prefab,
        ItemStack stack,
        Vector3 worldPosition,
        IWorldGrid worldGrid = null,
        Quaternion? rotation = null,
        Transform parent = null)
    {
        if (prefab == null || stack?.Item == null)
            return null;

        Quaternion spawnRotation = rotation ?? Quaternion.identity;
        SmallItemObject instance = InstantiateInactive(prefab, worldPosition, spawnRotation, parent);
        PrepareInstance(instance, stack, worldGrid);
        return instance;
    }

    public static SmallItemObject SpawnLocal(
        SmallItemObject prefab,
        ItemData definition,
        int count,
        Transform parent,
        Vector3 localPosition,
        IWorldGrid worldGrid = null)
    {
        if (prefab == null || definition == null || count < 1 || parent == null)
            return null;

        SmallItemObject instance = InstantiateInactive(prefab, Vector3.zero, Quaternion.identity, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        PrepareInstance(instance, definition, count, worldGrid);
        return instance;
    }

    // 활성 템플릿이면 잠시 비활성 후 Instantiate → Awake가 Configure 전에 돌지 않음.
    static SmallItemObject InstantiateInactive(
        SmallItemObject prefab,
        Vector3 worldPosition,
        Quaternion rotation,
        Transform parent)
    {
        GameObject template = prefab.gameObject;
        bool wasActive = template.activeSelf;
        if (wasActive)
            template.SetActive(false);

        SmallItemObject instance = parent != null
            ? Object.Instantiate(prefab, worldPosition, rotation, parent)
            : Object.Instantiate(prefab, worldPosition, rotation);

        if (wasActive)
            template.SetActive(true);

        return instance;
    }

    static void PrepareInstance(
        SmallItemObject instance,
        ItemData definition,
        int count,
        IWorldGrid worldGrid)
    {
        if (instance == null)
            return;

        instance.name = $"SmallItem_{definition.name}";
        instance.Configure(definition, count);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }

    static void PrepareInstance(SmallItemObject instance, ItemStack stack, IWorldGrid worldGrid)
    {
        if (instance == null)
            return;

        instance.name = $"SmallItem_{stack.Item.name}";
        instance.BindStack(stack);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }
}
