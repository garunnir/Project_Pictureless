// ============================================================
// GameViewDepthTextureDebugDriver — Play 모드 F10 토글 (렌더는 URP Feature)
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Play 모드에서만 F10으로 <see cref="GameViewDepthTextureDebug"/>를 토글한다.
/// 실제 그리기는 <see cref="GameViewDepthTextureDebugRendererFeature"/>가 담당한다.
/// </summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class GameViewDepthTextureDebugDriver : MonoBehaviour
{
    [SerializeField] Key _toggleKey = Key.F10;
    [SerializeField] bool _showHud = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (!Application.isPlaying)
            return;

        if (FindAnyObjectByType<GameViewDepthTextureDebugDriver>() != null)
            return;

        var go = new GameObject(nameof(GameViewDepthTextureDebugDriver))
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        go.AddComponent<GameViewDepthTextureDebugDriver>();
    }

    void OnDisable() => GameViewDepthTextureDebug.SetEnabled(false);

    void OnDestroy() => GameViewDepthTextureDebug.SetEnabled(false);

    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
            GameViewDepthTextureDebug.Toggle();
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !_showHud || !GameViewDepthTextureDebug.Enabled)
            return;

        GUI.Label(new Rect(8f, 8f, 360f, 24f), $"Depth (Linear01) — {_toggleKey} off");
    }
}
