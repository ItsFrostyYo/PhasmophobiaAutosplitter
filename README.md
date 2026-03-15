# LiveSplit Phasmophobia Autosplitter

Automatic start, split, reset, and optional load removal for Phasmophobia. Tracks the game via memory for start/split/reset; load removal uses pixel detection.

## Features
- **Start** when the contract is ready and you can move (truck loaded).
- **Split** when you finish a contract and leave from the truck.
- **Reset** when you leave a contract without finishing, when the game closes, or when a new run starts (configurable).
- **Multi-Contract** – chain multiple contracts in one run without resetting.
- **Load removal** – with Game Time, loading screens after a split are removed from the timer. Uses **pixel detection** (not memory), so it can be less consistent than start/split/reset; resolution and UI changes may affect it.

## Supported game
- Current Steam build (Unity 2022.3.40f1 / IL2CPP). Other versions may work partially.

## How to use
1. Open LiveSplit.
2. Right-click → Edit Splits.
3. Set **Game Name** to **Phasmophobia**.
4. Turn on the Auto Splitter.
5. Open the component settings and set start/split/reset and options as you like.

## Settings

**Start**
- Start on Contract Initialization – starts when the truck is loaded and you can move. A first-movement backup is used if that is missed.

**Split**
- Split on Contract Finish – splits when you trigger the contract-finish loading from inside the truck.

**Reset**
- Allow Resetting on Contract Leave, Game Close and New Run Start – when on, the timer resets when you leave without finishing, when the game closes, or when a new run is detected. When off, all of that is disabled.

**Options**
- **Multi-Contract** – lets you do several maps in one run without resetting after every finished contract. After your first split, auto-resets at lobby are blocked so you can chain contracts; splitting still happens every time you leave from the truck.
- **Load Time Removal (Game Time)** – pauses Game Time during loading after a split, then unpauses when you reach lobby or the next contract. Works on normal leaves as well as truck leaves. **Note:** this uses pixel detection, not memory, so it may not be as consistent as start/split/reset and can depend on resolution and possibly UI settings, attempts to scale with monitor.
- **Warn on Reset if Gold** – uses LiveSplit’s reset confirmation when the current attempt has at least one gold split. Applies to every reset.

## Known issues
- Load Time Removal uses pixel detection, not memory, so it may not be as consistent as start/split/reset and can depend on resolution and possibly UI settings, attempts to scale with monitor.
- Leaving the truck and re-entering WILL be treated as a split.
- Multiplayer can break memory, be unreliable, resulting in Double Splits, never unpause timer and possible never split.
- Game updates can break memory or pixel detection until the autosplitter is updated.
- Restarting the game can rarely break detection; restart LiveSplit or reload the component if that happens.

## Contributing
Bug reports and improvements are welcome.
