// ============================================================
// GameSaveLabels — 슬롯 저장/불러오기 문구 SSOT
// ============================================================

using System;

public static class GameSaveLabels
{
    const string KeyCategoryGame = "Settings.Category.Game";
    const string KeySave = "Settings.Game.Save";
    const string KeyLoad = "Settings.Game.Load";
    const string KeyPopupSaveTitle = "Settings.Game.Popup.SaveTitle";
    const string KeyPopupLoadTitle = "Settings.Game.Popup.LoadTitle";
    const string KeySlotTitleFormat = "Settings.Game.Slot.TitleFormat";
    const string KeySlotEmpty = "Settings.Game.Slot.Empty";
    const string KeySlotDayTimeFormat = "Settings.Game.Slot.DayTimeFormat";
    const string KeySlotSavedAtFormat = "Settings.Game.Slot.SavedAtFormat";
    const string KeyConfirmOverwrite = "Settings.Game.Confirm.Overwrite";
    const string KeyConfirmLoad = "Settings.Game.Confirm.Load";
    const string KeyConfirmYes = "Settings.Game.Confirm.Yes";
    const string KeyConfirmNo = "Settings.Game.Confirm.No";
    const string KeyClose = "Settings.Game.Close";

    public static string CategoryGame => Loc.Get(KeyCategoryGame);
    public static string Save => Loc.Get(KeySave);
    public static string Load => Loc.Get(KeyLoad);
    public static string PopupSaveTitle => Loc.Get(KeyPopupSaveTitle);
    public static string PopupLoadTitle => Loc.Get(KeyPopupLoadTitle);
    public static string SlotEmpty => Loc.Get(KeySlotEmpty);
    public static string ConfirmOverwrite => Loc.Get(KeyConfirmOverwrite);
    public static string ConfirmLoad => Loc.Get(KeyConfirmLoad);
    public static string ConfirmYes => Loc.Get(KeyConfirmYes);
    public static string ConfirmNo => Loc.Get(KeyConfirmNo);
    public static string Close => Loc.Get(KeyClose);

    public static string FormatSlotTitle(int displayNumber) =>
        Loc.Format(KeySlotTitleFormat, displayNumber);

    public static string FormatSlotSubtitle(GameSaveSlotInfo info)
    {
        if (!info.HasData)
            return SlotEmpty;

        if (info.HasClockSnapshot)
        {
            int hour = info.MinuteOfDay / 60;
            int minute = info.MinuteOfDay % 60;
            return Loc.Format(KeySlotDayTimeFormat, info.DayIndex, hour, minute);
        }

        if (info.SavedAtUtcTicks > 0)
        {
            DateTime saved = new DateTime(info.SavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            return Loc.Format(KeySlotSavedAtFormat, saved.ToString("yyyy-MM-dd HH:mm"));
        }

        return SlotEmpty;
    }
}
