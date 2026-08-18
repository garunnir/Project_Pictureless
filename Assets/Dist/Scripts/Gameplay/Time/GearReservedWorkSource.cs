// ============================================================
// GearReservedWorkSource — GearTimedAction → ReservedWorkHub
// ============================================================

using System;
using UnityEngine;

public sealed class GearReservedWorkSource : MonoBehaviour, IReservedWorkSource
{
    [SerializeField] PlayerGearHost _gearHost;

    bool _lastBusy;

    public bool HasActiveWork =>
        _gearHost != null && _gearHost.Service != null && _gearHost.Service.IsBusy;

    public event Action Changed;

    void Awake()
    {
        if (_gearHost == null)
            _gearHost = GetComponent<PlayerGearHost>();
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
