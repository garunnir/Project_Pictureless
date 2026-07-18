// ============================================================
// DoorToggleContextAction — 문 열기/닫기 리프 실행
// ============================================================

using Interactions;

public sealed class DoorToggleContextAction : IContextMenuAction
{
    readonly DoorInteractable _door;

    public DoorToggleContextAction(DoorInteractable door)
    {
        _door = door;
    }

    public string GetDisabledReason() => _door == null ? "missing" : null;

    public void Execute()
    {
        if (_door == null)
            return;

        _door.Toggle();
    }
}
