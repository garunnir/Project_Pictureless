// ============================================================
// CharacterFaction — 캐릭터 세력 식별 SO
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "CharacterFaction", menuName = "Dist/Character/Faction")]
public sealed class CharacterFaction : ScriptableObject
{
    [SerializeField] string _id;
    [SerializeField] string _displayName;

    public string Id => _id;
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;
}
