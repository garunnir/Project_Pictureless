// ============================================================
// RuntimeDebugConsoleInputBridge — 콘솔 창 열림에 Debug 인풋 모드 연동
// ============================================================

using System;
using IngameDebugConsole;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeDebugConsoleInputBridge : MonoBehaviour
{
    IDisposable _debugInputScope;
    bool _subscribed;

    void OnEnable()
    {
        TrySubscribe();
        SyncFromConsoleState();
    }

    void Start()
    {
        TrySubscribe();
        SyncFromConsoleState();
    }

    void OnDisable()
    {
        Unsubscribe();
        ReleaseDebugInput();
    }

    void OnDestroy()
    {
        Unsubscribe();
        ReleaseDebugInput();
    }

    void TrySubscribe()
    {
        if (_subscribed)
            return;

        DebugLogManager manager = DebugLogManager.Instance;
        if (manager == null)
            manager = GetComponent<DebugLogManager>();
        if (manager == null)
            return;

        manager.OnLogWindowShown += OnLogWindowShown;
        manager.OnLogWindowHidden += OnLogWindowHidden;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
            return;

        DebugLogManager manager = DebugLogManager.Instance;
        if (manager == null)
            manager = GetComponent<DebugLogManager>();

        if (manager != null)
        {
            manager.OnLogWindowShown -= OnLogWindowShown;
            manager.OnLogWindowHidden -= OnLogWindowHidden;
        }

        _subscribed = false;
    }

    void SyncFromConsoleState()
    {
        DebugLogManager manager = DebugLogManager.Instance;
        if (manager == null)
            manager = GetComponent<DebugLogManager>();
        if (manager == null)
            return;

        if (manager.IsLogWindowVisible)
            OnLogWindowShown();
        else
            OnLogWindowHidden();
    }

    void OnLogWindowShown()
    {
        if (_debugInputScope != null)
            return;

        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        _debugInputScope = input.AcquireDebugInput(this);
    }

    void OnLogWindowHidden()
    {
        ReleaseDebugInput();
    }

    void ReleaseDebugInput()
    {
        _debugInputScope?.Dispose();
        _debugInputScope = null;
    }
}
