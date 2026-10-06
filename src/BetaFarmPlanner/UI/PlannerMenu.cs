using BetaFarmPlanner.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace BetaFarmPlanner.UI;

internal sealed class PlannerMenu : IClickableMenu
{
    private enum Page { Today, Timeline, Season, Year, Reminders, Crops }
    private static readonly string[] PageKeys = { "today", "timeline", "season", "year", "reminders", "crops" };
    private readonly ModEntry Mod;
    private Page CurrentPage;
    private FarmDate SelectedDate;
    private Guid? SelectedReminderId;
    private ReminderRepeat NewReminderRepeat;
    private readonly int[] TimelineChoices = { 7, 14, 28 };

    public PlannerMenu(ModEntry mod, FarmDate? selectedDate = null)
        : base(24, 24, Math.Max(320, Game1.uiViewport.Width - 48), Math.Max(300, Game1.uiViewport.Height - 48))
    {
        Mod = mod;
        SelectedDate = selectedDate ?? mod.CurrentDate;
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
    }

    public override void draw(SpriteBatch b)
    {
        Rectangle frame = new(xPositionOnScreen, yPositionOnScreen, width, height);
        b.Draw(Game1.fadeToBlackRect, frame, new Color(38, 35, 39, 246));
        DrawBorder(b, frame, new Color(217, 195, 145));
        DrawText(b, Mod.Translate("title"), xPositionOnScreen + 22, yPositionOnScreen + 15, Game1.dialogueFont, new Color(248, 232, 191));
        DrawButton(b, new Rectangle(xPositionOnScreen + width - 52, yPositionOnScreen + 12, 34, 32), "×", false);

        int tabY = yPositionOnScreen + 60;
        int tabWidth = (width - 40) / PageKeys.Length;
        for (int i = 0; i < PageKeys.Length; i++)
        {
            Rectangle tab = new(xPositionOnScreen + 20 + i * tabWidth, tabY, tabWidth - 5, 38);
            DrawButton(b, tab, Mod.Translate($"tab.{PageKeys[i]}"), CurrentPage == (Page)i);
        }

        Rectangle content = new(xPositionOnScreen + 20, tabY + 52, width - 40, height - 150);
        DrawContent(b, content);
        DrawText(b, Mod.Translate("footer.close"), xPositionOnScreen + 22, yPositionOnScreen + height - 34, Game1.smallFont, new Color(201, 195, 178));
        drawMouse(b);
    }

    private void DrawContent(SpriteBatch b, Rectangle area)
    {
        switch (CurrentPage)
        {
            case Page.Today: DrawToday(b, area); break;
            case Page.Timeline: DrawTimeline(b, area); break;
            case Page.Season: DrawSeason(b, area); break;
            case Page.Year: DrawYear(b, area); break;
            case Page.Reminders: DrawReminders(b, area); break;
            case Page.Crops: DrawCrops(b, area); break;
        }
    }

    private void DrawToday(SpriteBatch b, Rectangle area)
    {
        FarmDate today = Mod.CurrentDate;
        DrawText(b, Mod.Translate("today.summary"), area.X + 12, area.Y + 6, Game1.dialogueFont, Color.White);
        DrawText(b, DateLabel(today), area.X + 12, area.Y + 48, Game1.smallFont, new Color(235, 221, 184));
        DrawText(b, Mod.Translate("season.remaining", 28 - today.DayOfSeason), area.X + 12, area.Y + 76, Game1.smallFont, Color.White);

        List<PlannerEvent> todaysEvents = EventsOn(today);
        int festivals = todaysEvents.Count(item => item.Kind == PlannerEventKind.Festival);
        int birthdays = todaysEvents.Count(item => item.Kind == PlannerEventKind.Birthday);
        int reminders = todaysEvents.Count(item => item.Kind == PlannerEventKind.Reminder);
        int cropDeadlines = Mod.ShowCropDeadlines ? Mod.GetCropDeadlines().Count(item => item.LatestPlantDate == today) : 0;
        DrawText(b, Mod.Translate("pressure", PlanningPressure.GetLabel(festivals, birthdays, reminders, cropDeadlines)), area.X + 12, area.Y + 106, Game1.smallFont, new Color(242, 204, 124));

        int y = area.Y + 156;
        if (todaysEvents.Count == 0)
            DrawText(b, Mod.Translate("today.empty"), area.X + 12, y, Game1.smallFont, Color.White);
        foreach (PlannerEvent item in todaysEvents.Take(8))
        {
            DrawText(b, $"• {KindLabel(item.Kind)}: {item.Title}", area.X + 12, y, Game1.smallFont, new Color(235, 235, 225));
            y += 27;
        }
        if (Mod.ShowCropDeadlines)
        {
            foreach (CropDeadline item in Mod.GetCropDeadlines().Where(item => item.LatestPlantDate == today).Take(3))
            {
                DrawText(b, $"• {Mod.Translate("event.crop")}: {item.HarvestName}", area.X + 12, y, Game1.smallFont, new Color(198, 223, 156));
                y += 27;
            }
        }
    }

