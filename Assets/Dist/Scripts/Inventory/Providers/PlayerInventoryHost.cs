// ============================================================
// PlayerInventoryHost — 플레이어 몸통 인벤 (Detector 대상 아님, Host가 Session 등록)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class PlayerInventoryHost : MonoBehaviour, IInventoryContainerProvider
{
    public const string DefaultInstanceId = "player-body";

    [Required, SerializeField] CharacterState _characterState;
    [SerializeField] ContainerDefinitionSO _bodyDefinition;
    [SerializeField, Min(0f)] float _baseMaxWeight = 50f;
    [SerializeField, Min(0f)] float _baseMaxVolume = 30f;

    InventoryContainer _container;
    PlayerCarryCapacityPolicy _capacityPolicy;

    public InventoryContainer Container => _container;
    public Vector3Int GridPosition => _characterState.GridPos;

    void Awake()
    {
        if (_bodyDefinition == null)
        {
            Debug.LogWarning("[PlayerInventoryHost] ContainerDefinitionSO is not assigned.", this);
            return;
        }

        _capacityPolicy = new PlayerCarryCapacityPolicy(
            () => _baseMaxWeight,
            () => _baseMaxVolume);
        _container = InventoryContainer.Create(_bodyDefinition, _capacityPolicy, DefaultInstanceId);
    }

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_characterState) TryGetComponent(out _characterState);
    }

    public bool IsAvailableToPlayer(GameObject player) =>
        player != null && player == _characterState.gameObject;

    public bool RegisterToSession(InventorySession session) =>
        session != null && _container != null && session.TryAddSidebarContainer(_container);

    public bool UnregisterFromSession(InventorySession session) =>
        session != null && _container != null && session.TryRemoveSidebarContainer(_container.InstanceId);
}
