// ============================================================
// GameplayMessageLog — 플레이어 중요 사건 ring buffer (UI 비의존)
// ============================================================
// Append는 의사결정에 영향 있는 사건만. 틱·miss 스팸·비플레이어는 호출하지 말 것.
// ============================================================

using System;
using System.Collections.Generic;

public static class GameplayMessageLog
{
    public const int Capacity = 100;

    static readonly MessageLogEntry[] Buffer = new MessageLogEntry[Capacity];
    static readonly List<MessageLogEntry> SnapshotList = new List<MessageLogEntry>(Capacity);
    static int _count;
    static int _nextWrite;

    public static event Action Changed;

    public static int Count => _count;

    /// <summary>중요 사건만. 잡음·틱·디버그는 넣지 않는다.</summary>
    public static void Append(
        MessageLogCategory category,
        MessageLogImportance importance,
        string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Buffer[_nextWrite] = new MessageLogEntry(category, importance, text);
        _nextWrite = (_nextWrite + 1) % Capacity;
        if (_count < Capacity)
            _count++;

        Changed?.Invoke();
    }

    /// <summary>오래된 것 → 최신 순.</summary>
    public static IReadOnlyList<MessageLogEntry> GetSnapshot()
    {
        SnapshotList.Clear();
        if (_count == 0)
            return SnapshotList;

        int start = _count < Capacity ? 0 : _nextWrite;
        for (int i = 0; i < _count; i++)
        {
            int index = (start + i) % Capacity;
            SnapshotList.Add(Buffer[index]);
        }

        return SnapshotList;
    }

    public static void Clear()
    {
        _count = 0;
        _nextWrite = 0;
        SnapshotList.Clear();
        Changed?.Invoke();
    }
}
