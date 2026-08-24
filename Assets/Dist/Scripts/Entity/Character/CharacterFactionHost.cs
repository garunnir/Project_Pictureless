// ============================================================
// CharacterFactionHost — 본체가 속한 세력을 보관
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterFactionHost : MonoBehaviour
{
    [SerializeField] CharacterFaction _faction;

    public CharacterFaction Faction => _faction;

    public void ApplyFromDefinition(CharacterDefinition definition)
    {
        _faction = definition != null ? definition.Faction : null;
    }
}
