// ============================================================
// UiMenuInputBehaviour — [A] UiMenu 입력 자동 획득 (건설 UI 등)
// ============================================================

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UiMenuInputBehaviour : MonoBehaviour
{
    IDisposable _scope;

    void OnEnable() => _scope = InputManager.Instance.AcquireUiMenuInput(this);

    void OnDisable()
    {
        _scope?.Dispose();
        _scope = null;
    }
}
