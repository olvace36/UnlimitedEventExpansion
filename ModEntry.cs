
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
using UnlimitedEventExpansion;

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
            ModEntry.npcConversationSummary = npcConversationSummary;
        }

        public void OpenScheduleEventTimeMenu(string eventNpcName, string eventType, string? npcResponseTemplate)
        {
            ModEntry.TryOpenScheduleEventTimeMenu(eventNpcName, eventType, npcResponseTemplate);
        }

        public bool CanScheduleNewEvent()
        {
            if (string.IsNullOrWhiteSpace(ModEntry.Config.Key))
            {
                return ModEntry.TotalEventRegisteredToday < ModEntry.DailyEventLimit;
            }
            return true;
        }

    }

    public partial class ModEntry : Mod
    {

        public static ModEntry Instance;

        public static IMonitor SMonitor;
        public static ModConfig Config;
        public static IModHelper SHelper;
        public static IAppMessengerApi iAppMessengerApi;
        public static ISmartphoneApi iSmartphoneApi;
        public sealed class ScheduledUnlimitedEvent
        {
            public string NpcName { get; set; } = string.Empty;
            public string EventType { get; set; } = string.Empty;
            public string TimeOfDay { get; set; } = string.Empty;
            public string? LocationName { get; set; }
            public List<string> ParticipantNames { get; set; } = new();
        }

        public static List<ScheduledUnlimitedEvent> PendingUnlimitedEvents = new();
        public static int TotalEventRegisteredToday = 0;


        // public static JToken eventString;

        private ModApi apiInstance;

        public override object GetApi()
        {
            return this.apiInstance ??= new ModApi(Monitor);
        }


        public static string birthdayGiftName = "";

        public static int totalSkippedEvent = 0;

        public static int DailyEventLimit = 4;


        private static readonly object queuedEventStartsLock = new();
        public static List<QueuedEventStart> queuedEventStarts = new();

        public class QueuedEventStart
        {
            public Event Event { get; }
            public string LocationName { get; }
            public int WarpX { get; }
            public int WarpY { get; }
            public int FacingDirection { get; }

            public QueuedEventStart(Event generatedEvent, string locationName, int warpX, int warpY, int facingDirection)
            {
                Event = generatedEvent;
                LocationName = locationName;
                WarpX = warpX;
                WarpY = warpY;
                FacingDirection = facingDirection;
            }
        }

        public static void QueueGeneratedEvent(Event generatedEvent, string locationName, int warpX, int warpY, int facingDirection)
        {
            if (generatedEvent == null || string.IsNullOrWhiteSpace(locationName))
                return;

            lock (queuedEventStartsLock)
            {
                queuedEventStarts.Add(new QueuedEventStart(generatedEvent, locationName, warpX, warpY, facingDirection));
            }
        }

        public static bool TryPeekGeneratedEvent(out QueuedEventStart queuedEvent)
        {
            lock (queuedEventStartsLock)
            {
                if (queuedEventStarts.Count == 0)
                {
                    queuedEvent = null;
                    return false;
                }

                queuedEvent = queuedEventStarts[0];
                return true;
            }
        }

        public static bool TryDequeueGeneratedEvent(QueuedEventStart queuedEvent)
        {
            if (queuedEvent == null)
                return false;

            lock (queuedEventStartsLock)
            {
                if (queuedEventStarts.Count == 0 || !ReferenceEquals(queuedEventStarts[0], queuedEvent))
                    return false;

                queuedEventStarts.RemoveAt(0);
                return true;
            }
        }

        public static void ClearGeneratedEventQueue()
        {
            lock (queuedEventStartsLock)
            {
                queuedEventStarts.Clear();
            }
        }

        public static Dictionary<string, string> NpcCharacteristicsLong = new();
        public static Dictionary<string, string> NpcCharacteristicsShort = new();
        public static Dictionary<string, string> NpcCharacteristicsMinimal = new();
        public static Dictionary<string, string> NpcPortrait = new();

        public static Dictionary<string, string> npcConversationSummary = new();


        public static Dictionary<string, BirthdayMapData> birthdayMap;
        public static Dictionary<string, List<string>> npcAges;

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

        public static List<string> socialNpcBlacklist = new List<string>
        {
            "Leo",
            "Krobus",
            "Dwarf",
            "Gunther",
            "Birdie",
            "Bouncer",
            "MoonSBV",
            "PanSBV",
            "RaccoonSBV",
            "Leximonster",
            "Dianna",
            "Torts"
        };

        public static Dictionary<string, PicnicMapData> picnicMap;


        public static Dictionary<string, CampfireMapData> campfireMap;



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


        public class SingleConversation
        {
            public string Music { get; set; } = "winter1";
            public List<SingleDialogueEntry> Dialogue { get; set; } = new();
        }

        public class SingleDialogueEntry
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


        public class MultipleConversation
        {
            public string Music { get; set; } = "winter1";
            public List<MultipleDialogueEntry> Dialogue { get; set; } = new();
        }

        public class MultipleDialogueEntry
        {
            public string Type { get; set; }
            public string Npc { get; set; }
            public string Dialogue { get; set; }
            public string Portrait { get; set; }
            public List<PlayerResponse> Player { get; set; } = new();
        }

        public static BirthdayMapData GetBirthdayMapDataForNPC(NPC? npc)
        {
            if (npc != null)
            {
                string mapKey = npc.DefaultMap;
                if (birthdayMap.TryGetValue(mapKey, out var data))
                {
                    return data;
                }
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

        public static void CheckTodayPlayerBirthday()
        {
            Game1.player.modData.TryGetValue("d5a1lamdtd.Smartphone-AppMessenger.BirthDate", out string playerBirthDate);
            Game1.player.modData.TryGetValue("d5a1lamdtd.Smartphone-AppMessenger.BirthSeason", out string playerBirthSeason);
            if (string.IsNullOrWhiteSpace(playerBirthDate) || string.IsNullOrWhiteSpace(playerBirthSeason))
            {
                return;
            }

            if (playerBirthDate == Game1.dayOfMonth.ToString() && string.Equals(playerBirthSeason, Game1.currentSeason, StringComparison.OrdinalIgnoreCase))
            {
                Game1.activeClickableMenu = new ConfirmationDialog(
                    $"It is your birthday today. Want to celebrate it?",
                    onConfirm: (Farmer who) =>
                    {
                        Game1.activeClickableMenu = null;
                        TryOpenSchedulePlayerBirthdayMenu();
                    },
                    onCancel: (Farmer who) =>
                    {
                        Game1.activeClickableMenu = null;
                    }
                );
            }
        }
    }



}