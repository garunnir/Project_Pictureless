// ============================================================
// SmallItemSpawner — 소형 아이템 월드 스폰 공용 API (테스트·드롭)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using IsoTilemap;
using UnityEngine;

public static class SmallItemSpawner
{
    public static SmallItemObject Spawn(
        SmallItemObject prefab,
        ItemDefinitionSO definition,
        int count,
        Vector3 worldPosition,
        IWorldGrid worldGrid = null,
        Quaternion? rotation = null,
        Transform parent = null)
    {
        if (prefab == null || definition == null || count < 1)
            return null;

        Quaternion spawnRotation = rotation ?? Quaternion.identity;
        SmallItemObject instance = parent != null
            ? Object.Instantiate(prefab, worldPosition, spawnRotation, parent)
            : Object.Instantiate(prefab, worldPosition, spawnRotation);

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
        SmallItemObject instance = parent != null
            ? Object.Instantiate(prefab, worldPosition, spawnRotation, parent)
            : Object.Instantiate(prefab, worldPosition, spawnRotation);

        PrepareInstance(instance, stack, worldGrid);
        return instance;
    }

    public static SmallItemObject SpawnLocal(
        SmallItemObject prefab,
        ItemDefinitionSO definition,
        int count,
        Transform parent,
        Vector3 localPosition,
        IWorldGrid worldGrid = null)
    {
        if (prefab == null || definition == null || count < 1 || parent == null)
            return null;

        SmallItemObject instance = Object.Instantiate(prefab, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        PrepareInstance(instance, definition, count, worldGrid);
        return instance;
    }

    static void PrepareInstance(
        SmallItemObject instance,
        ItemDefinitionSO definition,
        int count,
        IWorldGrid worldGrid)
    {
        if (instance == null)
            return;

        instance.gameObject.SetActive(false);
        instance.name = $"SmallItem_{definition.LocKey}";
        instance.Configure(definition, count);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }

    static void PrepareInstance(SmallItemObject instance, ItemStack stack, IWorldGrid worldGrid)
    {
        if (instance == null)
            return;

        instance.gameObject.SetActive(false);
        instance.name = $"SmallItem_{stack.Item.LocKey}";
        instance.BindStack(stack);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }
}
