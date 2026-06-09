using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {

        // --------
        // Multiple NPC Event
        // --------


        // BIRTHDAY PARTY
        public static async Task<string> GenerateBirthdayEvent(string npcTarget, string guestName, string locationName, List<Item> foodItems)
        {
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";
            string npcCharacteristic = "";

            List<string> guestNames = $"{npcTarget}, {guestName}"
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(npc => npc.Trim())
                .ToList();

            Shuffle(guestNames);
            foreach (var name in guestNames.Take(5))
                npcCharacteristic += GetNpcCharacteristicForPrompt(name, true);


            var system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes about a birthday party for a NPC in context of game Stardew Valley. The PLAYER and some other NPCs are attending the party. 
Your task is to generate engaging and naturalistic conversation for the host to exchange with the PLAYER and the guests, and some among the guests as well. Not all guests need to speak. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and question for the PLAYER (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER, not between NPCs.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the birthday party.
- Keep the tone consistent with the relationship and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Question for the PLAYER."",
      ""player"": [
        {{
          ""response"": ""Player response option for the question."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Must use the id only (for example $0), not the whole portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
      .Select(npc =>
      {
          var name = npc.Trim();
          var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
          return $"{name}: {portrait}";
      }))}

Some context for you:
Current time is {currentTime}, and party location is {locationName}.
NPCs characteristic: {npcCharacteristic}

Style Guidelines:
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting. Stay within the limit.
");

            var user = @$"{npcTarget} is holding a party at {locationName} to celebrate his/her birthday. Player {Game1.player.Name}, {string.Join(", ", guestNames.Skip(1).Take(4))} and more are attending.
      They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.DisplayName))} to the party, along with many gifts for {npcTarget}. {(string.IsNullOrEmpty(birthdayGiftName) ? "" : $"Player {Game1.player.Name} gift for {npcTarget} this year is {birthdayGiftName}.")}";
            birthdayGiftName = "";

            var responseMessage = await RequestAiResponseAsync(system, user);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;
            return string.Empty;
        }



        // CAMPFIRE
        public static async Task<string> GenerateCampfireEvent(string npcs, string locationName)
        {
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";

            string npcCharacteristicMinimal = "";

            List<string> guestNames = $"{npcs}"
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(npc => npc.Trim())
                .ToList();

            Shuffle(guestNames);
            foreach (var name in guestNames.Take(5))
                npcCharacteristicMinimal += GetNpcCharacteristicForPrompt(name, true);


            var system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes about a campfire event between the PLAYER and a group of close friends in context of game Stardew Valley. 
Your task is to generate dialogues for the NPC to exchange with Player around the campfire. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER, not between NPCs.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 3 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up.
- Pick a background music code based on the emotional tone and the context of the campfire event.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Question for the PLAYER."",
      ""player"": [
        {{
          ""response"": ""Player response option for the question."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Use the portrait id (for example $0), not the portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
      .Select(npc =>
      {
          var name = npc.Trim();
          var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
          return $"{name}: {portrait}";
      }))}

Some context for you:
Current time is {currentTime}, campfire location is {locationName}.
NPC characteristic: {npcCharacteristicMinimal}

Style Guidelines:
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not give emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting. Stay within the limit. Only return the structured JSON.
");


            var user = @$"NPC {string.Join(", ", guestNames.Take(5))} and Player {Game1.player.Name} is having a campfire outing together at {locationName}.
This is some other context you can use: {data}";

            var responseMessage = await RequestAiResponseAsync(system, user);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;
            return string.Empty;
        }










        // --------
        // Single NPC Event
        // --------



        // DINE OUT
        public static async Task<string> GenerateDineOutEvent(string npcTarget, string locationName, string playerDish, string npcDish)
        {
            NPC npc = Game1.getCharacterFromName(npcTarget);
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            int heartLevel = 0;
            if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
            string relation = heartLevel <= 2 ? "stranger" : heartLevel <= 4 ? "acquaintance" : heartLevel <= 6 ? "close friend" : "best friend";
            bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
            bool isRoommate = friendship != null && friendship.IsRoommate();
            bool isMarried = friendship != null && friendship.IsMarried();
            bool isEngaged = friendship != null && friendship.IsEngaged();
            bool isDivorced = friendship != null && friendship.IsDivorced();

            if (isDivorced) relation = "divorced";
            else if (isRoommate) relation = "roommate";
            else if (isMarried) relation = "married";
            else if (isEngaged) relation = "engaged";
            else if (isDating) relation = "dating";

            string summary = "";
            if (npcConversationSummary.ContainsKey(npc.Name))
                summary = $"\nSummary of previous conversation: {npcConversationSummary[npc.Name]}";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";


            string npcCharacteristic = GetNpcCharacteristicForPrompt(npcTarget);

            var system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for a dine out event between the PLAYER and an NPC in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for them to exchange during the meal. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER.
- If it is a monologue (D) or if it is the NPC's response to player's choices, you will pick 1 portrait that best fit with the what the NPC is saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the dine out event.
- Keep the tone consistent with the relationship, prior conversation, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. You must use the id only (for example $0), not the whole portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

Some context for you:
Current time is {currentTime}, party location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}.
NPC characteristic: {npcCharacteristic}

Style Guidelines:
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not include emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
- Do not include any icons, comments, or extra formatting.");

            var user = @$"NPC {npcTarget} and Player {Game1.player.Name} is dining out together at {locationName}. Player {Game1.player.Name} having {playerDish}, and {npcTarget} having {npcDish}.
This is some other context you can use: {summary} {data}";


            var responseMessage = await RequestAiResponseAsync(system, user);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;

            return string.Empty;
        }




        // PICNIC
        public static async Task<string> GeneratePicnicEvent(string npcTarget, string locationName, List<Item> foodItems)
        {
            NPC npc = Game1.getCharacterFromName(npcTarget);
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            int heartLevel = 0;
            if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
            string relation = heartLevel <= 2 ? "stranger" : heartLevel <= 4 ? "acquaintance" : heartLevel <= 6 ? "close friend" : "best friend";

            bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
            bool isRoommate = friendship != null && friendship.IsRoommate();
            bool isMarried = friendship != null && friendship.IsMarried();
            bool isEngaged = friendship != null && friendship.IsEngaged();
            bool isDivorced = friendship != null && friendship.IsDivorced();

            if (isDivorced) relation = "divorced";
            else if (isRoommate) relation = "roommate";
            else if (isMarried) relation = "married";
            else if (isEngaged) relation = "engaged";
            else if (isDating) relation = "dating";

            string summary = "";
            if (npcConversationSummary.ContainsKey(npc.Name))
                summary = $"\n{(npcConversationSummary.ContainsKey(npc.Name) ? "Summary of previous conversation: " + npcConversationSummary[npc.Name] : "")}";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";

            string npcCharacteristic = GetNpcCharacteristicForPrompt(npcTarget);

            var system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes about a picnic event between the PLAYER and an NPC in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for them to exchange during the picnic. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER.
- If it is a monologue (D) or if it is the NPC's response to player's choices, you will pick 1 portrait that best fit with the what the NPC is saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the picnic event.
- Keep the tone consistent with the relationship, prior conversation, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Use the id for example $0, not the portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

Some context for you:
Current time is {currentTime}, picnic location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}.
{npcTarget} characteristic: {npcCharacteristic}

