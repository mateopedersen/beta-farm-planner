namespace BetaFarmPlanner.Core;

public readonly record struct FarmDate : IComparable<FarmDate>
{
    public const int DaysPerSeason = 28;
    public const int SeasonsPerYear = 4;
    public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;
    public static readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

    public FarmDate(int year, int dayOfYear)
    {
        if (year < 1) throw new ArgumentOutOfRangeException(nameof(year));
        if (dayOfYear is < 1 or > DaysPerYear) throw new ArgumentOutOfRangeException(nameof(dayOfYear));
        Year = year;
        DayOfYear = dayOfYear;
    }

    public int Year { get; }
    public int DayOfYear { get; }
    public int SeasonIndex => (DayOfYear - 1) / DaysPerSeason;
    public int DayOfSeason => ((DayOfYear - 1) % DaysPerSeason) + 1;
    public string Season => Seasons[SeasonIndex];
    public long AbsoluteDay => ((long)Year - 1) * DaysPerYear + DayOfYear - 1;

    public FarmDate AddDays(int days)
    {
        long absolute = checked(AbsoluteDay + days);
        if (absolute < 0) throw new ArgumentOutOfRangeException(nameof(days), "The date can't precede Year 1, Spring 1.");
        return FromAbsoluteDay(absolute);
    }

    public long DaysUntil(FarmDate other) => other.AbsoluteDay - AbsoluteDay;

    public static FarmDate FromGameDate(int year, string season, int dayOfSeason)
    {
        int index = Array.FindIndex(Seasons, value => string.Equals(value, season, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new ArgumentException($"Unknown season '{season}'.", nameof(season));
        if (dayOfSeason is < 1 or > DaysPerSeason) throw new ArgumentOutOfRangeException(nameof(dayOfSeason));
        return new FarmDate(year, index * DaysPerSeason + dayOfSeason);
    }

    public static FarmDate FromAbsoluteDay(long absoluteDay)
    {
        if (absoluteDay < 0) throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        return new FarmDate(checked((int)(absoluteDay / DaysPerYear) + 1), (int)(absoluteDay % DaysPerYear) + 1);
    }

    public int CompareTo(FarmDate other) => AbsoluteDay.CompareTo(other.AbsoluteDay);

    public static bool operator <(FarmDate left, FarmDate right) => left.CompareTo(right) < 0;
    public static bool operator >(FarmDate left, FarmDate right) => left.CompareTo(right) > 0;
    public static bool operator <=(FarmDate left, FarmDate right) => left.CompareTo(right) <= 0;
    public static bool operator >=(FarmDate left, FarmDate right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{char.ToUpperInvariant(Season[0])}{Season[1..]} {DayOfSeason}, Year {Year}";
}
