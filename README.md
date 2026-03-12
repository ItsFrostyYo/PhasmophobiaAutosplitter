# LiveSplit.PhasmophobiaAutosplitter
This LiveSplit Auto Splitter provides automatic start, end split, and reset for Phasmophobia by tracking in-game memory values.

## Features (Tied to SRC Ruling)
- Automatic run start on truck load
- Automatic end split when leaving at contract end from truck context
- Optional reset on contract leave
- Automatic reset when the game process closes
- Restart loop handling (auto reset + restart when a new run start is detected while timer is running, paused, or ended)

## Supported Game Versions
- Current Steam build (Unity 2022.3.40f1 / IL2CPP)

Other game versions may work as well or partially.

## How to use
1. Open LiveSplit.
2. Right-click -> Edit Splits.
3. Set *Game Name* to Phasmophobia.
4. Activate the Auto Splitter.
5. Open Settings and configure start/end/reset options.

# Settings

### Start
- Start on Contract Initialization - Starts the timer when the player and truck are completely finished initalizing and the player is allowed to move.
(Backup trigger is always on: if contract initialization start is missed, timer starts when the player first moves.)
### End
- End on Contract Finish - Splits when a loading transition is triggered when you are inside the truck.
### Reset
- Allow Resetting on Contract Leave, Game Close and New Run Start - Enables all auto-reset behavior:
resets when a loading transition is triggered when you are not inside the truck,
resets on game close, and auto reset+start when a new run start is detected while the timer is running, paused, or ended.
(If you never leave the truck it will also count as a Reset)

## Options
- More Options might be added here in future updates. (Like Full Game Load Removal)

# Known Issues
- Leaving truck then re-entering can treat a normal leave loading transition as a split/end.
- Multiplayer may be unreliable.
- Game updates can break memory signatures until the autosplitter is updated.
- Restarting the game may rarely break the autosplitter.
If this happens, restart LiveSplit or reload the component.

# Contributing
Bug reports and code improvements are welcome.
