// ============================================================
// CraftingAltSelectAction — 재료/도구 대체재 선택 (보유만 활성)
// ============================================================

using System;

public sealed class CraftingAltSelectAction : IContextMenuAction
{
    /// <summary>공백만 = 비활성, 라벨에 사유를 붙이지 않음.</summary>
    const string UnavailableSilent = " ";

    readonly Action<int> _setAltIndex;
    readonly int _altIndex;
    readonly bool _owned;

    public CraftingAltSelectAction(Action<int> setAltIndex, int altIndex, bool owned)
    {
        _setAltIndex = setAltIndex;
        _altIndex = altIndex;
        _owned = owned;
    }

    public string GetDisabledReason() => _owned ? null : UnavailableSilent;

    public void Execute()
    {
        if (!_owned)
            return;

        _setAltIndex?.Invoke(_altIndex);
    }
}
