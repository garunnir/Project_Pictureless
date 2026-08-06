// ============================================================
// HandProficiencyIds — 보조손 숙련 스킬 ID SSOT (hand_l / hand_r)
// ============================================================

/// <summary>듀얼 보조손 OffHandDpsFactor 입력. BodyPartIds 해부와 문자열만 공유, 스킬 축.</summary>
public static class HandProficiencyIds
{
    public const string Left = "hand_l";
    public const string Right = "hand_r";

    public static string ForHand(WieldHand hand) =>
        hand == WieldHand.Left ? Left : Right;
}
