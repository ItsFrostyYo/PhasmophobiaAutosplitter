using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using Voxif.AutoSplitter;
using Voxif.IO;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    public class PhasmophobiaComponent : Voxif.AutoSplitter.Component
    {
        private readonly PhasmophobiaMemory memory;
        private bool queuedStartAfterAutoReset;
        private bool queuedUnpauseAfterSplitStart;
        private bool skipMemoryResetOnce;
        private bool pendingAutoRestartReset;

        protected override EGameTime GameTimeType => EGameTime.Loading;
        protected override bool IsGameTimeDefault => false;

        public PhasmophobiaComponent(LiveSplitState state) : base(state)
        {
#if DEBUG
            logger = new ConsoleLogger();
#else
            logger = new FileLogger("_" + Factory.ExAssembly.GetName().Name.Substring(10) + ".log");
#endif
            logger.StartLogger();

            settings = new PhasmophobiaSettings(state);
            memory = new PhasmophobiaMemory(logger, settings);
            memory.OnUnsupportedBuildWarning = ShowUnsupportedBuildWarning;
            memory.OnCompatibilityResult = ShowCompatibilityResult;
            logger?.Log("Component version: " + Factory.ExAssembly.GetName().Version);
        }

        private static void ShowUnsupportedBuildWarning(string assemblyHash)
        {
            Task.Run(() => MessageBox.Show(
                "This is not a supported Phasmophobia build version.\n\n"
                + "The autosplitter will run a safe background compatibility check. "
                + "It will remain disabled if the expected memory layout cannot be validated. "
                + "Even if validation succeeds, autosplitter behavior may fail if the game changed relevant fields.\n\n"
                + "GameAssembly SHA256:\n" + assemblyHash,
                "Phasmophobia Autosplitter - Unsupported Build",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly));
        }

        private static void ShowCompatibilityResult(bool success, string details)
        {
            Task.Run(() => MessageBox.Show(
                success
                    ? "Compatibility check succeeded.\n\n" + details
                    : "Compatibility check failed safely. The autosplitter will remain disabled for this game process.\n\n" + details,
                success
                    ? "Phasmophobia Autosplitter - Compatibility Succeeded"
                    : "Phasmophobia Autosplitter - Compatibility Failed",
                MessageBoxButtons.OK,
                success ? MessageBoxIcon.Information : MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly));
        }

        public override bool Update()
        {
            try
            {
                // Keep component logic running even while process is between sessions,
                // so pending split/reset flags can still be consumed.
                memory.Update();
                return true;
            }
            catch (Win32Exception ex)
            {
                logger?.Log("Win32Exception in memory.Update: " + ex.Message);
                return true;
            }
            catch (Exception ex)
            {
                logger?.Log("Unexpected exception in memory.Update: " + ex);
                return true;
            }
        }

        public override bool Start()
        {
            if (!settings.EnableStartSplit)
                return false;

            TimerPhase phase = timer.CurrentState.CurrentPhase;
            if (queuedUnpauseAfterSplitStart)
            {
                if (phase == TimerPhase.Paused)
                {
                    queuedUnpauseAfterSplitStart = false;
                    logger?.Log("Start: queued unpause after split run start");
                    return true;
                }

                // Drop stale queued unpause requests as soon as timer is no longer paused.
                if (phase != TimerPhase.Paused)
                    queuedUnpauseAfterSplitStart = false;
            }

            // In multi-contract mode, once we've already split in this run, don't allow
            // regular start logic to retrigger until an actual reset occurs.
            if (memory.HasSplitOccurredThisRun())
                return false;

            if (phase != TimerPhase.NotRunning)
                return false;

            if (queuedStartAfterAutoReset)
            {
                queuedStartAfterAutoReset = false;
                logger?.Log("Start: queued after auto-reset");
                return true;
            }

            return memory.ShouldStart();
        }

        public override bool Split() =>
            (timer.CurrentState.CurrentPhase == TimerPhase.Running
                || timer.CurrentState.CurrentPhase == TimerPhase.Paused)
            && memory.ShouldSplitEnd();

        public override bool Reset()
        {
            if (!settings.EnableAutoResetOnLeave)
            {
                queuedUnpauseAfterSplitStart = false;
                pendingAutoRestartReset = false;
                skipMemoryResetOnce = false;
                return false;
            }

            TimerPhase phase = timer.CurrentState.CurrentPhase;
            bool timerIsRunning = phase == TimerPhase.Running;
            bool timerIsPaused = phase == TimerPhase.Paused;
            bool timerCanAutoRestart = timerIsRunning || timerIsPaused;
            bool afterFirstSplitWithLoadRemoval =
                settings.EnableLoadTimeRemoval
                && memory.HasSplitOccurredThisRun();

            if (!pendingAutoRestartReset
                && timerCanAutoRestart
                && settings.EnableStartSplit
                && memory.ShouldStartForRestartLoop())
            {
                if (afterFirstSplitWithLoadRemoval)
                {
                    pendingAutoRestartReset = false;
                    skipMemoryResetOnce = false;
                    if (timerIsPaused)
                    {
                        queuedUnpauseAfterSplitStart = true;
                        logger?.Log("Start candidate: split run restart while paused -> queue unpause");
                    }
                }
                else
                {
                    logger?.Log("Reset candidate: restart loop on new contract start");
                    pendingAutoRestartReset = true;
                    skipMemoryResetOnce = true;
                }
            }

            if (!timerCanAutoRestart)
            {
                pendingAutoRestartReset = false;
                skipMemoryResetOnce = false;
            }

            if (pendingAutoRestartReset && timerCanAutoRestart)
                return true;

            return memory.ShouldResetOnLeave();
        }

        public override bool Loading() =>
            settings.EnableLoadTimeRemoval
            && (timer.CurrentState.CurrentPhase == TimerPhase.Running
                || timer.CurrentState.CurrentPhase == TimerPhase.Ended)
            && memory.IsLoadingScreenActive();

        public override void OnReset()
        {
            if (skipMemoryResetOnce)
            {
                skipMemoryResetOnce = false;
                if (pendingAutoRestartReset)
                {
                    pendingAutoRestartReset = false;
                    queuedStartAfterAutoReset = true;
                }
                return;
            }

            if (queuedStartAfterAutoReset)
                return;

            queuedUnpauseAfterSplitStart = false;
            pendingAutoRestartReset = false;
            memory.ResetRunState();
        }

        public override void Dispose()
        {
            memory?.Dispose();
            base.Dispose();
        }
    }
}
