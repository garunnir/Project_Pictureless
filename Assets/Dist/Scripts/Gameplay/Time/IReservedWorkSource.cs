// ============================================================
// IReservedWorkSource — 지능형 배속용 예약 작업 관찰 계약
// ============================================================

using System;

public interface IReservedWorkSource
{
    bool HasActiveWork { get; }

    event Action Changed;
}
