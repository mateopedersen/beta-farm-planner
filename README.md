# Beta Farm Planner

Beta Farm Planner is an offline Stardew Valley SMAPI mod for planning across the current day, upcoming days, a 28-day season, and the complete 112-day year. It brings birthdays, festivals, saved reminders, season transitions, and crop planting windows into one menu.

## Features

- Press **P** by default to open a planner menu. The key can be changed in `config.json`.
- **Today** shows the current in-game date, season countdown, scheduled events, and a deterministic planning-pressure label.
- **Timeline** shows the next 7, 14, or 28 game days. Click a day to select it.
- **Season** shows all 28 days with festival, birthday, reminder, season, and crop-deadline markers. The arrows move between seasons and years.
- **Year** shows Spring, Summer, Fall, and Winter together as 112 selectable day cells.
- **Reminders** are saved per save file. Add a title for the selected date, choose once, weekly, every season, or every year, select an item, and remove it when it is no longer needed.
- **Crop deadlines** are calculated from the loaded `Data/Crops` asset, including modded crop data when it follows the game schema.
- English and Turkish interface text are included.

## Installation

1. Install [SMAPI](https://smapi.io/).
2. Download the release ZIP and extract the `BetaFarmPlanner` folder into Stardew Valley's `Mods` folder.
3. Start the game through SMAPI. Press **P** after loading a save.

The mod does not need BetaCalendars.com, an internet connection, or another mod to run.

## Controls and reminders

Use the timeline, season, or year view to select a date. In the Reminders view, choose a repeat mode and select **Add reminder**. The game’s native name-entry screen is used for the title. Select a reminder row and use **Remove selected** to delete it.

Reminder recurrence is date-based: weekly reminders recur every seven game days; seasonal reminders recur on the same day number in every season; yearly reminders recur on the same season and day in later years. Reminders are stored through SMAPI’s save-data API and never rewrite the game’s own save structures.

## Crop deadline estimates

The crop view reads the game’s current crop data, sums the configured growth phases, and checks whether the crop’s allowed seasons cover each day through its first harvest. Multi-season crops can therefore mature across a season boundary when the data lists both seasons. Results show the latest safe planting date in the current season and the expected first harvest date.

This v1 estimate does not model fertilizer, Agriculturist, regrowth harvest timing, paddy adjacency, location-specific planting rules, or custom mechanics which change growth duration. It is a planning aid, not a guarantee; the mod avoids showing an estimate when its inputs are invalid or a crop cannot remain through harvest.

## Planning pressure

The label is transparent and deterministic: festivals count as 3 points, birthdays as 2, reminders as 1, and crop deadlines due today as 1. Scores 0–1 are **LOW**, 2–3 **NORMAL**, 4–6 **BUSY**, and 7 or more **CRITICAL**. It is not an AI prediction.

## Configuration

SMAPI creates `config.json` after the first launch. `OpenPlannerKey` defaults to `P`; `TimelineLength` accepts 7, 14, or 28; and the birthday, festival, and crop-deadline views can be toggled there.

## Compatibility and limitations

- Intended for Stardew Valley 1.6.14 or later and SMAPI 4.4.0 or later.
- Festival dates, NPC birthdays, and crop information are read from game data assets so compatible data changes can be reflected automatically.
- This first release has compile-time reference-assembly validation and automated domain tests. It has not been launched inside the game in this environment. The first-run UI, multiplayer host/client behavior, and third-party data variations still need player runtime reports.
- The mod contains no Harmony patches, telemetry, analytics, ads, external network requests, or automatic browser actions.

## Troubleshooting

If the menu does not open, verify SMAPI loaded Beta Farm Planner and check that the configured hotkey is not overridden by another mod. If an event or crop is missing, check whether the corresponding game data asset defines it. For a crash, include the SMAPI log when reporting the issue.

## Privacy and save safety

No player data leaves the computer. Reminders use SMAPI’s per-save data store. Removing the mod leaves the game’s own save data untouched; reminder data is mod-owned and will no longer be available while the mod is absent.

## Credits and license

Developed by Beta Calendars as an open-source game-planning utility. Stardew Valley is owned by ConcernedApe and is not affiliated with or endorsed by this project. This project is licensed under MIT; see [LICENSE](LICENSE).
