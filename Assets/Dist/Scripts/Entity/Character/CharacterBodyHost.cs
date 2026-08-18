// ============================================================
// CharacterBodyHost — 엔티티별 ICharacterBody 소유 (플레이어·NPC)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterBodyHost : MonoBehaviour
{
    [SerializeField] bool _useGameplayDataBody;
    [SerializeField] int _seedStrength = 8;
    [SerializeField] bool _prototypeSeed;

    ICharacterBody _body;

    static readonly List<CharacterBodyHost> s_active = new(16);

    public ICharacterBody Body
    {
        get
        {
            if (_body == null)
                EnsureBody();
            return _body;
        }
    }

    public bool UseGameplayDataBody => _useGameplayDataBody;

    public static int ActiveCount => s_active.Count;

    public static CharacterBodyHost GetActive(int index) => s_active[index];

    void Awake() => EnsureBody();

    void OnEnable()
    {
        if (!s_active.Contains(this))
            s_active.Add(this);
    }

    void OnDisable() => s_active.Remove(this);

    void EnsureBody()
    {
        if (_body != null)
            return;

        if (_useGameplayDataBody)
        {
            _body = GameplayData.Body;
            return;
        }

        _body = CharacterBody.CreateHumanDefault(_seedStrength, _prototypeSeed);
    }

    public void BindBody(ICharacterBody body)
    {
        _body = body;
    }

    public void ApplyBodyDto(CharacterBodyDto dto)
    {
        Body.FromDto(dto);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Sever Arm L")]
    void DebugSeverArmL() => Body?.RemovePart(BodyPartIds.UpperArmL);

    [ContextMenu("Debug/Regen Arm L")]
    void DebugRegenArmL() => BodyPartRestoreService.TryRegenerate(Body, BodyPartIds.UpperArmL);

    [ContextMenu("Debug/Attach Prosthetic Arm L")]
    void DebugAttachProstheticArmL() =>
        BodyPartRestoreService.TryAttachProsthetic(Body, BodyPartIds.UpperArmL);

    [ContextMenu("Debug/Sever Thigh L")]
    void DebugSeverThighL() => Body?.RemovePart(BodyPartIds.ThighL);

    [ContextMenu("Debug/Regen Thigh L")]
    void DebugRegenThighL() => BodyPartRestoreService.TryRegenerate(Body, BodyPartIds.ThighL);

    [ContextMenu("Debug/Attach Prosthetic Thigh L")]
    void DebugAttachProstheticThighL() =>
        BodyPartRestoreService.TryAttachProsthetic(Body, BodyPartIds.ThighL);

    [ContextMenu("Debug/Verify Body DTO Round-Trip")]
    void DebugVerifyBodyDtoRoundTrip()
    {
        Debug.Log("[CharacterBodyHost] CharacterBody DTO " + CharacterBodyDtoRoundTrip.Execute(), this);
    }
#endif
}
