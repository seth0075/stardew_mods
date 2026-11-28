# Carnivorous Crops and Insect Valley

This repository contains three Stardew Valley mods. **Carnivorous Crops** adds unusual plants and ways to process their harvests. **Insect Valley** adds insects to catch with bait and bug nets, and requires Carnivorous Crops. **Glassworks** adds new mining nodes, glass materials, beach forage, and glassworking machines. Carnivorous Crops can be played on its own.

## For players

### What the mods add

**[CP] Carnivorous Crops** adds 13 seasonal carnivorous plants, including Venus flytraps, pitcher plants, sundews, butterworts, cobra lilies, and more. Grow them from their seeds, then harvest their plant parts. Some plants regrow after harvest, while others need to be replanted.

The **Botanical Extractor** and selected existing artisan machines turn plant harvests into 20 botanical goods. The Botanical Extractor can be crafted after reaching Farming level 4. Five of the plants also have giant crop variants.

**Insect Valley** adds 268 collectible insects, seven types of specialized bait, and two tools: the Bug Net and Iridium Bug Net. Insects depend on where and when you catch them, and bait type and insect rarity affect which insects can be caught.

The Collections menu has an **Insects** page. Uncaught species appear undiscovered; after catching one, its sprite and catch count are shown. While holding a baited net, the HUD previews eligible insects and their catch chances.

**Glassworks** adds silica, sandstone, and natural glass nodes, plus Sea Glass forage on the Beach in every season. The Glass Kiln turns these materials into glass, and the Glassworking Station makes artisan glassware and stained-glass panels. Glass materials are crafting resources and don't receive the Artisan profession bonus; the glassware and panels do.

### Requirements and installation

- Stardew Valley with **SMAPI 4.0.0 or later**.
- **Content Patcher 2.0.0 or later** for `[CP] Carnivorous Crops`.
- **Item Extensions 1.15.0 or later** for Glassworks (which requires SMAPI 4.2.1 or later).
- **Extra Machine Config 1.18.0 or later** for Glassworks' recipe-specific machine ingredients.
- Install `[CP] Carnivorous Crops`, `InsectValley`, and `Glassworks` as separate mod folders. Insect Valley requires Carnivorous Crops because several bait recipes use its plant-based goods.

Install the three mod folders separately into `Stardew Valley/Mods`:

```text
Mods/
├── [CP] Carnivorous Crops/
├── InsectValley/
└── Glassworks/
```

Do not install the source-code folders from this repository as mod folders. Use the folders from a release or the developer build output.

### Getting started

1. Plant carnivorous crop seeds in their listed growing seasons. Check seed descriptions for season and growth information.
2. Craft a Botanical Extractor (Farming level 4) and use it, along with the supported artisan machines, to process harvests into botanical goods.
3. Equip a Bug Net or Iridium Bug Net and attach bait in its single bait slot. Bait is consumed on each swing, even if no insect can be caught in the current conditions.
4. Catch insects in suitable locations, seasons, and times. If your inventory is full, a caught insect drops nearby.
5. Check the Collections menu to see which insects you have found and how many of each you have caught.

When upgrading an older installation, remove the legacy `Mods/CarnivorousPlants` folder and any old split `Mods/InsectValley` folder before installing the current two folders. Insect and bait IDs use the `InsectValley.Core` namespace; saves or other content referring to their former `CarnivorousCrops.ContentPack` IDs may need those items replaced or references updated.

## For developers

### Repository layout

- `[CP] Carnivorous Crops/` is a content-only Content Patcher pack. Its `content.json`, `data/`, and `assets/` define the crops, seeds, plant products, machine outputs, recipes, and sprites. There is no C# project for this mod.
- `Glassworks/` is a content-only Content Patcher pack. It registers custom mine resources with Item Extensions and uses Extra Machine Config for per-recipe machine ingredients.
- `InsectValley.Core/` is the SMAPI C# mod. `data/` defines insect, bait, tool, and recipe data; `assets/` contains their textures. The `InsectValleyContent` class loads these definitions into the game's data assets.
- Insect definitions include custom fields for type, rarity, catch locations, seasons, times, and weight. `InsectDataCache` parses and indexes them; `InsectCatchRules` and `InsectCatchService` determine and perform catches. Harmony patches in `BugNetPatches` connect the nets to the game's tool behavior, while `InsectCollectionPatches` adds the insect collection page.
- Successful catches are stored per player in `Farmer.modData` under the `InsectValley.Core/Caught/` key prefix.
- `InsectValley.Tests/` contains unit tests for data and catch rules. `InsectValley.Tests/GameAcceptanceTests/` contains a separate in-game validation mod.
- `CarnivorousCrops.Tests/` tests the Content Patcher pack's data.

### Build and test

From the repository root, rebuild and stage the mods with:

```powershell
.\build-mods.ps1
```

The script discovers SMAPI projects from their manifests, skips test projects, stages the build, and replaces this repository's current and legacy mod folders in the resolved Stardew Valley `Mods` directory. The Insect Valley build also stages the `[CP] Carnivorous Crops` pack, and the script stages `Glassworks`. Package all three mod folders separately for release.

Run the unit tests with:

```powershell
dotnet test CarnivorousCrops.Tests\CarnivorousCrops.Tests.csproj
dotnet test InsectValley.Tests\InsectValley.Tests.csproj
```

To run the in-game acceptance checks, build `InsectValley.Tests\GameAcceptanceTests\CarnivorousPlants.GameAcceptanceTests.csproj`, then copy its output DLL and `manifest.json` to a new `Mods\CarnivorousPlants.GameAcceptanceTests` folder. Launch SMAPI with Content Patcher, Carnivorous Crops, and Insect Valley enabled, then load a save. The harness checks loaded crop schedules and harvest items, artisan machine outputs, insect and bait data, legendary insects, and bug-net attachments. It reports `PASS` and `FAIL` entries plus a final summary in the SMAPI log; the checks are read-only.

The Insect Valley core project targets .NET 6 and uses `Pathoschild.Stardew.ModBuildConfig`. Its unit-test project targets .NET 10.
