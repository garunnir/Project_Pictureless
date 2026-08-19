// ============================================================
// CharacterSessionHub — 본체 인벤·기어·액션 그래프 입구. Possess 시 플레이어 세션 선언
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
[RequireComponent(typeof(CharacterSkillsHost))]
[RequireComponent(typeof(PlayerGearHost))]
[RequireComponent(typeof(PlayerEncumbranceHost))]
public sealed class CharacterSessionHub : MonoBehaviour
{
    [SerializeField] CharacterBodyHost _bodyHost;
    [SerializeField] CharacterSkillsHost _skillsHost;
    [SerializeField] PlayerGearHost _gear;
    [SerializeField] PlayerEncumbranceHost _encumbrance;
    [SerializeField] InventoryTimedMoveHost _timedMove;
    [SerializeField] PlayerInventoryHost _inventory;
    [SerializeField] NearbyContainerDetector _detector;
    [SerializeField] CharacterActionHost _action;

    public static CharacterSessionHub Player { get; private set; }

    public CharacterBodyHost BodyHost => _bodyHost;
    public CharacterActionHost Action => _action;
    public PlayerInventoryHost Inventory => _inventory;
    public NearbyContainerDetector Detector => _detector;

    void Awake() => EnsureRefs();

    void OnValidate() => EnsureRefs();

    void Reset() => EnsureRefs();

    void OnDisable()
    {
        if (Player == this)
            Player = null;
    }

    public void BecomePlayer(PlayerMovement movement, PlayerInventoryRuntime inventoryRuntime)
    {
        EnsureRefs();
        Player = this;

        inventoryRuntime?.BindBody(_inventory, _detector);

        _timedMove?.ClaimActive();
        if (_encumbrance != null)
        {
            _encumbrance.BindMovement(movement);
            _encumbrance.ClaimActive();
        }

        if (_gear != null)
        {
            _gear.BindMovement(movement);
            _gear.ClaimActive();
        }

        if (_bodyHost != null && _bodyHost.Body != null)
            GameplayData.Body = _bodyHost.Body;

        if (_skillsHost != null && _skillsHost.Skills is DefaultCharacterSkills seeded)
            GameplayData.Stats = new DefaultPlayerStats(seeded);

        PlayerStatusUIBridge.RebindFromGameplayData();
    }

    void EnsureRefs()
    {
        if (_bodyHost == null)
            TryGetComponent(out _bodyHost);
        if (_skillsHost == null)
            TryGetComponent(out _skillsHost);
        if (_gear == null)
            TryGetComponent(out _gear);
        if (_encumbrance == null)
            TryGetComponent(out _encumbrance);
        if (_timedMove == null)
            TryGetComponent(out _timedMove);
        if (_inventory == null)
            TryGetComponent(out _inventory);
        if (_detector == null)
            TryGetComponent(out _detector);
        if (_action == null)
            TryGetComponent(out _action);
    }
}
