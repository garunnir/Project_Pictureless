// ============================================================
// InventorySessionBootstrap — UIInventoryController 전 임시 Session·Detector·Host 배선
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHost))]
[RequireComponent(typeof(NearbyContainerDetector))]
public sealed class InventorySessionBootstrap : MonoBehaviour
{
    [Required, SerializeField] PlayerInventoryHost _host;
    [Required, SerializeField] NearbyContainerDetector _detector;
    [SerializeField] bool _openInventoryOnStart = false;

    InventorySession _session;

    public InventorySession Session => _session;

    void Awake()
    {
        _session = new InventorySession();
        _detector.Bind(_session);
    }

    void Start()
    {
        if (!_openInventoryOnStart)
            return;

        _host.RegisterToSession(_session);
        _detector.Activate();
    }

    void OnDestroy()
    {
        _detector.Deactivate();
        _host.UnregisterFromSession(_session);
    }

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_host) TryGetComponent(out _host);
        if (!_detector) TryGetComponent(out _detector);
    }
}
