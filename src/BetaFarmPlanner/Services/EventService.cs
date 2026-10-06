using BetaFarmPlanner.Core;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Characters;

namespace BetaFarmPlanner.Services;

internal sealed class EventService
{
    private readonly IMonitor Monitor;
    private readonly ITranslationHelper Translation;

    public EventService(IMonitor monitor, ITranslationHelper translation)
    {
        Monitor = monitor;
        Translation = translation;
    }

    public IReadOnlyList<PlannerEvent> LoadEvents(FarmDate today, IReadOnlyList<PlannerReminder> reminders)
    {
        List<PlannerEvent> result = new();
        AddSeasonBoundaries(result, today.Year);
        AddSeasonBoundaries(result, today.Year + 1);
        AddFestivals(result, today.Year);
        AddFestivals(result, today.Year + 1);
        AddBirthdays(result, today.Year);
        AddBirthdays(result, today.Year + 1);
        foreach (PlannerReminder reminder in reminders)
            result.AddRange(reminder.GetEvents(today, 112));
        return CalendarEventOrdering.Sort(result);
    }

    private static void AddSeasonBoundaries(List<PlannerEvent> events, int year)
    {
        for (int i = 0; i < FarmDate.Seasons.Length; i++)
        {
            FarmDate date = new(year, i * FarmDate.DaysPerSeason + 1);
            events.Add(new PlannerEvent(date, $"{char.ToUpperInvariant(FarmDate.Seasons[i][0])}{FarmDate.Seasons[i][1..]}", PlannerEventKind.SeasonBoundary));
        }
    }

    private void AddFestivals(List<PlannerEvent> events, int year)
    {
        try
        {
            Dictionary<string, string> dates = Game1.content.Load<Dictionary<string, string>>("Data/Festivals/FestivalDates");
            foreach (KeyValuePair<string, string> entry in dates)
            {
                if (!TryParseFestivalKey(entry.Key, out int season, out int day)) continue;
                events.Add(new PlannerEvent(new FarmDate(year, (season * FarmDate.DaysPerSeason) + day), entry.Value, PlannerEventKind.Festival));
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Festival calendar data unavailable: {ex.Message}", LogLevel.Trace);
        }
    }

    private static bool TryParseFestivalKey(string key, out int seasonIndex, out int day)
    {
        seasonIndex = -1;
        day = 0;
        if (string.IsNullOrWhiteSpace(key)) return false;
        int numberStart = key.Length;
        while (numberStart > 0 && char.IsDigit(key[numberStart - 1])) numberStart--;
        if (numberStart == key.Length || !int.TryParse(key[numberStart..], out day) || day is < 1 or > 28) return false;
        string season = key[..numberStart];
        seasonIndex = Array.FindIndex(FarmDate.Seasons, item => string.Equals(item, season, StringComparison.OrdinalIgnoreCase));
        return seasonIndex >= 0;
    }

    private void AddBirthdays(List<PlannerEvent> events, int year)
    {
        try
        {
            Dictionary<string, CharacterData> npcs = Game1.content.Load<Dictionary<string, CharacterData>>("Data/Characters");
            foreach ((string name, CharacterData data) in npcs)
            {
                if (data.BirthDay is < 1 or > 28 || string.IsNullOrWhiteSpace(data.BirthSeason)) continue;
                int season = Array.FindIndex(FarmDate.Seasons, item => string.Equals(item, data.BirthSeason, StringComparison.OrdinalIgnoreCase));
                if (season < 0) continue;
                string displayName = name;
                try
                {
                    Dictionary<string, string> names = Game1.content.Load<Dictionary<string, string>>("Strings/NPCNames");
                    if (names.TryGetValue(name, out string? translated)) displayName = translated;
                }
                catch { /* Fall back to the stable NPC data key. */ }
                events.Add(new PlannerEvent(new FarmDate(year, season * FarmDate.DaysPerSeason + data.BirthDay), displayName, PlannerEventKind.Birthday));
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"NPC birthday data unavailable: {ex.Message}", LogLevel.Trace);
        }
    }
}
