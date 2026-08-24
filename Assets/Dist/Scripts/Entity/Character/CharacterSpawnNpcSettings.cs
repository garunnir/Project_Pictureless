// ============================================================
// CharacterSpawnNpcSettings — 스폰 행의 NpcManager 설정 (character는 스폰 후 채움)
// ============================================================

using System;
using UnityEngine;

[Serializable]
public sealed class CharacterSpawnNpcSettings
{
    public Transform[] waypoints;
    public MovementStyle patrolStyle;
    public MovementStyle chaseStyle;
    public MovementStyle holdStyle;
    [Min(0f)] public float attackStandDistance = NpcAgentDefaults.AttackStandDistance;
    [Min(0f)] public float alertSeconds = NpcAgentDefaults.AlertSeconds;
    [Tooltip("무력화: 조준 다리.")]
    public bool suppressMode;

    public NpcAgentEntry ToAgentEntry(Transform character)
    {
        return new NpcAgentEntry
        {
            character = character,
            waypoints = waypoints,
            patrolStyle = patrolStyle,
            chaseStyle = chaseStyle,
            holdStyle = holdStyle,
            attackStandDistance = attackStandDistance,
            alertSeconds = alertSeconds,
            suppressMode = suppressMode
        };
    }
}
