# Farms module

Farms is an add-on under `Scripts/Custom/Farms`. Its seeds use ServUO's built-in plant pots and plant growth/upkeep mechanics. The core plant type enum and metadata include the Farms crop names so they appear correctly in plant properties; crop resources and seeds are registered by the module.

## Included crops

| Seed item | Harvested item |
|---|---|
| `OnionSeed` | Onion |
| `PotatoSeed` | Potato |
| `CabbageSeed` | Cabbage |
| `CarrotSeed` | Carrot |
| `LettuceSeed` | Lettuce |
| `PumpkinSeed` | Pumpkin |
| `TurnipSeed` | Turnip |
| `GrapesSeed` | Grapes |
| `WatermelonSeed` | Watermelon |
| `WheatSeed` | Wheat sheaf |
| `AppleSeed` | Apple |
| `PeachSeed` | Peach |
| `PearSeed` | Pear |
| `BananaSeed` | Banana |
| `LemonSeed` | Lemon |
| `LimeSeed` | Lime |
| `CoconutSeed` | Coconut |
| `DatesSeed` | Dates |

Farm crops use the base plant system's fallback appearance while growing. Stages 7, 8, and 9 are harvest milestones: the first crop is available at stage 7, further growth adds more crops, and from stage 9 onward growth can also produce seeds.

## Usage

Build and start ServUO. Use any standard `PlantItem` pot and plant one of the seeds listed above. Keep the plant alive through all growth stages. At stage 7, use the pot's standard reproduction menu and choose **Gather resources**; growth at stages 8 and 9 adds further crops, and later growth at stage 9 produces seeds. Collect seeds from the same menu using **Gather seeds**.

## Save compatibility

The Farms plant identifiers are `44` through `61`. Do not reorder or reuse them: planted pots serialize these identifiers, and changing them can change the crop type after loading a world save. Farms registers its crop resources and crop seeds with the standard plant-resource system before world loading; standard pots retain built-in growth, upkeep, and harvesting. `FarmPlantPot` remains available for compatibility but is not required.

Separate growth-stage artwork is not included.
