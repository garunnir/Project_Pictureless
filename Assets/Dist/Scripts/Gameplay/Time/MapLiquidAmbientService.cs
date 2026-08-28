// ============================================================
// MapLiquidAmbientService — 액체 온도 경계조건을 날씨·시계에 연결
// ============================================================
// Dist.Map은 날씨 어셈블리를 참조하지 않으므로, MapClockSnapshot과 같은 훅 주입으로
// MapLiquidAmbient.TempCProvider를 채운다. 미주입 시 액체는 기본 기온만 본다.

using IsoTilemap;
using UnityEngine;

public static class MapLiquidAmbientService
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        MapLiquidAmbient.TempCProvider = ResolveAmbientTempC;
    }

    /// <remarks>
    /// 현재는 전부 야외로 본다. 실내 수조는 <see cref="WeatherExposure.IndoorAmbientTempC"/>를 써야 하지만,
    /// 셀 단위 실내 판정(building/space)을 이 경로에 물리는 건 별 작업이다.
    /// </remarks>
    static float ResolveAmbientTempC(Vector3Int cell)
    {
        WorldClock clock = WorldClock.Instance;
        DayPeriod period = clock != null ? clock.Period : DayPeriod.Day;
        return WeatherExposure.ResolveAmbientTempC(ResolveKindAt(cell), period, outdoor: true);
    }

    static WeatherKind ResolveKindAt(Vector3Int cell)
    {
        WorldWeatherHost weather = WorldWeatherHost.Instance;
        if (weather == null)
            return WeatherKind.Clear;

        if (weather.TryGetKindAt(cell.x, cell.z, out WeatherKind atCell))
            return atCell;

        return weather.CurrentKind;
    }
}
