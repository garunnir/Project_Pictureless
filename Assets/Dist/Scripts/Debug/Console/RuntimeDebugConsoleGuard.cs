// ============================================================
// RuntimeDebugConsoleGuard — 릴리스 빌드에서 런타임 콘솔 비활성화
// ============================================================

using UnityEngine;

public sealed class RuntimeDebugConsoleGuard : MonoBehaviour
{
    void Awake()
    {
        if (Debug.isDebugBuild)
            return;

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
