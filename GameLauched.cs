using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework;
using System.Reflection;
using static StardewValley.Minigames.MineCart;
using System.Runtime.CompilerServices;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.Minigames;
using StardewValley.GameData.Characters;
using StardewValley.Objects;
using StardewValley.Menus;
using StardewValley.Triggers;
using StardewValley.Delegates;
using StardewValley.Locations;
using xTile.Dimensions;
using StardewValley.GameData.Crops;
using xTile.Tiles;
using StardewValley.Characters;
using Newtonsoft.Json;
using UnlimitedEventExpansion.Data;



namespace UnlimitedEventExpansion
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        //
        // *************************** ENTRY ***************************
        //


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            ModEntry.Instance = this;

            SMonitor = Monitor;
            SHelper = helper;
            helper.Events.GameLoop.GameLaunched += OnGameLauched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;


            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper", PlayerWarpper);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.setupCamp", setupCamp);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.endCamp", endCamp);
            TriggerActionManager.RegisterAction("d5a1lamdtd.UnlimitedEventExpansion.campfireSkipWarpper", campfireSkipWarpper);
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool PlayerWarpper(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[2], out int x) || !int.TryParse(args[3], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }


            DelayedAction.functionAfterDelay(() =>
            {
                Game1.warpFarmer(locationName, x, y, false);
            }, 1500);

            totalSkippedEvent += 1;
            if (totalSkippedEvent >= 3){
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^You have skipped too many events. You will no longer able to get new event during this session.^^It is sad!!!^HaPyke.");
                iSmartPhoneApi.SendSmartphoneMessageFromNPC(args[4], "I am very unhappy that you left at the middle of our conversation. It is very rude of you!");
            }
            else if (totalSkippedEvent >= 1)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^Please note that each event cost $real-world USD$ to create. If you don’t wish to participate in an event, PLEASE <<cancel the invitation.<<^^" +
                    "]] Continuing to skip events may temporary prevent you from getting new events.^^Thank you for your understanding, HaPyke +++");
                iSmartPhoneApi.SendSmartphoneMessageFromNPC(args[4], "You dissappeared at the middle of our conversation. Please let me know next time so I don't have to look for you around.");
            }

            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool setupCamp(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }




            var campfire = new Torch("146", true);
            campfire.IsOn = true;
            if (!Game1.currentLocation.objects.ContainsKey(new Vector2(x, y)))
            {
                Game1.currentLocation.objects.Add(new Vector2(x, y), campfire);
                campfire.initializeLightSource(new Vector2(x, y));
            }
            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool endCamp(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }




            if (Game1.currentLocation.objects.TryGetValue(new Vector2(x, y), out StardewValley.Object obj))
            {
                if (obj is Torch torch && obj.ParentSheetIndex == 146)
                {
                    torch.performRemoveAction();
                    Game1.currentLocation.objects.Remove(new Vector2(x, y));

                }
            }

            return true;
        }

        /// <inheritdoc cref="TriggerActionDelegate" />
        public static bool campfireSkipWarpper(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            if (args.Length < 3)
            {
                error = "Expected arguments: locationName, x, y";
                return false;
            }

            string locationName = args[1];
            if (!int.TryParse(args[2], out int x) || !int.TryParse(args[3], out int y) || !int.TryParse(args[4], out int x1) || !int.TryParse(args[5], out int y1))
            {
                error = "Invalid coordinates. x and y must be integers.";
                return false;
            }

            if (Game1.currentLocation.objects.TryGetValue(new Vector2(x1, y1), out StardewValley.Object obj))
            {
                if (obj is Torch torch && obj.ParentSheetIndex == 146)
                {
                    // Properly clean up
                    torch.performRemoveAction();
                    Game1.currentLocation.objects.Remove(new Vector2(x1, y1));

                }
            }

            DelayedAction.functionAfterDelay(() =>
            {
                Game1.warpFarmer(locationName, x, y, false);
            }, 1500);


            totalSkippedEvent += 1;
            if (totalSkippedEvent >= 3)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^You have skipped too many events. You will no longer able to get new event during this session.^^It is sad!!!^HaPyke.");
                iSmartPhoneApi.SendSmartphoneMessageFromNPC(args[4], "I am very unhappy that you left at the middle of our conversation. It is very rude of you!");
            }
            else if (totalSkippedEvent >= 1)
            {
                Game1.drawLetterMessage("=== Event Skipping Warning ===^^Please note that each event cost $real-world USD$ to create. If you don’t wish to participate in an event, PLEASE <<cancel the invitation.<<^^" +
                    "]] Continuing to skip events may temporary prevent you from getting new events.^^Thank you for your understanding, HaPyke +++");
                iSmartPhoneApi.SendSmartphoneMessageFromNPC(args[4], "You dissappeared at the middle of our conversation. Please let me know next time so I don't have to look for you around.");
            }

            return true;
        }

        //
        // ***************************  END OF ENTRY ***************************
        //

        private void OnGameLauched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            this.Monitor.Log("Loading Smartphone.", LogLevel.Trace);

            iSmartPhoneApi = SHelper.ModRegistry.GetApi<ISmartPhoneApi>("d5a1lamdtd.Smartphone");

            if (iSmartPhoneApi == null)
            {
                Monitor.Log("Smartphone mod is not installed. It is not required, but recommended for best experience!", LogLevel.Warn);
                return;
            }
            this.Monitor.Log("Smartphone loaded.", LogLevel.Trace);
        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            string npc_characteristic = Helper.ModContent.GetInternalAssetName("assets/npc_characteristic.json").BaseName;
            NpcCharacteristics = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic);

            string npc_portraits = Helper.ModContent.GetInternalAssetName("assets/npc_portraits.json").BaseName;
            NpcPortrait = Helper.ModContent.Load<Dictionary<string, string>>(npc_portraits);

            string birthdayMapAsset = Helper.ModContent.GetInternalAssetName("assets/birthday_map.json").BaseName;
            birthdayMap = Helper.ModContent.Load<Dictionary<string, BirthdayMapData>>(birthdayMapAsset);

            string npcAgesAsset = Helper.ModContent.GetInternalAssetName("assets/npc_age.json").BaseName;
            npcAges = Helper.ModContent.Load<Dictionary<string, List<string>>>(npcAgesAsset);
            npcToAgeGroup = npcAges
                .SelectMany(kvp => kvp.Value.Select(npc => new { npc, group = kvp.Key }))
                .ToDictionary(x => x.npc, x => x.group);

            HashSet<string> universalHates = new HashSet<string>();
            HashSet<string> universalDislikes = new HashSet<string>();

            foreach (var entry in Game1.NPCGiftTastes)
            {
                if (entry.Key == "Universal_Hate")
                {
                    foreach (string id in entry.Value.Split(' '))
                        universalHates.Add(id);
                }
                else if (entry.Key == "Universal_Dislike")
                {
                    foreach (string id in entry.Value.Split(' '))
                        universalDislikes.Add(id);
                }
            }

            foreach (var pair in Game1.objectData)
            {
                var baseData = pair.Value;
                string baseId = pair.Key;


                if ((baseData.Category == -26 || baseData.Category == -7) && baseData.Price >= 150 && baseData.Price <= 1000
                    && !baseData.Name.Contains("Pickled") && !baseData.Name.Contains("Elixir") && !baseData.Name.Contains("Roe") && !baseData.Name.Contains("Mayonnaise") && !baseData.Name.Contains("Smoked") && !baseData.Name.Contains("Oil")
                    && !universalHates.Contains(baseId) && !universalDislikes.Contains(baseId))
                {
                    var item = new StardewValley.Object(baseId, 1);
                    cookingItems.Add(item);
                }

            }


            foreach (var npc in Utility.getAllCharacters())
            {
                if (!string.IsNullOrEmpty(npc.Birthday_Season) && npc.Birthday_Day > 0)
                {
                    var key = (npc.Birthday_Season, npc.Birthday_Day);
                    if (!NpcBirthdaysByDate.ContainsKey(key))
                        NpcBirthdaysByDate[key] = new List<NPC>();

                    NpcBirthdaysByDate[key].Add(npc);
                }
            }
        }

        public static bool onItemChange(Item i, int position, Item old, StorageContainer container, bool onRemoval)
        {
            if (!onRemoval)
            {
                if (i.Stack > 1 || i.Stack == 1 && old is { Stack: 1 } && i.canStackWith(old))
                {
                    if (old != null && old.canStackWith(i))
                    {
                        container.ItemsToGrabMenu.actualInventory[position].Stack = 1;
                        container.heldItem = old;
                        return false;
                    }

                    if (old != null)
                    {
                        StardewValley.Utility.addItemToInventory(old, position,
                            container.ItemsToGrabMenu.actualInventory);
                        container.heldItem = i;
                        return false;
                    }


                    int allButOne = i.Stack - 1;
                    Item reject = i.getOne();
                    reject.Stack = allButOne;
                    container.heldItem = reject;
                    i.Stack = 1;
                }
            }
            else if (old is { Stack: > 1 })
            {
                if (!old.Equals(i))
                {
                    return false;
                }
            }

            var itemToAdd = onRemoval && (old == null || old.Equals(i)) ? null : i;

            string itemNames = string.Join(", ", container.ItemsToGrabMenu.actualInventory
                .Where(item => item != null) // Filter out null items
                .Select(item => item.DisplayName)); // Get the display name for each item

            birthdayGiftName = itemNames;

            return true;
        }
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (CanTriggerEvent() && iSmartPhoneApi.GetPhoneNpcList() is var phoneNpcList && phoneNpcList != null && phoneNpcList.Count > 5)
            {
                double power = 1.4;
                int maxValue = Math.Min(phoneNpcList.Count, 20);
                if (maxValue < 0)
                    return;

                double rand = Game1.random.NextDouble();
                int result = (int)(Math.Pow(rand, power) * maxValue);
                string npcName = phoneNpcList[result];

                if (Game1.timeOfDay == 1830 && Game1.random.NextDouble() < 0.3)
                {
                    Game1.activeClickableMenu = new ConfirmationDialog(
                        $"{npcName} are inviting you for dinner",
                        onConfirm: (Farmer who) =>
                        {
                            Game1.activeClickableMenu = null;
                            TriggerDinnerEvent(npcName);

                        },
                        onCancel: (Farmer who) =>
                        {
                            Game1.activeClickableMenu = null;
                        }
                    );
                }
                else if (Game1.random.NextDouble() < 0.01)
                {
                    int eventCase = Game1.random.Next(1, 3);
                    switch (eventCase)
                    {
                        case 1:
                            Game1.activeClickableMenu = new ConfirmationDialog(
                                $"{npcName} are inviting you for a picnic",
                                onConfirm: (Farmer who) =>
                                {
                                    Game1.activeClickableMenu = null;
                                    TriggerPicnicEvent(npcName);

                                },
                                onCancel: (Farmer who) =>
                                {
                                    Game1.activeClickableMenu = null;
                                }
                            );
                            break;
                        case 2:
                            Game1.activeClickableMenu = new ConfirmationDialog(
                                $"{npcName} are inviting you for a campfire",
                                onConfirm: (Farmer who) =>
                                {
                                    Game1.activeClickableMenu = null;
                                    TriggerCampingEvent(npcName);

                                },
                                onCancel: (Farmer who) =>
                                {
                                    Game1.activeClickableMenu = null;
                                }
                            );
                            break;
                    }
                }
            }
        }

        public static bool CanTriggerEvent()
        {
            return totalSkippedEvent < 9;
        }

    }

}