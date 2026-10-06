namespace BetaFarmPlanner.Core;

public enum PlannerEventKind
{
    Festival,
    Birthday,
    Reminder,
    SeasonBoundary,
    CropDeadline
}

public sealed record PlannerEvent(FarmDate Date, string Title, PlannerEventKind Kind);

public static class CalendarEventOrdering
{
    public static IReadOnlyList<PlannerEvent> Sort(IEnumerable<PlannerEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return events.OrderBy(item => item.Date).ThenBy(item => item.Kind).ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
}

public enum ReminderRepeat
{
    Once,
    Weekly,
    EverySeason,
    EveryYear
}

public sealed record PlannerReminder(Guid Id, string Title, FarmDate DueDate, ReminderRepeat Repeat)
{
    public PlannerReminder(string title, FarmDate dueDate, ReminderRepeat repeat)
        : this(Guid.NewGuid(), title.Trim(), dueDate, repeat)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A reminder title is required.", nameof(title));
    }

    public bool OccursOn(FarmDate date)
    {
        if (date < DueDate) return false;
        return Repeat switch
        {
            ReminderRepeat.Once => date == DueDate,
            ReminderRepeat.Weekly => DueDate.DaysUntil(date) % 7 == 0,
            ReminderRepeat.EverySeason => date.DayOfSeason == DueDate.DayOfSeason,
            ReminderRepeat.EveryYear => date.DayOfYear == DueDate.DayOfYear,
            _ => false
        };
    }

    public IEnumerable<PlannerEvent> GetEvents(FarmDate start, int dayCount)
    {
        if (dayCount < 0) throw new ArgumentOutOfRangeException(nameof(dayCount));
        FarmDate end = start.AddDays(dayCount);
        FarmDate cursor = start < DueDate ? DueDate : start;
        while (cursor <= end)
        {
            if (OccursOn(cursor))
                yield return new PlannerEvent(cursor, Title, PlannerEventKind.Reminder);
            cursor = cursor.AddDays(1);
        }
    }
}

public sealed record CropDeadlineInput(
    string CropId,
    string HarvestName,
    IReadOnlyCollection<string> Seasons,
    IReadOnlyList<int> DaysInPhase,
    int RegrowDays = -1);

public sealed record CropDeadline(
    string CropId,
    string HarvestName,
    FarmDate LatestPlantDate,
    FarmDate FirstHarvestDate,
    int GrowthDays,
    int RegrowDays);

public static class CropDeadlineCalculator
{
    public static CropDeadline? Calculate(CropDeadlineInput crop, FarmDate referenceDate)
    {
        ArgumentNullException.ThrowIfNull(crop);
        if (crop.DaysInPhase.Count == 0 || crop.DaysInPhase.Any(days => days < 0)) return null;

        int growthDays = crop.DaysInPhase.Sum();
        if (growthDays <= 0) return null;

        if (!crop.Seasons.Contains(referenceDate.Season, StringComparer.OrdinalIgnoreCase)) return null;
        int seasonStart = referenceDate.SeasonIndex * FarmDate.DaysPerSeason + 1;
        int seasonEnd = seasonStart + FarmDate.DaysPerSeason - 1;
        for (int candidateDay = seasonEnd; candidateDay >= seasonStart; candidateDay--)
        {
            FarmDate plantDate = new(referenceDate.Year, candidateDay);
            FarmDate harvestDate = plantDate.AddDays(growthDays);
            if (!CanCropRemainThroughHarvest(crop.Seasons, plantDate, harvestDate))
                continue;

            return new CropDeadline(crop.CropId, crop.HarvestName, plantDate, harvestDate, growthDays, crop.RegrowDays);
        }
        return null;
    }

    private static bool CanCropRemainThroughHarvest(IReadOnlyCollection<string> seasons, FarmDate plant, FarmDate harvest)
    {
        if (!seasons.Contains(plant.Season, StringComparer.OrdinalIgnoreCase)) return false;
        for (FarmDate date = plant; date <= harvest; date = date.AddDays(1))
        {
            if (!seasons.Contains(date.Season, StringComparer.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}

public static class PlanningPressure
{
    public static string GetLabel(int festivals, int birthdays, int reminders, int cropDeadlines)
    {
        int score = (festivals * 3) + (birthdays * 2) + reminders + cropDeadlines;
        return score switch
        {
            <= 1 => "LOW",
            <= 3 => "NORMAL",
            <= 6 => "BUSY",
            _ => "CRITICAL"
        };
    }
}
