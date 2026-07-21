// ============================================================
// GameDataJson — BN/GameData JSON 직렬화 SSOT (Newtonsoft)
// ============================================================

using Newtonsoft.Json;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class GameDataJson
    {
        static readonly JsonSerializerSettings Settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };

        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public static string Serialize<T>(T value, bool indented = true)
        {
            Formatting formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(value, formatting, Settings);
        }

        public static T Clone<T>(T value) => Deserialize<T>(Serialize(value, false));
    }
}
