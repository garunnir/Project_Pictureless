// ============================================================
// MapLiquidCell — 액체 셀 저장 표현 (level 대략치 + remainderMl 정밀치)
// ============================================================

namespace IsoTilemap
{
    /// <summary>
    /// 한 그리드 셀의 액체 상태. <see cref="EffectiveMl"/>가 flow 연산의 유일한 단위이며,
    /// Level/RemainderMl은 저장·렌더 LOD용 파생 표현일 뿐입니다.
    /// </summary>
    public sealed class MapLiquidCell
    {
        public string TypeId;
        public byte Level;
        public ushort RemainderMl;

        public MapLiquidCell(string typeId, byte level, ushort remainderMl)
        {
            TypeId = typeId;
            Level = level;
            RemainderMl = remainderMl;
        }

        public int EffectiveMl => Level * MapLiquidConsts.MlPerLevel + RemainderMl;

        public bool IsEmpty => Level == 0 && RemainderMl == 0;

        public static MapLiquidCell FromEffectiveMl(string typeId, int effectiveMl)
        {
            var cell = new MapLiquidCell(typeId, 0, 0);
            cell.SetEffectiveMl(effectiveMl);
            return cell;
        }

        public void SetEffectiveMl(int effectiveMl)
        {
            if (effectiveMl <= 0)
            {
                Level = 0;
                RemainderMl = 0;
                return;
            }

            int mlPerLevel = MapLiquidConsts.MlPerLevel;
            int level = effectiveMl / mlPerLevel;
            int remainder = effectiveMl - level * mlPerLevel;

            if (level > MapLiquidConsts.MaxLevel)
            {
                // cap 초과분은 호출부(FlowSolver/MlBridge)가 이웃/위로 옮기는 책임 — 여기서는 표현만 clamp.
                // remainder가 잠시 MlPerLevel을 넘겨도 EffectiveMl 계산 자체는 정확함(ushort 범위 내 방어만).
                level = MapLiquidConsts.MaxLevel;
                remainder = effectiveMl - level * mlPerLevel;
                if (remainder > ushort.MaxValue)
                    remainder = ushort.MaxValue;
            }

            Level = (byte)level;
            RemainderMl = (ushort)remainder;
        }
    }
}