    private void DrawTimeline(SpriteBatch b, Rectangle area)
    {
        int days = Mod.TimelineLength;
        DrawText(b, Mod.Translate("timeline.length", days), area.X + 10, area.Y + 6, Game1.dialogueFont, Color.White);
        DrawButton(b, new Rectangle(area.Right - 164, area.Y + 2, 154, 34), $"Length: {days} ▸", false);
        int columns = days == 28 ? 2 : 1;
        int rows = days / columns;
        int cellWidth = (area.Width - 20) / columns;
        int cellHeight = Math.Min(42, (area.Height - 58) / rows);
        FarmDate start = Mod.CurrentDate;
        for (int i = 0; i < days; i++)
        {
            FarmDate date = start.AddDays(i);
            int col = i / rows;
            int row = i % rows;
            Rectangle cell = new(area.X + 10 + col * cellWidth, area.Y + 48 + row * cellHeight, cellWidth - 6, cellHeight - 4);
            DrawDateCell(b, cell, date, false);
        }
    }

    private void DrawSeason(SpriteBatch b, Rectangle area)
    {
        DrawButton(b, new Rectangle(area.X + 8, area.Y + 3, 44, 34), "‹", false);
        DrawText(b, Mod.Translate("season.year", SeasonName(SelectedDate.SeasonIndex), SelectedDate.Year), area.X + 64, area.Y + 7, Game1.dialogueFont, Color.White);
        DrawButton(b, new Rectangle(area.Right - 52, area.Y + 3, 44, 34), "›", false);
        DrawText(b, Mod.Translate("select.date", DateLabel(SelectedDate)), area.X + 8, area.Y + 43, Game1.smallFont, new Color(229, 218, 190));

        int gridX = area.X + 12;
        int gridY = area.Y + 84;
        int cellWidth = Math.Max(34, (area.Width - 24) / 7);
        int cellHeight = Math.Min(72, (area.Height - 98) / 4);
        int firstDay = SelectedDate.SeasonIndex * 28 + 1;
        for (int day = 1; day <= 28; day++)
        {
            FarmDate date = new(SelectedDate.Year, firstDay + day - 1);
            Rectangle cell = new(gridX + ((day - 1) % 7) * cellWidth, gridY + ((day - 1) / 7) * cellHeight, cellWidth - 4, cellHeight - 4);
            DrawDateCell(b, cell, date, true);
        }
        DrawSelectedDetails(b, area, gridY + cellHeight * 4 + 4, 2);
    }

    private void DrawYear(SpriteBatch b, Rectangle area)
    {
        DrawButton(b, new Rectangle(area.X + 8, area.Y + 3, 44, 34), "‹", false);
        DrawText(b, Mod.Translate("year.title", SelectedDate.Year), area.X + 64, area.Y + 7, Game1.dialogueFont, Color.White);
        DrawButton(b, new Rectangle(area.Right - 52, area.Y + 3, 44, 34), "›", false);
        DrawText(b, Mod.Translate("select.date", DateLabel(SelectedDate)), area.X + 8, area.Y + 43, Game1.smallFont, new Color(229, 218, 190));

        int gridX = area.X + 10;
        int gridY = area.Y + 82;
        int cellWidth = (area.Width - 20) / 28;
        int cellHeight = Math.Min(105, (area.Height - 92) / 4);
        for (int day = 1; day <= FarmDate.DaysPerYear; day++)
        {
            FarmDate date = new(SelectedDate.Year, day);
            Rectangle cell = new(gridX + ((day - 1) % 28) * cellWidth, gridY + ((day - 1) / 28) * cellHeight, cellWidth - 2, cellHeight - 4);
            DrawDateCell(b, cell, date, true);
        }
        DrawSelectedDetails(b, area, gridY + cellHeight * 4 + 1, 1);
    }

