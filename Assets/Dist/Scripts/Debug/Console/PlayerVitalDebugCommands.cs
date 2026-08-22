// ============================================================
// PlayerVitalDebugCommands — 플레이어 바이탈 런타임 디버그 명령
// ============================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using IngameDebugConsole;
using UnityEngine.Scripting;

public static class PlayerVitalDebugCommands
{
    public const string GetCommand = "player.vital.get";
    public const string SetCommand = "player.vital.set";

    [ConsoleMethod(GetCommand, "Returns a player vital value", "vitalId"), Preserve]
    public static string GetValue(string vitalId)
    {
        if (!RuntimeDebugCommandSupport.TryResolveVitalKey(vitalId, out string vitalKey))
            return $"ERROR: unknown vitalId '{vitalId}'";

        int current = GameplayData.Vitals.GetCurrent(vitalKey);
        int max = GameplayData.Vitals.GetMax(vitalKey);
        return $"{vitalKey} {current}/{max}";
    }

    [ConsoleMethod(SetCommand, "Sets a player vital value", "vitalId", "value"), Preserve]
    public static string SetValue(string vitalId, int value)
    {
        if (!RuntimeDebugCommandSupport.TryResolveVitalKey(vitalId, out string vitalKey))
            return $"ERROR: unknown vitalId '{vitalId}'";

        GameplayData.Vitals.SetCurrent(vitalKey, value);
        return GetValue(vitalKey);
    }

    public const string StomachGetCommand = "player.needs.stomach.get";
    public const string StomachSetCommand = "player.needs.stomach.set";

    [ConsoleMethod(StomachGetCommand, "Returns player stomach mlFood/mlWater/kcal"), Preserve]
    public static string GetStomach()
    {
        if (!RuntimeDebugCommandSupport.TryGetPlayerNeedsHost(out PlayerNeedsHost host))
            return "ERROR: PlayerNeedsHost is not active";

        return $"stomach mlFood={host.StomachMlFood} mlWater={host.StomachMlWater} kcal={host.StomachKcal}";
    }

    [ConsoleMethod(StomachSetCommand, "Sets player stomach pools", "mlFood", "mlWater", "kcal"), Preserve]
    public static string SetStomach(float mlFood, float mlWater, float kcal)
    {
        if (!RuntimeDebugCommandSupport.TryGetPlayerNeedsHost(out PlayerNeedsHost host))
            return "ERROR: PlayerNeedsHost is not active";

        host.SetStomach(mlFood, mlWater, kcal);
        return GetStomach();
    }
}
#endif
