// ============================================================
// CraftingReservedWorkSource — 제작 진행 → ReservedWorkHub
// ============================================================

using System;
using UnityEngine;

public sealed class CraftingReservedWorkSource : MonoBehaviour, IReservedWorkSource
{
    [SerializeField] UICraftingController _controller;

    bool _lastBusy;

    public bool HasActiveWork => _controller != null && _controller.IsCraftRunning;

    public event Action Changed;

    void Awake()
    {
        if (_controller == null)
            _controller = GetComponent<UICraftingController>();
    }

    void OnEnable() => ReservedWorkHub.Register(this);

    void OnDisable() => ReservedWorkHub.Unregister(this);

    void Update()
    {
        bool busy = HasActiveWork;
        if (busy == _lastBusy)
            return;

        _lastBusy = busy;
        Changed?.Invoke();
    }
}
