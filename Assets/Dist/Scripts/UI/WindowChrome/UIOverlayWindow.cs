// ============================================================
// UIOverlayWindow — 창 루트 Enable/Disable 시 hit-test 등록
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
}
