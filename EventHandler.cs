using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley;
using Newtonsoft.Json.Linq;
using StardewValley.Extensions;
using System.Text.RegularExpressions;
using StardewModdingAPI;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {


        public static void TriggerDineOutEvent(string npcTarget)
        {
            if (Game1.eventUp || Game1.getCharacterFromName(npcTarget) == null || !CanTriggerEvent())
                return;

            Task.Run(async () =>
            {
                Random random = new Random();
                NPC npc = Game1.getCharacterFromName(npcTarget);
                var event_map = "Saloon";
                GameLocation location = Game1.getLocationFromName(event_map);

                string addReward = "";
                string foodTile = "";
                int heartLevel = 0;
                List<Item> items = null;
                if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
                {
                    items = new List<Item> { heartLevel <= 3 ? GetRandomEatingItemInPriceRange(150, 300) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(200, 600) : GetRandomEatingItemInPriceRange(500, 1000),
                            heartLevel <= 3 ? GetRandomEatingItemInPriceRange(150, 300) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(200, 600) : GetRandomEatingItemInPriceRange(500, 1000) };

                    foodTile = $"/addObject 6 4 {items[0].QualifiedItemId} 1 /addObject 8 4 {items[1].QualifiedItemId} 1";
                    addReward = items[0] != null ? $"/addItem {items[0].QualifiedItemId}" : $"/addItem (O)220";
                }


                var aiRawString = await GenerateDineOutEvent(npcTarget, location.DisplayName, items[0].Name, items[1].Name);
                var conversationJson = ConvertToJToken(aiRawString);

                var conversation = conversationJson.ToObject<SingleConversation>();
                if (conversation != null)
                {
                    var sb = new StringBuilder();

                    if (npcTarget != "Emily" && npcTarget != "Gus" && Game1.random.NextDouble() < 0.25)
                    {
                        sb.Append($"gusviolin/7 8/farmer 5 6 1 {npcTarget} 10 5 2 Gus 11 6 2 Emily 4 11 1/fade/pause 400");
                        sb.Append($"/friendship {npcTarget} 10");
                        sb.Append($"/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper {event_map} 5 6 {npcTarget}\r\n");
                        sb.Append($"/animate Gus false true 723 16 17/pause 23000/stopAnimation Gus/pause 1000/faceDirection Gus 3/speak {npcTarget} \"Thanks, Gus. Your music is amazing.\"/faceDirection {npcTarget} 3/move Gus 0 2 2/doAction 11 9/move Gus 0 3 1/move Gus 12 0 3 true/pause 3000");
                        sb.Append($"/playSound woodyStep/move Emily 1 0 0/move Emily 0 -4 0/move Emily -4 0 0/move Emily 0 -1 0/speak Emily \"I've got a {items[0].DisplayName} for {Game1.player.Name} and a {items[1].DisplayName} for {npcTarget}\"{foodTile}/playSound woodyHit/speak {npcTarget} \"Thank you Emily.\"" +
                            $"/move Emily 4 0 2/move Emily 0 5 1 {npcTarget} 0 1 2/move {npcTarget} -1 0 3/move Emily 11 0 1 true/playMusic musicboxsong");
                    }
                    else
                    {
                        sb.Append($"{conversation.Music}/7 8/farmer 5 6 1 {npcTarget} 9 6 3/fade");
                        sb.Append($"{foodTile} /friendship {npcTarget} 10");
                        sb.Append($"/pause 400/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper {event_map} 5 6 {npcTarget}\r\n");
                    }

                    int lineNum = 1;
                    foreach (var entry in conversation.Dialogue)
                    {
                        string portraitId = entry.Portrait;

                        if (!string.IsNullOrEmpty(portraitId))
                        {
                            Match match = Regex.Match(portraitId, @"\$\d+");

                            if (match.Success)
                            {
                                portraitId = match.Value;
                            }
                            else
                            {
                                if (!portraitId.StartsWith("$"))
                                {
                                    portraitId = "$" + portraitId.Trim();
                                }
                            }
                        }


                        if (lineNum == 1)
                        {
                            if (entry.Type == "D")
                            {
                                sb.Append($"/pause 2000/beginSimultaneousCommand/speak {npcTarget} \"{entry.Dialogue}\"{portraitId}/endSimultaneousCommand");
                                lineNum++;
                            }
                            continue;
                        }
                        if (entry.Type == "D")
                        {
                            sb.Append($"/pause 300/speak {npcTarget} \"{entry.Dialogue}\"{portraitId}");
                            lineNum++;
                        }
                        else if (entry.Type == "Q")
                        {

                            sb.Append($"/speak {npcTarget} \"$y '{entry.Dialogue}");
                            for (int i = 0; i < entry.Player.Count; i++)
                            {
                                var option = entry.Player[i];

                                string repPortraitId = option.Portrait;

                                if (!string.IsNullOrEmpty(repPortraitId))
                                {
                                    Match match = Regex.Match(repPortraitId, @"\$\d+");

                                    if (match.Success)
                                    {
                                        repPortraitId = match.Value;
                                    }
                                    else
                                    {
                                        if (!repPortraitId.StartsWith("$"))
                                        {
                                            repPortraitId = "$" + repPortraitId.Trim();
                                        }
                                    }
                                }

                                sb.Append($"_{option.Response}_{option.Reaction}{repPortraitId}");
                            }
                            sb.Append("'\"");
                        }
                    }

                    sb.Append($"{addReward}/pause 500/end position 5 6");
                    string rawEvent = sb.ToString();



                    QueueGeneratedEvent(new Event(rawEvent.Trim(), Game1.player), event_map, 5, 6, 1);

                }
            });


        }

        public static void TriggerNpcBirthdayEvent(string npcTarget, string? selectedLocation = null, List<string>? selectedParticipantNames = null)
        {
            if (Game1.eventUp || Game1.getCharacterFromName(npcTarget) == null || !CanTriggerEvent())
                return;

            NPC npc = Game1.getCharacterFromName(npcTarget);
            if (!npc.isBirthday())
            {
                Game1.activeClickableMenu = new DialogueBox($"It is not {npcTarget}'s birthday??");
                return;
            }

            var container = new StorageContainer(
                new List<Item> { null },
                1,
                1,
                onItemChange,
                StardewValley.Utility.highlightShippableObjects
            );

            container.AllowExitWithHeldItem = false;
            Game1.activeClickableMenu = container;

            Task.Run(async () =>
            {
                while (Game1.activeClickableMenu != null)
                {
                    await Task.Delay(1000);
                }

                await BirthdayEventTask();
            });

            async Task BirthdayEventTask()
            {
                Random random = new Random();
                BirthdayMapData data;
                if (!string.IsNullOrWhiteSpace(selectedLocation)
                    && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData selectedData)
                    && selectedData != null
                    && Game1.getLocationFromName(selectedData.event_map) != null)
                {
                    data = selectedData;
                }
                else
                {
                    data = GetBirthdayMapDataForNPC(npc);
                }

                var event_map = data.event_map;
                var npc_tiles = data.npc_tiles;
                var gift_tiles = data.gift_tiles;
                var required_npc = data.required_npc;
                GameLocation location = Game1.getLocationFromName(event_map);


                // guest
                List<NPC> allVillagers = Utility.getAllVillagers().ToList();
                int count = Math.Min(npc_tiles.Count, allVillagers.Count);
                List<string> requiredNpcNames = required_npc.ToList();

                var eligibleNPCs = allVillagers
                    .Where(npc => !socialNpcBlacklist.Contains(npc.Name)
                        && !npc.IsInvisible
                        && npc.CanSocialize
                        && Game1.player.friendshipData.ContainsKey(npc.Name)
                        && (int)Game1.player.friendshipData[npc.Name].Points / 250 > 1
                        )
                    .ToList();

                bool hasManualSelection = selectedParticipantNames != null && selectedParticipantNames.Any();
                List<NPC> visitor;
                if (hasManualSelection)
                {
                    HashSet<string> desiredNames = new HashSet<string>(selectedParticipantNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);
                    foreach (string requiredName in requiredNpcNames)
                    {
                        if (!string.IsNullOrWhiteSpace(requiredName))
                            desiredNames.Add(requiredName.Trim());
                    }

                    desiredNames.RemoveWhere(name => string.Equals(name, npcTarget, StringComparison.OrdinalIgnoreCase));
                    visitor = allVillagers
                        .Where(candidate => desiredNames.Contains(candidate.Name))
                        .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .Take(npc_tiles.Count)
                        .ToList();
                }
                else
                {
                    List<NPC> requiredVisitors = eligibleNPCs
                        .Where(candidate => requiredNpcNames.Contains(candidate.Name) && candidate.Name != npcTarget)
                        .ToList();

                    List<NPC> pool = eligibleNPCs
                        .Where(candidate => !requiredVisitors.Contains(candidate) && candidate.Name != npcTarget)
                        .ToList();

                    int remainingSlots = npc_tiles.Count - requiredVisitors.Count;
                    List<NPC> randomVisitors = pool
                        .OrderBy(candidate => random.Next())
                        .Take(remainingSlots)
                        .ToList();

                    visitor = requiredVisitors.Concat(randomVisitors).ToList();
                }


                Shuffle(npc_tiles);
                string guestTile = string.Join(" ", visitor.Zip(npc_tiles, (npc, tile) =>
                    $"{npc.Name} {tile[0]} {tile[1]} {tile[2]}"
                ));


                Shuffle(visitor);
                string guestName = string.Join(", ", visitor.Select(v => v.Name));
                string friendshipReward = string.Join(" ", visitor.Select(npc => $"/friendship {npc.Name} 10"));
                string guestMessage = string.Join(" ",
                    visitor.OrderBy(_ => Guid.NewGuid()).Take(visitor.Count / 3)
                    .Select(npc => Game1.random.NextBool() ? $"/textAboveHead {npc.Name} \"Happy birthday {npcTarget}\"" : $"/textAboveHead {npc.Name} \"Best wish to you {npcTarget}\""));

                // food tile

                var food_tiles = data.food_tiles;
                var foodItems = new List<Item>();

                Item pinkCake = cookingItems.FirstOrDefault(item => item?.Name == "Pink Cake");
                if (pinkCake != null)
                    foodItems.Add(pinkCake);

                var otherItems = cookingItems
                    .Where(item => item?.Name != "Pink Cake")
                    .OrderBy(_ => Game1.random.Next())
                    .Take(food_tiles.Count - 1);

                foodItems.AddRange(otherItems);

                string foodTile = string.Join(" ", food_tiles.Zip(foodItems, (tile, item) =>
                    $"/addObject {tile[0]} {tile[1]} {item.QualifiedItemId} 1"
                ));

                var aiRawString = await GenerateBirthdayEvent(npcTarget, guestName, location.DisplayName, foodItems);
                var conversationJson = ConvertToJToken(aiRawString);

                var conversation = conversationJson.ToObject<MultipleConversation>();
                if (conversation != null)
                {
                    var host_tile = data.host_tile;
                    var player_tile = data.player_tile;
                    var decorable_tiles = data.decorable_tiles;


                    // furniture
                    string furnitureTile = string.Join(" ", decorable_tiles.Select(tile =>
                    {
                        string furnitureId = furnitureItems[Game1.random.Next(furnitureItems.Count)].Trim();
                        string furnitureIdShort = furnitureId.Replace("(F)", "").Trim();
                        var rect = Furniture.GetDefaultSourceRect(furnitureIdShort);

                        int tileHeight = rect.Height / 16;
                        int adjustedY = tile[1] - (tileHeight - 1);

                        return $"/addObject {tile[0]} {adjustedY} {furnitureId} 0";
                    }));

                    // gift
                    string giftTile = string.Join(" ", gift_tiles.Select(tile =>
                    {
                        string randomBox = new List<string> { "384 352 16 16", "400 352 16 16", "416 352 16 16", "432 352 16 16" }[Game1.random.Next(4)];
                        return $"/temporaryAnimatedSprite Maps\\festivals {randomBox} 9999999 1 1 {tile[0]} {tile[1]} false false 0 0 1 0 0 0 hold_last_frame";
                    }));



                    int heartLevel = 0;
                    if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
                    Item randomItem = heartLevel <= 3 ? GetRandomEatingItemInPriceRange(100, 200) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(200, 350) : GetRandomEatingItemInPriceRange(350, 700);
                    string addReward = randomItem != null ? $"/addItem {randomItem.QualifiedItemId}" : $"/addItem (O)220";

                    var sb = new StringBuilder();
                    sb.Append($"{conversation.Music}/{host_tile[0]} {host_tile[1] + 2}/farmer {player_tile[0]} {player_tile[1]} {player_tile[2]} {npcTarget} {host_tile[0]} {host_tile[1]} {host_tile[2]} {guestTile}/fade");
                    sb.Append($"{foodTile} {giftTile} {furnitureTile} {friendshipReward}");
                    sb.Append($"/pause 400/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper {event_map} {player_tile[0]} {player_tile[1]} {npcTarget}\r\n");
                    int lineNum = 1;
                    foreach (var entry in conversation.Dialogue)
                    {
                        string portraitId = entry.Portrait;

                        if (!string.IsNullOrEmpty(portraitId))
                        {
                            Match match = Regex.Match(portraitId, @"\$\d+");

                            if (match.Success)
                            {
                                portraitId = match.Value;
                            }
                            else
                            {
                                if (!portraitId.StartsWith("$"))
                                {
                                    portraitId = "$" + portraitId.Trim();
                                }
                            }
                        }

                        if (lineNum == 1)
                        {
                            if (entry.Type == "D")
                            {
                                sb.Append($"/pause 2000/beginSimultaneousCommand/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}/endSimultaneousCommand");
                                lineNum++;
                            }
                            continue;
                        }

                        sb.Append($"/beginSimultaneousCommand");
                        for (int i = 0; i < Game1.random.Next(0, 2); i++)
                        {
                            sb.Append($"/emote {visitor[Game1.random.Next(visitor.Count)].Name} {new List<string> { "20", "32", "56" }[Game1.random.Next(3)]} ");
                        }
                        sb.Append($"/endSimultaneousCommand");
                        if (entry.Type == "D")
                        {
                            sb.Append($"/pause 300/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}");
                            lineNum++;
                        }
                        else if (entry.Type == "Q")
                        {

                            sb.Append($"/speak {entry.Npc} \"$y '{entry.Dialogue}");
                            for (int i = 0; i < entry.Player.Count; i++)
                            {
                                var option = entry.Player[i];
                                string repPortraitId = option.Portrait;
                                if (!string.IsNullOrEmpty(repPortraitId))
                                {
                                    Match match = Regex.Match(repPortraitId, @"\$\d+");

                                    if (match.Success)
                                    {
                                        repPortraitId = match.Value;
                                    }
                                    else
                                    {
                                        if (!repPortraitId.StartsWith("$"))
                                        {
                                            repPortraitId = "$" + repPortraitId.Trim();
                                        }
                                    }
                                }

                                sb.Append($"_{option.Response}_{option.Reaction}{repPortraitId}");
                            }
                            sb.Append("'\"");
                        }
                    }

                    // Wrap up the event
                    sb.Append($"{addReward}/pause 500/end position {player_tile[0]} {player_tile[1]}");
                    string rawEvent = sb.ToString();

                    QueueGeneratedEvent(new Event(rawEvent.Trim(), Game1.player), event_map, player_tile[0], player_tile[1], player_tile[2]);
                }
            }
            ;


        }

        public static void TriggerPicnicEvent(string npcTarget, string? selectedLocation = null, List<string>? selectedParticipantNames = null)
        {
            if (Game1.eventUp || Game1.getCharacterFromName(npcTarget) == null || !CanTriggerEvent())
                return;

            string event_map = null;

            if (!string.IsNullOrWhiteSpace(selectedLocation)
                && picnicMap.TryGetValue(selectedLocation, out PicnicMapData selectedPicnicData)
                && selectedPicnicData != null
                && Game1.getLocationFromName(selectedLocation) != null)
            {
                event_map = selectedLocation;
            }

            List<string> keys = picnicMap.Keys.ToList();
            while (event_map == null && keys.Count > 0)
            {
                int index = Game1.random.Next(keys.Count);
                string candidate = keys[index];

                if (picnicMap[candidate] != null && Game1.getLocationFromName(candidate) != null)
                    event_map = candidate;
                else
                    keys.RemoveAt(index);

            }

            if (event_map == null)
                return;

            var picnicMapData = picnicMap[event_map];
            var npc_tile = picnicMapData.npc_tile;
            var player_tile = picnicMapData.player_tile;
            var position_tile = picnicMapData.position_tile;
            var decorable_tiles = picnicMapData.decorable_tiles;


            Task.Run(async () =>
            {
                Random random = new Random();
                NPC npc = Game1.getCharacterFromName(npcTarget);
                GameLocation location = Game1.getLocationFromName(event_map);

                string addReward = "";
                string foodTile = "";
                int heartLevel = 0;
                var items = new List<Item> { };
                if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
                {
                    items = new List<Item> { heartLevel <= 3 ? GetRandomEatingItemInPriceRange(0, 300) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(300, 600) : GetRandomEatingItemInPriceRange(500, 1000),
                            heartLevel <= 3 ? GetRandomEatingItemInPriceRange(0, 500) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(200, 600) : GetRandomEatingItemInPriceRange(350, 1000),
                            heartLevel <= 3 ? GetRandomEatingItemInPriceRange(0, 500) : heartLevel <= 6 ? GetRandomEatingItemInPriceRange(200, 600) : GetRandomEatingItemInPriceRange(350, 1000) };

                    foodTile = $"/addObject {position_tile[0]} {position_tile[1] + 2} {(items[0] != null ? items[0].QualifiedItemId : "(O)350")} 1 " +
                    $"/addObject {position_tile[0] + 3} {position_tile[1]} {(items[1] != null ? items[1].QualifiedItemId : "(O)350")} 1" +
                    $"/addObject {position_tile[0] + 2} {position_tile[1] + 1} {(items[2] != null ? items[2].QualifiedItemId : "(O)350")} 1";
                    addReward = items[0] != null ? $"/addItem {items[0].QualifiedItemId}" : $"/addItem (O)350";
                }



                var aiRawString = await GeneratePicnicEvent(npcTarget, location.DisplayName, items);
                var conversationJson = ConvertToJToken(aiRawString);

                var conversation = conversationJson.ToObject<SingleConversation>();
                if (conversation != null)
                {


                    // gift
                    string giftTile = $"/temporaryAnimatedSprite LooseSprites\\Cursors 0 1810 88 56 9999999 1 1 {position_tile[0]} {position_tile[1]} false false 0 0 1 0 0 0 hold_last_frame";

                    // furniture
                    string furnitureTile = string.Join(" ", decorable_tiles.Select(tile =>
                    {
                        string furnitureId = furnitureItems[Game1.random.Next(furnitureItems.Count)].Trim();
                        string furnitureIdShort = furnitureId.Replace("(F)", "").Trim();
                        var rect = Furniture.GetDefaultSourceRect(furnitureIdShort);

                        int tileHeight = rect.Height / 16;
                        int adjustedY = tile[1] - (tileHeight - 1);

                        return $"/addObject {tile[0]} {adjustedY} {furnitureId} 2";
                    }));

                    var sb = new StringBuilder();
                    sb.Append($"{conversation.Music}/{player_tile[0]} {player_tile[1]}/farmer {player_tile[0]} {player_tile[1]} {player_tile[2]} {npcTarget} {npc_tile[0]} {npc_tile[1]} {npc_tile[2]}/fade");
                    sb.Append($"{giftTile} {furnitureTile} {foodTile} /friendship {npcTarget} 20");
                    sb.Append($"/pause 400/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper {event_map} {player_tile[0]} {player_tile[1]} {npcTarget}\r\n");
                    int lineNum = 1;
                    foreach (var entry in conversation.Dialogue)
                    {
                        string portraitId = entry.Portrait;
                        if (!string.IsNullOrEmpty(portraitId))
                        {
                            Match match = Regex.Match(portraitId, @"\$\d+");

                            if (match.Success)
                            {
                                portraitId = match.Value;
                            }
                            else
                            {
                                if (!portraitId.StartsWith("$"))
                                {
                                    portraitId = "$" + portraitId.Trim();
                                }
                            }
                        }


                        if (lineNum == 1)
                        {
                            if (entry.Type == "D")
                            {
                                sb.Append($"/pause 2000/beginSimultaneousCommand/speak {npcTarget} \"{entry.Dialogue}\"{portraitId}/endSimultaneousCommand");
                                lineNum++;
                            }
                            continue;
                        }
                        if (entry.Type == "D")
                        {
                            sb.Append($"/pause 300/speak {npcTarget} \"{entry.Dialogue}\"{portraitId}");
                            lineNum++;
                        }
                        else if (entry.Type == "Q")
                        {

                            sb.Append($"/speak {npcTarget} \"$y '{entry.Dialogue}");
                            for (int i = 0; i < entry.Player.Count; i++)
                            {
                                var option = entry.Player[i];
                                string repPortraitId = option.Portrait;
                                if (!string.IsNullOrEmpty(repPortraitId))
                                {
                                    Match match = Regex.Match(repPortraitId, @"\$\d+");

                                    if (match.Success)
                                    {
                                        repPortraitId = match.Value;
                                    }
                                    else
                                    {
                                        if (!repPortraitId.StartsWith("$"))
                                        {
                                            repPortraitId = "$" + repPortraitId.Trim();
                                        }
                                    }
                                }

                                sb.Append($"_{option.Response}_{option.Reaction}{repPortraitId}");
                            }
                            sb.Append("'\"");
                        }
                    }

                    // Wrap up the event
                    sb.Append($"{addReward}/pause 500/end position {player_tile[0]} {player_tile[1]}");
                    string rawEvent = sb.ToString();

                    QueueGeneratedEvent(new Event(rawEvent.Trim(), Game1.player), event_map, player_tile[0], player_tile[1], 2);

                }
            });


        }

        public static void TriggerCampingEvent(string npcTarget, string? selectedLocation = null, List<string>? selectedParticipantNames = null)
        {
            if (Game1.eventUp || Game1.getCharacterFromName(npcTarget) == null || !CanTriggerEvent())
                return;
            string event_map = null;

            if (!string.IsNullOrWhiteSpace(selectedLocation)
                && campfireMap.TryGetValue(selectedLocation, out CampfireMapData selectedCampfireData)
                && selectedCampfireData != null
                && Game1.getLocationFromName(selectedLocation) != null)
            {
                event_map = selectedLocation;
            }

            List<string> keys = campfireMap.Keys.ToList();
            while (event_map == null && keys.Count > 0)
            {
                int index = Game1.random.Next(keys.Count);
                string candidate = keys[index];

                if (campfireMap[candidate] != null && Game1.getLocationFromName(candidate) != null)
                    event_map = candidate;
                else
                    keys.RemoveAt(index);

            }

            if (event_map == null)
                return;
            var picnicMapData = campfireMap[event_map];
            var player_tile = picnicMapData.player_tile;
            var campfire_tile = picnicMapData.campfire_tile;
            var npc_tiles = picnicMapData.npc_tiles;
            var chair_tiles = picnicMapData.chair_tiles;
            var log_tiles = picnicMapData.log_tiles;
            var decorable_tiles = picnicMapData.decorable_tiles;

            // campfire
            string setupCamp = $"/action d5a1lamdtd.UnlimitedEventExpansion.setupCamp {campfire_tile[0]} {campfire_tile[1]}";
            string endCamp = $"/action d5a1lamdtd.UnlimitedEventExpansion.endCamp {campfire_tile[0]} {campfire_tile[1]}";

            // chair
            string chairTiles = string.Join(" ", chair_tiles.Select(tile =>
            {
                return $"/addObject {tile[0]} {tile[1]} (BC)46 1";
            }));

            // log 
            string logTiles = string.Join(" ", log_tiles.Select(tile =>
            {
                return $"/addLantern 735 {tile[0]} {tile[1]} 1";
            }));

            // npc
            List<NPC> allVillagers = Utility.getAllVillagers().ToList();
            List<NPC> guests;
            bool hasManualSelection = selectedParticipantNames != null && selectedParticipantNames.Any();
            if (hasManualSelection)
            {
                HashSet<string> desiredNames = new HashSet<string>(selectedParticipantNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);
                desiredNames.Add(npcTarget);

                guests = allVillagers
                    .Where(candidate => desiredNames.Contains(candidate.Name))
                    .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Take(npc_tiles.Count)
                    .ToList();
            }
            else
            {
                var eligibleNPCs = allVillagers
                    .Where(candidate =>
                        !socialNpcBlacklist.Contains(candidate.Name) &&
                        !candidate.IsInvisible &&
                        candidate.CanSocialize &&
                        Game1.player.friendshipData.TryGetValue(candidate.Name, out var friendshipData) &&
                        friendshipData.Points >= 250
                    )
                    .ToList();

                NPC requiredGuest = Game1.getCharacterFromName(npcTarget);
                if (requiredGuest is null)
                    return;

                eligibleNPCs.Remove(requiredGuest);

                int maxGuests = npc_tiles.Count;
                int remainingSlots = Math.Max(0, maxGuests - 1);

                List<NPC> randomGuests = eligibleNPCs
                    .OrderBy(_ => Game1.random.Next())
                    .Take(remainingSlots)
                    .ToList();

                guests = new List<NPC> { requiredGuest };
                guests.AddRange(randomGuests);
            }

            Shuffle(npc_tiles);
            string guestTile = string.Join(" ", guests.Zip(npc_tiles, (npc, tile) =>
                $"{npc.Name} {tile[0]} {tile[1]} {tile[2]}"
            ));
            string allNpcName = string.Join(", ", guests.Select(npc => npc.Name));

            // reward
            string reward = string.Join(" ", guests.Select(npc =>
            {
                return $"/friendship {npc.Name} 25";
            }));

            Task.Run(async () =>
            {
                Random random = new Random();
                GameLocation location = Game1.getLocationFromName(event_map);



                var aiRawString = await GenerateCampfireEvent(allNpcName, location.DisplayName);
                var conversationJson = ConvertToJToken(aiRawString);


                var conversation = conversationJson.ToObject<MultipleConversation>();
                if (conversation != null)
                {

                    // furniture
                    string furnitureTile = string.Join(" ", decorable_tiles.Select(tile =>
                    {
                        string furnitureId = furnitureItems[Game1.random.Next(furnitureItems.Count)].Trim();
                        string furnitureIdShort = furnitureId.Replace("(F)", "").Trim();
                        var rect = Furniture.GetDefaultSourceRect(furnitureIdShort);

                        int tileHeight = rect.Height / 16;
                        int adjustedY = tile[1] - (tileHeight - 1);

                        return $"/addObject {tile[0]} {adjustedY} {furnitureId} 2";
                    }));

                    var sb = new StringBuilder();
                    sb.Append($"{conversation.Music}/{player_tile[0]} {player_tile[1]}/farmer {player_tile[0]} {player_tile[1]} {player_tile[2]} {guestTile}/fade"); // music, view, player-npc tiles
                    sb.Append($"{setupCamp} {chairTiles} {logTiles} {furnitureTile}/friendship {npcTarget} 20");
                    sb.Append($"/pause 400/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.campfireSkipWarpper {event_map} {player_tile[0]} {player_tile[1]} {campfire_tile[0]} {campfire_tile[1]} {npcTarget}\r\n");
                    int lineNum = 1;
                    foreach (var entry in conversation.Dialogue)
                    {
                        string portraitId = entry.Portrait;
                        if (!string.IsNullOrEmpty(portraitId))
                        {
                            Match match = Regex.Match(portraitId, @"\$\d+");

                            if (match.Success)
                            {
                                portraitId = match.Value;
                            }
                            else
                            {
                                if (!portraitId.StartsWith("$"))
                                {
                                    portraitId = "$" + portraitId.Trim();
                                }
                            }
                        }


                        if (lineNum == 1)
                        {
                            if (entry.Type == "D")
                            {
                                sb.Append($"/pause 2000/beginSimultaneousCommand/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}/endSimultaneousCommand");
                                lineNum++;
                            }
                            continue;
                        }

                        sb.Append($"/beginSimultaneousCommand");
                        for (int i = 0; i < Game1.random.Next(0, 2); i++)
                        {
                            sb.Append($"/emote {guests[Game1.random.Next(guests.Count)].Name} {new List<string> { "20", "32", "56" }[Game1.random.Next(3)]} ");
                        }
                        sb.Append($"/endSimultaneousCommand");
                        if (entry.Type == "D")
                        {
                            sb.Append($"/pause 300/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}");
                            lineNum++;
                        }
                        else if (entry.Type == "Q")
                        {

                            sb.Append($"/speak {entry.Npc} \"$y '{entry.Dialogue}");
                            for (int i = 0; i < entry.Player.Count; i++)
                            {
                                var option = entry.Player[i];
                                string repPortraitId = option.Portrait;
                                if (!string.IsNullOrEmpty(repPortraitId))
                                {
                                    Match match = Regex.Match(repPortraitId, @"\$\d+");

                                    if (match.Success)
                                    {
                                        repPortraitId = match.Value;
                                    }
                                    else
                                    {
                                        if (!repPortraitId.StartsWith("$"))
                                        {
                                            repPortraitId = "$" + repPortraitId.Trim();
                                        }
                                    }
                                }
                                sb.Append($"_{option.Response}_{option.Reaction}{repPortraitId}");
                            }
                            sb.Append("'\"");
                        }
                    }

                    // Wrap up the event
                    sb.Append($"{reward} {endCamp} /pause 500/end position {player_tile[0]} {player_tile[1]}");
                    string rawEvent = sb.ToString();

                    QueueGeneratedEvent(new Event(rawEvent.Trim(), Game1.player), event_map, player_tile[0], player_tile[1], 2);

                }
            });
        }

        public static void TriggerPlayerBirthdayEvent(string? selectedLocation = null, List<string>? selectedParticipantNames = null)
        {
            if (Game1.eventUp || !CanTriggerEvent())
                return;

            Task.Run(async () =>
            {
                Random random = new Random();
                BirthdayMapData data;
                if (!string.IsNullOrWhiteSpace(selectedLocation)
                    && birthdayMap.TryGetValue(selectedLocation, out BirthdayMapData selectedData)
                    && selectedData != null
                    && Game1.getLocationFromName(selectedData.event_map) != null)
                {
                    data = selectedData;
                }
                else
                {
                    data = GetBirthdayMapDataForNPC(null);
                }

                var event_map = data.event_map;
                var npc_tiles = data.npc_tiles;
                var gift_tiles = data.gift_tiles;
                var required_npc = data.required_npc;
                GameLocation location = Game1.getLocationFromName(event_map);


                // guest
                List<NPC> allVillagers = Utility.getAllVillagers().ToList();
                int count = Math.Min(npc_tiles.Count, allVillagers.Count);
                List<string> requiredNpcNames = required_npc.ToList();

                var eligibleNPCs = allVillagers
                    .Where(npc => !socialNpcBlacklist.Contains(npc.Name)
                        && !npc.IsInvisible
                        && npc.CanSocialize
                        && Game1.player.friendshipData.ContainsKey(npc.Name)
                        && (int)Game1.player.friendshipData[npc.Name].Points / 250 > 1
                        )
                    .ToList();

                bool hasManualSelection = selectedParticipantNames != null && selectedParticipantNames.Any();
                List<NPC> visitor;
                if (hasManualSelection)
                {
                    HashSet<string> desiredNames = new HashSet<string>(selectedParticipantNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);
                    foreach (string requiredName in requiredNpcNames)
                    {
                        if (!string.IsNullOrWhiteSpace(requiredName))
                            desiredNames.Add(requiredName.Trim());
                    }

                    visitor = allVillagers
                        .Where(candidate => desiredNames.Contains(candidate.Name))
                        .GroupBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .Take(npc_tiles.Count)
                        .ToList();
                }
                else
                {
                    List<NPC> requiredVisitors = eligibleNPCs
                        .Where(candidate => requiredNpcNames.Contains(candidate.Name))
                        .ToList();

                    List<NPC> pool = eligibleNPCs
                        .Where(candidate => !requiredVisitors.Contains(candidate))
                        .ToList();

                    int remainingSlots = npc_tiles.Count - requiredVisitors.Count;
                    List<NPC> randomVisitors = pool
                        .OrderBy(candidate => random.Next())
                        .Take(remainingSlots)
                        .ToList();

                    visitor = requiredVisitors.Concat(randomVisitors).ToList();
                }


                Shuffle(npc_tiles);
                string guestTile = string.Join(" ", visitor.Zip(npc_tiles, (npc, tile) =>
                    $"{npc.Name} {tile[0]} {tile[1]} {tile[2]}"
                ));


                Shuffle(visitor);
                string guestName = string.Join(", ", visitor.Select(v => v.Name));
                string friendshipReward = string.Join(" ", visitor.Select(npc => $"/friendship {npc.Name} 20"));
                string guestMessage = string.Join(" ",
                    visitor.OrderBy(_ => Guid.NewGuid()).Take(visitor.Count / 3)
                    .Select(npc => Game1.random.NextBool() ? $"/textAboveHead {npc.Name} \"Happy birthday {Game1.player.Name}\"" : $"/textAboveHead {npc.Name} \"Best wish to you {Game1.player.Name}\""));

                // food tile

                var food_tiles = data.food_tiles;
                var foodItems = new List<Item>();

                Item pinkCake = cookingItems.FirstOrDefault(item => item?.Name == "Pink Cake");
                if (pinkCake != null)
                    foodItems.Add(pinkCake);

                var otherItems = cookingItems
                    .Where(item => item?.Name != "Pink Cake")
                    .OrderBy(_ => Game1.random.Next())
                    .Take(food_tiles.Count - 1);

                foodItems.AddRange(otherItems);

                string foodTile = string.Join(" ", food_tiles.Zip(foodItems, (tile, item) =>
                    $"/addObject {tile[0]} {tile[1]} {item.QualifiedItemId} 1"
                ));


                string addReward = string.Join(" ", food_tiles.Zip(foodItems, (tile, item) =>
                    $"/addItem {item.QualifiedItemId}"
                ));

                var aiRawString = await GeneratePlayerBirthdayEvent(guestName, location.DisplayName, foodItems);
                var conversationJson = ConvertToJToken(aiRawString);

                var conversation = conversationJson.ToObject<MultipleConversation>();
                if (conversation != null)
                {
                    var host_tile = data.host_tile;
                    var player_tile = data.player_tile;
                    var decorable_tiles = data.decorable_tiles;


                    // furniture
                    string furnitureTile = string.Join(" ", decorable_tiles.Select(tile =>
                    {
                        string furnitureId = furnitureItems[Game1.random.Next(furnitureItems.Count)].Trim();
                        string furnitureIdShort = furnitureId.Replace("(F)", "").Trim();
                        var rect = Furniture.GetDefaultSourceRect(furnitureIdShort);

                        int tileHeight = rect.Height / 16;
                        int adjustedY = tile[1] - (tileHeight - 1);

                        return $"/addObject {tile[0]} {adjustedY} {furnitureId} 0";
                    }));

                    // gift
                    string giftTile = string.Join(" ", gift_tiles.Select(tile =>
                    {
                        string randomBox = new List<string> { "384 352 16 16", "400 352 16 16", "416 352 16 16", "432 352 16 16" }[Game1.random.Next(4)];
                        return $"/temporaryAnimatedSprite Maps\\festivals {randomBox} 9999999 1 1 {tile[0]} {tile[1]} false false 0 0 1 0 0 0 hold_last_frame";
                    }));

                    var sb = new StringBuilder();
                    sb.Append($"{conversation.Music}/{host_tile[0]} {host_tile[1] + 2}/farmer {host_tile[0]} {host_tile[1]} {host_tile[2]} {guestTile}/fade");
                    sb.Append($"{foodTile} {giftTile} {furnitureTile} {friendshipReward}");
                    sb.Append($"/pause 400/setSkipActions d5a1lamdtd.UnlimitedEventExpansion.PlayerWarpper {event_map} {player_tile[0]} {player_tile[1]} null\r\n");
                    int lineNum = 1;
                    foreach (var entry in conversation.Dialogue)
                    {
                        string portraitId = entry.Portrait;

                        if (!string.IsNullOrEmpty(portraitId))
                        {
                            Match match = Regex.Match(portraitId, @"\$\d+");

                            if (match.Success)
                            {
                                portraitId = match.Value;
                            }
                            else
                            {
                                if (!portraitId.StartsWith("$"))
                                {
                                    portraitId = "$" + portraitId.Trim();
                                }
                            }
                        }

                        if (lineNum == 1)
                        {
                            if (entry.Type == "D")
                            {
                                sb.Append($"/pause 2000/beginSimultaneousCommand/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}/endSimultaneousCommand");
                                lineNum++;
                            }
                            continue;
                        }

                        sb.Append($"/beginSimultaneousCommand");
                        for (int i = 0; i < Game1.random.Next(0, 2); i++)
                        {
                            sb.Append($"/emote {visitor[Game1.random.Next(visitor.Count)].Name} {new List<string> { "20", "32", "56" }[Game1.random.Next(3)]} ");
                        }
                        sb.Append($"/endSimultaneousCommand");
                        if (entry.Type == "D")
                        {
                            sb.Append($"/pause 300/speak {entry.Npc} \"{entry.Dialogue}\"{portraitId}");
                            lineNum++;
                        }
                        else if (entry.Type == "Q")
                        {

                            sb.Append($"/speak {entry.Npc} \"$y '{entry.Dialogue}");
                            for (int i = 0; i < entry.Player.Count; i++)
                            {
                                var option = entry.Player[i];
                                string repPortraitId = option.Portrait;
                                if (!string.IsNullOrEmpty(repPortraitId))
                                {
                                    Match match = Regex.Match(repPortraitId, @"\$\d+");

                                    if (match.Success)
                                    {
                                        repPortraitId = match.Value;
                                    }
                                    else
                                    {
                                        if (!repPortraitId.StartsWith("$"))
                                        {
                                            repPortraitId = "$" + repPortraitId.Trim();
                                        }
                                    }
                                }

                                sb.Append($"_{option.Response}_{option.Reaction}{repPortraitId}");
                            }
                            sb.Append("'\"");
                        }
                    }

                    // Wrap up the event
                    sb.Append($"{addReward}/pause 500/end position {player_tile[0]} {player_tile[1]}");
                    string rawEvent = sb.ToString();

                    QueueGeneratedEvent(new Event(rawEvent.Trim(), Game1.player), event_map, player_tile[0], player_tile[1], player_tile[2]);
                }
            }
            );
        }
    }
}
