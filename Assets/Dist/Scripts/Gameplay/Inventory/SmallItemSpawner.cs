// ============================================================
// SmallItemSpawner — 소형 아이템 월드 스폰 공용 API (테스트·드롭)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SmallItemSpawner
{
    public const string WorldMapRootName = "Map";
    public const string WorldItemsFolderName = "Items";

    public static Transform TryResolveWorldRoot()
    {
        Transform map = FindSceneRoot(WorldMapRootName);
        return map != null ? map.Find(WorldItemsFolderName) : null;
    }

    public static Transform ResolveWorldRoot()
    {
        Transform existing = TryResolveWorldRoot();
        if (existing != null)
            return existing;

        Transform map = FindSceneRoot(WorldMapRootName);
        if (map == null)
        {
            var mapGo = new GameObject(WorldMapRootName);
            map = mapGo.transform;
        }

        var itemsGo = new GameObject(WorldItemsFolderName);
        itemsGo.transform.SetParent(map, false);
        return itemsGo.transform;
    }

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

        if (parent == null)
            parent = ResolveWorldRoot();

        SmallItemObject instance = Object.Instantiate(prefab, worldPosition, rotation, parent);

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

        instance.name = $"SmallItem_{definition.id}";
        instance.Configure(definition, count);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }

    static void PrepareInstance(SmallItemObject instance, ItemStack stack, IWorldGrid worldGrid)
    {
        if (instance == null)
            return;

        instance.name = $"SmallItem_{stack.Item.id}";
        instance.BindStack(stack);

        if (worldGrid != null)
            instance.BindWorldGrid(worldGrid);

        instance.gameObject.SetActive(true);
    }

    static Transform FindSceneRoot(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root != null && root.name == name)
                return root.transform;
        }

        return null;
    }
}
