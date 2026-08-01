# Changelog

All notable changes to AutoRanging are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/); versions follow [SemVer](https://semver.org/).

## [Unreleased]

## [2.0.0] - 2026-08-01
### Added
- Ammo Aware Zeroing — zeroing is now calibrated from the round actually loaded instead of the weapon's default ammo, so drop matches the velocity of what you are firing. The gap is widest with subsonic loads: a 5.45x39 US gs travels at a third the speed of the PS gs the game assumes, and used to shoot well under the reticle. Toggleable in F12, on by default.
### Changed
- Now targets SPT 4.1.0 — requires SPT 4.1.x and will not load on 4.0.x
- Rebuilt against the deobfuscated 4.1 client assemblies

## [1.0.0] - 2026-06-28
### Added
- Initial release — automatically adjusts scope zeroing to the distance of whatever you're aiming at while ADS
- Auto Range mode (continuous while ADS, toggleable)
- Manual Range mode with keybind (Alt+R) for on-demand ranging
- F12 config: enable/disable, auto vs manual, custom keybind
