using BetaFarmPlanner.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;

namespace BetaFarmPlanner.Services;

internal sealed class CropDeadlineService
{
    private readonly IMonitor Monitor;
    private readonly ITranslationHelper Translation;

    public CropDeadlineService(IMonitor monitor, ITranslationHelper translation)
    {
        Monitor = monitor;
        Translation = translation;
    }

    public IReadOnlyList<CropDeadline> LoadDeadlines(FarmDate today)
    {
        try
        {
            Dictionary<string, CropData> data = Game1.content.Load<Dictionary<string, CropData>>("Data/Crops");
            List<CropDeadline> result = new();
            foreach ((string cropId, CropData crop) in data)
            {
                if (crop.DaysInPhase is null || crop.Seasons is null) continue;
                string harvestName = crop.HarvestItemId;
                try
                {
                    harvestName = ItemRegistry.GetData(crop.HarvestItemId).DisplayName;
                }
                catch { /* Keep the raw item ID for modded or unavailable items. */ }

                CropDeadlineInput input = new(cropId, harvestName, crop.Seasons.Select(season => season.ToString()).ToArray(), crop.DaysInPhase, crop.RegrowDays);
                CropDeadline? deadline = CropDeadlineCalculator.Calculate(input, today);
                if (deadline is not null)
                    result.Add(deadline);
            }
            return result.OrderBy(item => item.LatestPlantDate).ThenBy(item => item.HarvestName, StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Crop data unavailable; hiding deadline estimates. {ex.Message}", LogLevel.Trace);
            return Array.Empty<CropDeadline>();
        }
    }
}