    private void DrawReminders(SpriteBatch b, Rectangle area)
    {
        DrawText(b, Mod.Translate("select.date", DateLabel(SelectedDate)), area.X + 8, area.Y + 6, Game1.dialogueFont, Color.White);
        DrawButton(b, new Rectangle(area.X + 8, area.Y + 46, 172, 38), Mod.Translate("reminder.add"), false);
        DrawButton(b, new Rectangle(area.X + 190, area.Y + 46, 190, 38), Mod.Translate("reminder.repeat", RepeatLabel(NewReminderRepeat)), false);
        DrawButton(b, new Rectangle(area.X + 390, area.Y + 46, 194, 38), Mod.Translate("reminder.remove"), false);

        PlannerReminder[] reminders = Mod.GetReminders().OrderBy(item => item.DueDate).ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        if (reminders.Length == 0)
        {
            DrawText(b, Mod.Translate("reminder.empty"), area.X + 10, area.Y + 106, Game1.smallFont, Color.White);
            return;
        }
        int y = area.Y + 104;
        foreach (PlannerReminder reminder in reminders.Take(12))
        {
            bool selected = SelectedReminderId == reminder.Id;
            Rectangle row = new(area.X + 6, y - 2, area.Width - 12, 34);
            DrawButton(b, row, $"{DateLabel(reminder.DueDate)}   ·   {reminder.Title}   ·   {RepeatLabel(reminder.Repeat)}", selected);
            y += 39;
        }
    }

    private void DrawCrops(SpriteBatch b, Rectangle area)
    {
        DrawText(b, Mod.Translate("crop.header"), area.X + 10, area.Y + 6, Game1.dialogueFont, Color.White);
        DrawText(b, "Uses crop seasons and growth phases from the loaded game data. No fertilizer or Ginger Island assumptions.", area.X + 10, area.Y + 42, Game1.smallFont, new Color(204, 197, 178));
        int y = area.Y + 84;
        CropDeadline[] deadlines = Mod.GetCropDeadlines().Take(14).ToArray();
        if (deadlines.Length == 0)
        {
            DrawText(b, Mod.Translate("crop.none"), area.X + 10, y, Game1.smallFont, Color.White);
            return;
        }
        foreach (CropDeadline item in deadlines)
        {
            string line = Mod.Translate("crop.line", item.HarvestName, DateLabel(item.LatestPlantDate), DateLabel(item.FirstHarvestDate), item.GrowthDays);
            DrawText(b, line, area.X + 10, y, Game1.smallFont, new Color(230, 230, 220));
            y += 29;
        }
    }

    private void DrawDateCell(SpriteBatch b, Rectangle cell, FarmDate date, bool showEvents)
    {
        bool selected = date == SelectedDate;
        Color fill = selected ? new Color(103, 81, 54) : new Color(65, 61, 57);
        b.Draw(Game1.fadeToBlackRect, cell, fill);
        DrawBorder(b, cell, selected ? new Color(242, 206, 119) : new Color(112, 105, 96));
        DrawText(b, date.DayOfSeason.ToString(), cell.X + 5, cell.Y + 3, Game1.smallFont, Color.White);
        if (!showEvents) return;
        int eventCount = EventsOn(date).Count;
        if (Mod.ShowCropDeadlines)
            eventCount += Mod.GetCropDeadlines().Count(item => item.LatestPlantDate == date);
        if (eventCount > 0)
            DrawText(b, eventCount > 9 ? "9+" : eventCount.ToString(), cell.Right - 24, cell.Bottom - 24, Game1.smallFont, new Color(247, 205, 106));
    }

    private void DrawSelectedDetails(SpriteBatch b, Rectangle area, int y, int maxLines)
    {
        if (y + Game1.smallFont.LineSpacing >= area.Bottom) return;
        PlannerEvent[] items = EventsOn(SelectedDate).Take(maxLines).ToArray();
        if (items.Length == 0) return;
        for (int i = 0; i < items.Length && y + (i + 1) * 24 < area.Bottom; i++)
            DrawText(b, $"{KindLabel(items[i].Kind)}: {items[i].Title}", area.X + 10, y + i * 24, Game1.smallFont, new Color(235, 221, 184));
    }

