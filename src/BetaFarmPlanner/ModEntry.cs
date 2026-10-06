using BetaFarmPlanner.Config;
using BetaFarmPlanner.Core;
using BetaFarmPlanner.Services;
using BetaFarmPlanner.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BetaFarmPlanner;

public sealed class ModEntry : Mod
{
    private const string SaveDataKey = "planner-reminders-v1";
    private ModConfig Config { get; set; } = new();
    private List<PlannerReminder> Reminders { get; set; } = new();
    private IReadOnlyList<PlannerEvent> Events { get; set; } = Array.Empty<PlannerEvent>();
    private IReadOnlyList<CropDeadline> CropDeadlines { get; set; } = Array.Empty<CropDeadline>();
    private EventService EventService { get; set; } = null!;
    private CropDeadlineService CropDeadlineService { get; set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        EventService = new EventService(Monitor, helper.Translation);
        CropDeadlineService = new CropDeadlineService(Monitor, helper.Translation);
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        Monitor.Log($"Beta Farm Planner loaded. Hotkey: {Config.OpenPlannerKey}.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        try
        {
            Reminders = Helper.Data.ReadSaveData<List<PlannerReminder>>(SaveDataKey) ?? new List<PlannerReminder>();
        }
        catch (Exception ex)
        {
            Reminders = new List<PlannerReminder>();
            Monitor.Log($"Could not load planner reminders; starting with an empty list. {ex.Message}", LogLevel.Warn);
        }
        RefreshCalendarData();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsWorldReady)
            RefreshCalendarData();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != Config.OpenPlannerKey)
            return;

        Helper.Input.Suppress(e.Button);
        Game1.activeClickableMenu = new PlannerMenu(this);
    }

    public FarmDate CurrentDate => FarmDate.FromGameDate(Game1.year, Game1.currentSeason, Game1.dayOfMonth);
    public IReadOnlyList<PlannerReminder> GetReminders() => Reminders;
    public IReadOnlyList<PlannerEvent> GetEvents() => Events;
    public IReadOnlyList<CropDeadline> GetCropDeadlines() => CropDeadlines;
    public int TimelineLength => Config.TimelineLength is 7 or 14 or 28 ? Config.TimelineLength : 7;
    public bool ShowBirthdays => Config.ShowBirthdays;
    public bool ShowFestivals => Config.ShowFestivals;
    public bool ShowCropDeadlines => Config.ShowCropDeadlines;
    public void SetTimelineLength(int days)
    {
        if (days is not (7 or 14 or 28)) return;
        Config.TimelineLength = days;
        Helper.WriteConfig(Config);
    }
    public string Translate(string key, params object[] values)
    {
        string text = Helper.Translation.Get(key).ToString();
        return values.Length == 0 ? text : string.Format(System.Globalization.CultureInfo.CurrentCulture, text, values);
    }

    public void AddReminder(string title, FarmDate date, ReminderRepeat repeat)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        Reminders.Add(new PlannerReminder(title, date, repeat));
        SaveReminders();
        RefreshCalendarData();
        Game1.addHUDMessage(new HUDMessage(Translate("reminder.saved", date.ToString())) { noIcon = true, timeLeft = 3000f });
    }

    public void RemoveReminder(Guid id)
    {
        int removed = Reminders.RemoveAll(reminder => reminder.Id == id);
        if (removed == 0) return;
        SaveReminders();
        RefreshCalendarData();
        Game1.addHUDMessage(new HUDMessage(Translate("reminder.deleted")) { noIcon = true, timeLeft = 2500f });
    }

    private void SaveReminders()
    {
        try
        {
            Helper.Data.WriteSaveData(SaveDataKey, Reminders);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not save planner reminders. {ex}", LogLevel.Error);
        }
    }

    private void RefreshCalendarData()
    {
        try
        {
            FarmDate today = CurrentDate;
            Events = EventService.LoadEvents(today, Reminders);
            CropDeadlines = CropDeadlineService.LoadDeadlines(today);
        }
        catch (Exception ex)
        {
            Events = Reminders.SelectMany(reminder => reminder.GetEvents(CurrentDate, 28)).OrderBy(item => item.Date).ToArray();
            CropDeadlines = Array.Empty<CropDeadline>();
            Monitor.Log($"Some game data could not be read. Calendar views remain available. {ex}", LogLevel.Warn);
        }
    }
}
