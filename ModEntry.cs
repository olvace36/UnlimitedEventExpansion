
using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;
using UnlimitedEventExpansion.Data;

namespace UnlimitedEventExpansion
{
    /// <summary>The mod entry point.</summary>
    public class ModApi : IUnlimitedEventExpansionApi
    {
        private readonly IMonitor monitor;

        public ModApi(IMonitor monitor)
        {
            this.monitor = monitor;
        }

        public void SendNpcConversationSummary(Dictionary<string, string> npcConversationSummary)
        {
            monitor.Log("SendNpcConversationSummary was called", LogLevel.Info);
            ModEntry.npcConversationSummary = npcConversationSummary;
        }

        public void TriggerDinnerEvent(string npcName)
        {
            monitor.Log("TriggerDinnerEvent was called", LogLevel.Info);
            ModEntry.TriggerDinnerEvent(npcName);
        }

        public void TriggerNpcBirthdayEvent(string npcName)
        {
            monitor.Log("TriggerNpcBirthdayEvent was called", LogLevel.Info);
            ModEntry.TriggerNpcBirthdayEvent(npcName);
        }

        public void TriggerPicnicEvent(string npcName)
        {
            monitor.Log("TriggerPicnicEvent was called", LogLevel.Info);
            ModEntry.TriggerPicnicEvent(npcName);
        }

        public void TriggerCampingEvent(string npcName)
        {
            monitor.Log("TriggerCampingEvent was called", LogLevel.Info);
            ModEntry.TriggerCampingEvent(npcName);
        }
    }

    public partial class ModEntry : Mod
    {

        public static ModEntry Instance;

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        private static ISmartPhoneApi iSmartPhoneApi;


        public static ModConfig Config;


        public static JToken eventString;

        private ModApi apiInstance;

        public override object GetApi()
        {
            return this.apiInstance ??= new ModApi(Monitor);
        }

        public static string k1 = "sk-proj-LXv5saIeMLvwBMef4eSpJCqtzOmvWW6SVqt2MyWO5eXckyQOjeH2rcCktA8jNutZltPLnW71K-";
        public static string k2 = "T3BlbkFJQ6DlEYZ_2vaSshQ--V-HP1V6sxnjJ_OohH1MNzHngRH-";
        public static string k3 = "02GEkVUwiBLAQEJuKL88cRActaXbUA";

        public static string dinnerEventModel = "gpt-5.1";
        public static string birthdayEventModel = "gpt-5.1";
        public static string picnicEventModel = "gpt-5.1";
        public static string campfireEventModel = "gpt-5.1";

        public static string birthdayGiftName = "";

        public static int totalSkippedEvent = 0;


        public static Dictionary<(string season, int day), List<NPC>> NpcBirthdaysByDate = new();
        public static Dictionary<string, string> NpcCharacteristics = new();
        public static Dictionary<string, string> NpcPortrait = new();

        public static Dictionary<string, string> npcConversationSummary = new();


        public static Dictionary<string, BirthdayMapData> birthdayMap;
        public static Dictionary<string, List<string>> npcAges;
        public static Dictionary<string, string> npcToAgeGroup;

        public static List<Item> cookingItems = new List<Item>();
        public static List<string> furnitureItems = new List<string>
        {
            "(F)1296",
            "(F)1307",
            "(F)1376",
            "(F)1377",
            "(F)1378",
            "(F)1379",
            "(F)1380",
            "(F)1381",
            "(F)1382",
            "(F)1383",
            "(F)1384",
            "(F)1385",
            "(F)1386",
            "(F)1387",
            "(F)1388",
            "(F)1389",
            "(F)1390",
            "(F)1747",
            "(F)1760",
            "(F)1761",
            "(F)1762",
            "(F)1763",
            "(F)FancyHousePlant1",
            "(F)FancyHousePlant2",
            "(F)FancyHousePlant3",
            "(F)FancyHousePlant4",
            "(F)FancyHousePlant5",

            "(F)BlueSleepingJunimo",
            "(F)GraySleepingJunimo",
            "(F)GreenSleepingJunimo",
            "(F)OrangeSleepingJunimo",
            "(F)PurpleSleepingJunimo",
            "(F)RedSleepingJunimo",
            "(F)YellowSleepingJunimo",

            "(F)RetroPlant",
            "(F)TallHousePlant"
        };


