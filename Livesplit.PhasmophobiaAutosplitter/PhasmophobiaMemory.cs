using System;
using Voxif.AutoSplitter;
using System.Linq;
using Voxif.IO;
using Voxif.Memory;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    internal enum LoadRemovalState
    {
        None,
        FirstLoading,
        InLobby,
        SecondLoading
    }

    public class PhasmophobiaMemory : Memory
    {
        protected override string[] ProcessNames => new[] { "Phasmophobia" };

        private static readonly int[] PlayerBoolOffsets = { 0x28, 0x29, 0x2A, 0xE8, 0x114 };
        private static readonly int[] FirstPersonBoolOffsets = { 0x20, 0x21, 0x22, 0x23, 0x24, 0x40, 0x50, 0x51, 0x90, 0x91 };
        private static readonly int[] PcMenuBoolOffsets = { 0x20, 0x21 };
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
        private const int CctvTruckPlayersListOffset = 0x100;
        private static readonly int[] CctvTruckBoolOffsets = { 0x90, 0xA0, 0xA1 };
        private const int CctvTruckPresenceOffsetIndex = 1; // 0xA0
        private const int PlayerVrLoadingOffset = 0x1D0;
        private const int VrLoadingIsLoadScreenInstanceOffset = 0x48;
        private const int VrLoadingProgressOffset = 0x4C;
        private const int VrLoadingProgressAuxOffset = 0x50;
        private const int PlayerPhotonViewOffset = 0x20;
        private const int PhotonViewIsMineOffset = 0x68;
        private const int MainManagerServerManagerOffset = 0x50;
        private const int MainManagerLevelSelectionManagerOffset = 0x58;
        private const int ServerManagerLeaveButtonOffset = 0xD0;
        private const int ServerManagerStartGameButtonOffset = 0xE8;
        private const int SelectableInteractableOffset = 0xD8;
        private const int SelectableEnableCalledOffset = 0x20;
        private const int SelectableIsPointerDownOffset = 0xF1;
        private const int MaxLevelAreaCount = 512;
        private const int MaxPlayerListCount = 64;
        private const int MaxTriggerPlayersCount = 64;
        private static readonly TimeSpan PointerInitRetryDelay = TimeSpan.FromSeconds(1);
        private const int PauseMenuRecentFrames = 120;
        private static readonly TimeSpan LoadRemovalSecondPauseTimeout = TimeSpan.FromSeconds(45);

        // RVAs from tools/phasmophobia_dump_current (Unity 2022.3.40f1).
        private const int LevelControllerTypeInfoRva = 0x05D586A0;
        private const int MapControllerTypeInfoRva = 0x05D5FE60;
        private const int CCTVControllerTypeInfoRva = 0x05D701F0;
        private const int LoadingControllerTypeInfoRva = 0x05D5C1B0;
        private const int MainManagerTypeInfoRva = 0x05D5F6C8;
        private const int GameControllerTypeInfoRva = 0x05DB4D20;

        private readonly PhasmophobiaSettings settings;
        private DateTime nextPointerInitAttempt = DateTime.MinValue;

        private IntPtr levelControllerStaticAddress;
        private IntPtr mapControllerStaticAddress;
        private IntPtr cctvControllerStaticAddress;
        private IntPtr loadingControllerStaticAddress;
        private IntPtr mainManagerStaticAddress;
        private IntPtr gameControllerStaticAddress;

        private bool keySpawnedOld;
        private bool keySpawnedNew;
        private Vector2f moveInputOld;
        private Vector2f moveInputNew;
        private readonly bool[] pcMenuBoolOld = new bool[PcMenuBoolOffsets.Length];
        private readonly bool[] pcMenuBoolNew = new bool[PcMenuBoolOffsets.Length];
        private bool menuOpenOld;
        private bool menuOpenNew;
        private readonly bool[] playerBoolOld = new bool[PlayerBoolOffsets.Length];
        private readonly bool[] playerBoolNew = new bool[PlayerBoolOffsets.Length];
        private readonly bool[] firstPersonBoolOld = new bool[FirstPersonBoolOffsets.Length];
        private readonly bool[] firstPersonBoolNew = new bool[FirstPersonBoolOffsets.Length];
        private bool playerSignalPrimed;
        private bool loadingFlagOld;
        private bool loadingFlagNew;
        private bool loadingSignalAvailable;
        private bool vrLoadingSignalAvailable;
        private bool vrLoadScreenInstanceOld;
        private bool vrLoadScreenInstanceNew;
        private float vrLoadingProgressOld;
        private float vrLoadingProgressNew;
        private float vrLoadingProgressAuxOld;
        private float vrLoadingProgressAuxNew;
        private bool vrLoadingLikelyOld;
        private bool vrLoadingLikelyNew;
        private bool startArmedForMapLoad;
        private bool sawTruckKeySinceStart;
        private bool truckLoadedStartEdge;
        private bool exitLevelFlagOld;
        private bool exitLevelFlagNew;
        private bool exitSignalAvailable;
        private bool exitTriggerHasPlayersOld;
        private bool exitTriggerHasPlayersNew;
        private bool exitTriggerContainsLocalPlayerOld;
        private bool exitTriggerContainsLocalPlayerNew;
        private bool exitTriggerSignalAvailable;
        private bool localPlayerInTruckListOld;
        private bool localPlayerInTruckListNew;
        private readonly bool[] cctvTruckBoolOld = new bool[CctvTruckBoolOffsets.Length];
        private readonly bool[] cctvTruckBoolNew = new bool[CctvTruckBoolOffsets.Length];
        private bool sawTruckPresenceDropSinceStart;
        private bool sawTruckPresenceReturnSinceStart;
        private bool sawTruckUnloadSignalSinceStart;
        private bool sawTruckExitTriggerSignalSinceStart;
        private bool restartLoopStartSignal;
        private bool pendingResetAfterNonEndLeave;
        private int lobbyStableFrameCount;
        private int pauseMenuLikelyFramesAgo = int.MaxValue;
        private bool shouldReset;
        private bool suppressResetUntilNextStart;
        private bool startGameButtonDownOld;
        private bool startGameButtonDownNew;
        private bool startGameButtonEnabledOld;
        private bool startGameButtonEnabledNew;
        private bool startGameButtonInteractableOld;
        private bool startGameButtonInteractableNew;
        private bool leaveLobbyButtonDownOld;
        private bool leaveLobbyButtonDownNew;
        private bool leaveLobbyButtonEnabledOld;
        private bool leaveLobbyButtonEnabledNew;
        private bool leaveLobbyButtonInteractableOld;
        private bool leaveLobbyButtonInteractableNew;
        private bool contractBoardAvailableOld;
        private bool contractBoardAvailableNew;
        private bool gameControllerFlagF8Old;
        private bool gameControllerFlagF8New;
        private bool gameControllerFlagF9Old;
        private bool gameControllerFlagF9New;
        private IntPtr lastKnownLocalPlayer;
        private int framesSinceLocalPlayerSeen;
        private int localPlayerMissingInContractFrames;
        private bool deathLeaveLikelySinceStart;
        private bool localPlayerFirstPersonMissingNow;

        private IntPtr lastLevelController;
        private bool sawLoadingIntoMap;
        private bool shouldSplit;
        private bool hasSplitThisRun;
        private bool hadLocalPlayerLastFrame;

        private LoadRemovalState loadRemovalState = LoadRemovalState.None;
        private DateTime loadRemovalSecondPauseStartUtc = DateTime.MinValue;
        private DateTime loadRemovalInLobbySinceUtc = DateTime.MinValue;
        private DateTime loadRemovalFirstPauseStartUtc = DateTime.MinValue;
        private int loadRemovalLobbyReadyConsecutiveFrames;
        private bool loadRemovalFirstLoadingSawStartHidden;
        private int loadRemovalSecondLoadingInLevelNotLoadingCount;
        private bool loadRemovalSecondLoadingSawLoading;
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
                mainManagerStaticAddress = IntPtr.Zero;
                gameControllerStaticAddress = IntPtr.Zero;
                nextPointerInitAttempt = DateTime.MinValue;

                // Always prefer reset on process close so LiveSplit doesn't get stuck running.
                shouldSplit = false;
                shouldReset = settings != null && settings.ResetWhenAtLobby;
                suppressResetUntilNextStart = false;
                lastKnownLocalPlayer = IntPtr.Zero;
                framesSinceLocalPlayerSeen = int.MaxValue;
                localPlayerMissingInContractFrames = 0;
                deathLeaveLikelySinceStart = false;
                localPlayerFirstPersonMissingNow = false;
                if (shouldReset)
                    logger?.Log("Reset candidate: process exit");

                // Clear per-process transient signals without wiping run state flags that must be consumed.
                keySpawnedOld = false;
                keySpawnedNew = false;
                moveInputOld = default(Vector2f);
                moveInputNew = default(Vector2f);
                menuOpenOld = false;
                menuOpenNew = false;
                playerSignalPrimed = false;
                loadingFlagOld = false;
                loadingFlagNew = false;
                loadingSignalAvailable = false;
                vrLoadingSignalAvailable = false;
                vrLoadScreenInstanceOld = false;
                vrLoadScreenInstanceNew = false;
                vrLoadingProgressOld = 0f;
                vrLoadingProgressNew = 0f;
                vrLoadingProgressAuxOld = 0f;
                vrLoadingProgressAuxNew = 0f;
                vrLoadingLikelyOld = false;
                vrLoadingLikelyNew = false;
                startArmedForMapLoad = false;
                truckLoadedStartEdge = false;
                exitLevelFlagOld = false;
                exitLevelFlagNew = false;
                exitSignalAvailable = false;
                exitTriggerHasPlayersOld = false;
                exitTriggerHasPlayersNew = false;
                exitTriggerContainsLocalPlayerOld = false;
                exitTriggerContainsLocalPlayerNew = false;
                exitTriggerSignalAvailable = false;
                localPlayerInTruckListOld = false;
                localPlayerInTruckListNew = false;
                Array.Clear(cctvTruckBoolOld, 0, cctvTruckBoolOld.Length);
                Array.Clear(cctvTruckBoolNew, 0, cctvTruckBoolNew.Length);
                sawTruckPresenceDropSinceStart = false;
                sawTruckPresenceReturnSinceStart = false;
                sawTruckUnloadSignalSinceStart = false;
                sawTruckExitTriggerSignalSinceStart = false;
                restartLoopStartSignal = false;
                pendingResetAfterNonEndLeave = false;
                lobbyStableFrameCount = 0;
                pauseMenuLikelyFramesAgo = int.MaxValue;
                startGameButtonDownOld = false;
                startGameButtonDownNew = false;
                startGameButtonEnabledOld = false;
                startGameButtonEnabledNew = false;
                startGameButtonInteractableOld = false;
                startGameButtonInteractableNew = false;
                leaveLobbyButtonDownOld = false;
                leaveLobbyButtonDownNew = false;
                leaveLobbyButtonEnabledOld = false;
                leaveLobbyButtonEnabledNew = false;
                leaveLobbyButtonInteractableOld = false;
                leaveLobbyButtonInteractableNew = false;
                contractBoardAvailableOld = false;
                contractBoardAvailableNew = false;
                gameControllerFlagF8Old = false;
                gameControllerFlagF8New = false;
                gameControllerFlagF9Old = false;
                gameControllerFlagF9New = false;
                lastLevelController = IntPtr.Zero;
                hadLocalPlayerLastFrame = false;
                localPlayerMissingInContractFrames = 0;
                deathLeaveLikelySinceStart = false;
                localPlayerFirstPersonMissingNow = false;
                loadRemovalState = LoadRemovalState.None;
                loadRemovalFirstPauseStartUtc = DateTime.MinValue;
                loadRemovalSecondPauseStartUtc = DateTime.MinValue;
                loadRemovalInLobbySinceUtc = DateTime.MinValue;
                loadRemovalLobbyReadyConsecutiveFrames = 0;
                loadRemovalFirstLoadingSawStartHidden = false;
                loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                loadRemovalSecondLoadingSawLoading = false;

                Array.Clear(pcMenuBoolOld, 0, pcMenuBoolOld.Length);
                Array.Clear(pcMenuBoolNew, 0, pcMenuBoolNew.Length);
            };
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (!pointersInitialized || mainManagerStaticAddress == IntPtr.Zero || gameControllerStaticAddress == IntPtr.Zero)
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

            // Ignore lobby/menu transitions; contract start must come from an in-contract state.
            if (IsLobbyUiActive())
                return false;

            bool startWhenTruckLoaded = settings == null || settings.StartWhenTruckLoaded;
            bool startOnFirstMovement = settings == null || settings.StartOnFirstMovement;

            if (!startWhenTruckLoaded && !startOnFirstMovement)
                return false;

            if (startWhenTruckLoaded && truckLoadedStartEdge)
                return MarkStart("Start: contract initialized");

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
            hasSplitThisRun = true;
            // Keep reset suppression behavior independent of Multi-Contract;
            // multi-contract lobby reset protection is handled separately.
            suppressResetUntilNextStart = settings == null || !settings.EnableMultiContract;
            sawTruckKeySinceStart = false;
            sawTruckUnloadSignalSinceStart = false;
            sawTruckExitTriggerSignalSinceStart = false;
            sawTruckPresenceDropSinceStart = false;
            sawTruckPresenceReturnSinceStart = false;
            exitTriggerContainsLocalPlayerOld = false;
            exitTriggerContainsLocalPlayerNew = false;
            localPlayerInTruckListOld = false;
            localPlayerInTruckListNew = false;
            pendingResetAfterNonEndLeave = false;
            if (settings != null && settings.EnableLoadTimeRemoval)
            {
                loadRemovalState = LoadRemovalState.FirstLoading;
                loadRemovalFirstPauseStartUtc = DateTime.UtcNow;
                loadRemovalLobbyReadyConsecutiveFrames = 0;
                loadRemovalFirstLoadingSawStartHidden = false;
                logger?.Log("Load removal: split -> pausing for load");
            }
            logger?.Log("Split fired");
            return true;
        }

        public bool HasSplitOccurredThisRun() => hasSplitThisRun;

        public bool ShouldResetOnLeave()
        {
            if (suppressResetUntilNextStart)
            {
                shouldReset = false;
                return false;
            }

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
            hasSplitThisRun = false;
            hadLocalPlayerLastFrame = false;
            loadRemovalState = LoadRemovalState.None;
            loadRemovalFirstPauseStartUtc = DateTime.MinValue;
            loadRemovalSecondPauseStartUtc = DateTime.MinValue;
            loadRemovalInLobbySinceUtc = DateTime.MinValue;
            loadRemovalLobbyReadyConsecutiveFrames = 0;
            loadRemovalFirstLoadingSawStartHidden = false;
            loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
            loadRemovalSecondLoadingSawLoading = false;
            vrLoadingSignalAvailable = false;
            vrLoadScreenInstanceOld = false;
            vrLoadScreenInstanceNew = false;
            vrLoadingProgressOld = 0f;
            vrLoadingProgressNew = 0f;
            vrLoadingProgressAuxOld = 0f;
            vrLoadingProgressAuxNew = 0f;
            vrLoadingLikelyOld = false;
            vrLoadingLikelyNew = false;

            keySpawnedOld = false;
            keySpawnedNew = false;
            moveInputOld = default(Vector2f);
            moveInputNew = default(Vector2f);
            menuOpenOld = false;
            menuOpenNew = false;
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
            exitTriggerHasPlayersOld = false;
            exitTriggerHasPlayersNew = false;
            exitTriggerContainsLocalPlayerOld = false;
            exitTriggerContainsLocalPlayerNew = false;
            exitTriggerSignalAvailable = false;
            localPlayerInTruckListOld = false;
            localPlayerInTruckListNew = false;
            Array.Clear(cctvTruckBoolOld, 0, cctvTruckBoolOld.Length);
            Array.Clear(cctvTruckBoolNew, 0, cctvTruckBoolNew.Length);
            sawTruckPresenceDropSinceStart = false;
            sawTruckPresenceReturnSinceStart = false;
            deathLeaveLikelySinceStart = false;
            localPlayerMissingInContractFrames = 0;
            localPlayerFirstPersonMissingNow = false;
            sawTruckUnloadSignalSinceStart = false;
            sawTruckExitTriggerSignalSinceStart = false;
            restartLoopStartSignal = false;
            pendingResetAfterNonEndLeave = false;
            lobbyStableFrameCount = 0;
            pauseMenuLikelyFramesAgo = int.MaxValue;
            startGameButtonDownOld = false;
            startGameButtonDownNew = false;
            startGameButtonEnabledOld = false;
            startGameButtonEnabledNew = false;
            startGameButtonInteractableOld = false;
            startGameButtonInteractableNew = false;
            leaveLobbyButtonDownOld = false;
            leaveLobbyButtonDownNew = false;
            leaveLobbyButtonEnabledOld = false;
            leaveLobbyButtonEnabledNew = false;
            leaveLobbyButtonInteractableOld = false;
            leaveLobbyButtonInteractableNew = false;
            contractBoardAvailableOld = false;
            contractBoardAvailableNew = false;
            gameControllerFlagF8Old = false;
            gameControllerFlagF8New = false;
            gameControllerFlagF9Old = false;
            gameControllerFlagF9New = false;
            shouldReset = false;
            suppressResetUntilNextStart = false;
            lastLevelController = IntPtr.Zero;
            lastKnownLocalPlayer = IntPtr.Zero;
            framesSinceLocalPlayerSeen = int.MaxValue;
            localPlayerMissingInContractFrames = 0;
            deathLeaveLikelySinceStart = false;
            localPlayerFirstPersonMissingNow = false;

            Array.Clear(pcMenuBoolOld, 0, pcMenuBoolOld.Length);
            Array.Clear(pcMenuBoolNew, 0, pcMenuBoolNew.Length);

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
            sawTruckUnloadSignalSinceStart = false;
            sawTruckExitTriggerSignalSinceStart = false;
            sawTruckPresenceDropSinceStart = false;
            sawTruckPresenceReturnSinceStart = false;
            restartLoopStartSignal = false;
            exitTriggerContainsLocalPlayerOld = false;
            exitTriggerContainsLocalPlayerNew = false;
            localPlayerInTruckListOld = false;
            localPlayerInTruckListNew = false;
            pendingResetAfterNonEndLeave = false;
            lobbyStableFrameCount = 0;
            shouldSplit = false;
            shouldReset = false;
            suppressResetUntilNextStart = false;
            loadRemovalState = LoadRemovalState.None;
            loadRemovalFirstPauseStartUtc = DateTime.MinValue;
            loadRemovalSecondPauseStartUtc = DateTime.MinValue;
            loadRemovalInLobbySinceUtc = DateTime.MinValue;
            loadRemovalLobbyReadyConsecutiveFrames = 0;
            loadRemovalFirstLoadingSawStartHidden = false;
            loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
            loadRemovalSecondLoadingSawLoading = false;
            vrLoadingSignalAvailable = false;
            vrLoadScreenInstanceOld = false;
            vrLoadScreenInstanceNew = false;
            vrLoadingProgressOld = 0f;
            vrLoadingProgressNew = 0f;
            vrLoadingProgressAuxOld = 0f;
            vrLoadingProgressAuxNew = 0f;
            vrLoadingLikelyOld = false;
            vrLoadingLikelyNew = false;
            logger?.Log(reason);
            return true;
        }

        private bool IsLobbyUiActive() =>
            // Only treat UI flags as "lobby-active" while we're actually out of contract.
            // Some menu pointers can remain valid during transitions and would otherwise
            // suppress legitimate contract-start edges.
            lastLevelController == IntPtr.Zero
            && (startGameButtonEnabledNew
                || startGameButtonInteractableNew
                || leaveLobbyButtonEnabledNew
                || leaveLobbyButtonInteractableNew
                || contractBoardAvailableNew);

        public bool AreResetsBlockedByMultiContract() => false;

        public bool IsLoadingScreenActive() =>
            pointersInitialized
            && settings != null
            && settings.EnableLoadTimeRemoval
            && hasSplitThisRun
            && (loadRemovalState == LoadRemovalState.FirstLoading || loadRemovalState == LoadRemovalState.SecondLoading);

        private void UpdateLoadRemovalState(IntPtr levelController, bool truckLoadedStartEdge, bool loadingActive)
        {
            if (settings == null || !settings.EnableLoadTimeRemoval || !hasSplitThisRun)
            {
                loadRemovalState = LoadRemovalState.None;
                loadRemovalFirstPauseStartUtc = DateTime.MinValue;
                loadRemovalSecondPauseStartUtc = DateTime.MinValue;
                loadRemovalInLobbySinceUtc = DateTime.MinValue;
                loadRemovalLobbyReadyConsecutiveFrames = 0;
                loadRemovalFirstLoadingSawStartHidden = false;
                loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                loadRemovalSecondLoadingSawLoading = false;
                return;
            }
            if (loadRemovalState == LoadRemovalState.None)
            {
                loadRemovalInLobbySinceUtc = DateTime.MinValue;
                loadRemovalFirstPauseStartUtc = DateTime.MinValue;
                loadRemovalLobbyReadyConsecutiveFrames = 0;
                loadRemovalFirstLoadingSawStartHidden = false;
                loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                loadRemovalSecondLoadingSawLoading = false;
                return;
            }
            if (loadRemovalState == LoadRemovalState.FirstLoading)
            {
                bool startButtonPressed = !startGameButtonDownOld && startGameButtonDownNew;

                // If Start was pressed while still in first-loading state, hand off
                // directly so load-removal cannot deadlock in this state.
                if (startButtonPressed)
                {
                    loadRemovalState = LoadRemovalState.SecondLoading;
                    loadRemovalSecondPauseStartUtc = DateTime.UtcNow;
                    loadRemovalInLobbySinceUtc = DateTime.MinValue;
                    loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                    loadRemovalSecondLoadingSawLoading = false;
                    logger?.Log("Load removal: first-load handoff via Start press");
                    return;
                }

                if (loadRemovalFirstPauseStartUtc == DateTime.MinValue)
                    loadRemovalFirstPauseStartUtc = DateTime.UtcNow;
                TimeSpan firstLoadingDuration = DateTime.UtcNow - loadRemovalFirstPauseStartUtc;

                if (!startGameButtonEnabledNew)
                    loadRemovalFirstLoadingSawStartHidden = true;
                bool gameControllerStartEdge =
                    (!gameControllerFlagF8Old && gameControllerFlagF8New)
                    || (!gameControllerFlagF9Old && gameControllerFlagF9New);
                bool cctvTruckReadyEdge =
                    cctvTruckBoolOld.Length > 0
                    && cctvTruckBoolNew.Length > 0
                    && !cctvTruckBoolOld[0]
                    && cctvTruckBoolNew[0];
                bool contractStartedWhilePaused =
                    gameControllerStartEdge
                    && (truckLoadedStartEdge || cctvTruckReadyEdge)
                    && firstLoadingDuration >= TimeSpan.FromSeconds(0.5);
                if (contractStartedWhilePaused)
                {
                    loadRemovalState = LoadRemovalState.None;
                    loadRemovalFirstPauseStartUtc = DateTime.MinValue;
                    loadRemovalLobbyReadyConsecutiveFrames = 0;
                    loadRemovalFirstLoadingSawStartHidden = false;
                    logger?.Log("Load removal: contract started while paused -> unpause");
                    return;
                }

                bool lobbyReadyByStartSignal =
                    loadRemovalFirstLoadingSawStartHidden
                    && startGameButtonEnabledNew
                    && !startGameButtonInteractableNew
                    && !loadingActive;

                bool lobbyReady = lobbyReadyByStartSignal;
                if (lobbyReady)
                    loadRemovalLobbyReadyConsecutiveFrames++;
                else
                    loadRemovalLobbyReadyConsecutiveFrames = 0;

                bool conservativeFallbackReady = !loadingActive
                    && levelController == IntPtr.Zero
                    && firstLoadingDuration >= TimeSpan.FromSeconds(9.0);
                bool hardTimeout = firstLoadingDuration >= TimeSpan.FromSeconds(15.0);

                if (loadRemovalLobbyReadyConsecutiveFrames >= 2 || conservativeFallbackReady || hardTimeout)
                {
                    string firstLoadReleaseReason = "start-visible-noninteractable";
                    if (hardTimeout)
                        firstLoadReleaseReason = "hard-timeout";
                    else if (conservativeFallbackReady)
                        firstLoadReleaseReason = "conservative-fallback";

                    loadRemovalState = LoadRemovalState.InLobby;
                    loadRemovalInLobbySinceUtc = DateTime.UtcNow;
                    loadRemovalFirstPauseStartUtc = DateTime.MinValue;
                    loadRemovalLobbyReadyConsecutiveFrames = 0;
                    loadRemovalFirstLoadingSawStartHidden = false;
                    logger?.Log("Load removal: loading done -> lobby/board"
                        + " reason=" + firstLoadReleaseReason
                        + " durationMs=" + ((int)firstLoadingDuration.TotalMilliseconds));
                }
                return;
            }
            if (loadRemovalState == LoadRemovalState.InLobby)
            {
                if (loadRemovalInLobbySinceUtc == DateTime.MinValue)
                    loadRemovalInLobbySinceUtc = DateTime.UtcNow;
                bool startButtonPressed = !startGameButtonDownOld && startGameButtonDownNew;
                bool gameControllerLoadEdge =
                    (!gameControllerFlagF8Old && gameControllerFlagF8New)
                    || (!gameControllerFlagF9Old && gameControllerFlagF9New);
                bool secondLoadDetected =
                    startButtonPressed
                    || gameControllerLoadEdge;
                if (secondLoadDetected)
                {
                    loadRemovalState = LoadRemovalState.SecondLoading;
                    loadRemovalSecondPauseStartUtc = DateTime.UtcNow;
                    loadRemovalInLobbySinceUtc = DateTime.MinValue;
                    loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                    loadRemovalSecondLoadingSawLoading = false;
                    logger?.Log("Load removal: contract load started -> pausing");
                }
                return;
            }
            if (loadRemovalState == LoadRemovalState.SecondLoading)
            {
                if (loadingActive)
                    loadRemovalSecondLoadingSawLoading = true;
                bool inLevel = levelController != IntPtr.Zero;
                bool notLoading = !loadingActive;
                bool gameControllerStartEdge =
                    (!gameControllerFlagF8Old && gameControllerFlagF8New)
                    || (!gameControllerFlagF9Old && gameControllerFlagF9New);
                bool cctvTruckReadyEdge =
                    cctvTruckBoolOld.Length > 0
                    && cctvTruckBoolNew.Length > 0
                    && !cctvTruckBoolOld[0]
                    && cctvTruckBoolNew[0];
                if (inLevel && notLoading)
                    loadRemovalSecondLoadingInLevelNotLoadingCount++;
                else
                    loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                TimeSpan inSecondLoadingDuration =
                    loadRemovalSecondPauseStartUtc != DateTime.MinValue
                    ? (DateTime.UtcNow - loadRemovalSecondPauseStartUtc)
                    : TimeSpan.Zero;
                bool minPauseElapsed = inSecondLoadingDuration >= TimeSpan.FromSeconds(0.75);
                bool fallbackUnpauseOk =
                    inLevel
                    && loadRemovalSecondLoadingInLevelNotLoadingCount >= 2
                    && minPauseElapsed
                    && (loadRemovalSecondLoadingSawLoading || inSecondLoadingDuration >= TimeSpan.FromSeconds(2.0))
                    && inSecondLoadingDuration >= TimeSpan.FromSeconds(10.0);
                bool strictContractStartUnpause =
                    inLevel
                    && gameControllerStartEdge
                    && (truckLoadedStartEdge || cctvTruckReadyEdge || loadRemovalSecondLoadingSawLoading);
                if (strictContractStartUnpause || fallbackUnpauseOk)
                {
                    loadRemovalState = LoadRemovalState.None;
                    loadRemovalSecondPauseStartUtc = DateTime.MinValue;
                    loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                    loadRemovalSecondLoadingSawLoading = false;
                    logger?.Log("Load removal: contract started -> unpause");
                }
                else if (loadRemovalSecondPauseStartUtc != DateTime.MinValue
                    && (DateTime.UtcNow - loadRemovalSecondPauseStartUtc) >= LoadRemovalSecondPauseTimeout)
                {
                    loadRemovalState = LoadRemovalState.None;
                    loadRemovalSecondPauseStartUtc = DateTime.MinValue;
                    loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                    loadRemovalSecondLoadingSawLoading = false;
                    logger?.Log("Load removal: timeout -> unpause");
                }
            }
        }
        private bool IsVrLoadingLikelyActive()
        {
            if (!vrLoadingSignalAvailable || !vrLoadScreenInstanceNew)
                return false;

            float progressDelta = Math.Abs(vrLoadingProgressNew - vrLoadingProgressOld);
            float auxDelta = Math.Abs(vrLoadingProgressAuxNew - vrLoadingProgressAuxOld);
            bool hasProgressValue = vrLoadingProgressNew > 0.0005f || vrLoadingProgressAuxNew > 0.0005f;
            bool hasProgressMotion = progressDelta > 0.0005f || auxDelta > 0.0005f;
            return hasProgressValue || hasProgressMotion;
        }

        private bool HasControlStateEdge()
        {
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
            {
                if (playerBoolOld[i] != playerBoolNew[i])
                    return true;
            }

            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
            {
                if (firstPersonBoolOld[i] != firstPersonBoolNew[i])
                    return true;
            }

            return false;
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
                bool wasInitialized = pointersInitialized;
                IntPtr oldLevelAddress = levelControllerStaticAddress;
                IntPtr oldMapAddress = mapControllerStaticAddress;
                IntPtr oldCctvAddress = cctvControllerStaticAddress;
                IntPtr oldLoadingAddress = loadingControllerStaticAddress;
                IntPtr oldMainManagerAddress = mainManagerStaticAddress;
                IntPtr oldGameControllerAddress = gameControllerStaticAddress;

                IntPtr gameAssemblyBase = GetGameAssemblyBaseAddress();
                if (gameAssemblyBase == IntPtr.Zero)
                    return;

                levelControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, LevelControllerTypeInfoRva, "LevelController");
                mapControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, MapControllerTypeInfoRva, "MapController");
                cctvControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, CCTVControllerTypeInfoRva, "CCTVController");
                loadingControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, LoadingControllerTypeInfoRva, "LoadingController");
                // Optional pointer; do not spam logs if unavailable in current game context.
                mainManagerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, MainManagerTypeInfoRva, "MainManager", logNulls: false);
                // Optional pointer used for additional game-state debug probes.
                gameControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, GameControllerTypeInfoRva, "GameController", logNulls: false);

                // Level and map pointers are required; others are optional.
                pointersInitialized = levelControllerStaticAddress != IntPtr.Zero
                                   && mapControllerStaticAddress != IntPtr.Zero;

                bool pointerSetChanged =
                    oldLevelAddress != levelControllerStaticAddress
                    || oldMapAddress != mapControllerStaticAddress
                    || oldCctvAddress != cctvControllerStaticAddress
                    || oldLoadingAddress != loadingControllerStaticAddress
                    || oldMainManagerAddress != mainManagerStaticAddress
                    || oldGameControllerAddress != gameControllerStaticAddress;

                if (pointersInitialized && (!wasInitialized || pointerSetChanged))
                {
                    logger?.Log("Pointers initialized (TypeInfo RVAs)");
                    logger?.Log("  LevelController singleton ptr addr: 0x" + levelControllerStaticAddress.ToString("X"));
                    logger?.Log("  MapController singleton ptr addr:   0x" + mapControllerStaticAddress.ToString("X"));
                    logger?.Log("  CCTVController singleton ptr addr:  0x" + cctvControllerStaticAddress.ToString("X"));
                    logger?.Log("  LoadingController singleton ptr addr: 0x" + loadingControllerStaticAddress.ToString("X"));
                    logger?.Log("  MainManager singleton ptr addr: 0x" + mainManagerStaticAddress.ToString("X"));
                    logger?.Log("  GameController singleton ptr addr: 0x" + gameControllerStaticAddress.ToString("X"));
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

        private IntPtr ResolveSingletonPointerAddress(IntPtr gameAssemblyBase, int typeInfoRva, string label, bool logNulls = true)
        {
            IntPtr typeInfoAddress = gameAssemblyBase + typeInfoRva;
            IntPtr klass = game.Read<IntPtr>(typeInfoAddress);
            if (klass == IntPtr.Zero)
            {
                if (logNulls)
                    logger?.Log(label + " class pointer is null at 0x" + typeInfoAddress.ToString("X"));
                return IntPtr.Zero;
            }

            IntPtr staticFields = game.Read<IntPtr>(klass + Il2CppClassStaticFieldsOffset);
            if (staticFields == IntPtr.Zero)
            {
                if (logNulls)
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

        private bool ReadSelectablePointerDown(IntPtr selectablePointer)
        {
            if (selectablePointer == IntPtr.Zero || game == null)
                return false;

            return game.Read<bool>(selectablePointer + SelectableIsPointerDownOffset);
        }

        private bool ReadSelectableInteractable(IntPtr selectablePointer)
        {
            if (selectablePointer == IntPtr.Zero || game == null)
                return false;

            return game.Read<bool>(selectablePointer + SelectableInteractableOffset);
        }

        private bool ReadSelectableEnableCalled(IntPtr selectablePointer)
        {
            if (selectablePointer == IntPtr.Zero || game == null)
                return false;

            return game.Read<bool>(selectablePointer + SelectableEnableCalledOffset);
        }

        private IntPtr ReadLocalPlayerFromList(IntPtr listPointer)
        {
            if (listPointer == IntPtr.Zero)
                return IntPtr.Zero;

            int size = game.Read<int>(listPointer + ListSizeOffset);
            if (size <= 0 || size > MaxPlayerListCount)
                return IntPtr.Zero;

            IntPtr itemsArray = game.Read<IntPtr>(listPointer + ListItemsOffset);
            if (itemsArray == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr firstNonNullPlayer = IntPtr.Zero;
            IntPtr firstElement = itemsArray + ArrayDataOffset;
            for (int i = 0; i < size; i++)
            {
                IntPtr player = game.Read<IntPtr>(firstElement + (i * game.PointerSize));
                if (player == IntPtr.Zero)
                    continue;

                if (firstNonNullPlayer == IntPtr.Zero)
                    firstNonNullPlayer = player;

                IntPtr photonView = game.Read<IntPtr>(player + PlayerPhotonViewOffset);
                if (photonView == IntPtr.Zero)
                    continue;

                bool isMine = game.Read<bool>(photonView + PhotonViewIsMineOffset);
                if (isMine)
                    return player;
            }

            return firstNonNullPlayer;
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
            if (areaCount <= 0 || areaCount > MaxLevelAreaCount)
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
            if (areaCount <= 0 || areaCount > MaxLevelAreaCount)
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
                if (size > 0 && size <= MaxTriggerPlayersCount)
                    return true;
            }

            return false;
        }

        private bool ReadAnyExitTriggerContainsLocalPlayer(IntPtr levelController, IntPtr localPlayer, out bool hasExitTrigger)
        {
            hasExitTrigger = false;
            if (levelController == IntPtr.Zero)
                return false;

            IntPtr levelAreasArray = game.Read<IntPtr>(levelController + LevelAreasArrayOffset);
            if (levelAreasArray == IntPtr.Zero)
                return false;

            int areaCount = game.Read<int>(levelAreasArray + ArrayLengthOffset);
            if (areaCount <= 0 || areaCount > MaxLevelAreaCount)
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
                if (size <= 0 || size > MaxTriggerPlayersCount)
                    continue;

                IntPtr itemsArray = game.Read<IntPtr>(playersList + ListItemsOffset);
                if (itemsArray == IntPtr.Zero)
                    continue;

                IntPtr firstItem = itemsArray + ArrayDataOffset;
                for (int p = 0; p < size; p++)
                {
                    IntPtr playerPtr = game.Read<IntPtr>(firstItem + (p * game.PointerSize));
                    if (playerPtr == IntPtr.Zero)
                        continue;

                    if (localPlayer != IntPtr.Zero && playerPtr == localPlayer)
                        return true;

                    IntPtr photonView = game.Read<IntPtr>(playerPtr + PlayerPhotonViewOffset);
                    if (photonView == IntPtr.Zero)
                        continue;

                    if (game.Read<bool>(photonView + PhotonViewIsMineOffset))
                        return true;
                }
            }

            return false;
        }

        private bool ReadListContainsLocalPlayer(IntPtr listPointer, IntPtr localPlayer, out bool hasList)
        {
            hasList = listPointer != IntPtr.Zero;
            if (listPointer == IntPtr.Zero)
                return false;

            int size = game.Read<int>(listPointer + ListSizeOffset);
            if (size <= 0 || size > MaxTriggerPlayersCount)
                return false;

            IntPtr itemsArray = game.Read<IntPtr>(listPointer + ListItemsOffset);
            if (itemsArray == IntPtr.Zero)
                return false;

            IntPtr firstItem = itemsArray + ArrayDataOffset;
            for (int i = 0; i < size; i++)
            {
                IntPtr playerPtr = game.Read<IntPtr>(firstItem + (i * game.PointerSize));
                if (playerPtr == IntPtr.Zero)
                    continue;

                if (localPlayer != IntPtr.Zero && playerPtr == localPlayer)
                    return true;

                IntPtr photonView = game.Read<IntPtr>(playerPtr + PlayerPhotonViewOffset);
                if (photonView == IntPtr.Zero)
                    continue;

                if (game.Read<bool>(photonView + PhotonViewIsMineOffset))
                    return true;
            }

            return false;
        }

        public bool ShouldStartForRestartLoop()
        {
            if (!pointersInitialized || !sawLoadingIntoMap || !startedTimerBefore)
                return false;

            if (IsLobbyUiActive())
                return false;

            if (!restartLoopStartSignal)
                return false;

            restartLoopStartSignal = false;

            // Consume once per detected contract-start signal so component-side reset/start
            // orchestration cannot retrigger in a tight loop.
            startArmedForMapLoad = false;
            truckLoadedStartEdge = false;
            sawTruckPresenceDropSinceStart = false;
            sawTruckPresenceReturnSinceStart = false;

            logger?.Log("Start candidate: restart loop signal");
            return true;
        }

        private void UpdateSignals()
        {
            IntPtr levelController = ReadSingletonInstance(levelControllerStaticAddress);
            IntPtr mapController = ReadSingletonInstance(mapControllerStaticAddress);
            IntPtr cctvController = ReadSingletonInstance(cctvControllerStaticAddress);
            IntPtr loadingController = ReadSingletonInstance(loadingControllerStaticAddress);
            bool wasInContract = lastLevelController != IntPtr.Zero;
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
            }

            keySpawnedOld = keySpawnedNew;
            keySpawnedNew = keyPointer != IntPtr.Zero;
            bool keySpawnedEdge = keySpawnedNew && !keySpawnedOld;
            if (keySpawnedEdge && levelController != IntPtr.Zero)
            {
                // Fallback for cases where level controller does not drop to null between contracts.
                sawLoadingIntoMap = true;
                startArmedForMapLoad = true;
            }

            if (levelController != IntPtr.Zero)
                sawLoadingIntoMap = true;

            IntPtr localPlayer = IntPtr.Zero;
            if (mapController != IntPtr.Zero)
            {
                IntPtr mapPlayersList = game.Read<IntPtr>(mapController + 0x28); // MapController.players
                localPlayer = ReadLocalPlayerFromList(mapPlayersList);
            }

            IntPtr cctvPlayersList = IntPtr.Zero;
            if (localPlayer == IntPtr.Zero && cctvController != IntPtr.Zero)
            {
                cctvPlayersList = game.Read<IntPtr>(cctvController + CctvTruckPlayersListOffset); // CCTVController.player list (truck context)
                localPlayer = ReadLocalPlayerFromList(cctvPlayersList);
            }
            else if (cctvController != IntPtr.Zero)
            {
                cctvPlayersList = game.Read<IntPtr>(cctvController + CctvTruckPlayersListOffset); // CCTVController.player list (truck context)
            }

            if (localPlayer != IntPtr.Zero)
            {
                lastKnownLocalPlayer = localPlayer;
                framesSinceLocalPlayerSeen = 0;
            }
            else
            {
                if (framesSinceLocalPlayerSeen < int.MaxValue - 1)
                    framesSinceLocalPlayerSeen++;
                if (framesSinceLocalPlayerSeen > 600)
                    lastKnownLocalPlayer = IntPtr.Zero;
            }

            if (localPlayer == IntPtr.Zero)
            {
                playerSignalPrimed = false;
                AdvancePlayerSignalsWithoutChanges();
                localPlayerFirstPersonMissingNow = false;
            }
            else if (!playerSignalPrimed)
            {
                PrimePlayerSignals(localPlayer);
                playerSignalPrimed = true;
                localPlayerFirstPersonMissingNow = game.Read<IntPtr>(localPlayer + 0x128) == IntPtr.Zero;
            }
            else
            {
                UpdatePlayerSignals(localPlayer);
                localPlayerFirstPersonMissingNow = game.Read<IntPtr>(localPlayer + 0x128) == IntPtr.Zero;
            }

            vrLoadScreenInstanceOld = vrLoadScreenInstanceNew;
            vrLoadingProgressOld = vrLoadingProgressNew;
            vrLoadingProgressAuxOld = vrLoadingProgressAuxNew;
            vrLoadingSignalAvailable = false;
            vrLoadScreenInstanceNew = false;
            vrLoadingProgressNew = 0f;
            vrLoadingProgressAuxNew = 0f;
            IntPtr localPlayerForVrLoading = localPlayer != IntPtr.Zero ? localPlayer : lastKnownLocalPlayer;
            if (localPlayerForVrLoading != IntPtr.Zero)
            {
                IntPtr vrLoading = game.Read<IntPtr>(localPlayerForVrLoading + PlayerVrLoadingOffset); // Player.vrLoading
                if (vrLoading != IntPtr.Zero)
                {
                    vrLoadingSignalAvailable = true;
                    vrLoadScreenInstanceNew = game.Read<bool>(vrLoading + VrLoadingIsLoadScreenInstanceOffset);
                    vrLoadingProgressNew = game.Read<float>(vrLoading + VrLoadingProgressOffset);
                    vrLoadingProgressAuxNew = game.Read<float>(vrLoading + VrLoadingProgressAuxOffset);
                }
            }
            vrLoadingLikelyOld = vrLoadingLikelyNew;
            vrLoadingLikelyNew = IsVrLoadingLikelyActive();
            if (vrLoadScreenInstanceOld != vrLoadScreenInstanceNew)
            {
                logger?.Log("VRLoading instance edge (" + vrLoadScreenInstanceOld + " -> " + vrLoadScreenInstanceNew + ")");
            }
            if (vrLoadingLikelyOld != vrLoadingLikelyNew)
            {
                logger?.Log("VRLoading candidate edge (" + vrLoadingLikelyOld + " -> " + vrLoadingLikelyNew + ")"
                    + " instance=" + vrLoadScreenInstanceNew
                    + " progress=" + vrLoadingProgressNew.ToString("0.000")
                    + " aux=" + vrLoadingProgressAuxNew.ToString("0.000"));
            }

            loadingFlagOld = loadingFlagNew;
            loadingFlagNew = false;
            loadingSignalAvailable = loadingController != IntPtr.Zero;
            if (loadingSignalAvailable)
            {
                loadingFlagNew = game.Read<bool>(loadingController + 0x30); // LoadingController loading flag candidate
            }

            startGameButtonDownOld = startGameButtonDownNew;
            leaveLobbyButtonDownOld = leaveLobbyButtonDownNew;
            startGameButtonEnabledOld = startGameButtonEnabledNew;
            leaveLobbyButtonEnabledOld = leaveLobbyButtonEnabledNew;
            startGameButtonInteractableOld = startGameButtonInteractableNew;
            leaveLobbyButtonInteractableOld = leaveLobbyButtonInteractableNew;
            contractBoardAvailableOld = contractBoardAvailableNew;
            gameControllerFlagF8Old = gameControllerFlagF8New;
            gameControllerFlagF9Old = gameControllerFlagF9New;
            startGameButtonDownNew = false;
            leaveLobbyButtonDownNew = false;
            startGameButtonEnabledNew = false;
            leaveLobbyButtonEnabledNew = false;
            startGameButtonInteractableNew = false;
            leaveLobbyButtonInteractableNew = false;
            contractBoardAvailableNew = false;
            gameControllerFlagF8New = false;
            gameControllerFlagF9New = false;
            IntPtr mainManager = ReadSingletonInstance(mainManagerStaticAddress);
            if (mainManager != IntPtr.Zero)
            {
                IntPtr serverManager = game.Read<IntPtr>(mainManager + MainManagerServerManagerOffset);
                if (serverManager != IntPtr.Zero)
                {
                    IntPtr leaveButton = game.Read<IntPtr>(serverManager + ServerManagerLeaveButtonOffset);
                    IntPtr startButton = game.Read<IntPtr>(serverManager + ServerManagerStartGameButtonOffset);
                    leaveLobbyButtonDownNew = ReadSelectablePointerDown(leaveButton);
                    startGameButtonDownNew = ReadSelectablePointerDown(startButton);
                    leaveLobbyButtonEnabledNew = ReadSelectableEnableCalled(leaveButton);
                    startGameButtonEnabledNew = ReadSelectableEnableCalled(startButton);
                    leaveLobbyButtonInteractableNew = ReadSelectableInteractable(leaveButton);
                    startGameButtonInteractableNew = ReadSelectableInteractable(startButton);
                }
                IntPtr levelSelectionManager = game.Read<IntPtr>(mainManager + MainManagerLevelSelectionManagerOffset);
                contractBoardAvailableNew = levelSelectionManager != IntPtr.Zero;
            }
            IntPtr gameController = ReadSingletonInstance(gameControllerStaticAddress);
            if (gameController != IntPtr.Zero)
            {
                gameControllerFlagF8New = game.Read<bool>(gameController + 0xF8);
                gameControllerFlagF9New = game.Read<bool>(gameController + 0xF9);
            }
            if (startGameButtonDownOld != startGameButtonDownNew)
            {
                logger?.Log("Lobby Start button edge ("
                    + startGameButtonDownOld + " -> " + startGameButtonDownNew + ")");
            }
            if (leaveLobbyButtonDownOld != leaveLobbyButtonDownNew)
            {
                logger?.Log("Lobby Leave button edge ("
                    + leaveLobbyButtonDownOld + " -> " + leaveLobbyButtonDownNew + ")");
            }
            if (startGameButtonInteractableOld != startGameButtonInteractableNew)
            {
                logger?.Log("Lobby Start interactable edge ("
                    + startGameButtonInteractableOld + " -> " + startGameButtonInteractableNew + ")");
            }
            if (startGameButtonEnabledOld != startGameButtonEnabledNew)
            {
                logger?.Log("Lobby Start enabled edge ("
                    + startGameButtonEnabledOld + " -> " + startGameButtonEnabledNew + ")");
            }
            if (leaveLobbyButtonInteractableOld != leaveLobbyButtonInteractableNew)
            {
                logger?.Log("Lobby Leave interactable edge ("
                    + leaveLobbyButtonInteractableOld + " -> " + leaveLobbyButtonInteractableNew + ")");
            }
            if (leaveLobbyButtonEnabledOld != leaveLobbyButtonEnabledNew)
            {
                logger?.Log("Lobby Leave enabled edge ("
                    + leaveLobbyButtonEnabledOld + " -> " + leaveLobbyButtonEnabledNew + ")");
            }
            if (contractBoardAvailableOld != contractBoardAvailableNew)
            {
                logger?.Log("ContractBoard availability edge ("
                    + contractBoardAvailableOld + " -> " + contractBoardAvailableNew + ")");
            }
            if (gameControllerFlagF8Old != gameControllerFlagF8New)
            {
                logger?.Log("GameController flag 0xF8 edge ("
                    + gameControllerFlagF8Old + " -> " + gameControllerFlagF8New + ")");
            }
            if (gameControllerFlagF9Old != gameControllerFlagF9New)
            {
                logger?.Log("GameController flag 0xF9 edge ("
                    + gameControllerFlagF9Old + " -> " + gameControllerFlagF9New + ")");
            }

            exitLevelFlagOld = exitLevelFlagNew;
            exitLevelFlagNew = ReadAnyExitLevelTruckUnloadFlag(levelController, out bool hasExitLevel);
            exitSignalAvailable = hasExitLevel;

            exitTriggerHasPlayersOld = exitTriggerHasPlayersNew;
            exitTriggerHasPlayersNew = ReadAnyExitTriggerHasPlayers(levelController, out bool hasExitTrigger);
            exitTriggerSignalAvailable = hasExitTrigger;
            exitTriggerContainsLocalPlayerOld = exitTriggerContainsLocalPlayerNew;
            exitTriggerContainsLocalPlayerNew = ReadAnyExitTriggerContainsLocalPlayer(levelController, localPlayer, out bool hasExitTriggerLocalPlayer);
            if (!exitTriggerSignalAvailable)
                exitTriggerSignalAvailable = hasExitTriggerLocalPlayer;

            localPlayerInTruckListOld = localPlayerInTruckListNew;
            localPlayerInTruckListNew = ReadListContainsLocalPlayer(cctvPlayersList, localPlayer, out bool _);
            for (int i = 0; i < CctvTruckBoolOffsets.Length; i++)
            {
                cctvTruckBoolOld[i] = cctvTruckBoolNew[i];
                cctvTruckBoolNew[i] = cctvController != IntPtr.Zero && game.Read<bool>(cctvController + CctvTruckBoolOffsets[i]);
                if (cctvTruckBoolOld[i] != cctvTruckBoolNew[i])
                {
                    logger?.Log("CCTV truck bool edge 0x" + CctvTruckBoolOffsets[i].ToString("X")
                        + " (" + cctvTruckBoolOld[i] + " -> " + cctvTruckBoolNew[i] + ")");
                }
            }
            bool cctvTruckPresenceOld = cctvTruckBoolOld.Length > CctvTruckPresenceOffsetIndex && cctvTruckBoolOld[CctvTruckPresenceOffsetIndex];
            bool cctvTruckPresenceNew = cctvTruckBoolNew.Length > CctvTruckPresenceOffsetIndex && cctvTruckBoolNew[CctvTruckPresenceOffsetIndex];

            if (exitSignalAvailable && exitLevelFlagOld != exitLevelFlagNew)
            {
                logger?.Log("Truck unload signal edge (" + exitLevelFlagOld + " -> " + exitLevelFlagNew + ")");
            }

            if (exitTriggerSignalAvailable && exitTriggerHasPlayersOld != exitTriggerHasPlayersNew)
            {
                logger?.Log("Truck exit trigger players edge (" + exitTriggerHasPlayersOld + " -> " + exitTriggerHasPlayersNew + ")");
            }
            if (exitTriggerSignalAvailable && exitTriggerContainsLocalPlayerOld != exitTriggerContainsLocalPlayerNew)
            {
                logger?.Log("Truck exit trigger local-player edge (" + exitTriggerContainsLocalPlayerOld + " -> " + exitTriggerContainsLocalPlayerNew + ")");
            }

            bool truckLoadedStartEdgeBefore = truckLoadedStartEdge;
            if (startArmedForMapLoad
                && !truckLoadedStartEdge
                && localPlayer != IntPtr.Zero
                && (!loadingSignalAvailable || !loadingFlagNew))
            {
                truckLoadedStartEdge = true;
            }
            bool truckLoadedStartTransition = !truckLoadedStartEdgeBefore && truckLoadedStartEdge;
            if (startedTimerBefore && truckLoadedStartTransition)
            {
                restartLoopStartSignal = true;
            }

            if (startedTimerBefore && keySpawnedNew)
            {
                sawTruckKeySinceStart = true;
            }

            if (startedTimerBefore && levelController != IntPtr.Zero && !loadingFlagNew)
            {
                bool localPlayerLikelyDeadOrDetached = localPlayer == IntPtr.Zero || localPlayerFirstPersonMissingNow;
                if (localPlayerLikelyDeadOrDetached)
                {
                    if (localPlayerMissingInContractFrames < int.MaxValue - 1)
                        localPlayerMissingInContractFrames++;
                }
                else
                {
                    localPlayerMissingInContractFrames = 0;
                }

                if (!deathLeaveLikelySinceStart && localPlayerMissingInContractFrames >= 30)
                {
                    deathLeaveLikelySinceStart = true;
                    logger?.Log("Death-leave signal armed: player dead/detached in-contract");
                }
            }
            else
            {
                localPlayerMissingInContractFrames = 0;
            }

            if (startedTimerBefore && exitTriggerSignalAvailable && exitTriggerHasPlayersNew)
            {
                sawTruckExitTriggerSignalSinceStart = true;
            }

            if (startedTimerBefore && exitSignalAvailable && (exitLevelFlagNew || exitLevelFlagOld))
            {
                sawTruckUnloadSignalSinceStart = true;
            }

            if (startedTimerBefore && exitTriggerSignalAvailable && (exitTriggerContainsLocalPlayerNew || exitTriggerContainsLocalPlayerOld))
            {
                sawTruckExitTriggerSignalSinceStart = true;
            }
            if (startedTimerBefore && cctvTruckPresenceOld && !cctvTruckPresenceNew)
            {
                sawTruckPresenceDropSinceStart = true;
            }
            if (startedTimerBefore && sawTruckPresenceDropSinceStart && !cctvTruckPresenceOld && cctvTruckPresenceNew)
            {
                sawTruckPresenceReturnSinceStart = true;
            }
            if (startedTimerBefore
                && keySpawnedEdge
                && levelController != IntPtr.Zero
                && (!loadingSignalAvailable || !loadingFlagNew))
            {
                restartLoopStartSignal = true;
            }

            bool pauseMenuLikelyOld = pcMenuBoolOld.Length >= 2 && pcMenuBoolOld[0] && pcMenuBoolOld[1];
            bool pauseMenuLikelyNew = pcMenuBoolNew.Length >= 2 && pcMenuBoolNew[0] && pcMenuBoolNew[1];
            if (pauseMenuLikelyNew)
                pauseMenuLikelyFramesAgo = 0;
            else if (pauseMenuLikelyFramesAgo < int.MaxValue - 1)
                pauseMenuLikelyFramesAgo++;

            bool loadingEdgeStarted = loadingFlagNew && !loadingFlagOld;
            bool loadingEdgeFinished = !loadingFlagNew && loadingFlagOld;
            bool resetWhenAtLobby = settings != null && settings.ResetWhenAtLobby;
            bool useMultiContract = settings != null && settings.EnableMultiContract;
            bool localPlayerLikelyPresent = localPlayer != IntPtr.Zero || lastKnownLocalPlayer != IntPtr.Zero;
            bool vrLoadingLikelyActive = vrLoadingLikelyNew;
            bool vrLoadingVisualActive = vrLoadScreenInstanceNew || vrLoadingLikelyActive;
            bool inLobbyNow = levelController == IntPtr.Zero;
            bool cctvTruckFlagOld = cctvTruckBoolOld.Length > 0 && cctvTruckBoolOld[0];
            bool cctvTruckFlagNew = cctvTruckBoolNew.Length > 0 && cctvTruckBoolNew[0];
            bool cctvTruckFadeOld = cctvTruckBoolOld.Length > 2 && cctvTruckBoolOld[2];
            bool cctvTruckFadeNew = cctvTruckBoolNew.Length > 2 && cctvTruckBoolNew[2];

            UpdateLoadRemovalState(levelController, truckLoadedStartEdge, loadingFlagNew || vrLoadingVisualActive);

            if (startedTimerBefore
                && !shouldSplit
                && sawTruckKeySinceStart
                && wasInContract
                && levelController != IntPtr.Zero
                && loadingEdgeStarted)
            {
                bool hasTruckTriggerSignal =
                    exitTriggerSignalAvailable
                    && (exitTriggerHasPlayersNew
                        || exitTriggerHasPlayersOld
                        || exitTriggerContainsLocalPlayerNew
                        || exitTriggerContainsLocalPlayerOld
                        || sawTruckExitTriggerSignalSinceStart);

                bool hasTruckUnloadStateSignal =
                    exitSignalAvailable
                    && (exitLevelFlagNew || exitLevelFlagOld || sawTruckUnloadSignalSinceStart);

                // Use the truck exit trigger containing the local player as the authoritative
                // end signal. The unload bool can be missing/late on some runs.
                bool hasStrictTruckUnloadSignal = hasTruckTriggerSignal || hasTruckUnloadStateSignal;
                bool relaxedSplitAllowed = useMultiContract && hasSplitThisRun;
                bool shouldEndForTruckLeave =
                    (settings == null || settings.EndOnTruckUnload)
                    && hasStrictTruckUnloadSignal
                    && (relaxedSplitAllowed || sawTruckPresenceDropSinceStart);
                bool cctvTruckPresenceNow =
                    CctvTruckBoolOffsets.Length > CctvTruckPresenceOffsetIndex
                    && cctvTruckBoolNew[CctvTruckPresenceOffsetIndex];
                bool menuDrivenTransition =
                    pauseMenuLikelyOld
                    || pauseMenuLikelyNew
                    || pauseMenuLikelyFramesAgo <= PauseMenuRecentFrames;
                bool fallbackTruckLeaveSignal =
                    (settings == null || settings.EndOnTruckUnload)
                    && cctvTruckPresenceNow
                    && (relaxedSplitAllowed || sawTruckPresenceReturnSinceStart)
                    && !menuDrivenTransition;
                bool deathLeaveSignal =
                    settings != null
                    && settings.SplitOnDeathLeave
                    && deathLeaveLikelySinceStart;
                bool deathLeaveHeuristicSignal =
                    settings != null
                    && settings.SplitOnDeathLeave
                    && !menuDrivenTransition
                    && !hasStrictTruckUnloadSignal
                    && !cctvTruckPresenceNow
                    && sawTruckPresenceDropSinceStart
                    && !sawTruckPresenceReturnSinceStart
                    && !exitTriggerContainsLocalPlayerNew
                    && !exitTriggerContainsLocalPlayerOld;

                if (shouldEndForTruckLeave || fallbackTruckLeaveSignal || deathLeaveSignal || deathLeaveHeuristicSignal)
                {
                    shouldSplit = true;
                    pendingResetAfterNonEndLeave = false;
                    if (shouldEndForTruckLeave)
                    {
                        logger?.Log("Split candidate: truck unload signal confirmed");
                    }
                    else if (fallbackTruckLeaveSignal)
                    {
                        logger?.Log("Split candidate fallback: CCTV truck presence");
                    }
                    else if (deathLeaveSignal)
                    {
                        logger?.Log("Split candidate: death leave enabled");
                    }
                    else
                    {
                        logger?.Log("Split candidate: death leave heuristic");
                    }
                }
                else
                {
                    logger?.Log("Loading edge strict check failed"
                        + " strict=" + hasStrictTruckUnloadSignal
                        + " unloadSignal=" + hasTruckUnloadStateSignal
                        + " triggerSignal=" + hasTruckTriggerSignal
                        + " triggerHasPlayers=" + (exitTriggerHasPlayersNew || exitTriggerHasPlayersOld)
                        + " triggerLocalPlayer=" + (exitTriggerContainsLocalPlayerNew || exitTriggerContainsLocalPlayerOld)
                        + " localPlayerNull=" + (localPlayer == IntPtr.Zero)
                        + " firstPersonMissing=" + localPlayerFirstPersonMissingNow
                        + " truckPresenceNow=" + cctvTruckPresenceNow
                        + " truckListPresence=" + (localPlayerInTruckListOld || localPlayerInTruckListNew)
                        + " cctv90=" + cctvTruckBoolNew[0]
                        + " cctvA0=" + cctvTruckBoolNew[1]
                        + " cctvA1=" + cctvTruckBoolNew[2]
                        + " deathLeaveLikely=" + deathLeaveLikelySinceStart
                        + " truckDropSeen=" + sawTruckPresenceDropSinceStart
                        + " truckReturnSeen=" + sawTruckPresenceReturnSinceStart
                        + " pcMenu20=" + pcMenuBoolNew[0]
                        + " pcMenu21=" + pcMenuBoolNew[1]
                        + " pauseMenuRecentFrames=" + pauseMenuLikelyFramesAgo
                        + " menuDriven=" + menuDrivenTransition);

                    // Only arm a non-end-leave reset when not in multi-contract mode
                    // after a split. In multi-contract runs, we never want a lobby
                    // reset just because the leave wasn't classified as an "end".
                    if (resetWhenAtLobby && !(useMultiContract && hasSplitThisRun))
                    {
                        pendingResetAfterNonEndLeave = true;
                        logger?.Log("Reset armed: non-end leave transition");
                    }

                    // When Multi-Contract + Load Removal are on, still run load removal
                    // on normal leaves (no split): pause during loading, unpause in lobby/next contract.
                    if (useMultiContract && hasSplitThisRun && settings != null && settings.EnableLoadTimeRemoval)
                    {
                        loadRemovalState = LoadRemovalState.FirstLoading;
                        loadRemovalFirstPauseStartUtc = DateTime.UtcNow;
                        loadRemovalLobbyReadyConsecutiveFrames = 0;
                        loadRemovalFirstLoadingSawStartHidden = false;
                        loadRemovalSecondPauseStartUtc = DateTime.MinValue;
                        loadRemovalInLobbySinceUtc = DateTime.MinValue;
                        loadRemovalSecondLoadingInLevelNotLoadingCount = 0;
                        loadRemovalSecondLoadingSawLoading = false;
                        logger?.Log("Load removal: normal leave -> pausing for load");
                    }
                }
            }

            if (startedTimerBefore
             && !shouldSplit
             && resetWhenAtLobby
             && pendingResetAfterNonEndLeave
             && loadingEdgeFinished)
            {
                // In multi-contract runs after a split, do not turn a non-end
                // leave into a reset; the run should continue to the next contract.
                if (!suppressResetUntilNextStart && !(useMultiContract && hasSplitThisRun))
                {
                    shouldReset = true;
                    logger?.Log("Reset candidate: non-end leave settled");
                }

                pendingResetAfterNonEndLeave = false;
            }

            bool atLobbyNow = !loadingFlagNew
                           && levelController == IntPtr.Zero;

            if (startedTimerBefore && !shouldSplit && !suppressResetUntilNextStart && atLobbyNow && resetWhenAtLobby)
                lobbyStableFrameCount++;
            else
                lobbyStableFrameCount = 0;

            if (startedTimerBefore
             && !shouldSplit
             && !suppressResetUntilNextStart
             && resetWhenAtLobby
             && !(useMultiContract && hasSplitThisRun)
             && lobbyStableFrameCount >= LobbyStableFramesRequired)
            {
                shouldReset = true;
                pendingResetAfterNonEndLeave = false;
            }

            hadLocalPlayerLastFrame = localPlayerLikelyPresent;
            lastLevelController = levelController;
        }

        private void PrimePlayerSignals(IntPtr player)
        {
            moveInputNew = default(Vector2f);
            menuOpenNew = false;
            Array.Clear(pcMenuBoolNew, 0, pcMenuBoolNew.Length);

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
                for (int i = 0; i < PcMenuBoolOffsets.Length; i++)
                {
                    bool value = game.Read<bool>(pcMenu + PcMenuBoolOffsets[i]);
                    pcMenuBoolNew[i] = value;
                    menuOpenNew |= value;
                }
            }

            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolNew[i] = game.Read<bool>(player + PlayerBoolOffsets[i]);

            moveInputOld = moveInputNew;
            menuOpenOld = menuOpenNew;
            Array.Copy(pcMenuBoolNew, pcMenuBoolOld, pcMenuBoolNew.Length);
            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolOld[i] = playerBoolNew[i];
            for (int i = 0; i < FirstPersonBoolOffsets.Length; i++)
                firstPersonBoolOld[i] = firstPersonBoolNew[i];
        }

        private void UpdatePlayerSignals(IntPtr player)
        {
            moveInputOld = moveInputNew;
            menuOpenOld = menuOpenNew;
            Array.Copy(pcMenuBoolNew, pcMenuBoolOld, pcMenuBoolNew.Length);
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
            menuOpenNew = false;
            Array.Clear(pcMenuBoolNew, 0, pcMenuBoolNew.Length);
            if (pcMenu != IntPtr.Zero)
            {
                for (int i = 0; i < PcMenuBoolOffsets.Length; i++)
                {
                    bool value = game.Read<bool>(pcMenu + PcMenuBoolOffsets[i]);
                    pcMenuBoolNew[i] = value;
                    menuOpenNew |= value;
                }
            }

            for (int i = 0; i < PlayerBoolOffsets.Length; i++)
                playerBoolNew[i] = game.Read<bool>(player + PlayerBoolOffsets[i]);

            if (menuOpenOld != menuOpenNew)
                logger?.Log("Menu open edge (" + menuOpenOld + " -> " + menuOpenNew + ")");
        }

        private void AdvancePlayerSignalsWithoutChanges()
        {
            moveInputOld = moveInputNew;
            menuOpenOld = menuOpenNew;
            Array.Copy(pcMenuBoolNew, pcMenuBoolOld, pcMenuBoolNew.Length);
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


