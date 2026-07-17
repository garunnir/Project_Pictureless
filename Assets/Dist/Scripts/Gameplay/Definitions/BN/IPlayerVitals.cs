// ============================================================
// IPlayerVitals — 전역 생존 바이탈 계약 (공복/갈증/스태미나)
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    public interface IPlayerVitals
    {
        event Action<string> Changed;

        int GetCurrent(string vitalKey);
        int GetMax(string vitalKey);
        void SetCurrent(string vitalKey, int value);
    }
}
