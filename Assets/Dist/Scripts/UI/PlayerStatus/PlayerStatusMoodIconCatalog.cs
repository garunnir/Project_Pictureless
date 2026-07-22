// ============================================================
// PlayerStatusMoodIconCatalog — MoodIconId → Front Sprite, 공용 Back SSOT
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatusMoodIconCatalog",
    menuName = "Dist/PlayerStatus/Mood Icon Catalog")]
public sealed class PlayerStatusMoodIconCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public MoodIconId IconId;
        public Sprite FrontSprite;
    }

    [SerializeField] Sprite _backPlate;
    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    public Sprite BackPlate => _backPlate;

    public bool TryGetFront(MoodIconId iconId, out Sprite sprite)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].IconId != iconId)
                continue;

            sprite = _entries[i].FrontSprite;
            return sprite != null;
        }

        sprite = null;
        return false;
    }
}
