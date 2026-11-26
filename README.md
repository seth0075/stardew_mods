# stardew_mods

## CarnivorousPlants

Install by copying the single `CarnivorousPlants` folder into `Stardew Valley\Mods`. It contains:

- `[CP] Carnivorous Crops` - Content Patcher pack (crops, items, machines, bait). Requires Content Patcher.
- `CarnivorousPlants.Core` - C# SMAPI mod (insect bait category label and bug net attachment logic).

Running `dotnet build` in `CarnivorousPlants\CarnivorousPlants.Core` compiles the DLL and deploys the whole folder to `Mods\CarnivorousPlants`. For a distributable copy, zip the deployed `Mods\CarnivorousPlants` folder, since the source folder has no DLL next to its manifest.

The Bug Net has one slot for insect bait, but cannot catch insects yet. There is no normal acquisition method yet; use an item spawner to obtain `(T)CarnivorousCrops.ContentPack_BugNet` for testing.