        public static Dictionary<string, PicnicMapData> picnicMap = new Dictionary<string, PicnicMapData>
        {
            {
                "Beach", new PicnicMapData
                {
                    position_tile = new int[] { 39, 19 },
                    player_tile = new int[] { 40, 21, 2 },
                    npc_tile = new int[] { 42, 21, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 38, 19 },
                        new int[] { 38, 21 },
                        new int[] { 36, 23 },
                        new int[] { 39, 18 },
                        new int[] { 40, 17 },
                        new int[] { 42, 18 },
                        new int[] { 44, 20 },
                        new int[] { 44, 22 },
                    }
                }
            },
            {
                "Town", new PicnicMapData
                {
                    position_tile = new int[] { 23, 97 },
                    player_tile = new int[] { 24, 99, 2 },
                    npc_tile = new int[] { 26, 99, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 21, 98 },
                        new int[] { 22, 97 },
                        new int[] { 24, 96 },
                        new int[] { 28, 96 },
                        new int[] { 29, 98 },
                        new int[] { 28, 100 },
                    }
                }
            },
            {
                "Forest", new PicnicMapData
                {
                    position_tile = new int[] { 69, 47 },
                    player_tile = new int[] { 72, 49, 2 },
                    npc_tile = new int[] { 70, 49, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 67, 49 },
                        new int[] { 68, 50 },
                        new int[] { 75, 47 },
                        new int[] { 75, 49 },
                        new int[] { 73, 45 },
                        new int[] { 71, 46 },
                        new int[] { 68, 47 },
                        new int[] { 69, 46 },
                    }
                }
            },
            {
                "Mountain", new PicnicMapData
                {
                    position_tile = new int[] { 44, 31 },
                    player_tile = new int[] { 47, 33, 2 },
                    npc_tile = new int[] { 45, 33, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 46, 36 },
                        new int[] { 42, 31 },
                        new int[] { 42, 33 },
                        new int[] { 48, 30 },
                        new int[] { 49, 36 },
                    }
                }
            },
            {
                "Custom_Ridgeside_RSVCliff",  new PicnicMapData
                {
                    position_tile = new int[] { 72, 20 },
                    player_tile = new int[] { 73, 22, 2 },
                    npc_tile = new int[] { 75, 22, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 70, 21 },
                        new int[] { 69, 24 },
                        new int[] { 78, 24 },
                        new int[] { 77, 21 },
                        new int[] { 79, 23 },
                    }
                }
            },
            {
                "Custom_BlueMoonVineyard", new PicnicMapData
                {
                    position_tile = new int[] { 26, 53 },
                    player_tile = new int[] { 27, 55, 2 },
                    npc_tile = new int[] { 29, 55, 2 },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 23, 54 },
                        new int[] { 33, 54 },
                        new int[] { 29, 52 },
                        new int[] { 27, 52 },
                    }
                }
            }
        };


        public static Dictionary<string, CampfireMapData> campfireMap = new Dictionary<string, CampfireMapData>
        {
            {
                "Beach", new CampfireMapData
                {
                    campfire_tile = new int[] {28, 8},
                    player_tile = new int[] { 27, 6, 2 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 30, 7, 3 },
                        new int[] { 28, 11, 0 },
                        new int[] { 25, 9, 1 },
                        new int[] { 26, 7, 1 },
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 25, 6 },
                        new int[] { 32, 9 },
                        new int[] { 29, 10},
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 25, 7 },
                        new int[] { 34, 7 },
                        new int[] { 31, 13 },
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 23, 5 },
                        new int[] { 31, 5 },
                        new int[] { 32, 6 },
                        new int[] { 23, 8 },
                    }
                }
            },
            {
                "Town", new CampfireMapData
                {
                    campfire_tile = new int[] {106, 65},
                    player_tile = new int[] { 109, 65, 3 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 107, 62, 2},
                        new int[] { 105, 63, 2 },
                        new int[] { 107, 68, 0 }
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 104, 63 },
                        new int[] { 105, 67 },
                        new int[] { 110, 66 },
                        new int[] { 111, 64},
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 111, 66 },
                        new int[] { 106, 60 },
                        new int[] { 102, 67 }
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 103, 60 },
                        new int[] { 111, 62 },
                        new int[] { 110, 61 },
                        new int[] { 104, 67 },
                    }
                }
            },
            {
                "Forest", new CampfireMapData
                {
                    campfire_tile = new int[] {70, 47},
                    player_tile = new int[] { 66, 47, 1 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 69, 44, 2},
                        new int[] { 72, 46, 3 },
                        new int[] { 73, 48, 3 },
                        new int[] { 68, 49, 1 },
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 65, 48 },
                        new int[] { 67, 47 },
                        new int[] { 75, 47 },
                        new int[] { 111, 64},
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 65, 44 },
                        new int[] { 72, 42 },
                        new int[] { 75, 49 },
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 75, 46 },
                        new int[] { 73, 43 },
                        new int[] { 71, 44 },
                        new int[] { 68, 45 },
                    }
                }
            },
            {
                "Mountain", new CampfireMapData
                {
                    campfire_tile = new int[] { 57, 22 },
                    player_tile = new int[] { 57, 20, 2 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 59, 21, 3},
                        new int[] { 58, 24, 0 },
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 56, 18 },
                        new int[] { 60, 22 },
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 68, 24 },
                        new int[] { 55, 17 },
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 60, 19 },
                        new int[] { 58, 25 },
                    }
                }
            },
            {
                "Custom_Ridgeside_RidgesideVillage",  new CampfireMapData
                {
                    campfire_tile = new int[] { 126, 129 },
                    player_tile = new int[] { 123, 129, 1 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 127, 132, 0},
                        new int[] { 125, 127, 2 },
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 122, 128 },
                        new int[] { 124, 132 },
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 130, 128 },
                        new int[] { 131, 129 },
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 122, 126 },
                        new int[] { 120, 131 },
                        new int[] { 120, 127 },
                        new int[] { 129, 132 },
                        new int[] { 130, 132 },
                        new int[] { 131, 132 },
                    }
                }
            },
            {
                "Custom_ShearwaterBridge", new CampfireMapData
                {
                    campfire_tile = new int[] { 7, 15 },
                    player_tile = new int[] { 6, 12, 2 },
                    npc_tiles = new List<int[]>
                    {
                        new int[] { 11, 14, 3},
                        new int[] { 10, 16, 3 },
                        new int[] { 3, 15, 1 },
                    },
                    chair_tiles = new List<int[]>
                    {
                        new int[] { 5, 12 },
                        new int[] { 10, 11 },
                        new int[] { 8, 18 },
                    },
                    log_tiles = new List<int[]>
                    {
                        new int[] { 2, 18 },
                        new int[] { 3, 17 },
                    },
                    decorable_tiles = new List<int[]>
                    {
                        new int[] { 2, 16 },
                        new int[] { 11, 17 },
                        new int[] { 13, 14 },
                        new int[] { 4, 11 },
                        new int[] { 9, 9 },
                    }
                }
            }
        };



        public class BirthdayMapData
        {
            public string event_map { get; set; }
            public int[] host_tile { get; set; }
            public int[] player_tile { get; set; }
            public List<int[]> npc_tiles { get; set; }
            public List<int[]> food_tiles { get; set; }
            public List<int[]> decorable_tiles { get; set; }
            public List<int[]> gift_tiles { get; set; }
            public string[] required_npc { get; set; }
        }

        public class PicnicMapData
        {
            public int[] player_tile { get; set; }
            public int[] npc_tile { get; set; }
            public int[] position_tile { get; set; }
            public List<int[]> decorable_tiles { get; set; }
        }

        public class CampfireMapData
        {
            public int[] campfire_tile { get; set; }
            public int[] player_tile { get; set; }
            public List<int[]> npc_tiles { get; set; }
            public List<int[]> chair_tiles { get; set; }
            public List<int[]> log_tiles { get; set; }
            public List<int[]> decorable_tiles { get; set; }

        }


        public class Conversation
        {
            public string Music { get; set; } = "winter1";
            public List<DialogueEntry> Dialogue { get; set; } = new();
        }

        public class DialogueEntry
        {
            public string Type { get; set; }
            public string Dialogue { get; set; }
            public string Portrait { get; set; }
            public List<PlayerResponse> Player { get; set; } = new();
        }

        public class PlayerResponse
        {
            public string Response { get; set; }
            public string Reaction { get; set; }
            public string Portrait { get; set; }
        }


        public class Conversation1
        {
            public string Music { get; set; } = "winter1";
            public string Npc { get; set; }
            public List<DialogueEntry1> Dialogue { get; set; } = new();
        }

        public class DialogueEntry1
        {
            public string Type { get; set; }
            public string Npc { get; set; }
            public string Dialogue { get; set; }
            public string Portrait { get; set; }
            public List<PlayerResponse1> Player { get; set; } = new();
        }

        public class PlayerResponse1
        {
            public string Response { get; set; }
            public string Reaction { get; set; }
            public string Portrait { get; set; }
        }



        public static List<NPC> GetNpcsWithBirthdayToday()
        {
            int today = Game1.dayOfMonth;
            string season = Game1.currentSeason;

            return NpcBirthdaysByDate.TryGetValue((season, today), out var list)
                ? list
                : new List<NPC>();
        }

        public static BirthdayMapData GetBirthdayMapDataForNPC(NPC npc)
        {
            string mapKey = npc.DefaultMap; // Or any custom logic
            if (birthdayMap.TryGetValue(mapKey, out var data))
            {
                return data;
            }

            List<string> fallbackOptions = new List<string> { "Saloon" };

            if (Game1.MasterPlayer.hasCompletedCommunityCenter())
                fallbackOptions.Add("CommunityCenter");

            string chosen = fallbackOptions[Game1.random.Next(fallbackOptions.Count)];

            return birthdayMap[chosen];
        }

        private static void Shuffle<T>(List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }
        }

        public static Item GetRandomEatingItemInPriceRange(int minPrice, int maxPrice)
        {
            var random = new Random();
            var filteredItems = cookingItems
                .Where(item => item.salePrice() >= minPrice && item.salePrice() <= maxPrice)
                .ToList();

            if (filteredItems.Count == 0)
                return null;

            int index = random.Next(filteredItems.Count);
            return filteredItems[index];
        }




    }



}