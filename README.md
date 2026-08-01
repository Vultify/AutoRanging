# AutoRanging

Automatically adjusts your scope zeroing to the distance of whatever you're aiming at while ADS.

Aim at something, and the sight snaps to the closest zeroing distance for it. No more clicking through zeroing steps mid-fight.

## Two ways to range

**Auto** (default) — while you're ADS the mod checks the distance ahead every 0.3s and keeps the zero matched to your target.

**Manual** — flip Auto Range off in F12 and range on demand with Alt+R (rebindable) instead.

## Ammo aware zeroing

Tarkov calibrates a scope's zeroing against the weapon's *default* ammo, whatever you actually loaded — an AK-74 is always zeroed for PS gs at 890 m/s. Load 5.45 US gs at 303 m/s and your rounds land well under the reticle.

This recalculates the zero from the cartridge actually chambered, using the game's own trajectory solver, so drop matches the velocity of what you're firing. It re-solves when you switch ammo mid-raid, and does nothing when the loaded round is already the default. Toggleable in F12.

## Setup

Extract the release zip into your SPT root folder — it lands as `BepInEx/plugins/AutoRanging/AutoRanging.dll`. Runs on SPT 4.1.x / BepInEx 5.x.

---

Bugs: open an [issue](../../issues) or comment on the mod page. History in [CHANGELOG.md](CHANGELOG.md).
