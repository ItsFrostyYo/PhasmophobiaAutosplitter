# LiveSplit Phasmophobia Autosplitter

## This may be discontinued in the Future. Supported builds are tested directly, while unsupported updates safely fail instead of freezing or crashing LiveSplit.

## Description

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
- Supported Phasmophobia versions: `0.16.1.2`, `0.19.0.0`, `0.19.0.1`, and `0.19.1.0`.
- The autosplitter verifies the exact `GameAssembly.dll` build before reading memory.
- Unknown future builds show a warning and run one non-blocking compatibility lookup.
- Dynamic results must match all required controllers and pass repeated memory-layout validation before automation is enabled.
- Failed or incompatible lookups leave the autosplitter safely disabled without blocking LiveSplit or reading through unvalidated pointers.

## Update `1.0.19.0`
- Improved post-contract load removal so game time resumes after the loading overlay has actually been rendered away.

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
- Re-Entering and Staying Inside the Truck WILL be Treated as a Split and Not Reset.
- Multiplayer memory state can be unreliable and may cause missed or duplicate behavior.
- Load-removal timing is not perfect, quitting out to lobby wont unpause the timer until Singleplayer/Multiplayer is selected again
- Unsupported builds only try a brief safe lookup; most builds other than the explicitly supported versions will not work.
- Restarting the game can rarely desync detection; reload the component or restart LiveSplit.

## Contributing
Bug reports and improvements are welcome.

Not Affiliated or Associated with Kinetic Games/Phasmophobia and has NO Ill Intent, only for helping the Speedrunning Community.
