using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LiveSplit.PhasmophobiaContractResolver
{
    internal enum ResolverDisplayMode
    {
        ProcessNotFound,
        AwaitingContract,
        Resolved
    }

    internal sealed class ContractResolverMemory : IDisposable
    {
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint LIST_MODULES_ALL = 0x03;
        private const int Il2CppClassStaticFieldsOffset = 0xB8;
        private const int SingletonStaticFieldOffset = 0x0;
        private const int ListItemsOffset = 0x10;
        private const int ListSizeOffset = 0x18;
        private const int Il2CppStringLengthOffset = 0x10;
        private const int Il2CppStringFirstCharOffset = 0x14;
        private const int LevelValuesDifficultyOffset = 0x38;
        private const int DifficultyChosenCursedItemsOffset = 0x98;
        private const int DifficultyActualCursedItemsOffset = 0xA8;
        private const int ArrayLengthOffset = 0x18;
        private const int ArrayDataOffset = 0x20;
        private const int LevelStatsBoneRoomOffset = 0xB8;
        private static readonly TimeSpan ProcessScanDelay = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan PointerInitRetryDelay = TimeSpan.FromSeconds(1.0);

        private const int CursedItemsControllerCurrentListOffset = 0x90;

        private Process process;
        private IntPtr processHandle = IntPtr.Zero;
        private int targetPointerSize = IntPtr.Size;
        private DateTime nextProcessScanUtc = DateTime.MinValue;
        private DateTime nextPointerInitUtc = DateTime.MinValue;

        private IntPtr levelControllerStaticAddress;
        private IntPtr cursedItemsControllerStaticAddress;
        private IntPtr levelValuesStaticAddress;
        private IntPtr levelStatsStaticAddress;
        private PhasmophobiaResolverBuildProfile activeBuildProfile;
        private string activeGameAssemblySha256;
        private string activeGameAssemblyPath;

        public ResolverDisplayMode DisplayMode { get; private set; } = ResolverDisplayMode.ProcessNotFound;
        public string SingleLineText { get; private set; } = "Trying to find process";
        public string CursedText { get; private set; } = string.Empty;
        public string BoneText { get; private set; } = string.Empty;

        public void Update()
        {
            if (!EnsureProcessAttached())
            {
                SetProcessNotFound();
                return;
            }

            if (!TryInitPointers())
            {
                SetAwaiting();
                return;
            }

            IntPtr levelController = ReadSingletonInstance(levelControllerStaticAddress);
            if (levelController == IntPtr.Zero)
            {
                SetAwaiting();
                return;
            }

            DisplayMode = ResolverDisplayMode.Resolved;
            SingleLineText = string.Empty;
            UpdateCursed();
            UpdateBone();
        }

        private bool EnsureProcessAttached()
        {
            if (process != null)
            {
                try
                {
                    if (!process.HasExited && processHandle != IntPtr.Zero)
                        return true;
                }
                catch
                {
                }
            }

            DetachProcess();

            if (DateTime.UtcNow < nextProcessScanUtc)
                return false;

            nextProcessScanUtc = DateTime.UtcNow + ProcessScanDelay;
            foreach (var candidate in Process.GetProcessesByName("Phasmophobia"))
            {
                if (candidate == null || candidate.HasExited)
                    continue;

                IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, candidate.Id);
                if (handle == IntPtr.Zero)
                    continue;

                process = candidate;
                processHandle = handle;
                if (!IsWow64Process(handle, out bool isWow64))
                    isWow64 = !Environment.Is64BitOperatingSystem;
                targetPointerSize = (Environment.Is64BitOperatingSystem && !isWow64) ? 8 : 4;
                nextPointerInitUtc = DateTime.MinValue;
                levelControllerStaticAddress = IntPtr.Zero;
                cursedItemsControllerStaticAddress = IntPtr.Zero;
                levelValuesStaticAddress = IntPtr.Zero;
                levelStatsStaticAddress = IntPtr.Zero;
                activeBuildProfile = null;
                activeGameAssemblySha256 = null;
                activeGameAssemblyPath = null;
                return true;
            }

            return false;
        }

        private bool TryInitPointers()
        {
            // LevelController is required to know "in contract" vs "awaiting".
            // Optional pointers may initialize later, so keep retrying until all are set.
            bool needsRequired = levelControllerStaticAddress == IntPtr.Zero;
            bool needsOptional =
                cursedItemsControllerStaticAddress == IntPtr.Zero ||
                levelValuesStaticAddress == IntPtr.Zero ||
                levelStatsStaticAddress == IntPtr.Zero;

            if (!needsRequired && !needsOptional)
                return true;

            if (DateTime.UtcNow < nextPointerInitUtc)
                return levelControllerStaticAddress != IntPtr.Zero;

            nextPointerInitUtc = DateTime.UtcNow + PointerInitRetryDelay;

            if (activeBuildProfile == null)
            {
                DetectBuildProfile();
                if (activeBuildProfile == null)
                    return false;
            }

            IntPtr gameAssemblyBase = GetGameAssemblyBaseAddress();
            if (gameAssemblyBase == IntPtr.Zero)
                return levelControllerStaticAddress != IntPtr.Zero;

            if (levelControllerStaticAddress == IntPtr.Zero)
                levelControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, activeBuildProfile.LevelControllerTypeInfoRva);

            if (cursedItemsControllerStaticAddress == IntPtr.Zero)
                cursedItemsControllerStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, activeBuildProfile.CursedItemsControllerTypeInfoRva);

            // Best-effort optional pointers; don't block the resolver state if these fail.
            if (levelValuesStaticAddress == IntPtr.Zero)
                levelValuesStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, activeBuildProfile.LevelValuesTypeInfoRva);

            if (levelStatsStaticAddress == IntPtr.Zero)
                levelStatsStaticAddress = ResolveSingletonPointerAddress(gameAssemblyBase, activeBuildProfile.LevelStatsTypeInfoRva);

            return levelControllerStaticAddress != IntPtr.Zero;
        }

        private void DetectBuildProfile()
        {
            string gameAssemblyPath = GetGameAssemblyFilePath();
            if (string.IsNullOrWhiteSpace(gameAssemblyPath))
                return;

            string hash = ComputeFileSha256(gameAssemblyPath);
            if (string.Equals(activeGameAssemblyPath, gameAssemblyPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(activeGameAssemblySha256, hash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            activeGameAssemblyPath = gameAssemblyPath;
            activeGameAssemblySha256 = hash;
            activeBuildProfile = PhasmophobiaResolverBuildProfiles.FindByGameAssemblySha256(hash);
        }

        private IntPtr GetGameAssemblyBaseAddress()
        {
            if (process == null || process.HasExited || processHandle == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                IntPtr[] modules = new IntPtr[1024];
                uint cb = (uint)(IntPtr.Size * modules.Length);
                if (EnumProcessModulesEx(processHandle, modules, cb, out uint bytesNeeded, LIST_MODULES_ALL))
                {
                    int moduleCount = (int)(bytesNeeded / (uint)IntPtr.Size);
                    var name = new StringBuilder(260);
                    for (int i = 0; i < moduleCount && i < modules.Length; i++)
                    {
                        name.Clear();
                        if (GetModuleBaseName(processHandle, modules[i], name, (uint)name.Capacity) == 0)
                            continue;
                        if (name.ToString().Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase))
                            return modules[i];
                    }
                }
            }
            catch
            {
            }

            // Fallback for environments where PSAPI module enumeration is restricted.
            try
            {
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName.Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase))
                        return module.BaseAddress;
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }

        private string GetGameAssemblyFilePath()
        {
            if (process == null || process.HasExited)
                return null;

            try
            {
                foreach (ProcessModule module in process.Modules)
                {
                    if (module.ModuleName.Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase))
                        return module.FileName;
                }
            }
            catch
            {
            }

            try
            {
                string exePath = process.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                    return null;

                string candidate = Path.Combine(Path.GetDirectoryName(exePath) ?? string.Empty, "GameAssembly.dll");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
            }

            return null;
        }

        private static string ComputeFileSha256(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private IntPtr ResolveSingletonPointerAddress(IntPtr gameAssemblyBase, int typeInfoRva)
        {
            IntPtr typeInfoAddress = gameAssemblyBase + typeInfoRva;
            IntPtr klass = ReadPointer(typeInfoAddress);
            if (klass == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr staticFields = ReadPointer(klass + Il2CppClassStaticFieldsOffset);
            if (staticFields == IntPtr.Zero)
                return IntPtr.Zero;

            return staticFields + SingletonStaticFieldOffset;
        }

        private IntPtr ReadSingletonInstance(IntPtr staticAddress)
        {
            if (staticAddress == IntPtr.Zero)
                return IntPtr.Zero;
            return ReadPointer(staticAddress);
        }

        private void UpdateCursed()
        {
            try
            {
                if (levelValuesStaticAddress == IntPtr.Zero)
                {
                    TryUpdateCursedFromController();
                    return;
                }

                IntPtr levelValues = ReadSingletonInstance(levelValuesStaticAddress);
                if (levelValues == IntPtr.Zero)
                {
                    TryUpdateCursedFromController();
                    return;
                }

                // In current builds there are multiple Difficulty refs on LevelValues.
                // Try both the primary and fallback offsets.
                IntPtr difficulty = ReadPointer(levelValues + LevelValuesDifficultyOffset);
                if (difficulty == IntPtr.Zero)
                    difficulty = ReadPointer(levelValues + 0x40);
                if (difficulty == IntPtr.Zero)
                {
                    TryUpdateCursedFromController();
                    return;
                }

                IntPtr cursedItemsArray = ReadPointer(difficulty + DifficultyActualCursedItemsOffset);
                if (cursedItemsArray == IntPtr.Zero)
                    cursedItemsArray = ReadPointer(difficulty + DifficultyChosenCursedItemsOffset);
                if (cursedItemsArray == IntPtr.Zero)
                {
                    TryUpdateCursedFromController();
                    return;
                }

                int count = ReadInt32(cursedItemsArray + ArrayLengthOffset);
                if (count <= 0)
                {
                    TryUpdateCursedFromController();
                    return;
                }

                string cursed = ReadFirstValidCursedFromArray(cursedItemsArray, count);
                CursedText = string.IsNullOrWhiteSpace(cursed) ? "Finding Cursed Possession" : cursed;
            }
            catch
            {
                TryUpdateCursedFromController();
            }
        }

        private void UpdateBone()
        {
            try
            {
                if (levelStatsStaticAddress == IntPtr.Zero)
                {
                    BoneText = "Finding Bone Room";
                    return;
                }

                IntPtr levelStats = ReadSingletonInstance(levelStatsStaticAddress);
                if (levelStats == IntPtr.Zero)
                {
                    BoneText = "Finding Bone Room";
                    return;
                }

                IntPtr boneRoomString = ReadPointer(levelStats + LevelStatsBoneRoomOffset);
                string room = ReadIl2CppString(boneRoomString);
                BoneText = string.IsNullOrWhiteSpace(room) ? "Finding Bone Room" : room;
            }
            catch
            {
                BoneText = "Finding Bone Room";
            }
        }

        private void TryUpdateCursedFromController()
        {
            try
            {
                if (cursedItemsControllerStaticAddress == IntPtr.Zero)
                {
                    CursedText = "Finding Cursed Possession";
                    return;
                }

                IntPtr controller = ReadSingletonInstance(cursedItemsControllerStaticAddress);
                if (controller == IntPtr.Zero)
                {
                    CursedText = "Finding Cursed Possession";
                    return;
                }

                IntPtr list = ReadPointer(controller + CursedItemsControllerCurrentListOffset);
                if (list == IntPtr.Zero)
                {
                    CursedText = "Finding Cursed Possession";
                    return;
                }

                int size = ReadInt32(list + ListSizeOffset);
                if (size <= 0)
                {
                    CursedText = "Finding Cursed Possession";
                    return;
                }

                IntPtr items = ReadPointer(list + ListItemsOffset);
                if (items == IntPtr.Zero)
                {
                    CursedText = "Finding Cursed Possession";
                    return;
                }

                string cursed = ReadFirstValidCursedFromArray(items, size);
                CursedText = string.IsNullOrWhiteSpace(cursed) ? "Finding Cursed Possession" : cursed;
            }
            catch
            {
                CursedText = "Finding Cursed Possession";
            }
        }

        private string ReadFirstValidCursedFromArray(IntPtr arrayPtr, int count)
        {
            int safeCount = Math.Max(0, Math.Min(count, 16));
            for (int i = 0; i < safeCount; i++)
            {
                int item = ReadInt32(arrayPtr + ArrayDataOffset + (i * sizeof(int)));
                string mapped = CursedPossessionEnumToDisplayName(item);
                if (!string.IsNullOrWhiteSpace(mapped))
                    return mapped;
            }
            return null;
        }

        private static string CursedPossessionEnumToDisplayName(int value)
        {
            switch (value)
            {
                case 1: return "Tarot Cards";
                case 2: return "Ouija Board";
                case 3: return "Mirror";
                case 4: return "Music Box";
                case 5: return "Summoning Circle";
                case 6: return "Voodoo Doll";
                case 7: return "Monkey Paw";
                // Fallback map for alternate enum values seen in other cursed-related enums.
                case 8: return "Music Box";
                case 9: return "Tarot Cards";
                case 10: return "Summoning Circle";
                case 11: return "Mirror";
                case 12: return "Voodoo Doll";
                case 16: return "Monkey Paw";
                default: return null;
            }
        }

        private string ReadIl2CppString(IntPtr stringPtr)
        {
            if (stringPtr == IntPtr.Zero)
                return null;

            int length = ReadInt32(stringPtr + Il2CppStringLengthOffset);
            if (length <= 0 || length > 256)
                return null;

            byte[] bytes = ReadBytes(stringPtr + Il2CppStringFirstCharOffset, length * 2);
            if (bytes == null || bytes.Length == 0)
                return null;

            string text = Encoding.Unicode.GetString(bytes);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private IntPtr ReadPointer(IntPtr address)
        {
            int size = targetPointerSize;
            byte[] data = ReadBytes(address, size);
            if (data == null || data.Length != size)
                return IntPtr.Zero;

            if (size == 8)
            {
                long ptr64 = BitConverter.ToInt64(data, 0);
                if (IntPtr.Size == 4)
                {
                    // 32-bit hosts cannot represent full 64-bit addresses safely.
                    if ((ptr64 >> 32) != 0)
                        return IntPtr.Zero;
                    return new IntPtr(unchecked((int)ptr64));
                }
                return new IntPtr(ptr64);
            }

            return new IntPtr(BitConverter.ToInt32(data, 0));
        }

        private int ReadInt32(IntPtr address)
        {
            byte[] data = ReadBytes(address, sizeof(int));
            if (data == null || data.Length != sizeof(int))
                return 0;
            return BitConverter.ToInt32(data, 0);
        }

        private byte[] ReadBytes(IntPtr address, int size)
        {
            if (processHandle == IntPtr.Zero || address == IntPtr.Zero || size <= 0)
                return null;

            byte[] buffer = new byte[size];
            if (!ReadProcessMemory(processHandle, address, buffer, size, out IntPtr bytesRead))
                return null;
            if (bytesRead.ToInt64() < size)
                return null;
            return buffer;
        }

        private void SetAwaiting()
        {
            DisplayMode = ResolverDisplayMode.AwaitingContract;
            SingleLineText = "Awaiting Contract";
            CursedText = string.Empty;
            BoneText = string.Empty;
        }

        private void SetProcessNotFound()
        {
            DisplayMode = ResolverDisplayMode.ProcessNotFound;
            SingleLineText = "Trying to find process";
            CursedText = string.Empty;
            BoneText = string.Empty;
        }

        private void DetachProcess()
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }

            process = null;
            targetPointerSize = IntPtr.Size;
            levelControllerStaticAddress = IntPtr.Zero;
            cursedItemsControllerStaticAddress = IntPtr.Zero;
            levelValuesStaticAddress = IntPtr.Zero;
            levelStatsStaticAddress = IntPtr.Zero;
        }

        public void Dispose()
        {
            DetachProcess();
            GC.SuppressFinalize(this);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModulesEx(
            IntPtr hProcess,
            [Out] IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded,
            uint dwFilterFlag);

        [DllImport("psapi.dll", CharSet = CharSet.Auto)]
        private static extern uint GetModuleBaseName(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            uint nSize);
    }
}
