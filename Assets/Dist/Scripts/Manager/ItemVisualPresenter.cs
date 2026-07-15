// ============================================================
// ItemVisualPresenter — 아이템 표시 아이콘 공통 진입점 (UI·월드 SSOT)
// ============================================================

using UnityEngine;

public static class ItemVisualPresenter
{
    // TODO: Addressables/Resources 기반 아이콘 로딩 전략 결정 후 구현
    public static Sprite GetDisplayIcon(string itemId)
    {
        return null;
    }

    public static Sprite GetDefaultIcon() => null;
}
