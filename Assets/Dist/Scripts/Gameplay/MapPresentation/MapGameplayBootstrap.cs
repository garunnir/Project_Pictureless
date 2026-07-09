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

    void Start()
    {
        if (_tileMapManager == null)
            _tileMapManager = GetComponent<TileMapManager>();

        if (_tileMapManager == null)
            return;

        IWorldGrid worldGrid = _tileMapManager.WorldGrid;
        if (worldGrid != null)
            BindWorldGridToCharacters(worldGrid);

        BindMapCollisionServices(_tileMapManager);
        BindWorldGridToContainers(worldGrid);
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

        var movements = FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < movements.Length; i++)
            movements[i].BindMapCollision(services);

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
}
