using System;
using Voxif.AutoSplitter;
using System.Linq;
using Voxif.IO;
using Voxif.Memory;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    public class PhasmophobiaMemory : Memory
    {
        protected override string[] ProcessNames => new[] { "Phasmophobia" };

        private static readonly int[] PlayerBoolOffsets = { 0x28, 0x29, 0x2A, 0xE8, 0x114 };
        private static readonly int[] FirstPersonBoolOffsets = { 0x20, 0x21, 0x22, 0x23, 0x24, 0x40, 0x50, 0x51, 0x90, 0x91 };
        private const int Il2CppClassStaticFieldsOffset = 0xB8;
        private const int SingletonStaticFieldOffset = 0x0;
        private const int LobbyStableFramesRequired = 20;
        private const int ListItemsOffset = 0x10;
        private const int ListSizeOffset = 0x18;
        private const int ArrayDataOffset = 0x20;
        private const int ArrayLengthOffset = 0x18;
        private const int LevelAreasArrayOffset = 0xA8;
        private const int LevelAreaExitLevelOffset = 0x38;
        private const int ExitLevelTruckUnloadFlagOffset = 0x28;
        private const int ExitLevelTriggerOffset = 0x30;
        private const int ExitLevelTriggerPlayersListOffset = 0x20;
        private static readonly TimeSpan EndFallbackMinRunTime = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan NonEndResetArmMinRunTime = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MenuLeaveWindow = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan PointerInitRetryDelay = TimeSpan.FromSeconds(1);

        // RVAs from tools/phasmophobia_dump_current (Unity 2022.3.40f1).
        private const int LevelControllerTypeInfoRva = 0x05D586A0;
        private const int MapControllerTypeInfoRva = 0x05D5FE60;
        private const int CCTVControllerTypeInfoRva = 0x05D701F0;
        private const int LoadingControllerTypeInfoRva = 0x05D5C1B0;

        private readonly PhasmophobiaSettings settings;
        private DateTime nextPointerInitAttempt = DateTime.MinValue;

        private IntPtr levelControllerStaticAddress;
        private IntPtr mapControllerStaticAddress;
        private IntPtr cctvControllerStaticAddress;
        private IntPtr loadingControllerStaticAddress;

        private bool keySpawnedOld;
        private bool keySpawnedNew;
        private Vector2f moveInputOld;
        private Vector2f moveInputNew;
        private bool menuFlagOldA;
        private bool menuFlagNewA;
        private bool menuFlagOldB;
        private bool menuFlagNewB;
        private readonly bool[] playerBoolOld = new bool[PlayerBoolOffsets.Length];
        private readonly bool[] playerBoolNew = new bool[PlayerBoolOffsets.Length];
        private readonly bool[] firstPersonBoolOld = new bool[FirstPersonBoolOffsets.Length];
        private readonly bool[] firstPersonBoolNew = new bool[FirstPersonBoolOffsets.Length];
        private bool playerSignalPrimed;
        private bool loadingFlagOld;
        private bool loadingFlagNew;
        private bool loadingSignalAvailable;
        private bool startArmedForMapLoad;
        private bool sawTruckKeySinceStart;
        private bool truckLoadedStartEdge;
        private bool exitLevelFlagOld;
        private bool exitLevelFlagNew;
        private bool exitSignalAvailable;
        private bool sawExitSignalSinceStart;
        private bool exitTriggerHasPlayersOld;
        private bool exitTriggerHasPlayersNew;
        private bool exitTriggerSignalAvailable;
        private bool sawExitTriggerSinceStart;
        private bool sawIgnoredLoadingEdgeSinceStart;
        private bool pendingResetAfterNonEndLeave;
        private DateTime lastMenuOpenUtc = DateTime.MinValue;
        private int lobbyStableFrameCount;
        private bool shouldReset;

        private IntPtr lastLevelController;
        private bool sawLoadingIntoMap;
        private bool shouldSplit;
        private DateTime runStartTimeUtc = DateTime.MinValue;

        public bool pointersInitialized;
        public bool startedTimerBefore;

        public PhasmophobiaMemory(Logger logger, PhasmophobiaSettings settings) : base(logger)
        {
            this.settings = settings;

            OnHook += delegate
            {
                pointersInitialized = false;
                nextPointerInitAttempt = DateTime.MinValue;
                TryInitPointers(force: true);
            };

            OnExit += delegate
            {
                pointersInitialized = false;
                levelControllerStaticAddress = IntPtr.Zero;
                mapControllerStaticAddress = IntPtr.Zero;
                cctvControllerStaticAddress = IntPtr.Zero;
                loadingControllerStaticAddress = IntPtr.Zero;
                nextPointerInitAttempt = DateTime.MinValue;

                // Always prefer reset on process close so LiveSplit doesn't get stuck running.
                shouldSplit = false;
                shouldReset = true;
                logger?.Log("Reset candidate: process exit");

                // Clear per-process transient signals without wiping run state flags that must be consumed.
                keySpawnedOld = false;
                keySpawnedNew = false;
                moveInputOld = default(Vector2f);
                moveInputNew = default(Vector2f);
                menuFlagOldA = false;
                menuFlagNewA = false;
                menuFlagOldB = false;
                menuFlagNewB = false;
                playerSignalPrimed = false;
                loadingFlagOld = false;
                loadingFlagNew = false;
                loadingSignalAvailable = false;
                startArmedForMapLoad = false;
                truckLoadedStartEdge = false;
                exitLevelFlagOld = false;
                exitLevelFlagNew = false;
                exitSignalAvailable = false;
                sawExitSignalSinceStart = false;
                exitTriggerHasPlayersOld = false;
                exitTriggerHasPlayersNew = false;
                exitTriggerSignalAvailable = false;
                sawExitTriggerSinceStart = false;
                sawIgnoredLoadingEdgeSinceStart = false;
                pendingResetAfterNonEndLeave = false;
                runStartTimeUtc = DateTime.MinValue;
                lastMenuOpenUtc = DateTime.MinValue;
                lobbyStableFrameCount = 0;
                lastLevelController = IntPtr.Zero;
            };
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (!pointersInitialized)
                TryInitPointers();

            if (!pointersInitialized || game == null)
                return true;

            try
            {
                UpdateSignals();
            }
            catch (Exception ex)
            {
                logger?.Log("Memory update error: " + ex.Message);
            }

            return true;
        }

        public bool ShouldStart()
        {
            if (!pointersInitialized || startedTimerBefore || !sawLoadingIntoMap || !startArmedForMapLoad)
                return false;

            bool startWhenTruckLoaded = settings == null || settings.StartWhenTruckLoaded;
            bool startOnFirstMovement = settings == null || settings.StartOnFirstMovement;

            if (!startWhenTruckLoaded && !startOnFirstMovement)
                return false;

            if (startWhenTruckLoaded && truckLoadedStartEdge)
                return MarkStart("Start: truck loaded");

            // Unfreeze edge remains a fallback for the truck-loaded start mode.
            if (startWhenTruckLoaded && (!loadingSignalAvailable || !loadingFlagNew) && HasUnfreezeEdge())
                return MarkStart("Start: player unfreeze candidate");

            if (startOnFirstMovement && HasMovementEdge())
                return MarkStart("Start fallback: first movement input");

            return false;
        }

        public bool ShouldSplitEnd()
        {
            if (!startedTimerBefore || !shouldSplit)
                return false;

            shouldSplit = false;
            sawExitSignalSinceStart = false;
            sawExitTriggerSinceStart = false;
            sawIgnoredLoadingEdgeSinceStart = false;
            sawTruckKeySinceStart = false;
            logger?.Log("End split fired");
            return true;
        }

        public bool ShouldResetOnLeave()
        {
            if (!shouldReset)
                return false;

            shouldReset = false;
            logger?.Log("Reset candidate: at lobby / out of contract");
            return true;
        }

        public void ResetRunState()
        {
            startedTimerBefore = false;
            shouldSplit = false;
            sawLoadingIntoMap = false;
            runStartTimeUtc = DateTime.MinValue;
            lastMenuOpenUtc = DateTime.MinValue;

            keySpawnedOld = false;
            keySpawnedNew = false;
            moveInputOld = default(Vector2f);
            moveInputNew = default(Vector2f);
            menuFlagOldA = false;
            menuFlagNewA = false;
            menuFlagOldB = false;
            menuFlagNewB = false;
            playerSignalPrimed = false;
            loadingFlagOld = false;
            loadingFlagNew = false;
            loadingSignalAvailable = false;
            startArmedForMapLoad = false;
            sawTruckKeySinceStart = false;
            truckLoadedStartEdge = false;
            exitLevelFlagOld = false;
            exitLevelFlagNew = false;
            exitSignalAvailable = false;
            sawExitSignalSinceStart = false;
            exitTriggerHasPlayersOld = false;
            exitTriggerHasPlayersNew = false;
            exitTriggerSignalAvailable = false;
            sawExitTriggerSinceStart = false;
            sawIgnoredLoadingEdgeSinceStart = false;
            pendingResetAfterNonEndLeave = false;
            lobbyStableFrameCount = 0;
            shouldReset = false;
            lastLevelController = IntPtr.Zero;

            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
            {
                playerBoolOld[i] = false;
                playerBoolNew[i] = false;
            }

            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
            {
                firstPersonBoolOld[i] = false;
                firstPersonBoolNew[i] = false;
            }
        }

        private bool MarkStart(string reason)
        {
            startedTimerBefore = true;
            startArmedForMapLoad = false;
            sawTruckKeySinceStart = false;
            truckLoadedStartEdge = false;
            sawExitSignalSinceStart = false;
            sawExitTriggerSinceStart = false;
            sawIgnoredLoadingEdgeSinceStart = false;
            pendingResetAfterNonEndLeave = false;
            runStartTimeUtc = DateTime.UtcNow;
            lastMenuOpenUtc = DateTime.MinValue;
            lobbyStableFrameCount = 0;
            shouldSplit = false;
            shouldReset = false;
            logger?.Log(reason);
            return true;
        }

        private bool HasUnfreezeEdge()
        {
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
            {
                if (playerBoolOld[i] != playerBoolNew[i])
                {
                    logger?.Log("Unfreeze candidate edge on player bool offset 0x" + PlayerBoolOffsets[i].ToString("X")
                             + " (" + playerBoolOld[i] + " -> " + playerBoolNew[i] + ")");
                    return true;
                }
            }

            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
            {
                if (firstPersonBoolOld[i] != firstPersonBoolNew[i])
                {
                    logger?.Log("Unfreeze candidate edge on first-person bool offset 0x" + FirstPersonBoolOffsets[i].ToString("X")
                             + " (" + firstPersonBoolOld[i] + " -> " + firstPersonBoolNew[i] + ")");
                    return true;
                }
            }
            return false;
        }

        private bool HasMovementEdge()
        {
            const float threshold = 0.08f;
            bool oldActive = moveInputOld.HasMagnitude(threshold);
            bool newActive = moveInputNew.HasMagnitude(threshold);
            return !oldActive && newActive;
        }

        private void TryInitPointers(bool force = false)
        {
            if (game == null)
                return;

            if (!force && DateTime.UtcNow < nextPointerInitAttempt)
                return;

            nextPointerInitAttempt = DateTime.UtcNow + PointerInitRetryDelay;

            try
            {
                IntPtr gameAssemblyBase = GetGameAssemblyBaseAddress();
                if (gameAssemblyBase == IntPtr.Zero)
                    return;

                levelControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, LevelControllerTypeInfoRva, "LevelController");
                mapControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, MapControllerTypeInfoRva, "MapController");
                cctvControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, CCTVControllerTypeInfoRva, "CCTVController");
                loadingControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, LoadingControllerTypeInfoRva, "LoadingController");

                // Level and map pointers are required; others are optional.
                pointersInitialized = levelControllerStaticAddress != IntPtr.Zero
                                   && mapControllerStaticAddress != IntPtr.Zero;

                if (pointersInitialized)
                {
                    logger?.Log("Pointers initialized (TypeInfo RVAs)");
                    logger?.Log("  LevelController singleton ptr addr: 0x" + levelControllerStaticAddress.ToString("X"));
                    logger?.Log("  MapController singleton ptr addr:   0x" + mapControllerStaticAddress.ToString("X"));
                    logger?.Log("  CCTVController singleton ptr addr:  0x" + cctvControllerStaticAddress.ToString("X"));
                    logger?.Log("  LoadingController singleton ptr addr: 0x" + loadingControllerStaticAddress.ToString("X"));
                }
            }
            catch (Exception ex)
            {
                logger?.Log("Pointer initialization failed: " + ex.Message);
            }
        }

        private IntPtr GetGameAssemblyBaseAddress()
        {
            var module = game.Process.Modules().FirstOrDefault(m =>
                m.ModuleName.Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase));

            if (module == null)
            {
                logger?.Log("GameAssembly.dll module not found yet");
                return IntPtr.Zero;
            }

            return module.BaseAddress;
        }

        private IntPtr ResolveSingletonPointerAddress(IntPtr gameAssemblyBase, int typeInfoRva, string label)
        {
            IntPtr typeInfoAddress = gameAssemblyBase + typeInfoRva;
            IntPtr klass = game.Read<IntPtr>(typeInfoAddress);
            if (klass == IntPtr.Zero)
            {
                logger?.Log(label + " class pointer is null at 0x" + typeInfoAddress.ToString("X"));
                return IntPtr.Zero;
            }

            IntPtr staticFields = game.Read<IntPtr>(klass + Il2CppClassStaticFieldsOffset);
            if (staticFields == IntPtr.Zero)
            {
                logger?.Log(label + " static fields pointer is null");
                return IntPtr.Zero;
            }

            return staticFields + SingletonStaticFieldOffset;
        }

        private IntPtr ReadSingletonInstance(IntPtr staticAddress)
        {
            if (staticAddress == IntPtr.Zero || game == null)
                return IntPtr.Zero;
            return game.Read<IntPtr>(staticAddress);
        }

        private IntPtr ReadFirstPlayerFromList(IntPtr listPointer)
        {
            if (listPointer == IntPtr.Zero)
                return IntPtr.Zero;

            int size = game.Read<int>(listPointer + ListSizeOffset);
            if (size <= 0 || size > 16)
                return IntPtr.Zero;

            IntPtr itemsArray = game.Read<IntPtr>(listPointer + ListItemsOffset);
            if (itemsArray == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr firstElement = itemsArray + ArrayDataOffset;
            for (int i = 0; i < size; i++)
            {
                IntPtr player = game.Read<IntPtr>(firstElement + (i * game.PointerSize));
                if (player != IntPtr.Zero)
                    return player;
            }

            return IntPtr.Zero;
        }

        private bool ReadAnyExitLevelTruckUnloadFlag(IntPtr levelController, out bool hasExitLevel)
        {
            hasExitLevel = false;
            if (levelController == IntPtr.Zero)
                return false;

            IntPtr levelAreasArray = game.Read<IntPtr>(levelController + LevelAreasArrayOffset);
            if (levelAreasArray == IntPtr.Zero)
                return false;

            int areaCount = game.Read<int>(levelAreasArray + ArrayLengthOffset);
            if (areaCount <= 0 || areaCount > 64)
                return false;

            IntPtr firstElement = levelAreasArray + ArrayDataOffset;
            bool anyExitFlagTrue = false;
            for (int i = 0; i < areaCount; i++)
            {
                IntPtr levelArea = game.Read<IntPtr>(firstElement + (i * game.PointerSize));
                if (levelArea == IntPtr.Zero)
                    continue;

                IntPtr exitLevel = game.Read<IntPtr>(levelArea + LevelAreaExitLevelOffset);
                if (exitLevel != IntPtr.Zero)
                {
                    hasExitLevel = true;
                    if (game.Read<bool>(exitLevel + ExitLevelTruckUnloadFlagOffset))
                        anyExitFlagTrue = true;
                }
            }

            return anyExitFlagTrue;
        }

        private bool ReadAnyExitTriggerHasPlayers(IntPtr levelController, out bool hasExitTrigger)
        {
            hasExitTrigger = false;
            if (levelController == IntPtr.Zero)
                return false;

            IntPtr levelAreasArray = game.Read<IntPtr>(levelController + LevelAreasArrayOffset);
            if (levelAreasArray == IntPtr.Zero)
                return false;

            int areaCount = game.Read<int>(levelAreasArray + ArrayLengthOffset);
            if (areaCount <= 0 || areaCount > 64)
                return false;

            IntPtr firstElement = levelAreasArray + ArrayDataOffset;
            for (int i = 0; i < areaCount; i++)
            {
                IntPtr levelArea = game.Read<IntPtr>(firstElement + (i * game.PointerSize));
                if (levelArea == IntPtr.Zero)
                    continue;

                IntPtr exitLevel = game.Read<IntPtr>(levelArea + LevelAreaExitLevelOffset);
                if (exitLevel == IntPtr.Zero)
                    continue;

                IntPtr trigger = game.Read<IntPtr>(exitLevel + ExitLevelTriggerOffset);
                if (trigger == IntPtr.Zero)
                    continue;

                hasExitTrigger = true;
                IntPtr playersList = game.Read<IntPtr>(trigger + ExitLevelTriggerPlayersListOffset);
                if (playersList == IntPtr.Zero)
                    continue;

                int size = game.Read<int>(playersList + ListSizeOffset);
                if (size > 0 && size <= 16)
                    return true;
            }

            return false;
        }

        public bool ShouldStartForRestartLoop()
        {
            if (!pointersInitialized || !sawLoadingIntoMap || !startArmedForMapLoad)
                return false;

            bool startWhenTruckLoaded = settings == null || settings.StartWhenTruckLoaded;
            bool startOnFirstMovement = settings == null || settings.StartOnFirstMovement;

            if (!startWhenTruckLoaded && !startOnFirstMovement)
                return false;

            if (startWhenTruckLoaded && truckLoadedStartEdge)
                return MarkStart("Start: truck loaded");

            if (startWhenTruckLoaded && (!loadingSignalAvailable || !loadingFlagNew) && HasUnfreezeEdge())
                return MarkStart("Start: player unfreeze candidate");

            if (startOnFirstMovement && HasMovementEdge())
                return MarkStart("Start fallback: first movement input");

            return false;
        }

        private void UpdateSignals()
        {
            IntPtr levelController = ReadSingletonInstance(levelControllerStaticAddress);
            IntPtr mapController = ReadSingletonInstance(mapControllerStaticAddress);
            IntPtr cctvController = ReadSingletonInstance(cctvControllerStaticAddress);
            IntPtr loadingController = ReadSingletonInstance(loadingControllerStaticAddress);
            bool levelLoadedNow = lastLevelController == IntPtr.Zero && levelController != IntPtr.Zero;

            IntPtr keyPointer = IntPtr.Zero;
            if (levelController != IntPtr.Zero)
            {
                keyPointer = game.Read<IntPtr>(levelController + 0xE0); // LevelController.key
            }

            if (levelLoadedNow)
            {
                sawLoadingIntoMap = true;
                startArmedForMapLoad = true;
                playerSignalPrimed = false;
                truckLoadedStartEdge = false;
                sawExitSignalSinceStart = false;
            }

            keySpawnedOld = keySpawnedNew;
            keySpawnedNew = keyPointer != IntPtr.Zero;

            if (levelController != IntPtr.Zero)
                sawLoadingIntoMap = true;

            IntPtr localPlayer = IntPtr.Zero;
            if (mapController != IntPtr.Zero)
            {
                IntPtr mapPlayersList = game.Read<IntPtr>(mapController + 0x28); // MapController.players
                localPlayer = ReadFirstPlayerFromList(mapPlayersList);
            }

            if (localPlayer == IntPtr.Zero && cctvController != IntPtr.Zero)
            {
                IntPtr cctvPlayersList = game.Read<IntPtr>(cctvController + 0x100); // CCTVController.playerList
                localPlayer = ReadFirstPlayerFromList(cctvPlayersList);
            }

            if (localPlayer == IntPtr.Zero)
            {
                playerSignalPrimed = false;
                AdvancePlayerSignalsWithoutChanges();
            }
            else if (!playerSignalPrimed)
            {
                PrimePlayerSignals(localPlayer);
                playerSignalPrimed = true;
            }
            else
            {
                UpdatePlayerSignals(localPlayer);
            }

            loadingFlagOld = loadingFlagNew;
            loadingFlagNew = false;
            if (loadingController != IntPtr.Zero)
            {
                loadingSignalAvailable = true;
                loadingFlagNew = game.Read<bool>(loadingController + 0x30); // LoadingController loading flag candidate
            }

            exitLevelFlagOld = exitLevelFlagNew;
            exitLevelFlagNew = ReadAnyExitLevelTruckUnloadFlag(levelController, out bool hasExitLevel);
            exitSignalAvailable = hasExitLevel;

            exitTriggerHasPlayersOld = exitTriggerHasPlayersNew;
            exitTriggerHasPlayersNew = ReadAnyExitTriggerHasPlayers(levelController, out bool hasExitTrigger);
            exitTriggerSignalAvailable = hasExitTrigger;

            if (startArmedForMapLoad
                && !truckLoadedStartEdge
                && localPlayer != IntPtr.Zero
                && (!loadingSignalAvailable || !loadingFlagNew))
            {
                truckLoadedStartEdge = true;
            }

            if (startedTimerBefore && keySpawnedNew)
            {
                sawTruckKeySinceStart = true;
            }

            if (startedTimerBefore && exitSignalAvailable && exitLevelFlagNew)
                sawExitSignalSinceStart = true;
            if (startedTimerBefore && exitTriggerSignalAvailable && exitTriggerHasPlayersNew)
                sawExitTriggerSinceStart = true;
            if (startedTimerBefore && (menuFlagNewA || menuFlagNewB))
                lastMenuOpenUtc = DateTime.UtcNow;

            if (startedTimerBefore
                && !shouldSplit
                && sawTruckKeySinceStart
                && levelController != IntPtr.Zero
                && loadingFlagNew && !loadingFlagOld)
            {
                bool hasStrictTruckUnloadSignal =
                    sawExitSignalSinceStart
                    || (exitSignalAvailable && exitLevelFlagNew)
                    || (exitSignalAvailable && (exitLevelFlagOld != exitLevelFlagNew))
                    || sawExitTriggerSinceStart
                    || exitTriggerHasPlayersOld
                    || (exitTriggerSignalAvailable && exitTriggerHasPlayersNew);
                bool hasExitTriggerEdge = exitTriggerSignalAvailable && (exitTriggerHasPlayersOld != exitTriggerHasPlayersNew);
                if (!hasStrictTruckUnloadSignal && hasExitTriggerEdge)
                    hasStrictTruckUnloadSignal = true;
                bool resetWhenAtLobby = settings != null && settings.ResetWhenAtLobby;
                bool runAgeReady = runStartTimeUtc != DateTime.MinValue
                                && (DateTime.UtcNow - runStartTimeUtc) >= NonEndResetArmMinRunTime;
                bool canUseFallback = runStartTimeUtc != DateTime.MinValue
                                   && (DateTime.UtcNow - runStartTimeUtc) >= EndFallbackMinRunTime;

                if (hasStrictTruckUnloadSignal)
                {
                    shouldSplit = true;
                    logger?.Log("End candidate: truck unload loading fade started");

                    if (resetWhenAtLobby && runAgeReady)
                    {
                        pendingResetAfterNonEndLeave = true;
                        logger?.Log("Reset armed: post-end return to lobby");
                    }
                }
                else if (canUseFallback)
                {
                    bool recentMenuLeave =
                        lastMenuOpenUtc != DateTime.MinValue
                        && (DateTime.UtcNow - lastMenuOpenUtc) <= MenuLeaveWindow;

                    if (resetWhenAtLobby
                     && !hasStrictTruckUnloadSignal
                     && (recentMenuLeave || sawIgnoredLoadingEdgeSinceStart || pendingResetAfterNonEndLeave))
                    {
                        shouldReset = true;
                        logger?.Log("Reset candidate: non-end leave fallback conversion"
                                 + (recentMenuLeave ? " (recent menu leave)" : ""));
                    }
                    else
                    {
                        shouldSplit = true;
                        logger?.Log("End candidate fallback: loading fade after active contract");
                    }
                }
                else
                {
                    logger?.Log("Loading edge ignored for end (not truck unload)");
                    sawIgnoredLoadingEdgeSinceStart = true;
                    if (resetWhenAtLobby && runAgeReady)
                    {
                        pendingResetAfterNonEndLeave = true;
                        logger?.Log("Reset armed: non-end leave transition");
                    }
                }
            }

            if (startedTimerBefore
             && !shouldSplit
             && settings != null
             && settings.ResetWhenAtLobby
             && pendingResetAfterNonEndLeave
             && !loadingFlagNew)
            {
                shouldReset = true;
                pendingResetAfterNonEndLeave = false;
                logger?.Log("Reset candidate: non-end leave settled");
            }

            bool atLobbyNow = !loadingFlagNew
                           && levelController == IntPtr.Zero;

            if (startedTimerBefore && !shouldSplit && atLobbyNow && settings != null && settings.ResetWhenAtLobby)
                lobbyStableFrameCount++;
            else
                lobbyStableFrameCount = 0;

            if (startedTimerBefore
             && !shouldSplit
             && settings != null
             && settings.ResetWhenAtLobby
             && lobbyStableFrameCount >= LobbyStableFramesRequired)
            {
                shouldReset = true;
                pendingResetAfterNonEndLeave = false;
            }

            // Always reset on a new contract load if a previous run is still active.
            if (startedTimerBefore && !shouldSplit && levelLoadedNow)
            {
                shouldReset = true;
                logger?.Log("Reset candidate: new contract loaded while previous run active");
            }

            lastLevelController = levelController;
        }

        private void PrimePlayerSignals(IntPtr player)
        {
            moveInputNew = default(Vector2f);
            menuFlagNewA = false;
            menuFlagNewB = false;

            IntPtr firstPersonController = game.Read<IntPtr>(player + 0x128); // Player.firstPersonController
            if (firstPersonController != IntPtr.Zero)
            {
                moveInputNew = game.Read<Vector2f>(firstPersonController + 0x70); // FirstPersonController movement input
                for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                    firstPersonBoolNew[i] = game.Read<bool>(firstPersonController + FirstPersonBoolOffsets[i]);
            }

            IntPtr pcMenu = game.Read<IntPtr>(player + 0x150); // Player.pcMenu
            if (pcMenu != IntPtr.Zero)
            {
                menuFlagNewA = game.Read<bool>(pcMenu + 0x20); // PCMenu candidate open flag
                menuFlagNewB = game.Read<bool>(pcMenu + 0x21); // PCMenu candidate open flag
            }

            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolNew[i] = game.Read<bool>(player + PlayerBoolOffsets[i]);

            moveInputOld = moveInputNew;
            menuFlagOldA = menuFlagNewA;
            menuFlagOldB = menuFlagNewB;
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolOld[i] = playerBoolNew[i];
            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                firstPersonBoolOld[i] = firstPersonBoolNew[i];
        }

        private void UpdatePlayerSignals(IntPtr player)
        {
            moveInputOld = moveInputNew;
            menuFlagOldA = menuFlagNewA;
            menuFlagOldB = menuFlagNewB;
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolOld[i] = playerBoolNew[i];
            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                firstPersonBoolOld[i] = firstPersonBoolNew[i];

            IntPtr firstPersonController = game.Read<IntPtr>(player + 0x128); // Player.firstPersonController
            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                firstPersonBoolNew[i] = false;
            if (firstPersonController != IntPtr.Zero)
            {
                moveInputNew = game.Read<Vector2f>(firstPersonController + 0x70); // FirstPersonController movement input
                for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                    firstPersonBoolNew[i] = game.Read<bool>(firstPersonController + FirstPersonBoolOffsets[i]);
            }
            else
            {
                moveInputNew = default(Vector2f);
            }

            IntPtr pcMenu = game.Read<IntPtr>(player + 0x150); // Player.pcMenu
            menuFlagNewA = false;
            menuFlagNewB = false;
            if (pcMenu != IntPtr.Zero)
            {
                menuFlagNewA = game.Read<bool>(pcMenu + 0x20); // PCMenu candidate open flag
                menuFlagNewB = game.Read<bool>(pcMenu + 0x21); // PCMenu candidate open flag
            }

            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolNew[i] = game.Read<bool>(player + PlayerBoolOffsets[i]);
        }

        private void AdvancePlayerSignalsWithoutChanges()
        {
            moveInputOld = moveInputNew;
            menuFlagOldA = menuFlagNewA;
            menuFlagOldB = menuFlagNewB;
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolOld[i] = playerBoolNew[i];
            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                firstPersonBoolOld[i] = firstPersonBoolNew[i];
        }

        private struct Vector2f
        {
#pragma warning disable CS0649
            public float X;
            public float Y;
#pragma warning restore CS0649

            public bool HasMagnitude(float threshold)
            {
                float sqr = (X * X) + (Y * Y);
                return sqr >= (threshold * threshold);
            }
        }
    }
}

