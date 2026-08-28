using System;
using System.Collections.Generic;
using System.Linq;
using Voxif.Helpers.Unity;
using Voxif.IO;
using Voxif.Memory;

namespace LiveSplit.PhasmophobiaDynamicLookup
{
    internal static class PhasmophobiaDynamicIl2CppResolver
    {
        public static bool TryResolveSingletonStaticBases(
            ProcessWrapper processWrapper,
            IEnumerable<string> classNames,
            Logger logger,
            out Dictionary<string, IntPtr> staticBases,
            out string diagnostic,
            int timeoutMs = 3000)
        {
            staticBases = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
            diagnostic = null;
            if (processWrapper == null || classNames == null)
            {
                diagnostic = "The process or controller list was unavailable.";
                return false;
            }

            using (var task = new BlockingUnityLookupTask(processWrapper, logger))
            {
                return task.TryResolve(classNames, out staticBases, out diagnostic, timeoutMs);
            }
        }

        public static bool TryResolveSingletonStaticBases(
            ProcessWrapper processWrapper,
            IEnumerable<string> classNames,
            Logger logger,
            out Dictionary<string, IntPtr> staticBases,
            int timeoutMs = 3000)
        {
            return TryResolveSingletonStaticBases(
                processWrapper, classNames, logger, out staticBases, out _, timeoutMs);
        }

        private sealed class BlockingUnityLookupTask : UnityHelperTask
        {
            public BlockingUnityLookupTask(ProcessWrapper wrapper, Logger logger)
                : base(wrapper, logger)
            {
            }

            public bool TryResolve(
                IEnumerable<string> classNames,
                out Dictionary<string, IntPtr> staticBases,
                out string diagnostic,
                int timeoutMs)
            {
                string[] requested = classNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                var resolved = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
                Diagnostic = null;

                Run(helper =>
                {
                    foreach (string className in requested)
                    {
                        IntPtr klass = helper.TryFindClassOnce(className);
                        if (klass == IntPtr.Zero)
                            continue;

                        IntPtr staticBase = helper.GetStaticAddress(klass);
                        if (staticBase != IntPtr.Zero)
                            resolved[className] = staticBase;
                    }
                });

                bool completed = task != null && task.Wait(timeoutMs);
                if (!completed)
                {
                    tokenSource?.Cancel();
                    staticBases = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
                    diagnostic = "IL2CPP class lookup timed out after " + timeoutMs + " ms.";
                    return false;
                }

                staticBases = resolved;
                string[] missing = requested.Where(name => !resolved.ContainsKey(name)).ToArray();
                diagnostic = Diagnostic;
                if (missing.Length > 0)
                {
                    string missingText = "Missing controller classes: " + string.Join(", ", missing) + ".";
                    diagnostic = string.IsNullOrWhiteSpace(diagnostic)
                        ? missingText
                        : missingText + " " + diagnostic;
                }

                return resolved.Count > 0;
            }

            public string Diagnostic { get; private set; }

            protected override void Log(string msg)
            {
                if (!string.IsNullOrWhiteSpace(msg)
                    && msg.IndexOf("Task aborted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Diagnostic = msg;
                }

                base.Log(msg);
            }
        }
    }
}
