// ============================================================
// CharacterFactory — CharacterDefinition 프리팹 Instantiate + Apply
// ============================================================

using UnityEngine;

public static class CharacterFactory
{
    public static GameObject Instantiate(
        CharacterDefinition definition,
        Vector3 position,
        Transform parent = null)
    {
        GameObject instance = InstantiateInactive(definition, position, parent);
        if (instance == null)
            return null;

        instance.SetActive(true);
        return instance;
    }

    public static GameObject InstantiateInactive(
        CharacterDefinition definition,
        Vector3 position,
        Transform parent = null)
    {
        if (definition == null)
        {
            Debug.LogError("[CharacterFactory] definition is null.");
            return null;
        }

        if (definition.Prefab == null)
        {
            Debug.LogError($"[CharacterFactory] Prefab missing on definition '{definition.name}'.");
            return null;
        }

        GameObject template = definition.Prefab;
        bool wasActive = template.activeSelf;
        if (wasActive)
            template.SetActive(false);

        GameObject instance = Object.Instantiate(template, position, Quaternion.identity, parent);

        if (wasActive)
            template.SetActive(true);

        if (instance == null)
            return null;

        CharacterDefinitionBinder binder = instance.GetComponent<CharacterDefinitionBinder>();
        if (binder == null)
            binder = instance.AddComponent<CharacterDefinitionBinder>();

        binder.Apply(definition);
        return instance;
    }
}
