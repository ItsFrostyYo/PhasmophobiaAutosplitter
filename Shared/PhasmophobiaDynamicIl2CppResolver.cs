using System;
using System.Collections.Generic;
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
            int timeoutMs = 3000)
        {
            staticBases = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
            if (processWrapper == null || classNames == null)
                return false;

            using (var task = new BlockingUnityLookupTask(processWrapper, logger))
            {
                return task.TryResolve(classNames, out staticBases, timeoutMs);
            }
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
                int timeoutMs)
            {
                var resolved = new Dictionary<string, IntPtr>(StringComparer.Ordinal);

                Run(helper =>
                {
                    foreach (string className in classNames)
                    {
                        if (string.IsNullOrWhiteSpace(className))
                            continue;

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
                    return false;
                }

                staticBases = resolved;
                return resolved.Count > 0;
            }
        }
    }
}
