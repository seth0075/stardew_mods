# stardew_mods

## CarnivorousPlants

Install by copying the single `CarnivorousPlants` folder into `Stardew Valley\Mods`. It contains:

- `[CP] Carnivorous Crops` - Content Patcher pack (crops, items, machines, bait). Requires Content Patcher.
- `CarnivorousPlants.Core` - C# SMAPI mod (insect and insect bait category labels and bug net attachment logic).

Running `dotnet build` in `CarnivorousPlants\CarnivorousPlants.Core` compiles the DLL and deploys the whole folder to `Mods\CarnivorousPlants`. For a distributable copy, zip the deployed `Mods\CarnivorousPlants` folder, since the source folder has no DLL next to its manifest.

The Bug Net and Iridium Bug Net each have one slot for insect bait and use the scythe swing animation and sound once per press, but cannot catch insects yet. Swinging does not consume bait or affect the world. There is no normal acquisition method yet; use an item spawner to obtain `(T)CarnivorousCrops.ContentPack_BugNet` or `(T)CarnivorousCrops.ContentPack_IridiumBugNet` for testing.

The pack also defines 268 insect and other bug items, including all 80 entries from the [New Horizons bug list](https://nookipedia.com/wiki/Bug/New_Horizons). Each has a sell value and namespaced `CustomFields` for rarity (`common`, `rare`, or `legendary`), catch locations, seasons, active time range, and relative `CatchWeight`. Catch weights should be normalized among eligible insects; this metadata does not implement spawning or catching yet. The insects have no normal acquisition method; for example, use an item spawner to obtain `(O)CarnivorousCrops.ContentPack_InsectMonarchButterfly`. Their `carnivorousplants_insect` context tag identifies the Insect group; the core mod displays its category label, including for non-insect bugs.