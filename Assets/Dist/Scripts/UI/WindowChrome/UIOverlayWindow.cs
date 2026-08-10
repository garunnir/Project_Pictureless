// ============================================================
// UIOverlayWindow — 창 루트 hit-test 등록 + 레이어 내 BringToFront
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIOverlayWindow : MonoBehaviour
{
    RectTransform _rect;

    void OnEnable()
    {
        if (_rect == null)
            _rect = transform as RectTransform;

        UIOverlayWindowHitTest.Register(_rect);
    }

    void OnDisable()
    {
        if (_rect == null)
            _rect = transform as RectTransform;

        UIOverlayWindowHitTest.Unregister(_rect);
    }

    /// <summary>같은 부모(레이어 루트) 형제 중 맨 위. 레이어 승격 없음.</summary>
    public void BringToFront() => transform.SetAsLastSibling();
}
