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

            if (Config.NpcProfileTheme == "you_are_the_king")
                system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for a dark royal fantasy reimagining of the world. The event is the **Born-Day Feast** (birthday party) of a specific NPC courtier. The PLAYER is the absolute Majesty (King or Queen) of the realm, attending the feast alongside other courtly guests who are bound to the Crown. 

Your task is to generate high-impact, immersive courtly dialogue for the host courtier to exchange with their Majesty and the other guests. Output a structured JSON format. {GetPreferedEventLength()}

**Monarch Profile:**
- Title: {(Game1.player.IsMale ? "King" : "Queen")}
- Identity: {Game1.player.Name}

**Your Objectives:**
- **The Scale of Feudal Devotion:** Every NPC begins as a literal nobody—a peasant, nervous recruit, or humble apprentice begging for patronage. Your dialogue must strictly reflect the courtier's progression based on their relationship status.
- **High-Impact Fantasy Vocabulary:** Courtiers must speak using condensed, archaic, and evocative feudal terminology (e.g., *Sovereign, My Liege, Your Grace, Born-Day, Fealty, Patronage, Ascension, Vassal, Decree*). Completely eliminate casual, modern, or overly familiar phrasing.
- **Structure:** Use a mix of monologue (D) and questions for the Monarch (Q), but mostly focus on monologues (D). Question (Q) must only be used when a courtier is directly addressing or petitioning their Sovereign, never between NPCs. 
- **Dark Courtly Atmosphere:** Occasionally, include details with grim, dynamic dark fantasy elements happening.
- **Pacing:** The scene must include a formal opening setup as your Majesty graces the feast hall, a political or emotional build-up as tributes are acknowledged, and a formal wrap-up/dismissal to signal the end of the royal audience.

