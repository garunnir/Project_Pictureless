// ============================================================
// CharacterSpawnGizmoColors — 스폰 셀 와이어 색 SSOT
// ============================================================

using UnityEngine;

public static class CharacterSpawnGizmoColors
{
    public static readonly Color Possessed = new Color(0.3f, 0.85f, 0.4f, 0.9f);
    public static readonly Color Npc = new Color(0.95f, 0.55f, 0.2f, 0.9f);
    public static readonly Color MarkerMismatch = new Color(1f, 0.25f, 0.2f, 0.95f);

    public static Color ForRole(CharacterSpawnRole role) =>
        role == CharacterSpawnRole.Possessed ? Possessed : Npc;
}
