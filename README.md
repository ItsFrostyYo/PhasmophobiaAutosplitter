# LiveSplit Phasmophobia Autosplitter

Automatic start, split, reset, and load removal for Phasmophobia.
All Features are based on and built from Phasmophobia's Speedrun.com Rules - https://www.speedrun.com/phasmophobia
Github for the Phasmophobia Autosplitter - https://github.com/ItsFrostyYo/PhasmophobiaAutosplitter

## Features
- Start when the contract is initialized and the player can move.
- Split on contract finish when leaving from truck context.
- Split on leaving the contract after dying to the ghost when `Split on Death Leave` is enabled.
- Reset on non-finish leave, game close, or new-run start (configurable).
- Multi-Contract support to chain contracts in one attempt.
- Load Time Removal for Game Time between contract transitions.

## Supported game
- Supported Phasmophobia versions: `0.16.0.0`, `0.16.1.1`, and `0.16.1.2`.
- The autosplitter detects the running game build and automatically uses the correct pointer profile for supported versions.
- A dynamic IL2CPP lookup fallback is also included for minor updates where the controller classes move but their names stay the same.
- Larger updates can still require a manual refresh if the game changes structure or class names.

## Update `1.0.10.0`
- Hardened dynamic IL2CPP controller lookup fallback for unknown or shifted builds.
- Kept explicit support for `0.16.0.0`, `0.16.1.1`, and `0.16.1.2` with version-aware profile selection.

## How to use
1. Open LiveSplit.
2. Right-click -> Edit Splits.
3. Set Game Name to `Phasmophobia`.
4. Enable the Auto Splitter.
5. Open component settings and configure options.

## Settings


### Start
- `Start on Contract Initialization`
Starts when contract initialization is complete. (Game no longer Frozen) If that edge is missed, first movement is used as backup.

### Split
- `Split on Contract Finish`
Splits on contract-finish leave transition from truck context.
- `Split on Death Leave`
Splits on Leaving the Contract after Dying to the Ghost. (Mainly for Hug%)
### Reset
- `Allow Resetting on Contract Leave, Game Close and New Run Start`
Master toggle for all auto-reset behavior.

### Options
- `Multi-Contract`
Allows chained contracts without resetting after each split.
- `Load Time Removal (Game Time)`
Pauses Game Time during load transitions and resumes at lobby/board readiness or contract start readiness.
- `Warn on Reset if Gold`
Uses LiveSplit reset confirmation when the current run has a gold split.

## Known issues
- Leaving the truck and re-entering WILL be treated as a split.
- Multiplayer memory state can be unreliable and may cause missed or duplicate behavior.
- Load-removal timing can vary slightly on some transitions because game/UI readiness edges are not identical every run.
- Major game updates can still require a manual refresh if class names or structures are renamed.
- Restarting the game can rarely desync detection; reload the component or restart LiveSplit.

## Contributing
Bug reports and improvements are welcome.
