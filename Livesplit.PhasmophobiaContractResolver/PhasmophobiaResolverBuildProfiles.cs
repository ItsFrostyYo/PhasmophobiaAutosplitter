using System;
using System.Collections.Generic;

namespace LiveSplit.PhasmophobiaContractResolver
{
    internal sealed class PhasmophobiaResolverBuildProfile
    {
        public PhasmophobiaResolverBuildProfile(
            string gameVersion,
            string dumpFolderName,
            string gameAssemblySha256,
            int levelControllerTypeInfoRva,
            int cursedItemsControllerTypeInfoRva,
            int levelValuesTypeInfoRva,
            int levelStatsTypeInfoRva)
        {
            GameVersion = gameVersion;
            DumpFolderName = dumpFolderName;
            GameAssemblySha256 = gameAssemblySha256;
            LevelControllerTypeInfoRva = levelControllerTypeInfoRva;
            CursedItemsControllerTypeInfoRva = cursedItemsControllerTypeInfoRva;
            LevelValuesTypeInfoRva = levelValuesTypeInfoRva;
            LevelStatsTypeInfoRva = levelStatsTypeInfoRva;
        }

        public string GameVersion { get; }
        public string DumpFolderName { get; }
        public string GameAssemblySha256 { get; }
        public int LevelControllerTypeInfoRva { get; }
        public int CursedItemsControllerTypeInfoRva { get; }
        public int LevelValuesTypeInfoRva { get; }
        public int LevelStatsTypeInfoRva { get; }
    }

    internal static class PhasmophobiaResolverBuildProfiles
    {
        public static readonly PhasmophobiaResolverBuildProfile V0_16_0_0 = new PhasmophobiaResolverBuildProfile(
            gameVersion: "0.16.0.0",
            dumpFolderName: "tools/phasmophobia_dump_0.16.0.0",
            gameAssemblySha256: "5DEE1A72B060F4CC64BF69E500C7A9C2DE32AD919712B542B54BA25877A47D89",
            levelControllerTypeInfoRva: 0x05D586A0,
            cursedItemsControllerTypeInfoRva: 0x05D837E8,
            levelValuesTypeInfoRva: 0x05D58928,
            levelStatsTypeInfoRva: 0x05D58850);

        public static readonly PhasmophobiaResolverBuildProfile V0_16_1_1 = new PhasmophobiaResolverBuildProfile(
            gameVersion: "0.16.1.1",
            dumpFolderName: "tools/phasmophobia_dump_0.16.1.1",
            gameAssemblySha256: "F4EB8A97A54D8F50F4000B4356AA4E6B74E6F528DF8808E1F92CD3928FD89D20",
            levelControllerTypeInfoRva: 0x05CC6E78,
            cursedItemsControllerTypeInfoRva: 0x05CF1EE0,
            levelValuesTypeInfoRva: 0x05CC7100,
            levelStatsTypeInfoRva: 0x05CC7028);

        private static readonly Dictionary<string, PhasmophobiaResolverBuildProfile> ProfilesByGameAssemblySha256 =
            new Dictionary<string, PhasmophobiaResolverBuildProfile>(StringComparer.OrdinalIgnoreCase)
            {
                [V0_16_0_0.GameAssemblySha256] = V0_16_0_0,
                [V0_16_1_1.GameAssemblySha256] = V0_16_1_1,
            };

        public static PhasmophobiaResolverBuildProfile FindByGameAssemblySha256(string gameAssemblySha256)
        {
            if (string.IsNullOrWhiteSpace(gameAssemblySha256))
                return null;

            ProfilesByGameAssemblySha256.TryGetValue(gameAssemblySha256.Trim(), out PhasmophobiaResolverBuildProfile profile);
            return profile;
        }
    }
}
