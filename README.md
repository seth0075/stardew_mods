# stardew_mods

## Carnivorous Crops and Insect Valley

The project ships as two mods:

- `[CP] Carnivorous Crops` is a Content Patcher pack containing the carnivorous crops, giant crops, Botanical Extractor, and plant-based artisan goods. It requires Content Patcher.
- `InsectValley` is a single SMAPI mod containing its code, data, and assets. It adds insects, bait, bug nets, and bug catching, and requires Carnivorous Crops.

Install `[CP] Carnivorous Crops` and `InsectValley` as separate folders in `Stardew Valley\Mods`. Insect Valley declares Carnivorous Crops as a required dependency because its bait recipes use that mod's artisan goods. When upgrading, remove the old `Mods\CarnivorousPlants` folder and any previous split `Mods\InsectValley` folder before copying in the new installation.

Rebuild all SMAPI mod projects from the repository root with:

```powershell
.\build-mods.ps1
```

The script discovers mod projects by their `manifest.json` and rebuilds them without incremental compilation. Test projects are skipped. It stages the build first, then replaces this project's current and legacy mod folders in the resolved Stardew Valley `Mods` directory with the freshly built folders. Carnivorous Crops is a content-only Content Patcher pack, so it has no project to compile; Insect Valley's build stages both mods. For a release, package `[CP] Carnivorous Crops` separately from the `InsectValley` folder, which contains one SMAPI manifest and DLL alongside Insect Valley's data and assets.

Run the unit tests with:

```powershell
dotnet test CarnivorousCrops.Tests\CarnivorousCrops.Tests.csproj
dotnet test InsectValley.Tests\InsectValley.Tests.csproj
```

For in-game acceptance checks, build `InsectValley.Tests\GameAcceptanceTests\CarnivorousPlants.GameAcceptanceTests.csproj`, then copy its output DLL and `manifest.json` into a new `Mods\CarnivorousPlants.GameAcceptanceTests` folder. Launch SMAPI with Content Patcher, Carnivorous Crops, and Insect Valley enabled, then load a save. The harness checks crop data, artisan machine outputs, insect and bait data, and bug-net attachments; results appear in the SMAPI log as `PASS`, `FAIL`, and a final summary. These checks are read-only.

The Bug Net and Iridium Bug Net each have one bait slot and use the scythe swing animation and sound. Each swing consumes one attached bait and attempts a catch using the current location, season, time, insect rarity, and bait type. If no insects match, bait is still consumed; if the inventory is full, the insect drops nearby. While holding a baited bug net, the HUD shows remaining bait and per-swing catch chances. For testing, use an item spawner to obtain `(T)InsectValley.Core_BugNet` or `(T)InsectValley.Core_IridiumBugNet`; run `bugnet_pool` in the SMAPI console to list eligible insects.

The Collections menu includes an Insects page with every species shown as undiscovered until caught. Each player's catch count is saved and shown in the insect's collection description.

Insect Valley defines 268 insects and seven bait types. Bait recipes use artisan goods from Carnivorous Crops. Insect and bait IDs use the `InsectValley.Core` namespace, so existing saves or content that refer to their former `CarnivorousCrops.ContentPack` IDs may need those items replaced or references updated.
