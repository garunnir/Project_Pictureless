// ============================================================
// CharacterEmoteCatalog — EmoteId → sprite / tint / ObserverOnly SSOT
// ============================================================

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterEmoteCatalog",
    menuName = "Dist/Character/Emote Catalog")]
public sealed class CharacterEmoteCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Character/CharacterEmoteCatalog.asset";

    [Serializable]
    public struct Entry
    {
        public EmoteId Id;
        public Sprite Sprite;
        public Color Tint;
        public bool ObserverOnly;
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    public bool TryGetEntry(EmoteId id, out Entry entry)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Id != id)
                continue;

            entry = _entries[i];
            return entry.Sprite != null;
        }

        entry = default;
        return false;
    }
}
