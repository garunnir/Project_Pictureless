// ============================================================
// InteractionLabels — 상호작용 힌트 표시 문구 SSOT
// ============================================================

public static class InteractionLabels
{
    const string KeyDoorOpen = "Interaction.DoorOpen";
    const string KeyDoorClose = "Interaction.DoorClose";

    public static string DoorOpen => Loc.Get(KeyDoorOpen, "문 열기");
    public static string DoorClose => Loc.Get(KeyDoorClose, "문 닫기");
}
