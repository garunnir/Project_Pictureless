// ============================================================
// HudLayoutEdit — HUD 레이아웃 편집 모드 플래그 SSOT
// ============================================================

using System;

public static class HudLayoutEdit
{
    static bool _active;

    public static bool IsActive => _active;

    public static event Action Changed;

    /// <summary>
    /// UI에서 동일 값으로 다시 SetActive를 호출하는 경우가 있어
    /// 참가자들의 시각/히트 상태를 강제로 재동기화한다.
    /// </summary>
    public static void Refresh() => Changed?.Invoke();

    public static void SetActive(bool active)
    {
        if (_active == active)
            return;

        _active = active;
        Changed?.Invoke();
    }
}
