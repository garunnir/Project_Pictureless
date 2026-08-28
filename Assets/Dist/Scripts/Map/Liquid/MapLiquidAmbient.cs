// ============================================================
// MapLiquidAmbient — 액체가 접하는 대기 온도의 주입 지점
// ============================================================
// Dist.Map은 날씨·시계 시스템을 참조하지 않는다(어셈블리 경계). MapClockSnapshot과 같은
// 델리게이트 훅 패턴으로 외부(DistScript)가 공급자를 꽂고, 미주입이면 기본 기온을 쓴다.

using System;
using UnityEngine;

namespace IsoTilemap
{
    public static class MapLiquidAmbient
    {
        /// <summary>셀 위치의 기온(°C)을 돌려주는 공급자. null이면 기본값이 쓰인다.</summary>
        public static Func<Vector3Int, float> TempCProvider;

        public static short ResolveDeciC(Vector3Int cell)
        {
            Func<Vector3Int, float> provider = TempCProvider;
            if (provider == null)
                return MapLiquidConsts.DefaultAmbientDeciC;

            return MapLiquidConsts.ToDeciC(provider(cell));
        }
    }
}
