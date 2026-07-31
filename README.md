# Auto Bundle Donations

A [Stardew Valley](https://www.stardewvalley.net/) [SMAPI](https://smapi.io/) mod that automatically donates eligible items to unlocked, incomplete Community Center bundles the moment they enter your inventory — no more digging through your bundle list by hand every time you pick up a rare item.

## Features

- Watches your inventory and automatically fills any matching, still-open bundle slot as soon as you receive an item (from foraging, farming, fishing, shipping, gifts, chests — anywhere).
- Covers all six vanilla bundle rooms: Pantry, Crafts Room, Fish Tank, Boiler Room, Bulletin Board, and the Missing Bundle.
- Each room can be toggled off individually if you'd rather donate some by hand.
- Optional chat notification whenever an auto-donation happens.
- **Unlockable Bundles support**: if the [Unlockable Bundles](https://www.nexusmods.com/stardewvalley/mods/21598) framework is installed, this mod also auto-donates to custom bundles from content packs built on it — like *Visit Mount Vapius* or *Joja Civic Center*. Donations only start once you've physically discovered a bundle (or its parent book) in the world, matching vanilla's own "you have to see it first" rule, and it never touches bundles with a cutscene-driven completion (those still need to be finished in person).
- Configurable via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) if installed — toggle the mod, individual rooms, notifications, and the Unlockable Bundles integration without editing files.

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

## License

[MIT](LICENSE)
