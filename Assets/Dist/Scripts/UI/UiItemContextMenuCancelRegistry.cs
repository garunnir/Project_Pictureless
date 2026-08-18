// ============================================================
// UiItemContextMenuCancelRegistry — 인벤 컨텍스트 메뉴 ESC 닫기 (asmdef 경계)
// ============================================================

using System;
using System.Collections.Generic;

public static class UiItemContextMenuCancelRegistry
{
    static readonly List<Func<bool>> _tryCloseHandlers = new(4);

    public static void Register(Func<bool> tryClose)
    {
        if (tryClose == null || _tryCloseHandlers.Contains(tryClose))
            return;

        _tryCloseHandlers.Add(tryClose);
    }

    public static void Unregister(Func<bool> tryClose)
    {
        if (tryClose == null)
            return;

        _tryCloseHandlers.Remove(tryClose);
    }

    public static bool TryCloseAnyOpen()
    {
        for (int i = _tryCloseHandlers.Count - 1; i >= 0; i--)
        {
            Func<bool> handler = _tryCloseHandlers[i];
            if (handler == null)
                continue;

            if (handler())
                return true;
        }

        return false;
    }
}
