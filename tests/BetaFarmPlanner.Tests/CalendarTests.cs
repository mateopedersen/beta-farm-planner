using BetaFarmPlanner.Core;

namespace BetaFarmPlanner.Tests;

public sealed class CalendarTests
{
    [Theory]
    [InlineData("spring", 28, "summer", 1, 1)]
    [InlineData("summer", 28, "fall", 1, 1)]
    [InlineData("fall", 28, "winter", 1, 1)]
    [InlineData("winter", 28, "spring", 1, 2)]
    public void AddDays_crosses_season_and_year_boundaries(string season, int day, string expectedSeason, int expectedDay, int expectedYear)
    {
        FarmDate result = FarmDate.FromGameDate(1, season, day).AddDays(1);
        Assert.Equal(expectedSeason, result.Season);
        Assert.Equal(expectedDay, result.DayOfSeason);
        Assert.Equal(expectedYear, result.Year);
    }

    [Fact]
    public void Absolute_day_round_trips_across_years()
    {
        FarmDate expected = new(12, 112);
        Assert.Equal(expected, FarmDate.FromAbsoluteDay(expected.AbsoluteDay));
        Assert.Equal(12, expected.Year);
    }

    [Fact]
    public void Days_until_is_signed_and_uses_absolute_game_days()
    {
        FarmDate winter28 = FarmDate.FromGameDate(3, "winter", 28);
        Assert.Equal(1, winter28.DaysUntil(winter28.AddDays(1)));
        Assert.Equal(-1, winter28.AddDays(1).DaysUntil(winter28));
    }

    [Fact]
    public void Date_creation_rejects_invalid_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FarmDate(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FarmDate(1, 113));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FarmDate(1, 1).AddDays(-1));
    }
}

public sealed class ReminderTests
{
    private static readonly FarmDate Due = FarmDate.FromGameDate(1, "spring", 8);

    [Fact]
    public void Once_occurs_only_on_due_date()
    {
        PlannerReminder reminder = new("Test", Due, ReminderRepeat.Once);
        Assert.True(reminder.OccursOn(Due));
        Assert.False(reminder.OccursOn(Due.AddDays(1)));
    }

    [Fact]
    public void Weekly_repeats_every_seven_game_days()
    {
        PlannerReminder reminder = new("Test", Due, ReminderRepeat.Weekly);
        Assert.True(reminder.OccursOn(Due.AddDays(7)));
        Assert.False(reminder.OccursOn(Due.AddDays(6)));
    }

    [Fact]
    public void Seasonal_repeat_uses_same_day_of_each_season()
    {
        PlannerReminder reminder = new("Test", Due, ReminderRepeat.EverySeason);
        Assert.True(reminder.OccursOn(FarmDate.FromGameDate(1, "fall", 8)));
        Assert.False(reminder.OccursOn(FarmDate.FromGameDate(1, "fall", 9)));
    }

    [Fact]
    public void Yearly_repeat_uses_same_season_and_day_in_later_years()
    {
        PlannerReminder reminder = new("Test", Due, ReminderRepeat.EveryYear);
        Assert.True(reminder.OccursOn(FarmDate.FromGameDate(2, "spring", 8)));
        Assert.False(reminder.OccursOn(FarmDate.FromGameDate(2, "summer", 8)));
    }

    [Fact]
    public void Window_includes_recurrences_in_date_order()
    {
        PlannerReminder reminder = new("Weekly", Due, ReminderRepeat.Weekly);
        PlannerEvent[] events = reminder.GetEvents(Due, 15).ToArray();
        Assert.Equal(3, events.Length);
        Assert.Equal(Due.AddDays(7), events[1].Date);
        Assert.Equal(Due.AddDays(14), events[2].Date);
    }
}

public sealed class CropDeadlineTests
{
    [Fact]
    public void Single_season_crop_has_last_safe_plant_day_from_growth_phases()
    {
        CropDeadlineInput crop = new("parsnip", "Parsnip", new[] { "spring" }, new[] { 1, 1, 1, 1 });
        CropDeadline result = CropDeadlineCalculator.Calculate(crop, FarmDate.FromGameDate(1, "spring", 20))!;
        Assert.Equal(24, result.LatestPlantDate.DayOfSeason);
        Assert.Equal(28, result.FirstHarvestDate.DayOfSeason);
        Assert.Equal(4, result.GrowthDays);
    }

    [Fact]
    public void Multi_season_crop_can_finish_after_transition_if_both_seasons_are_allowed()
    {
        CropDeadlineInput crop = new("corn", "Corn", new[] { "summer", "fall" }, new[] { 2, 3, 3, 3 });
        CropDeadline result = CropDeadlineCalculator.Calculate(crop, FarmDate.FromGameDate(1, "summer", 20))!;
        Assert.Equal(28, result.LatestPlantDate.DayOfSeason);
        Assert.Equal("fall", result.FirstHarvestDate.Season);
    }

    [Fact]
    public void Impossible_crop_or_invalid_phase_data_returns_no_estimate()
    {
        CropDeadlineInput wrongSeason = new("melon", "Melon", new[] { "summer" }, new[] { 1, 1 });
        CropDeadlineInput invalid = new("bad", "Unknown", new[] { "spring" }, new[] { 0, -1 });
        Assert.Null(CropDeadlineCalculator.Calculate(wrongSeason, FarmDate.FromGameDate(1, "spring", 1)));
        Assert.Null(CropDeadlineCalculator.Calculate(invalid, FarmDate.FromGameDate(1, "spring", 1)));
    }

    [Fact]
    public void Crop_with_no_remaining_safe_days_is_not_shown()
    {
        CropDeadlineInput crop = new("ancient", "Ancient Fruit", new[] { "spring" }, new[] { 28 });
        Assert.Null(CropDeadlineCalculator.Calculate(crop, FarmDate.FromGameDate(1, "spring", 28)));
    }
}

public sealed class PlanningPressureTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, "LOW")]
    [InlineData(1, 0, 0, 0, "BUSY")]
    [InlineData(0, 1, 0, 0, "NORMAL")]
    [InlineData(1, 1, 2, 1, "CRITICAL")]
    public void Pressure_is_deterministic(int festivals, int birthdays, int reminders, int crops, string expected)
    {
        Assert.Equal(expected, PlanningPressure.GetLabel(festivals, birthdays, reminders, crops));
    }
}
