// ============================================================
// CharacterActionGaugeLayout — 월드 행동 게이지 프리팹 수치 SSOT (Patch용)
// ============================================================

using UnityEngine;

public static class CharacterActionGaugeLayout
{
    public const string PrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Character/Grp_CharacterActionGauge.prefab";
    public const string RootName = "Grp_CharacterActionGauge";
    public const string FillName = "Img_Fill";
    public const float WorldScale = 0.01f;
    public const float LocalY = 2.2f;
    public static readonly Vector2 Size = new(120f, 14f);
    public static readonly Color FillColor = new(0.35f, 0.82f, 0.45f, 0.95f);
}