Style Guidelines:
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not include emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting.

");

            var user = @$"{npcTarget} and Player {Game1.player.Name} is going for a picnic together at {locationName}. They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.DisplayName))} for the trip.
This is some other context you can use: {summary} {data}";

            var responseMessage = await RequestAiResponseAsync(system, user);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;
            return string.Empty;
        }








        // ------------
        // Player event
        // ------------

        public static async Task<string> GeneratePlayerBirthdayEvent(string guestName, string locationName, List<Item> foodItems)
        {
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";
            string npcCharacteristic = "";

            List<string> guestNames = $"{guestName}"
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(npc => npc.Trim())
                .ToList();

            Shuffle(guestNames);

            foreach (var name in guestNames.Take(5))
                npcCharacteristic += GetNpcCharacteristicForPrompt(name, true);


            var system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes about a birthday party for the player in context of game Stardew Valley. They invited some NPCs who are attending the party.
Your task is to generate engaging and naturalistic conversation for them to exchange. Not all guests need to speak. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and question for the PLAYER (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER, not between NPCs.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the birthday party.
- Keep the tone consistent with the relationship and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Question for the PLAYER."",
      ""player"": [
        {{
          ""response"": ""Player response option for the question."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Must use the id only (for example $0), not the whole portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
      .Select(npc =>
      {
          var name = npc.Trim();
          var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
          return $"{name}: {portrait}";
      }))}

Some context for you:
Current time is {currentTime}, and party location is {locationName}.
NPCs characteristic: {npcCharacteristic}

Style Guidelines:
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting. Stay within the limit.
");

            var user = @$"Player {Game1.player.Name} is holding a party at {locationName} to celebrate his/her birthday. NPCs {string.Join(", ", guestNames.Take(5))} and more are attending.
      They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.DisplayName))} to the party, along with many gifts for the player.";

            var responseMessage = await RequestAiResponseAsync(system, user);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;
            return string.Empty;
        }

    }
}