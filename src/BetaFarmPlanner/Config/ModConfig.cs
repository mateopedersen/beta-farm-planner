using StardewModdingAPI;

namespace BetaFarmPlanner.Config;

public sealed class ModConfig
{
    public SButton OpenPlannerKey { get; set; } = SButton.P;
    public int TimelineLength { get; set; } = 7;
    public bool ShowBirthdays { get; set; } = true;
    public bool ShowFestivals { get; set; } = true;
    public bool ShowCropDeadlines { get; set; } = true;
}
