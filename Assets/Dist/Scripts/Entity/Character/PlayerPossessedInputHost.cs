// ============================================================
// PlayerPossessedInputHost — 디바이스 입력을 possessed 본체 공용 API에 연결
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerPossessedInputHost : MonoBehaviour, IPlayControllable
{
    [SerializeField] PlayerMovement _movement;
    [SerializeField] PlayerAimController _aimController;
    [SerializeField] PlayerCombatController _combatController;
    [SerializeField] TileObjectPointerController _tileObjectPointer;
    [SerializeField] PlayerFloorVisibilityDriver _floorVisibility;
    [SerializeField] SightLineProximityBlendDriver _sightLineBlend;
    [SerializeField] CharacterVisibilityBroadcaster _visibilityBroadcaster;
    [SerializeField] PlayerInventoryRuntime _inventoryRuntime;

    GameObject _body;
    CharacterState _bodyState;
    Transform _bodyTransform;

    public Transform BodyTransform => _bodyTransform;
    public CharacterState BodyState => _bodyState;
    public GameObject Body => _body;

    void Awake() => EnsureRefs();

    void OnValidate() => EnsureRefs();

    void EnsureRefs()
    {
        if (_movement == null)
            TryGetComponent(out _movement);
        if (_aimController == null)
            TryGetComponent(out _aimController);
        if (_combatController == null)
            TryGetComponent(out _combatController);
        if (_tileObjectPointer == null)
            TryGetComponent(out _tileObjectPointer);
        if (_inventoryRuntime == null)
            TryGetComponent(out _inventoryRuntime);
    }

    public void Bind(GameObject body)
    {
        EnsureRefs();
        _body = body;
        _bodyTransform = body != null ? body.transform : null;
        _bodyState = body != null ? body.GetComponent<CharacterState>() : null;

        CharacterMotor motor = body != null ? body.GetComponent<CharacterMotor>() : null;
        CharacterAttacker attacker = body != null ? body.GetComponent<CharacterAttacker>() : null;
        CharacterActionHost actionHost = body != null ? body.GetComponent<CharacterActionHost>() : null;
        CharacterFacingAnim facing = body != null ? body.GetComponent<CharacterFacingAnim>() : null;

        _movement?.BindBody(motor, _bodyState, facing);
        _aimController?.BindBody(_bodyState, _bodyTransform);
        _combatController?.BindBody(attacker, _bodyState, actionHost);

        _floorVisibility?.SetPlayerState(_bodyState);
        _sightLineBlend?.SetPlayerState(_bodyState);
        _visibilityBroadcaster?.BindPlayerState(_bodyState);

        TileMapManager map = FindFirstObjectByType<TileMapManager>();
        if (map != null && map.MapCollisionServices != null)
            _aimController?.BindMapCollision(map.MapCollisionServices.LineCast);

        if (body != null && body.TryGetComponent(out CharacterSessionHub session))
            session.BecomePlayer(_movement, _inventoryRuntime);
        else
            Debug.LogError("[PlayerPossessedInputHost] CharacterSessionHub is required on the possessed body.", this);
    }

    public void SetControlEnabled(bool enabled)
    {
        _movement?.SetControllEnabled(enabled);
        _aimController?.SetEnabled(enabled);
        _combatController?.SetEnabled(enabled);
        _tileObjectPointer?.SetEnabled(enabled);
    }
}
