using LiveSplit.Model;
using System;
using System.ComponentModel;
using Voxif.AutoSplitter;
using Voxif.IO;

namespace LiveSplit.Phasmophobia
{
    public class PhasmophobiaComponent : Voxif.AutoSplitter.Component
    {
        private readonly PhasmophobiaMemory memory;
        private bool queuedStartAfterAutoReset;

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
            if (timer.CurrentState.CurrentPhase != TimerPhase.NotRunning)
                return false;

            if (queuedStartAfterAutoReset)
            {
                queuedStartAfterAutoReset = false;
                logger?.Log("Start candidate: restart loop");
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
            bool restartLoopPhase =
                timer.CurrentState.CurrentPhase == TimerPhase.Paused
                || timer.CurrentState.CurrentPhase == TimerPhase.Ended;

            if (restartLoopPhase && settings.EnableStartSplit && memory.ShouldStartForRestartLoop())
            {
                queuedStartAfterAutoReset = true;
                logger?.Log("Reset candidate: restart loop on new contract start");
                return true;
            }

            return memory.ShouldResetOnLeave();
        }

        public override bool Loading() => false;

        public override void OnReset()
        {
            if (queuedStartAfterAutoReset)
                return;

            memory.ResetRunState();
        }

        public override void Dispose()
        {
            memory?.Dispose();
            base.Dispose();
        }
    }
}
