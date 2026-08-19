// ============================================================
// CharacterWorldRoot — 캐릭터 월드 부모 폴더 SSOT
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharacterWorldRoot
{
    public const string WorldCharactersFolderName = "Characters";

    public static Transform TryResolve()
    {
        Transform map = FindSceneRoot(SmallItemSpawner.WorldMapRootName);
        return map != null ? map.Find(WorldCharactersFolderName) : null;
    }

    public static Transform Resolve()
    {
        Transform existing = TryResolve();
        if (existing != null)
            return existing;

        Transform map = FindSceneRoot(SmallItemSpawner.WorldMapRootName);
        if (map == null)
        {
            var mapGo = new GameObject(SmallItemSpawner.WorldMapRootName);
            map = mapGo.transform;
        }

        var folder = new GameObject(WorldCharactersFolderName);
        folder.transform.SetParent(map, false);
        return folder.transform;
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
