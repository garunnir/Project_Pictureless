// ============================================================
// AttributeIds — 기본 능력치 숙련 ID SSOT (레거시 Status.* 폐기)
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class AttributeIds
    {
        public const string Str = "str";
        public const string Con = "con";
        public const string Dex = "dex";
        public const string Int = "int";
        public const string Wis = "wis";
        public const string Cha = "cha";

        public static readonly string[] All =
        {
            Str, Con, Dex, Int, Wis, Cha
        };

        public static bool IsAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] == id)
                    return true;
            }

            return false;
        }
    }
}
