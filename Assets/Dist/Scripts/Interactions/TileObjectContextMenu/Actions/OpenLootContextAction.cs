// ============================================================
// OpenLootContextAction — 월드 컨테이너 루팅 UI 열기
// ============================================================

public sealed class OpenLootContextAction : IContextMenuAction
{
    readonly ContainerInteractable _container;

    public OpenLootContextAction(ContainerInteractable container)
    {
        _container = container;
    }

    public string GetDisabledReason()
    {
        if (_container == null || _container.Container == null)
            return "missing";

        return null;
    }

    public void Execute()
    {
        if (_container == null)
            return;

        _container.OpenLoot();
    }
}
