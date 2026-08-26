// ============================================================
// CharacterFactionCatalog — 세력 쌍의 적대/중립/아군 관계표
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public enum FactionStance
{
    Ally = 0,
    Neutral = 1,
    Hostile = 2
}

[Serializable]
public struct CharacterFactionRelation
{
    public CharacterFaction a;
    public CharacterFaction b;
    public FactionStance stance;
}

[CreateAssetMenu(fileName = "CharacterFactionCatalog", menuName = "Dist/Character/Faction Catalog")]
public sealed class CharacterFactionCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Character/CharacterFactionCatalog.Default.asset";

    [SerializeField] List<CharacterFactionRelation> _relations = new();

    public bool TryGetStance(CharacterFaction a, CharacterFaction b, out FactionStance stance)
    {
        stance = FactionStance.Neutral;
        if (a == null || b == null)
            return false;
        if (ReferenceEquals(a, b))
        {
            stance = FactionStance.Ally;
            return true;
        }

        for (int i = 0; i < _relations.Count; i++)
        {
            CharacterFactionRelation row = _relations[i];
            if (row.a == null || row.b == null)
                continue;

            bool match =
                (ReferenceEquals(row.a, a) && ReferenceEquals(row.b, b)) ||
                (ReferenceEquals(row.a, b) && ReferenceEquals(row.b, a));
            if (!match)
                continue;

            stance = row.stance;
            return true;
        }

        return false;
    }
}