    private List<PlannerEvent> EventsOn(FarmDate date)
    {
        return Mod.GetEvents().Where(item => item.Date == date && (item.Kind != PlannerEventKind.Festival || Mod.ShowFestivals) && (item.Kind != PlannerEventKind.Birthday || Mod.ShowBirthdays)).ToList();
    }

    private string KindLabel(PlannerEventKind kind) => kind switch
    {
        PlannerEventKind.Festival => Mod.Translate("event.festival"),
        PlannerEventKind.Birthday => Mod.Translate("event.birthday"),
        PlannerEventKind.Reminder => Mod.Translate("event.reminder"),
        PlannerEventKind.SeasonBoundary => Mod.Translate("event.season"),
        _ => Mod.Translate("event.crop")
    };

    private string DateLabel(FarmDate date) => $"{SeasonName(date.SeasonIndex)} {date.DayOfSeason}, Y{date.Year}";
    private string SeasonName(int index)
    {
        string season = FarmDate.Seasons[index];
        return char.ToUpperInvariant(season[0]) + season[1..];
    }

    private string RepeatLabel(ReminderRepeat repeat) => repeat switch
    {
        ReminderRepeat.Once => Mod.Translate("repeat.once"),
        ReminderRepeat.Weekly => Mod.Translate("repeat.weekly"),
        ReminderRepeat.EverySeason => Mod.Translate("repeat.season"),
        ReminderRepeat.EveryYear => Mod.Translate("repeat.yearly"),
        _ => Mod.Translate("repeat.once")
    };

    private void DrawButton(SpriteBatch b, Rectangle rect, string text, bool selected)
    {
        b.Draw(Game1.fadeToBlackRect, rect, selected ? new Color(100, 79, 51) : new Color(67, 63, 58));
        DrawBorder(b, rect, selected ? new Color(240, 205, 130) : new Color(132, 119, 101));
        DrawText(b, text, rect.X + 7, rect.Y + (rect.Height - Game1.smallFont.LineSpacing) / 2, Game1.smallFont, Color.White);
    }

