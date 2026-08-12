// ============================================================
// CharacterAimIntent — 조준 부위 선호 (액션·무기와 독립)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterAimIntent : MonoBehaviour
{
    [SerializeField] string _preferredPartId = BodyPartIds.Torso;

    public string PreferredPartId =>
        string.IsNullOrEmpty(_preferredPartId) ? BodyPartIds.Torso : _preferredPartId;

    /// <summary>NPC 조준 유지. 플레이어는 CharacterState.IsAiming이 SSOT.</summary>
    public bool AimHeld { get; private set; }

    public void SetPreferredPart(string partId)
    {
        _preferredPartId = string.IsNullOrEmpty(partId) ? BodyPartIds.Torso : partId;
    }

    public void SetAimHeld(bool held) => AimHeld = held;
}
