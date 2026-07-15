// ============================================================
// PlayerInventoryHost — 플레이어 몸통 인벤 (Detector 대상 아님, Host가 Session 등록)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class PlayerInventoryHost : MonoBehaviour, IInventoryContainerProvider
{
    public const string DefaultInstanceId = "player-body";
    public const string DefaultContainerDefId = "player_body";

    [Required, SerializeField] CharacterState _characterState;
    [SerializeField] string _containerDefId = DefaultContainerDefId;
    [SerializeField] string _containerId = DefaultInstanceId;
    [SerializeField, Min(0f)] float _baseMaxWeight = 50f;
    [SerializeField, Min(0f)] float _baseMaxVolume = 30f;

    InventoryContainer _container;
    PlayerCarryCapacityPolicy _capacityPolicy;

    public InventoryContainer Container => _container;
    public string ContainerId => _containerId;
    public Vector3 WorldPosition => transform.position;
    public Vector3Int GridPosition => _characterState.GridPos;

    void Awake()
    {
        ContainerData containerDef = GameplayData.GetContainer(_containerDefId);
        if (containerDef == null)
        {
            Debug.LogWarning($"[PlayerInventoryHost] Container definition '{_containerDefId}' not found in GameData.", this);
            return;
        }

        _capacityPolicy = new PlayerCarryCapacityPolicy(
            () => _baseMaxWeight,
            () => _baseMaxVolume);
        string instanceId = string.IsNullOrWhiteSpace(_containerId) ? DefaultInstanceId : _containerId;
        _container = InventoryContainer.Create(containerDef, _capacityPolicy, instanceId);
    }

    void OnEnable() => InventoryContainerRegistry.Register(this);
    void OnDisable() => InventoryContainerRegistry.Unregister(this);

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_characterState) TryGetComponent(out _characterState);
        if (string.IsNullOrWhiteSpace(_containerId))
            _containerId = DefaultInstanceId;
    }

    public bool IsAvailableToPlayer(GameObject player) =>
        player != null && player == _characterState.gameObject;

    public bool RegisterToSession(InventorySession session) =>
        session != null && _container != null && session.TryAddSidebarContainer(_container);

    public bool UnregisterFromSession(InventorySession session) =>
        session != null && _container != null && session.TryRemoveSidebarContainer(_container.InstanceId);
}
