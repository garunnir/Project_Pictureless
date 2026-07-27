// ============================================================
// MessageLogViewModel — GameplayMessageLog 구독 + HUD 스냅샷
// ============================================================

using System;
using System.Collections.Generic;

public sealed class MessageLogViewModel
{
    readonly List<MessageLogEntry> _entries = new List<MessageLogEntry>(GameplayMessageLog.Capacity);

    public event Action Changed;

    public IReadOnlyList<MessageLogEntry> Entries => _entries;

    public void Bind()
    {
        Unbind();
        GameplayMessageLog.Changed += OnLogChanged;
        CopySnapshot();
        Changed?.Invoke();
    }

    public void Unbind()
    {
        GameplayMessageLog.Changed -= OnLogChanged;
    }

    void OnLogChanged()
    {
        CopySnapshot();
        Changed?.Invoke();
    }

    void CopySnapshot()
    {
        _entries.Clear();
        IReadOnlyList<MessageLogEntry> snapshot = GameplayMessageLog.GetSnapshot();
        for (int i = 0; i < snapshot.Count; i++)
            _entries.Add(snapshot[i]);
    }
}
