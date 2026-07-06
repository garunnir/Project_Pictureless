// ============================================================
// SceneSingleton — 씬 배치 싱글톤 (Legacy Singleton과 분리)
// ============================================================

using UnityEngine;

public abstract class SceneSingleton<T> : MonoBehaviour where T : class
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this as T)
        {
            Debug.LogWarning($"[SceneSingleton] Duplicate {typeof(T).Name} ignored.", this);
            return;
        }

        Instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this as T)
            Instance = null;
    }
}
