// ============================================================
// InventoryReservedWorkSource — InventoryTimedMoveHost → ReservedWorkHub
// ============================================================

using System;
using UnityEngine;

public sealed class InventoryReservedWorkSource : MonoBehaviour, IReservedWorkSource
{
    [SerializeField] InventoryTimedMoveHost _moveHost;

    bool _lastBusy;

    public bool HasActiveWork => _moveHost != null && _moveHost.IsBusy;

    public event Action Changed;

    void Awake()
    {
        if (_moveHost == null)
            _moveHost = GetComponent<InventoryTimedMoveHost>();
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
