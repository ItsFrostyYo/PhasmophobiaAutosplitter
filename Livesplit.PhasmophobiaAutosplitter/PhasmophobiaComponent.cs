using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Voxif.AutoSplitter;
using Voxif.IO;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    public class PhasmophobiaComponent : Voxif.AutoSplitter.Component
    {
        private readonly PhasmophobiaMemory memory;
        private bool queuedStartAfterAutoReset;
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
            logger?.Log("Component version: " + Factory.ExAssembly.GetName().Version);
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
            // In multi-contract mode, once we've already split in this run, don't allow
            // start logic to retrigger until an actual reset occurs.
            if (memory.HasSplitOccurredThisRun())
                return false;

            if (timer.CurrentState.CurrentPhase != TimerPhase.NotRunning)
                return false;

            if (queuedStartAfterAutoReset)
            {
                queuedStartAfterAutoReset = false;
                logger?.Log("Start: queued after auto-reset");
                return true;
            }

            return settings.EnableStartSplit && memory.ShouldStart();
        }

        public override bool Split() =>
            settings.EnableEndSplit
            && timer.CurrentState.CurrentPhase == TimerPhase.Running
            && memory.ShouldSplitEnd();

        public override bool Reset()
        {
            if (!settings.EnableAutoResetOnLeave)
            {
                pendingAutoRestartReset = false;
                skipMemoryResetOnce = false;
                return false;
            }

            TimerPhase phase = timer.CurrentState.CurrentPhase;
            bool timerIsRunning = phase == TimerPhase.Running;
            bool restartSignal = false;

            if (!pendingAutoRestartReset
                && timerIsRunning
                && settings.EnableStartSplit
                && !memory.HasSplitOccurredThisRun())
            {
                restartSignal = memory.ShouldStartForRestartLoop();

                if (restartSignal)
                {
                    logger?.Log("Reset candidate: restart loop on new contract start");
                    pendingAutoRestartReset = true;
                    skipMemoryResetOnce = true;
                }
            }

            if (!timerIsRunning)
            {
                pendingAutoRestartReset = false;
                skipMemoryResetOnce = false;
            }

            if (pendingAutoRestartReset && timerIsRunning)
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
