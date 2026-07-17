// ============================================================
// DefaultPlayerVitals — 전역 바이탈 인메모리 기본 구현
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DefaultPlayerVitals : IPlayerVitals
    {
        public const int DefaultHungerMax = 100;
        public const int DefaultThirstMax = 100;
        public const int DefaultStaminaMax = 100;

        readonly Dictionary<string, int> _current = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _max = new(StringComparer.Ordinal);

        public event Action<string> Changed;

        public DefaultPlayerVitals()
        {
            SetMaxAndFill(VitalKeys.Hunger, DefaultHungerMax);
            SetMaxAndFill(VitalKeys.Thirst, DefaultThirstMax);
            SetMaxAndFill(VitalKeys.Stamina, DefaultStaminaMax);
        }

        public int GetCurrent(string vitalKey)
        {
            if (string.IsNullOrEmpty(vitalKey))
                return 0;
            return _current.TryGetValue(vitalKey, out int value) ? value : 0;
        }

        public int GetMax(string vitalKey)
        {
            if (string.IsNullOrEmpty(vitalKey))
                return 0;
            return _max.TryGetValue(vitalKey, out int value) ? value : 0;
        }

        public void SetCurrent(string vitalKey, int value)
        {
            if (string.IsNullOrEmpty(vitalKey) || !_max.ContainsKey(vitalKey))
                return;

            int max = _max[vitalKey];
            int clamped = value < 0 ? 0 : (value > max ? max : value);
            _current[vitalKey] = clamped;
            Changed?.Invoke(vitalKey);
        }

        void SetMaxAndFill(string key, int max)
        {
            _max[key] = max;
            _current[key] = max;
        }
    }
}
