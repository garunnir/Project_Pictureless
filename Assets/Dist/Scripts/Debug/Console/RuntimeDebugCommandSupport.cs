// ============================================================
// RuntimeDebugCommandSupport — 런타임 디버그 명령의 공용 키 해석
// ============================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Garunnir.Runtime.Gameplay.Data;

static class RuntimeDebugCommandSupport
{
    const string VitalPrefix = "Vital.";

    public static bool TryResolveVitalKey(string input, out string vitalKey)
    {
        vitalKey = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();
        for (int i = 0; i < VitalKeys.All.Length; i++)
        {
            string candidate = VitalKeys.All[i];
            string shortKey = candidate.StartsWith(VitalPrefix, StringComparison.Ordinal)
                ? candidate.Substring(VitalPrefix.Length)
                : candidate;

            if (string.Equals(input, candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, shortKey, StringComparison.OrdinalIgnoreCase))
            {
                vitalKey = candidate;
                return true;
            }
        }

        return false;
    }
}
#endif
