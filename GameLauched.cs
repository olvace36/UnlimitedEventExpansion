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
using UnlimitedEventExpansion;
using UnlimitedEventExpansion.Data;
using ContentPatcher;
using StardewValley.GameData.Objects;


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
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.OneSecondUpdateTicked += onOneSecondUpdated;


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

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            this.Monitor.Log("Loading Unlimited Event Expansion.", LogLevel.Trace);
            var api = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            ConfigMenu(api, this.ModManifest, Helper);

            iSmartPhoneApi = SHelper.ModRegistry.GetApi<ISmartPhoneApi>("d5a1lamdtd.Smartphone");

            if (iSmartPhoneApi == null)
            {
                Monitor.Log("Smartphone mod is not installed.", LogLevel.Warn);
            }
            else
            {
                this.Monitor.Log("Smartphone loaded.", LogLevel.Trace);
                bool AllowEarlyEvent = !string.IsNullOrWhiteSpace(Config.OpenAIKey) && Config.AllowEarlyEvent;

                iSmartPhoneApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Birthday",
                    triggerEvent: npcName => TriggerNpcBirthdayEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 2,
                    toolDescription: "");

                iSmartPhoneApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Campfire",
                    triggerEvent: npcName => TriggerCampingEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 3,
                    toolDescription: "");

                iSmartPhoneApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Picnic",
                    triggerEvent: npcName => TriggerPicnicEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 4,
                    toolDescription: "");

                iSmartPhoneApi.RegisterUnlimitedEvent(
                    ownerModId: this.ModManifest.UniqueID,
                    eventType: "Dine Out",
                    triggerEvent: npcName => TriggerDineOutEvent(npcName),
                    minimumHeartLevel: AllowEarlyEvent ? 0 : 5,
                    toolDescription: "");
            }
        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {

            string npc_characteristic = Helper.ModContent.GetInternalAssetName("assets/npc_characteristics_short.json").BaseName;
            string npc_characteristic_minimal = Helper.ModContent.GetInternalAssetName("assets/npc_characteristics_minimal.json").BaseName;
            string npc_characteristic_long = Helper.ModContent.GetInternalAssetName("assets/npc_characteristics_long.json").BaseName;

            NpcCharacteristicsShort = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic);
            NpcCharacteristicsMinimal = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic_minimal);
            NpcCharacteristicsLong = Helper.ModContent.Load<Dictionary<string, string>>(npc_characteristic_long);

            string npc_portraits = Helper.ModContent.GetInternalAssetName("assets/npc_portraits.json").BaseName;
            NpcPortrait = Helper.ModContent.Load<Dictionary<string, string>>(npc_portraits);

            string birthdayMapAsset = Helper.ModContent.GetInternalAssetName("assets/birthday_map.json").BaseName;
            birthdayMap = Helper.ModContent.Load<Dictionary<string, BirthdayMapData>>(birthdayMapAsset);

            string picnicMapAsset = Helper.ModContent.GetInternalAssetName("assets/picnic_map.json").BaseName;
            picnicMap = Helper.ModContent.Load<Dictionary<string, PicnicMapData>>(picnicMapAsset);

            string campfireMapAsset = Helper.ModContent.GetInternalAssetName("assets/campfire_map.json").BaseName;
            campfireMap = Helper.ModContent.Load<Dictionary<string, CampfireMapData>>(campfireMapAsset);

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


                if (baseData.Category == -7 && baseData.Price >= 150 && baseData.Price <= 1000
                    && !baseData.Name.Contains("Pickled") && !baseData.Name.Contains("Elixir") && !baseData.Name.Contains("Roe") && !baseData.Name.Contains("Mayonnaise") && !baseData.Name.Contains("Smoked") 
                    && !baseData.Name.Contains("Oil") && !baseData.Name.Contains("Jelly") && !baseData.Name.Contains("Honey") && !baseData.Name.Contains("Wine") && !baseData.Name.Contains("Dried") 
                    && !baseData.Name.Contains("Juice") && !universalHates.Contains(baseId) && !universalDislikes.Contains(baseId))
                {
                    var item = new StardewValley.Object(baseId, 1);
                    cookingItems.Add(item);
                }

            }
        }

        private void onOneSecondUpdated(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            if (Game1.eventUp || Game1.activeClickableMenu != null || !Game1.player.canMove)
                return;

            if (!TryPeekGeneratedEvent(out QueuedEventStart queuedEvent))
                return;

            GameLocation location = Game1.getLocationFromName(queuedEvent.LocationName);
            if (location == null)
            {
                TryDequeueGeneratedEvent(queuedEvent);
                return;
            }

            if (!TryDequeueGeneratedEvent(queuedEvent))
                return;

            Game1.activeClickableMenu = null;
            if (Game1.player.currentLocation != location)
                Game1.warpFarmer(queuedEvent.LocationName, queuedEvent.WarpX, queuedEvent.WarpY, queuedEvent.FacingDirection);
            location.startEvent(queuedEvent.Event);

            Game1.player.completelyStopAnimatingOrDoingAction();
            Game1.player.Halt();
            Game1.player.canMove = false;
            Game1.player.freezePause = 0;
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

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            CheckAIUsage();
            ClearGeneratedEventQueue();
            totalSkippedEvent = 0;
            PendingUnlimitedEvents.Clear();
        }


        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (PendingUnlimitedEvents.Count > 0 && isPlayerFree() && e.NewTime < 2500)
            {
                foreach (var scheduledEvent in PendingUnlimitedEvents.ToList())
                {
                    if (!int.TryParse(scheduledEvent.TimeOfDay, out int eventTime))
                    {
                        PendingUnlimitedEvents.Remove(scheduledEvent);
                        continue;
                    }

                    if (e.NewTime >= eventTime)
                    {
                        string eventType = scheduledEvent.EventType;
                        string npcName = scheduledEvent.NpcName;

                        switch (eventType.ToLower())
                        {
                            case "birthday":
                                TriggerNpcBirthdayEvent(npcName);
                                break;
                            case "campfire":
                                TriggerCampingEvent(npcName);
                                break;
                            case "picnic":
                                TriggerPicnicEvent(npcName);
                                break;
                            case "dine out":
                                TriggerDineOutEvent(npcName);
                                break;
                        }

                        PendingUnlimitedEvents.Remove(scheduledEvent);
                        break;
                    }
                }
            }

            return;
            if (CanTriggerEvent() && iSmartPhoneApi.GetPhoneNpcList() is var phoneNpcList && phoneNpcList != null && phoneNpcList.Count > 5)
            {
                double power = 1.4;
                int maxValue = Math.Min(phoneNpcList.Count, 20);
                if (maxValue < 0)
                    return;

                double rand = Game1.random.NextDouble();
                int result = (int)(Math.Pow(rand, power) * maxValue);
                string npcName = phoneNpcList[result];

                if (Game1.timeOfDay == 1830 && Game1.random.NextDouble() < 0.2)
                {
                    Game1.activeClickableMenu = new ConfirmationDialog(
                        $"{npcName} are inviting you for dinner",
                        onConfirm: (Farmer who) =>
                        {
                            Game1.activeClickableMenu = null;
                            TriggerDineOutEvent(npcName);

                        },
                        onCancel: (Farmer who) =>
                        {
                            Game1.activeClickableMenu = null;
                        }
                    );
                }
                else if (Game1.random.NextDouble() < 0.01 && !Game1.currentLocation.IsRainingHere() && !Game1.currentLocation.IsGreenRainingHere() && !Game1.currentLocation.IsLightningHere())
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
            return totalSkippedEvent < 3;
        }

        internal static void CheckAIUsage()
        {
            if (!string.IsNullOrWhiteSpace(Config.OpenAIKey))
            {
                EventModel = Config.OpenAIModel;
                EventKey = Config.OpenAIKey;

                switch (EventModel)
                {
                    case ModConfig.OpenAIModel_51:
                        EventReasoning = new { effort = "none" };
                        break;
                    case ModConfig.OpenAIModel_5mini:
                        EventReasoning = new { effort = "minimal" };
                        break;
                    case ModConfig.OpenAIModel_5nano:
                        EventReasoning = new { effort = "minimal" };
                        break;
                    case ModConfig.OpenAIModel_54mini:
                        EventReasoning = new { effort = "none" };
                        break;
                    case ModConfig.OpenAIModel_54nano:
                        EventReasoning = new { effort = "none" };
                        break;
                    default:
                        EventReasoning = new { effort = "minimal" };
                        break;
                }
                return;
            }
            else
            {
                // set 1
                string k1 = "sk-proj-EcsOH35lsXluhKPQfghxgRprEWtuKSJZULD6uWNTkV8";
                string k2 = "_C1UugAKmxITkeJoWGiLs-oPqwVDEZqT3BlbkFJqk_DVafGmHLfHCja253VsxdI";
                string k3 = "-m0NsMFDcewMAzEfuYy-F8_x0GzYj5teeVTFSJl9PfkdCTk4wA";

                string xk1 = "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";
                string xk2 = "";
                string xk3 = "";

                // set 2
                string k11 = "sk-proj-EcsOH35lsXluhKPQfghxgRprEWtuKSJZULD6uWNTkV8";
                string k21 = "_C1UugAKmxITkeJoWGiLs-oPqwVDEZqT3BlbkFJqk_DVafGmHLfHCja253VsxdI";
                string k31 = "-m0NsMFDcewMAzEfuYy-F8_x0GzYj5teeVTFSJl9PfkdCTk4wA";

                string xk11 = "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";
                string xk21 = "";
                string xk31 = "";


                Task.Run(async () =>
                {
                    var (premium, regular) = await GetOpenAIUsage(xk1 + xk2 + xk3);
                    if (premium == -1 || premium > 1000000)
                    {
                        switched = true;
                    }
                    else if (premium < 1000000)
                    {
                        switched = false;
                        EventKey = k1 + k2 + k3;
                        EventModel = "gpt-5.1";
                        EventReasoning = new { effort = "none" };
                    }
                });

                if (switched)
                {
                    Task.Run(async () =>
                    {
                        var (premium, regular) = await GetOpenAIUsage(xk11 + xk21 + xk31);
                        if (premium == -1 || regular == -1)
                        {
                            totalFailedCheck += 1;
                            if (totalFailedCheck >= 3)
                            {
                                EventKey = "";
                                IsMaxedLimit = true;
                                iSmartPhoneApi.SendSmartphoneNotification("=== Unlimited Event Expansion ===^^Failed to check AI usage for 3 times in a row, AI usage is temporarily disabled.^^Please check mod page for support. HaPyke!", "Unlimited Event Expansion");
                                return;
                            }
                        }

                        // case handler
                        IsMaxedLimit = false;
                        if (regular > 4500000 && premium > 250000)
                        {
                            EventKey = "";
                            IsMaxedLimit = true;
                            iSmartPhoneApi.SendSmartphoneNotification("=== Unlimited Event Expansion ===^^Total AI usage reached its limit and is temporarily disabled.^^This will be reset the next day in timezone UTC+0. HaPyke!", "Unlimited Event Expansion");
                            return;
                        }

                        EventKey = k11 + k21 + k31;
                        if (premium > 250000)
                        {
                            EventModel = "gpt-5.4-mini";
                            EventReasoning = new { effort = "none" };
                        }
                        else
                        {
                            EventModel = "gpt-5.1";
                            EventReasoning = new { effort = "none" };
                        }

                        // maxed premium
                        if (regular > 3000000)
                        {
                            EventModel = "gpt-5-nano";
                            EventReasoning = new { effort = "minimal" };
                        }
                        else if (regular > 2500000)
                        {
                            EventModel = "gpt-5-mini";
                            EventReasoning = new { effort = "minimal" };
                        }
                        else if (premium > 250000 && regular < 2500000)
                        {
                            EventModel = "gpt-5.4-mini";
                            EventReasoning = new { effort = "none" };
                        }
                    });
                }
            }
        }

        private bool isPlayerFree()
        {
            return Game1.timeOfDay > 600
            && Game1.player.CanMove
            && !(Game1.player.isRidingHorse()
                || Game1.currentLocation == null
                || Game1.eventUp
                || Game1.isFestival()
                || Game1.IsFading()
                || Game1.activeClickableMenu != null
                || Game1.dialogueUp
                || Game1.player.UsingTool);
        }
    }

}