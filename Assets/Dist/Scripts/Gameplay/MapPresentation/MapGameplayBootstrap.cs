// ============================================================
// MapGameplayBootstrap — 맵 로드 후 플레이어·컨테이너에 Map 서비스 바인딩
// ============================================================

using IsoTilemap;
using UnityEngine;

[DefaultExecutionOrder(-49)]
[DisallowMultipleComponent]
public sealed class MapGameplayBootstrap : MonoBehaviour
{
    [SerializeField] TileMapManager _tileMapManager;
    [SerializeField] CharacterFactionCatalog _factionCatalog;

    void Start()
    {
        if (_tileMapManager == null)
            _tileMapManager = GetComponent<TileMapManager>();

        if (_tileMapManager == null)
            return;

        if (_factionCatalog == null)
        {
            Debug.LogError(
                "[MapGameplayBootstrap] CharacterFactionCatalog is not assigned.",
                this);
        }

        CharacterHostility.BindCatalog(_factionCatalog);

        IWorldGrid worldGrid = _tileMapManager.WorldGrid;
        if (worldGrid != null)
            BindWorldGridToCharacters(worldGrid);

        BindMapCollisionServices(_tileMapManager);
        BindWorldGridToContainers(worldGrid);
        BindWorldGridToSmallItems(worldGrid);
    }

    public void BindSpawnedCharacter(GameObject instance)
    {
        if (instance == null)
            return;

        if (_tileMapManager == null)
            _tileMapManager = GetComponent<TileMapManager>();
        if (_tileMapManager == null)
            return;

        IWorldGrid worldGrid = _tileMapManager.WorldGrid;
        CharacterState state = instance.GetComponent<CharacterState>();
        if (state != null && worldGrid != null)
            state.BindWorldGrid(worldGrid);

        if (_tileMapManager.Model is not TileMapModel)
            return;

        MapCollisionServices services = _tileMapManager.MapCollisionServices;
        if (services == null)
            return;

        CharacterMotor motor = instance.GetComponent<CharacterMotor>();
        motor?.BindMapCollision(services);

        EnsureSwimHosts(instance);

        CharacterAttacker attacker = instance.GetBodyComponent<CharacterAttacker>();
        attacker?.BindMapCollision(services.LineCast);

        CharacterHearing hearing = instance.GetBodyComponent<CharacterHearing>();
        hearing?.BindMapCollision(services.LineCast);

        DirectionalRaycaster raycaster = instance.GetComponent<DirectionalRaycaster>();
        if (raycaster != null && state != null)
            raycaster.BindMapCollision(services.LineCast, state);
    }

    static void BindWorldGridToCharacters(IWorldGrid worldGrid)
    {
        var states = FindObjectsByType<CharacterState>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < states.Length; i++)
            states[i].BindWorldGrid(worldGrid);
    }

    static void BindMapCollisionServices(TileMapManager manager)
    {
        if (manager.Model is not TileMapModel tileModel)
            return;

        MapCollisionServices services = manager.MapCollisionServices;
        if (services == null)
            return;

        BindCharacterLocomotions<CharacterMotor>(services);
        BindCharacterHearing(services.LineCast);
        EnsureSwimHostsOnSceneCharacters();

        var attackers = FindObjectsByType<CharacterAttacker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < attackers.Length; i++)
            attackers[i].BindMapCollision(services.LineCast);

        var aimControllers = FindObjectsByType<PlayerAimController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < aimControllers.Length; i++)
            aimControllers[i].BindMapCollision(services.LineCast);

        var raycasters = FindObjectsByType<DirectionalRaycaster>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < raycasters.Length; i++)
        {
            var state = raycasters[i].GetComponent<CharacterState>();
            if (state == null)
                continue;

            raycasters[i].BindMapCollision(services.LineCast, state);
        }
    }

    static void BindCharacterLocomotions<T>(MapCollisionServices services)
        where T : MonoBehaviour, ICharacterLocomotion
    {
        var locomotions = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < locomotions.Length; i++)
            locomotions[i].BindMapCollision(services);
    }

    static void BindCharacterHearing(MapTopologyLineCast lineCast)
    {
        if (lineCast == null)
            return;

        var hearings = FindObjectsByType<CharacterHearing>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < hearings.Length; i++)
            hearings[i].BindMapCollision(lineCast);
    }

    static void BindWorldGridToContainers(IWorldGrid worldGrid)
    {
        if (worldGrid == null)
            return;

        var interactables = FindObjectsByType<ContainerInteractable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < interactables.Length; i++)
            interactables[i].BindWorldGrid(worldGrid);
    }

    static void BindWorldGridToSmallItems(IWorldGrid worldGrid)
    {
        if (worldGrid == null)
            return;

        var items = FindObjectsByType<SmallItemObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < items.Length; i++)
            items[i].BindWorldGrid(worldGrid);
    }

    static void EnsureSwimHostsOnSceneCharacters()
    {
        var states = FindObjectsByType<CharacterState>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < states.Length; i++)
            EnsureSwimHosts(states[i].gameObject);
    }

    static void EnsureSwimHosts(GameObject instance)
    {
        if (instance == null || instance.GetBodyComponent<CharacterState>() == null)
            return;

        if (instance.GetBodyComponent<CharacterBodyHost>() == null)
            return;

        if (instance.GetComponent<CharacterSwimHost>() == null)
            instance.AddComponent<CharacterSwimHost>();
        if (instance.GetComponent<CharacterBreathHost>() == null)
            instance.AddComponent<CharacterBreathHost>();
    }
}
