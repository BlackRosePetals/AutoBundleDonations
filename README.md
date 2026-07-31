# Auto Bundle Donations

A [Stardew Valley](https://www.stardewvalley.net/) [SMAPI](https://smapi.io/) mod that automatically donates eligible items to unlocked, incomplete Community Center bundles the moment they enter your inventory — no more digging through your bundle list by hand every time you pick up a rare item.

## Features

- Watches your inventory and automatically fills any matching, still-open bundle slot as soon as you receive an item (from foraging, farming, fishing, shipping, gifts, chests — anywhere).
- Covers all six vanilla bundle rooms: Pantry, Crafts Room, Fish Tank, Boiler Room, Bulletin Board, and the Missing Bundle.
- Each room can be toggled off individually if you'd rather donate some by hand.
- Optional chat notification whenever an auto-donation happens.
- **Unlockable Bundles support**: if the [Unlockable Bundles](https://www.nexusmods.com/stardewvalley/mods/21598) framework is installed, this mod also auto-donates to custom bundles from content packs built on it — like *Visit Mount Vapius* or *Joja Civic Center*. Donations only start once you've physically discovered a bundle (or its parent book) in the world, matching vanilla's own "you have to see it first" rule, and it never touches bundles with a cutscene-driven completion (those still need to be finished in person).
- **Auto Museum Donations compatibility**: if [Auto Museum Donations](https://www.nexusmods.com/stardewvalley/mods/45916) is also installed, an optional "Prioritize Museum donations" setting lets it claim artifacts and minerals first, before this mod donates them to a Community Center bundle instead (with an automatic fallback so nothing gets stuck if the museum mod declines an item).
- **Withhold valuable items**: optionally keep Prismatic Shard, Dinosaur Egg, and the basic gems out of auto-donation entirely (currently relevant to the Dye Bundle's Aquamarine and the Missing Bundle's Prismatic Shard), so a single copy isn't silently spent before you've decided whether you'd rather keep it.
- Configurable via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) if installed — toggle the mod, individual rooms, notifications, and the mod-compatibility options without editing files.

## Install

1. Install [SMAPI](https://smapi.io/).
2. Extract this mod's folder into `Stardew Valley/Mods`.
3. Run the game via SMAPI.

## Requirements

| Mod | Required? | Why |
|---|---|---|
| [SMAPI](https://smapi.io/) | Required | It's a SMAPI mod — won't run without it. |
| [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) | Optional | Gives you an in-game settings menu. Without it, you can still edit `config.json` by hand. |
| [Unlockable Bundles](https://www.nexusmods.com/stardewvalley/mods/21598) (`DLX.Bundles`) | Optional | Only needed if you want auto-donation extended to non-vanilla bundles from packs like Visit Mount Vapius or Joja Civic Center. Vanilla Community Center donation works with or without it. |
| [Auto Museum Donations](https://www.nexusmods.com/stardewvalley/mods/45916) (`geisiel.AutoMuseumDonations`) | Optional | Only needed if you want to use the "Prioritize Museum donations" setting. Works fine with or without it otherwise. |

Compatible with Stardew Valley 1.6+ and SMAPI 4.0.0+.

## Configuration

`config.json` (created after first run), or via Generic Mod Config Menu in-game:

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master on/off switch for the mod. |
| `ShowNotifications` | `true` | Show a chat message whenever an item is auto-donated. |
| `DonatePantry` | `true` | Auto-donate to the Pantry bundles. |
| `DonateCraftsRoom` | `true` | Auto-donate to the Crafts Room bundles. |
| `DonateFishTank` | `true` | Auto-donate to the Fish Tank bundles. |
| `DonateBoilerRoom` | `true` | Auto-donate to the Boiler Room bundles. |
| `DonateBulletinBoard` | `true` | Auto-donate to the Bulletin Board bundles. |
| `DonateMissingBundle` | `true` | Auto-donate to the Missing Bundle. |
| `EnableUnlockableBundlesIntegration` | `true` | Also auto-donate to bundles from the Unlockable Bundles framework, if installed. |
| `PrioritizeMuseum` | `false` | If Auto Museum Donations is also installed, let it claim artifacts and minerals for the Museum first, before this mod donates them to a Community Center bundle instead. No effect unless Auto Museum Donations is installed. |
| `WithholdValuableItems` | `false` | Keep Prismatic Shard, Dinosaur Egg, and the basic gems (Diamond, Ruby, Emerald, Jade, Aquamarine, Topaz, Amethyst) in your inventory instead of auto-donating them to a bundle. |

## License

[MIT](LICENSE)
