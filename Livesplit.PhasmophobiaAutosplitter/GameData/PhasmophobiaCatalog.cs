using System.Collections.Generic;

namespace LiveSplit.PhasmophobiaAutosplitter.GameData
{
    // Reference catalog for future split logic and UI features.
    public enum PhasmophobiaMap
    {
        Random,
        Training,
        MainLobby,
        TanglewoodStreetHouse,
        EdgefieldStreetHouse,
        RidgeviewRoadHouse,
        GraftonFarmhouse,
        BleasdaleFarmhouse,
        WillowStreetHouse,
        BrownstoneHighSchool,
        Prison,
        MapleLodgeCampsite,
        CampWoodwind,
        SunnyMeadows,
        SunnyMeadowsRestricted,
        PointHope,
        NellsDiner
    }

    public enum GhostType
    {
        Spirit,
        Wraith,
        Phantom,
        Poltergeist,
        Banshee,
        Jinn,
        Mare,
        Revenant,
        Shade,
        Demon,
        Yurei,
        Oni,
        Yokai,
        Hantu,
        Goryo,
        Myling,
        Onryo,
        TheTwins,
        Raiju,
        Obake,
        TheMimic,
        Moroi,
        Deogen,
        Thaye,
        None,
        Gallu,
        Dayan,
        Obambo
    }

    public enum EquipmentType
    {
        Crucifix,
        DotsProjector,
        EmfReader,
        Firelight,
        Flashlight,
        GhostWritingBook,
        HeadMountedCamera,
        Igniter,
        Incense,
        MotionSensor,
        ParabolicMicrophone,
        PhotoCamera,
        Salt,
        SanityMedication,
        SoundRecorder,
        SoundSensor,
        SpiritBox,
        Thermometer,
        Tripod,
        UvLight,
        VideoCamera
    }

