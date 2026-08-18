// ============================================================
// UiContextMenuCancelConsumer — ContextMenu ESC 소비 (DistScript 어댑터)
// ============================================================

using UnityEngine;

public sealed class UiContextMenuCancelConsumer : MonoBehaviour, IUiCancelConsumer
{
    public int CancelPriority => UiCancelPriority.ContextMenu;

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable() => UiCancelRouter.Unregister(this);

    public bool TryHandleCancel()
    {
        UIContextMenuHost host = UIContextMenuHost.Instance;
        if (host != null && host.IsOpen)
        {
            host.Hide();
            return true;
        }

        if (UiItemContextMenuCancelRegistry.TryCloseAnyOpen())
            return true;

        return false;
    }
}
