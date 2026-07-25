// ============================================================
// BuffableStat — Base 저장 / Buffed 런타임 (Refresh로 재조립)
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// OpenNefia <c>Stat&lt;T&gt;</c>와 동일한 계약: 직렬화는 Base만, Buffed는 Refresh에서 재계산.
    /// </summary>
    public sealed class BuffableStat : IEquatable<BuffableStat>
    {
        int _base;
        int _buffed;
        bool _isBuffed;

        public int Base
        {
            get => _base;
            set
            {
                _base = value;
                if (!_isBuffed)
                    _buffed = value;
            }
        }

        public int Buffed
        {
            get => _buffed;
            set
            {
                if (_buffed != value)
                    _isBuffed = true;
                _buffed = value;
            }
        }

        public bool IsBuffed => _isBuffed;

        public BuffableStat() : this(0)
        {
        }

        public BuffableStat(int baseValue) : this(baseValue, baseValue)
        {
        }

        public BuffableStat(int baseValue, int buffedValue)
        {
            _base = baseValue;
            _buffed = buffedValue;
            _isBuffed = baseValue != buffedValue;
        }

        public void Reset()
        {
            _buffed = _base;
            _isBuffed = false;
        }

        public bool Equals(BuffableStat other)
        {
            if (other is null)
                return false;
            return _base == other._base;
        }

        public override bool Equals(object obj) => Equals(obj as BuffableStat);

        public override int GetHashCode() => _base;

        public override string ToString() => $"{_buffed}({_base})";
    }
}
