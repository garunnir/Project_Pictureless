// ============================================================
// PlayerInventoryHost — 캐릭터 몸통 인벤 (인스턴스별, Detector 대상 아님)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class PlayerInventoryHost : MonoBehaviour, IInventoryContainerProvider
{
    public const string DefaultInstanceId = "player-body";
    public const string DefaultContainerDefId = "player_body";
    public const string UniqueBodyInstanceIdPrefix = "character-body-";

    public static string CreateUniqueBodyInstanceId() =>
        UniqueBodyInstanceIdPrefix + Guid.NewGuid().ToString("N");

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

    public void AssignInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return;

        if (_container != null)
        {
            Debug.LogError("[PlayerInventoryHost] AssignInstanceId must run before Awake.", this);
            return;
        }

        _containerId = instanceId;
    }

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

    /// <summary>
    /// 자기 몸만 true. 쓰러진 NPC 루팅은 이 게이트를 연다 (살아 있으면 Nearby 스캔에서 제외).
    /// </summary>
    public bool IsAvailableToPlayer(GameObject player) =>
        player != null && player == _characterState.gameObject;

    public bool RegisterToSession(InventorySession session) =>
        session != null && _container != null && session.TryAddSidebarContainer(_container);

    public bool UnregisterFromSession(InventorySession session) =>
        session != null && _container != null && session.TryRemoveSidebarContainer(_container.InstanceId);
}