**Formatting Rules:**
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Courtier speaks here with intense reverence, fierce feudal pride, or dark romantic passion."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Host presents a direct question, oath, or Born-Day petition to the Monarch."",
      ""player"": [
        {{
          ""response"": ""The Sovereign's choice or absolute decree."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

**Background Music Options:**
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

**Portrait Options:**
Use one of the following as the ""portrait"" field. Must use the id only (for example $0), not the whole portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
     .Select(npc =>
     {
         var name = npc.Trim();
         var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
         return $"{name}: {portrait}";
     }))}

**Some context for your design:**
- Current Time: {currentTime}
- Feast Hall Location: {locationName}
- Host Characteristics & Court Station: {npcCharacteristic}

**Style Guidelines:**
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue must consist of raw sentences showing how the courtier speaks. Do not include explicit action or emotional explanations (e.g., do *not* write *kneels deeply* or *trembles*), and never use the character '\'. Let the heavy weight of the vocabulary convey their stance.
 - Questions should open opportunities for meaningful roleplay, terrifying authority, or deep political/romantic escalation. Give the Player options to be a benevolent protector or an absolute, iron-fisted ruler.
 - Do not include any icons, comments, or extra formatting. Stay strictly within the structural limits.
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

            if (Config.NpcProfileTheme == "you_are_the_king")
                system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for a campfire resting event between the absolute Majesty (the PLAYER) and a circle of trusted courtiers, high wardens, or military vassals in a dark fantasy realm. Your role is to generate high-impact, atmospheric dialogue for them to exchange around the crackling embers. Output a structured JSON format. {GetPreferedEventLength()}

**Monarch Profile:**
- Title: {(Game1.player.IsMale ? "King" : "Queen")}
- Identity: {Game1.player.Name}

**Your Objectives:**
- **Feudal Progression & Tone:** Speak with absolute reverence, deference, or fierce feudal loyalty. The courtiers' attitudes must strictly reflect their current relationship status and progression paths. Low-relationship NPCs speak with intimidated awe, eager to prove their worth under the dark sky. High-relationship NPCs speak with fierce protective pride, sharing heavy strategic insights or intense, quiet devotion by the firelight.
- **High-Impact Vocabulary:** Use condensed, evocative fantasy vocabulary. Avoid any modern, casual, or overly familiar friendship phrasing.
- **Structure:** Use a mix of monologue (D) and player choice moments (Q), but mostly focus on monologues (D) from the courtiers. Question (Q) must only be used when a courtier is directly petitioning or addressing their Sovereign, never between NPCs.
- **Atmosphere:** Create a welcoming, friendly while trusted, loyal and respectful conversation.
- **Pacing:** Ensure the scene includes a setup as the circle gathers around the fire, a deeper emotional or strategic build-up, and a respectful wrap-up as the embers fade to signal the end of the event.

**Formatting Rules:**
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Courtier speaks here using high-impact fantasy vocabulary and deep reverence."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Courtier presents a question, midnight vow, or strategic petition to the Monarch."",
      ""player"": [
        {{
          ""response"": ""The Majesty's royal decree or choice."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

**Background Music Options:**
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

**Portrait Options:**
Use one of the following as the ""portrait"" field. Use the portrait id (for example $0), not the portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
                      .Select(npc =>
                      {
                          var name = npc.Trim();
                          var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
                          return $"{name}: {portrait}";
                      }))}

**Some context for your design:**
- Current Time: {currentTime}
- Encampment Location: {locationName}
- Courtier Characteristics & Stations: {npcCharacteristicMinimal}

**Style Guidelines:**
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue must consist of raw sentences showing how the courtier speaks. Do not include explicit action or emotional descriptions (e.g., do *not* write *stirs the coals*), and never use the character '\'. Let the weight of the dark fantasy prose convey their stance.
 - Questions should open opportunities for meaningful roleplay, terrifying authority, or deep political/romantic escalation. Give the Player options to rule with a benevolent hand or an iron fist.
 - Do not include any icons, comments, or extra formatting. Stay strictly within the structural limits. Only return the structured JSON.
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

            if (Config.NpcProfileTheme == "you_are_the_king")
                system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for a private banquet or intimate courtly audience over a meal between the absolute Majesty (the PLAYER) and a courtier in a dark fantasy realm. Your role is to generate high-impact, atmospheric dialogue for them to exchange. Output a structured JSON format. {GetPreferedEventLength()}

**Monarch Profile:**
- Title: {(Game1.player.IsMale ? "King" : "Queen")}
- Identity: {Game1.player.Name}

**Your Objectives:**
- **Feudal Progression & Tone:** Speak with absolute reverence, deference, or fierce feudal loyalty. The courtier's attitude must strictly reflect their current relationship status and character development progress. If relationship is low, they speak as an intimidated peasant, a nervous recruit, or a humble apprentice begging for patronage. If relationship is high, they speak as a proud warden, master alchemist, or a fiercely protective, passionate rising Royal Consort.
- **High-Impact Vocabulary:** Use condensed, evocative fantasy and courtly vocabulary. Avoid modern phrasings entirely.
- **Structure:** Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) must only be used when the courtier is directly petitioning or addressing their Monarch.
- **Courtly Atmosphere:** Weave in sharp, dynamic dark fantasy details happening in the feast hall or keep to give the interaction weight. 
- **Pacing:** Ensure the scene includes a formal setup/arrival, emotional or political build-up, and a formal wrap-up/dismissal to signal the end of the royal interaction.

**Formatting Rules:**
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""Courtier speaks here using high-impact fantasy vocabulary and deep reverence."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""Courtier presents a formal question, vow, or petition to the Monarch."",
      ""player"": [
        {{
          ""response"": ""The Majesty's royal decree or choice."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

**Background Music Options:**
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

**Portrait Options:**
Use one of the following as the ""portrait"" field. You must use the id only (for example $0), not the whole portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

**Realm Context:**
- Current Time: {currentTime}
- Audience Location: {locationName}
- Courtier Profile & Personality: {npcCharacteristic}
- Current NPC development progress/relationship with the Monarch: {relation}

**Style Guidelines:**
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue must consist of raw sentences showing how the courtier speaks. Do not include explicit action or emotional descriptions (e.g., do *not* write *bows low* or *with a trembling voice*), and never use the character '\'. Let the weight of the vocabulary convey the emotion.
 - Questions should open opportunities for meaningful roleplay, terrifying authority, or deep political/romantic escalation. Give the Player options to rule with a benevolent hand or an iron fist.
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

            if (Config.NpcProfileTheme == "you_are_the_king")
                system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for an outdoor excursion, a retreat in the imperial gardens, or a brief respite beyond the castle walls between the absolute Majesty (the PLAYER) and a courtier in a dark fantasy realm. Your role is to generate high-impact, atmospheric dialogue for them to exchange during this rare moment away from the throne room. Output a structured JSON format. {GetPreferedEventLength()}

**Monarch Profile:**
- Title: {(Game1.player.IsMale ? "King" : "Queen")}
- Identity: {Game1.player.Name}

**Your Objectives:**
- **Feudal Progression & Tone:** Speak with absolute reverence, deference, or fierce feudal loyalty. The courtier's attitude must strictly reflect their current relationship status and character development progress. A low-relationship NPC acts as an intimidated peasant or cautious recruit overwhelmed to be in your presence outside the heavy safety of the keep. A high-relationship NPC speaks as a trusted warden, master alchemist, or a fiercely protective, passionate rising Royal Consort sharing a rare personal moment under the open sky.
- **High-Impact Vocabulary:** Use condensed, evocative fantasy and courtly vocabulary. Avoid any modern casual phrasing.
- **Structure:** Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) must only be used when the courtier is directly petitioning or addressing their Monarch.
- **Dark Fantasy Respite Atmosphere:** Instead of mundane picnic details, weave in sharp, dynamic dark fantasy or wild details appropriate to an outdoor audience (e.g., the scent of crushed mountain briars, watchtowers guarding the border on the horizon, checking the perimeter for hidden assassins, pouring wine from an iron flask, or the shadow of the fortress towering over the clearing). 
- **Pacing:** Ensure the scene includes a formal setup/arrival at the clearing or outlook, emotional or political build-up as they speak in the open air, and a formal wrap-up to signal the return to your royal duties.

**Formatting Rules:**
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""Courtier speaks here using high-impact fantasy vocabulary and deep reverence."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""Courtier presents a question, vow, or field petition to the Monarch."",
      ""player"": [
        {{
          ""response"": ""The Majesty's royal decree or choice."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

**Background Music Options:**
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

**Portrait Options:**
Use one of the following as the ""portrait"" field. You must use the id only (for example $0), not the whole portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

**Realm Context:**
- Current Time: {currentTime}
- Excursion Location: {locationName}
- Courtier Profile & Personality: {npcCharacteristic}
- Current NPC development progress/relationship with the Monarch: {relation}

**Style Guidelines:**
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue must consist of raw sentences showing how the courtier speaks. Do not include explicit action or emotional descriptions (e.g., do *not* write *kneels on the grass*), and never use the character '\'. Let the heavy weight of the vocabulary convey their stance.
 - Questions should open opportunities for meaningful roleplay, terrifying authority, or deep political/romantic escalation. Give the Player options to rule with a benevolent hand or an iron fist.
 - Do not include any icons, comments, or extra formatting. Stay strictly within the structural limits.
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

            if (Config.NpcProfileTheme == "you_are_the_king")
                system = AppendLanguageInstruction(@$"
You are a narrative design assistant specializing in creating dialogue scenes for the grand Imperial Name-Day Feast or Day of Tribute honoring the absolute Majesty (the PLAYER) in a dark fantasy realm. Selected courtiers, vassals, and recruits have gathered in the great keep to present offerings and swear oaths of fealty. Not all attending courtiers need to speak. Output a structured JSON format. {GetPreferedEventLength()}

**Monarch Profile:**
- Title: {(Game1.player.IsMale ? "King" : "Queen")}
- Identity: {Game1.player.Name}

**Your Objectives:**
- **Feudal Progression & Tone:** Every courtier must speak with absolute reverence, awe, or fierce feudal loyalty. Their attitude must strictly reflect their current relationship status and development path.
- **High-Impact Vocabulary:** Use condensed, evocative fantasy and courtly vocabulary (e.g., *Sovereign, Liege, Name-Day, Tribute, Fealty, Crown, Vassal, Patronage, Ascension*). Eliminate all modern or casual phrasings.
- **Structure:** Use a mix of monologue (D) and questions for the Monarch (Q), but mostly focus on monologues (D) from the courtiers. Question (Q) must only be used when a courtier is directly addressing or offering a choice to their Sovereign, never between NPCs.
- **Imperial Feast Atmosphere:** Instead of mundane birthday details, weave in sharp, dynamic dark fantasy elements appropriate to a royal tribute hall.
- **Pacing:** Ensure the scene includes a formal opening setup as courtiers approach the throne or high table, political or emotional build-up as tributes are offered, and a formal wrap-up acknowledging the enduring might of your reign.

**Formatting Rules:**
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Courtier speaks here using high-impact fantasy vocabulary to praise or address the Sovereign."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Courtier presents a tribute choice, petition, or vow directly to the Monarch."",
      ""player"": [
        {{
          ""response"": ""The Majesty's royal decree or choice."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

**Background Music Options:**
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

**Portrait Options:**
Use one of the following as the ""portrait"" field. Must use the id only (for example $0), not the whole portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
                     .Select(npc =>
                     {
                         var name = npc.Trim();
                         var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
                         return $"{name}: {portrait}";
                     }))}

**Some context for your design:**
- Current Time: {currentTime}
- Feast Location: {locationName}
- Courtier Characteristics & Stations: {npcCharacteristic}

**Style Guidelines:**
 - NPC name must be exactly as provided, including spaces, unique identifiers, symbols, etc.
 - Dialogue must consist of raw sentences showing how the courtier speaks. Do not include explicit action or emotional descriptions (e.g., do *not* write *kneels at the dais* or *smiles warmly*), and never use the character '\'. Let the weight of the vocabulary convey the performance.
 - Questions should open opportunities for meaningful roleplay, terrifying authority, or deep political/romantic escalation. Give the Player options to be a benevolent protector or an iron-fisted ruler.
 - Do not include any icons, comments, or extra formatting. Stay strictly within the structural limits.
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