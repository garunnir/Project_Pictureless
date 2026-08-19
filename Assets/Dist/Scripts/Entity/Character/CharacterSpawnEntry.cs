// ============================================================
// CharacterSpawnEntry — 캐릭터 한 명의 스폰 행 (셀 SSOT)
// ============================================================

using System;
using UnityEngine;

[Serializable]
public sealed class CharacterSpawnEntry
{
    public CharacterDefinition definition;
    public Vector3Int cell;
    public CharacterSpawnPoint marker;
    public CharacterSpawnRole role = CharacterSpawnRole.Npc;
    public CharacterSpawnNpcSettings npc = new();

    public Vector3Int ResolveCell() => cell;

    public void SyncCellFromMarker()
    {
        if (marker != null)
            cell = marker.Cell;
    }
}
