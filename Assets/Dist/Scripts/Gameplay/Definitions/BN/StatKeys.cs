// ============================================================
// StatKeys / VitalKeys — 스탯·전역 바이탈 키 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class StatKeys
    {
        // Legacy Actor field strings — keep identical for compatibility.
        public const string Str = "Status.Str";
        public const string Con = "Status.Con";
        public const string Dex = "Status.Dex";
        public const string Int = "Status.Int";
        public const string Wis = "Status.Wis";
        public const string Cha = "Status.Cha";

        public static readonly string[] AbilityKeys =
        {
            Str, Con, Dex, Int, Wis, Cha
        };
    }

    public static class VitalKeys
    {
        public const string Hunger = "Vital.Hunger";
        public const string Thirst = "Vital.Thirst";
        public const string Stamina = "Vital.Stamina";

        public static readonly string[] All =
        {
            Hunger, Thirst, Stamina
        };
    }
}
