// ============================================================
// GameViewDepthTextureDebug — Game 뷰 depth texture 디버그 표시 SSOT
// ============================================================

/// <summary>
/// URP <c>_CameraDepthTexture</c>를 Game 뷰에 Linear01 그레이스케일로 오버레이한다.
/// <see cref="GameViewDepthTextureDebugDriver"/>가 구동한다.
/// </summary>
public static class GameViewDepthTextureDebug
{
    public static bool Enabled { get; private set; }

    public static void Toggle() => SetEnabled(!Enabled);

    public static void SetEnabled(bool enabled) => Enabled = enabled;
}
