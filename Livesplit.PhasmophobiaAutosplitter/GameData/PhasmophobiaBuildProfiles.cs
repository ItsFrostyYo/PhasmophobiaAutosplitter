using System;
using System.Collections.Generic;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    internal sealed class PhasmophobiaBuildProfile
    {
        public PhasmophobiaBuildProfile(
            string gameVersion,
            string dumpFolderName,
            string gameAssemblySha256,
            int levelControllerTypeInfoRva,
            int mapControllerTypeInfoRva,
            int cctvControllerTypeInfoRva,
            int loadingControllerTypeInfoRva,
            int mainManagerTypeInfoRva,
            int gameControllerTypeInfoRva)
        {
            GameVersion = gameVersion;
            DumpFolderName = dumpFolderName;
            GameAssemblySha256 = gameAssemblySha256;
            LevelControllerTypeInfoRva = levelControllerTypeInfoRva;
            MapControllerTypeInfoRva = mapControllerTypeInfoRva;
            CCTVControllerTypeInfoRva = cctvControllerTypeInfoRva;
            LoadingControllerTypeInfoRva = loadingControllerTypeInfoRva;
            MainManagerTypeInfoRva = mainManagerTypeInfoRva;
            GameControllerTypeInfoRva = gameControllerTypeInfoRva;
        }

        public string GameVersion { get; }
        public string DumpFolderName { get; }
        public string GameAssemblySha256 { get; }
        public int LevelControllerTypeInfoRva { get; }
        public int MapControllerTypeInfoRva { get; }
        public int CCTVControllerTypeInfoRva { get; }
        public int LoadingControllerTypeInfoRva { get; }
        public int MainManagerTypeInfoRva { get; }
        public int GameControllerTypeInfoRva { get; }
    }

    internal static class PhasmophobiaBuildProfiles
    {
        public static readonly PhasmophobiaBuildProfile V0_16_0_0 = new PhasmophobiaBuildProfile(
            gameVersion: "0.16.0.0",
            dumpFolderName: "tools/phasmophobia_dump_0.16.0.0",
            gameAssemblySha256: "5DEE1A72B060F4CC64BF69E500C7A9C2DE32AD919712B542B54BA25877A47D89",
            levelControllerTypeInfoRva: 0x05D586A0,
            mapControllerTypeInfoRva: 0x05D5FE60,
            cctvControllerTypeInfoRva: 0x05D701F0,
            loadingControllerTypeInfoRva: 0x05D5C1B0,
            mainManagerTypeInfoRva: 0x05D5F6C8,
            gameControllerTypeInfoRva: 0x05DB4D20);

        public static readonly PhasmophobiaBuildProfile V0_16_1_1 = new PhasmophobiaBuildProfile(
            gameVersion: "0.16.1.1",
            dumpFolderName: "tools/phasmophobia_dump_0.16.1.1",
            gameAssemblySha256: "F4EB8A97A54D8F50F4000B4356AA4E6B74E6F528DF8808E1F92CD3928FD89D20",
            levelControllerTypeInfoRva: 0x05CC6E78,
            mapControllerTypeInfoRva: 0x05CCE640,
            cctvControllerTypeInfoRva: 0x05CDE8F0,
            loadingControllerTypeInfoRva: 0x05CCA988,
            mainManagerTypeInfoRva: 0x05CCDEA8,
            gameControllerTypeInfoRva: 0x05D232F8);

        private static readonly Dictionary<string, PhasmophobiaBuildProfile> ProfilesByGameAssemblySha256 =
            new Dictionary<string, PhasmophobiaBuildProfile>(StringComparer.OrdinalIgnoreCase)
            {
                [V0_16_0_0.GameAssemblySha256] = V0_16_0_0,
                [V0_16_1_1.GameAssemblySha256] = V0_16_1_1,
            };

        public static PhasmophobiaBuildProfile FindByGameAssemblySha256(string gameAssemblySha256)
        {
            if (string.IsNullOrWhiteSpace(gameAssemblySha256))
                return null;

            ProfilesByGameAssemblySha256.TryGetValue(gameAssemblySha256.Trim(), out PhasmophobiaBuildProfile profile);
            return profile;
        }
    }
}
