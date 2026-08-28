using System;
using System.Collections.Generic;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    internal sealed class PhasmophobiaMemoryLayout
    {
        public PhasmophobiaMemoryLayout(
            int levelAreasArrayOffset,
            int levelControllerKeyOffset,
            int playerDeadPlayerOffset,
            int networkPlayerLocalPlayerOffset,
            int firstPersonControllerOffset,
            int pcMenuOffset,
            int gameControllerPrimaryFlagOffset,
            int gameControllerSecondaryFlagOffset)
        {
            LevelAreasArrayOffset = levelAreasArrayOffset;
            LevelControllerKeyOffset = levelControllerKeyOffset;
            PlayerDeadPlayerOffset = playerDeadPlayerOffset;
            NetworkPlayerLocalPlayerOffset = networkPlayerLocalPlayerOffset;
            FirstPersonControllerOffset = firstPersonControllerOffset;
            PcMenuOffset = pcMenuOffset;
            GameControllerPrimaryFlagOffset = gameControllerPrimaryFlagOffset;
            GameControllerSecondaryFlagOffset = gameControllerSecondaryFlagOffset;
        }

        public int LevelAreasArrayOffset { get; }
        public int LevelControllerKeyOffset { get; }
        public int PlayerDeadPlayerOffset { get; }
        public int NetworkPlayerLocalPlayerOffset { get; }
        public int FirstPersonControllerOffset { get; }
        public int PcMenuOffset { get; }
        public int GameControllerPrimaryFlagOffset { get; }
        public int GameControllerSecondaryFlagOffset { get; }
        public bool UsesNestedLocalPlayer => NetworkPlayerLocalPlayerOffset != 0;
    }

    internal sealed class PhasmophobiaBuildProfile
    {
        public PhasmophobiaBuildProfile(
            string gameVersion,
            string gameAssemblySha256,
            int levelControllerTypeInfoRva,
            int mapControllerTypeInfoRva,
            int cctvControllerTypeInfoRva,
            int loadingControllerTypeInfoRva,
            int mainManagerTypeInfoRva,
            int gameControllerTypeInfoRva,
            PhasmophobiaMemoryLayout memoryLayout)
        {
            GameVersion = gameVersion;
            GameAssemblySha256 = gameAssemblySha256;
            LevelControllerTypeInfoRva = levelControllerTypeInfoRva;
            MapControllerTypeInfoRva = mapControllerTypeInfoRva;
            CCTVControllerTypeInfoRva = cctvControllerTypeInfoRva;
            LoadingControllerTypeInfoRva = loadingControllerTypeInfoRva;
            MainManagerTypeInfoRva = mainManagerTypeInfoRva;
            GameControllerTypeInfoRva = gameControllerTypeInfoRva;
            MemoryLayout = memoryLayout ?? throw new ArgumentNullException(nameof(memoryLayout));
        }

        public string GameVersion { get; }
        public string GameAssemblySha256 { get; }
        public int LevelControllerTypeInfoRva { get; }
        public int MapControllerTypeInfoRva { get; }
        public int CCTVControllerTypeInfoRva { get; }
        public int LoadingControllerTypeInfoRva { get; }
        public int MainManagerTypeInfoRva { get; }
        public int GameControllerTypeInfoRva { get; }
        public PhasmophobiaMemoryLayout MemoryLayout { get; }
    }

    internal static class PhasmophobiaBuildProfiles
    {
        public static readonly PhasmophobiaMemoryLayout Legacy016Layout = new PhasmophobiaMemoryLayout(
            levelAreasArrayOffset: 0xA8,
            levelControllerKeyOffset: 0xE0,
            playerDeadPlayerOffset: 0xB8,
            networkPlayerLocalPlayerOffset: 0,
            firstPersonControllerOffset: 0x128,
            pcMenuOffset: 0x150,
            gameControllerPrimaryFlagOffset: 0xF8,
            gameControllerSecondaryFlagOffset: 0xF9);

        public static readonly PhasmophobiaMemoryLayout CurrentLayout = new PhasmophobiaMemoryLayout(
            levelAreasArrayOffset: 0xB0,
            levelControllerKeyOffset: 0xE8,
            playerDeadPlayerOffset: 0xD8,
            networkPlayerLocalPlayerOffset: 0x110,
            firstPersonControllerOffset: 0x128,
            pcMenuOffset: 0x130,
            gameControllerPrimaryFlagOffset: 0x100,
            gameControllerSecondaryFlagOffset: 0x101);

        public static readonly PhasmophobiaBuildProfile Legacy01612 = new PhasmophobiaBuildProfile(
            gameVersion: "0.16.1.2",
            gameAssemblySha256: "5B8FF13ADF4A758939B6EC7578177D3858DA2AE9CCB895E7D01C6FDA19504F60",
            levelControllerTypeInfoRva: 0x05CC4E78,
            mapControllerTypeInfoRva: 0x05CCC640,
            cctvControllerTypeInfoRva: 0x05CDC8F0,
            loadingControllerTypeInfoRva: 0x05CC8988,
            mainManagerTypeInfoRva: 0x05CCBEA8,
            gameControllerTypeInfoRva: 0x05D212F8,
            memoryLayout: Legacy016Layout);

        public static readonly PhasmophobiaBuildProfile Current = new PhasmophobiaBuildProfile(
            gameVersion: "0.19.0.0",
            gameAssemblySha256: "4CBB1C067A167B31DDAA808C3B4B9A3AEC7824F8D6863C9D2D28872244EA644B",
            levelControllerTypeInfoRva: 0x064106B8,
            mapControllerTypeInfoRva: 0x064190D8,
            cctvControllerTypeInfoRva: 0x06425EB0,
            loadingControllerTypeInfoRva: 0x064146C8,
            mainManagerTypeInfoRva: 0x06418868,
            gameControllerTypeInfoRva: 0x06471B98,
            memoryLayout: CurrentLayout);

        private static readonly Dictionary<string, PhasmophobiaBuildProfile> ProfilesByGameAssemblySha256 =
            new Dictionary<string, PhasmophobiaBuildProfile>(StringComparer.OrdinalIgnoreCase)
            {
                [Legacy01612.GameAssemblySha256] = Legacy01612,
                [Current.GameAssemblySha256] = Current,
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
