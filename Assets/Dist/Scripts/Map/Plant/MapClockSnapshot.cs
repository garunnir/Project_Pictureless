// ============================================================
// MapClockSnapshot — 맵 JSON dayIndex/minuteOfDay 왕복 브리지
// ============================================================

using System;

namespace IsoTilemap
{
    public static class MapClockSnapshot
    {
        public static Func<int> GetDayIndex;
        public static Func<int> GetMinuteOfDay;
        public static Func<int> GetMinutesPerDay;
        public static Action<int, int> SetTime;

        /// <summary>WorldClock.MinuteChanged 브리지. WorldClock 미존재 시 false — 호출부가 지연 재시도.</summary>
        public static Func<Action, bool> TrySubscribeMinuteChanged;
        public static Action<Action> UnsubscribeMinuteChanged;

        public static int CurrentWorldMinute()
        {
            int day = GetDayIndex != null ? GetDayIndex() : 0;
            int minute = GetMinuteOfDay != null ? GetMinuteOfDay() : 0;
            int perDay = GetMinutesPerDay != null
                ? GetMinutesPerDay()
                : MapPlantConsts.FallbackMinutesPerDay;
            if (perDay < 1)
                perDay = MapPlantConsts.FallbackMinutesPerDay;
            return day * perDay + minute;
        }

        public static void WriteToDto(MapSaveJsonDto dto)
        {
            if (dto == null)
                return;

            if (GetDayIndex == null || GetMinuteOfDay == null)
            {
                dto.hasClockSnapshot = false;
                return;
            }

            dto.hasClockSnapshot = true;
            dto.dayIndex = GetDayIndex();
            dto.minuteOfDay = GetMinuteOfDay();
        }

        public static void RestoreFromDto(MapSaveJsonDto dto)
        {
            if (dto == null || !dto.hasClockSnapshot || SetTime == null)
                return;

            SetTime(dto.dayIndex, dto.minuteOfDay);
        }
    }
}
