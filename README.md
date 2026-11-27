# stardew_mods

## CarnivorousPlants

Install by copying the single `CarnivorousPlants` folder into `Stardew Valley\Mods`. It contains:

- `[CP] Carnivorous Crops` - Content Patcher pack (crops, items, machines, bait). Requires Content Patcher.
- `CarnivorousPlants.Core` - C# SMAPI mod (insect and bait category labels, bug net attachments, and insect catching).

Running `dotnet build` in `CarnivorousPlants\CarnivorousPlants.Core` compiles the DLL and deploys the whole folder to `Mods\CarnivorousPlants`. For a distributable copy, zip the deployed `Mods\CarnivorousPlants` folder, since the source folder has no DLL next to its manifest.

Run `dotnet test CarnivorousPlants\CarnivorousPlants.Tests\CarnivorousPlants.Tests.csproj` to verify crop schedules and harvests, artisan machine outputs, insect availability metadata, bait restrictions, and catch rarity/location rules.

For in-game acceptance checks, build `CarnivorousPlants\CarnivorousPlants.Tests\GameAcceptanceTests\CarnivorousPlants.GameAcceptanceTests.csproj`, then copy its output `CarnivorousPlants.GameAcceptanceTests.dll` and `manifest.json` into a new `Mods\CarnivorousPlants.GameAcceptanceTests` folder. Launch SMAPI with the main mod and Content Patcher enabled, then load a save. The harness checks the game-loaded crop, insect, bait, and machine data and the bug-net attachment patch; results appear in the SMAPI log as `PASS`, `FAIL`, and a final summary. These checks are read-only and can be removed by deleting the test mod folder after the run.

The Bug Net and Iridium Bug Net each have one bait slot and use the scythe swing animation and sound. Each swing consumes one attached bait and attempts a catch using the current location, season, time, insect rarity, and bait type. If no insects match, the bait is still consumed; if the inventory is full, the insect drops nearby. For testing, use an item spawner to obtain `(T)CarnivorousCrops.ContentPack_BugNet` or `(T)CarnivorousCrops.ContentPack_IridiumBugNet`.

The pack also defines 268 insect and other bug items, including all 80 entries from the [New Horizons bug list](https://nookipedia.com/wiki/Bug/New_Horizons). Each has a sell value and namespaced `CustomFields` for rarity (`common`, `rare`, or `legendary`), catch locations, seasons, active time range, and relative `CatchWeight`. The core mod normalizes catch weights among eligible insects and applies the bait's rarity rules. Their `carnivorousplants_insect` context tag identifies the Insect group; the core mod displays its category label, including for non-insect bugs.