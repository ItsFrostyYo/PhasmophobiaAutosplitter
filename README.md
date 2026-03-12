# LiveSplit.PhasmophobiaAutosplitter
This LiveSplit Auto Splitter provides automatic start, end split, and reset for Phasmophobia by tracking in-game memory values.

## Features
- Automatic run start on truck load
- Optional backup start on first movement
- Automatic end split on truck unload transition
- Optional reset on contract leave
- Automatic reset when the game process closes
- Restart loop handling (auto reset + restart when a new run start is detected)

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

## Start / End / Reset
- Start on Truck Load - Starts the timer when the truck/map load is complete.
- +Start on First Movement - Optional backup start trigger if truck-load start is missed.
- End on Truck Unload - Splits when the truck-close unload transition is detected.
- Reset on Contract Leave - Resets when leaving a contract without a valid end transition.

## Options
- Options will be added here in future updates.

# Known Issues
- Reset and End can be flipped so it resets instead of ends or ends instead of resets
- Game updates may temporarily break memory signatures
- Restarting the game may rarely break the Auto Splitter
If this happens, restart LiveSplit or reload the Auto Splitter

# Contributing
Bug reports and code improvements are welcome.

