// ============================================================
// InteractionLabels — 상호작용 힌트 표시 문구 SSOT
// ============================================================

public static class InteractionLabels
{
    public const string KeyDoorOpen = "Interaction.DoorOpen";
    public const string KeyDoorClose = "Interaction.DoorClose";
    public const string KeyOpenContainer = "Interaction.Crate.Hint";
    public const string KeyInteractKey = "Interaction.Key";

    public static string DoorOpen => Loc.Get(KeyDoorOpen);
    public static string DoorClose => Loc.Get(KeyDoorClose);
    public static string OpenContainer => Loc.Get(KeyOpenContainer);
    public static string InteractKey => Loc.Get(KeyInteractKey);
}