    private static void DrawBorder(SpriteBatch b, Rectangle rect, Color color)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }

    private static void DrawText(SpriteBatch b, string text, int x, int y, SpriteFont font, Color color) => b.DrawString(font, text, new Vector2(x, y), color);

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (new Rectangle(xPositionOnScreen + width - 52, yPositionOnScreen + 12, 34, 32).Contains(x, y))
        {
            Game1.exitActiveMenu();
            return;
        }

        int tabY = yPositionOnScreen + 60;
        int tabWidth = (width - 40) / PageKeys.Length;
        for (int i = 0; i < PageKeys.Length; i++)
        {
            Rectangle tab = new(xPositionOnScreen + 20 + i * tabWidth, tabY, tabWidth - 5, 38);
            if (!tab.Contains(x, y)) continue;
            CurrentPage = (Page)i;
            Game1.playSound("smallSelect");
            return;
        }

        Rectangle area = new(xPositionOnScreen + 20, tabY + 52, width - 40, height - 150);
        if (CurrentPage == Page.Timeline && new Rectangle(area.Right - 164, area.Y + 2, 154, 34).Contains(x, y))
        {
            int index = Array.IndexOf(TimelineChoices, Mod.TimelineLength);
            Mod.SetTimelineLength(TimelineChoices[(index + 1) % TimelineChoices.Length]);
            return;
        }
        if (CurrentPage == Page.Season && new Rectangle(area.X + 8, area.Y + 3, 44, 34).Contains(x, y))
        {
            if (SelectedDate.AbsoluteDay >= 28) SelectedDate = SelectedDate.AddDays(-28);
            return;
        }
        if (CurrentPage == Page.Season && new Rectangle(area.Right - 52, area.Y + 3, 44, 34).Contains(x, y))
        {
            SelectedDate = SelectedDate.AddDays(28);
            return;
        }
        if (CurrentPage == Page.Year && new Rectangle(area.X + 8, area.Y + 3, 44, 34).Contains(x, y))
        {
            if (SelectedDate.Year > 1) SelectedDate = SelectedDate.AddDays(-112);
            return;
        }
        if (CurrentPage == Page.Year && new Rectangle(area.Right - 52, area.Y + 3, 44, 34).Contains(x, y))
        {
            SelectedDate = SelectedDate.AddDays(112);
            return;
        }
        if (CurrentPage == Page.Reminders)
        {
            if (new Rectangle(area.X + 8, area.Y + 46, 172, 38).Contains(x, y))
            {
                FarmDate date = SelectedDate;
                ReminderRepeat repeat = NewReminderRepeat;
                Game1.activeClickableMenu = new NamingMenu(name =>
                {
                    Mod.AddReminder(name, date, repeat);
                    Game1.activeClickableMenu = new PlannerMenu(Mod, date) { CurrentPage = Page.Reminders };
                }, Mod.Translate("reminder.prompt"));
                return;
            }
            if (new Rectangle(area.X + 190, area.Y + 46, 190, 38).Contains(x, y))
            {
                NewReminderRepeat = (ReminderRepeat)(((int)NewReminderRepeat + 1) % Enum.GetValues<ReminderRepeat>().Length);
                return;
            }
            if (new Rectangle(area.X + 390, area.Y + 46, 194, 38).Contains(x, y) && SelectedReminderId.HasValue)
            {
                Mod.RemoveReminder(SelectedReminderId.Value);
                SelectedReminderId = null;
                return;
            }
            PlannerReminder[] reminders = Mod.GetReminders().OrderBy(item => item.DueDate).ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase).Take(12).ToArray();
            for (int i = 0; i < reminders.Length; i++)
            {
                Rectangle row = new(area.X + 6, area.Y + 102 + i * 39, area.Width - 12, 34);
                if (!row.Contains(x, y)) continue;
                SelectedReminderId = reminders[i].Id;
                SelectedDate = reminders[i].DueDate;
                return;
            }
        }
        if (CurrentPage == Page.Season)
        {
            int gridX = area.X + 12;
            int gridY = area.Y + 84;
            int cellWidth = Math.Max(34, (area.Width - 24) / 7);
            int cellHeight = Math.Min(72, (area.Height - 98) / 4);
            for (int day = 1; day <= 28; day++)
            {
                Rectangle cell = new(gridX + ((day - 1) % 7) * cellWidth, gridY + ((day - 1) / 7) * cellHeight, cellWidth - 4, cellHeight - 4);
                if (!cell.Contains(x, y)) continue;
                SelectedDate = new FarmDate(SelectedDate.Year, SelectedDate.SeasonIndex * 28 + day);
                return;
            }
        }
        if (CurrentPage == Page.Year)
        {
            int gridX = area.X + 10;
            int gridY = area.Y + 82;
            int cellWidth = (area.Width - 20) / 28;
            int cellHeight = Math.Min(105, (area.Height - 92) / 4);
            for (int day = 1; day <= 112; day++)
            {
                Rectangle cell = new(gridX + ((day - 1) % 28) * cellWidth, gridY + ((day - 1) / 28) * cellHeight, cellWidth - 2, cellHeight - 4);
                if (!cell.Contains(x, y)) continue;
                SelectedDate = new FarmDate(SelectedDate.Year, day);
                return;
            }
        }
        if (CurrentPage == Page.Timeline)
        {
            int days = Mod.TimelineLength;
            int columns = days == 28 ? 2 : 1;
            int rows = days / columns;
            int cellWidth = (area.Width - 20) / columns;
            int cellHeight = Math.Min(42, (area.Height - 58) / rows);
            for (int i = 0; i < days; i++)
            {
                int col = i / rows;
                int row = i % rows;
                Rectangle cell = new(area.X + 10 + col * cellWidth, area.Y + 48 + row * cellHeight, cellWidth - 6, cellHeight - 4);
                if (!cell.Contains(x, y)) continue;
                SelectedDate = Mod.CurrentDate.AddDays(i);
                return;
            }
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            Game1.exitActiveMenu();
            return;
        }
        if (key == Keys.Left && SelectedDate.AbsoluteDay > 0) SelectedDate = SelectedDate.AddDays(-1);
        else if (key == Keys.Right) SelectedDate = SelectedDate.AddDays(1);
        else if (key == Keys.PageUp && SelectedDate.AbsoluteDay >= 28) SelectedDate = SelectedDate.AddDays(-28);
        else if (key == Keys.PageDown) SelectedDate = SelectedDate.AddDays(28);
        base.receiveKeyPress(key);
    }
}
