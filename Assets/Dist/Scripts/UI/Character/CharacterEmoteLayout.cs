// ============================================================
// CharacterEmoteLayout — 월드 이모트 프리팹 수치 SSOT (Patch용)
// ============================================================

using UnityEngine;

public static class CharacterEmoteLayout
{
    public const string PrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Character/Grp_CharacterEmote.prefab";
    public const string RootName = "Grp_CharacterEmote";
    public const string IconName = "Img_Emote";
    public const float WorldScale = 0.01f;
    public const float LocalY = 2.55f;
    public static readonly Vector2 Size = new(48f, 48f);
    public static readonly Color DefaultIconColor = Color.white;
}