    public static class PhasmophobiaCatalog
    {
        public static readonly IReadOnlyDictionary<PhasmophobiaMap, IReadOnlyList<string>> MapLocations =
            new Dictionary<PhasmophobiaMap, IReadOnlyList<string>>
            {
                [PhasmophobiaMap.MainLobby] = new[]
                {
                    "Main Lobby",
                    "Equipment Shop",
                    "Loadout Board",
                    "Contract Board",
                    "Truck Setup Area"
                },
                [PhasmophobiaMap.Random] = new[]
                {
                    "Random Contract Target"
                },
                [PhasmophobiaMap.Training] = new[]
                {
                    "Training"
                },
                [PhasmophobiaMap.TanglewoodStreetHouse] = new[]
                {
                    "Foyer",
                    "Living Room",
                    "Dining Room",
                    "Kitchen",
                    "Utility",
                    "Garage",
                    "Basement",
                    "Hallway",
                    "Nursery",
                    "Master Bedroom",
                    "Boys Bedroom",
                    "Girls Bedroom",
                    "Bathroom"
                },
                [PhasmophobiaMap.EdgefieldStreetHouse] = new[]
                {
                    "Foyer",
                    "Hallway",
                    "Living Room",
                    "Dining Room",
                    "Kitchen",
                    "Utility",
                    "Garage",
                    "Storage Room",
                    "Basement",
                    "Basement Hallway",
                    "Upstairs Hallway",
                    "Master Bedroom",
                    "Blue Bedroom",
                    "Orange Bedroom",
                    "Pink Bedroom",
                    "Bathroom"
                },
                [PhasmophobiaMap.RidgeviewRoadHouse] = new[]
                {
                    "Foyer",
                    "Living Room",
                    "Dining Room",
                    "Kitchen",
                    "Utility",
                    "Garage",
                    "Basement",
                    "Basement Hallway",
                    "Basement Utility",
                    "Upstairs Hallway",
                    "Master Bedroom",
                    "Blue Bedroom",
                    "Green Bedroom",
                    "Teen Bedroom",
                    "Bathroom"
                },
                [PhasmophobiaMap.GraftonFarmhouse] = new[]
                {
                    "Foyer",
                    "Living Room",
                    "Dining Room",
                    "Kitchen",
                    "Storage",
                    "Utility",
                    "Workshop",
                    "Master Bedroom",
                    "Nursery",
                    "Twin Bedroom",
                    "Boys Bedroom",
                    "Bathroom",
                    "Attic",
                    "Hallway"
                },
                [PhasmophobiaMap.BleasdaleFarmhouse] = new[]
                {
                    "Entrance",
                    "Living Room",
                    "Dining Room",
                    "Kitchen",
                    "Office",
                    "Pantry",
                    "Utility",
                    "Storage",
                    "Boys Bedroom",
                    "Girls Bedroom",
                    "Master Bedroom",
                    "Bathroom",
                    "Attic",
                    "Hallway"
                },
                [PhasmophobiaMap.WillowStreetHouse] = new[]
                {
                    "Foyer",
                    "Living Room",
                    "Kitchen",
                    "Garage",
                    "Basement",
                    "Basement Hallway",
                    "Master Bedroom",
                    "Kids Bedroom",
                    "Blue Bedroom",
                    "Bathroom",
                    "Hallway"
                },
                [PhasmophobiaMap.BrownstoneHighSchool] = new[]
                {
                    "Front Hallway",
                    "Back Hallway",
                    "North Corridor",
                    "South Corridor",
                    "Classroom",
                    "Library",
                    "Cafeteria",
                    "Kitchen",
                    "Gym",
                    "Locker Room",
                    "Storage",
                    "Basement"
                },
                [PhasmophobiaMap.Prison] = new[]
                {
                    "Entrance",
                    "Reception",
                    "Central Hall",
                    "Cell Block A",
                    "Cell Block B",
                    "Infirmary",
                    "Security",
                    "Cafeteria",
                    "Kitchen",
                    "Library",
                    "Warden Office",
                    "Visitation",
                    "Workshop"
                },
                [PhasmophobiaMap.MapleLodgeCampsite] = new[]
                {
                    "Camp Entrance",
                    "Picnic Area",
                    "Campsite",
                    "Main Tent",
                    "Blue Tent",
                    "White Tent",
                    "Cabin",
                    "Toilets",
                    "Shower Block",
                    "Games Tent",
                    "Campfire",
                    "Wood Path"
                },
                [PhasmophobiaMap.CampWoodwind] = new[]
                {
                    "Camp Entrance",
                    "Campfire",
                    "Tent Area",
                    "Bathroom",
                    "Picnic Tables",
                    "Pathways"
                },
                [PhasmophobiaMap.SunnyMeadows] = new[]
                {
                    "Reception",
                    "Courtyard",
                    "Chapel",
                    "Female Wing",
                    "Male Wing",
                    "East Wing",
                    "West Wing",
                    "Patient Rooms",
                    "Treatment",
                    "Day Room",
                    "Kitchen",
                    "Laundry",
                    "Basement"
                },
                [PhasmophobiaMap.SunnyMeadowsRestricted] = new[]
                {
                    "Reception",
                    "Courtyard",
                    "Chapel",
                    "Selected Wing Rooms",
                    "Treatment",
                    "Day Room",
                    "Basement"
                },
                [PhasmophobiaMap.PointHope] = new[]
                {
                    "Ground Floor",
                    "Entry House",
                    "Kitchen",
                    "Dining Area",
                    "Dining Room",
                    "Games Room",
                    "Office",
                    "Downstairs Bathroom",
                    "Bedroom",
                    "Bedroom One",
                    "Bedroom Two",
                    "4th Floor Hallway",
                    "5th Floor Hallway",
                    "6th Floor Hallway",
                    "Maintenance Room",
                    "Lantern Room",
                    "Lighthouse Lower Stairs",
                    "Lighthouse Mid Stairs",
                    "Lighthouse Upper Stairs",
                    "Lighthouse Top"
                },
                [PhasmophobiaMap.NellsDiner] = new[]
                {
                    "Entrance",
                    "Foyer",
                    "Hallway",
                    "Dining Area",
                    "Dining Floor",
                    "Counter Area",
                    "Kitchen",
                    "Break Room",
                    "Cooler Room",
                    "Manager's Office",
                    "Staff Hallway",
                    "Storage",
                    "Staff Bathroom",
                    "Men's Bathroom",
                    "Women's Bathroom",
                    "Bathroom",
                    "Counter",
                    "Outside Parking"
                }
            };
    }
}
