// ============================================================
// ReservedWorkHub — 예약 작업 소스 레지스트리 (확장 SSOT)
// ============================================================

using System;
using System.Collections.Generic;

public static class ReservedWorkHub
{
    static readonly List<IReservedWorkSource> _sources = new(8);

    public static event Action Changed;

    public static bool HasAnyActiveWork
    {
        get
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                IReservedWorkSource source = _sources[i];
                if (source != null && source.HasActiveWork)
                    return true;
            }

            return false;
        }
    }

    public static void Register(IReservedWorkSource source)
    {
        if (source == null || _sources.Contains(source))
            return;

        _sources.Add(source);
        source.Changed += OnSourceChanged;
        Changed?.Invoke();
    }

    public static void Unregister(IReservedWorkSource source)
    {
        if (source == null || !_sources.Remove(source))
            return;

        source.Changed -= OnSourceChanged;
        Changed?.Invoke();
    }

    static void OnSourceChanged() => Changed?.Invoke();
}
