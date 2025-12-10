using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley.Menus;
using StardewValley.Objects;

namespace UnlimitedEventExpansion
{
    public partial class ModEntry
    {

        public static async Task GenerateBirthdayEvent(string npcTarget, string guestName, string locationName, List<Item> foodItems)
        {
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";


            var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a birthday party for NPC {npcTarget} in context of game Stardew Valley. There are player {Game1.player.Name}, NPC {guestName} are attending the party. 
Your task is to generate engaging and naturalistic conversation for the host to exchange with the PLAYER and the guests, and some among the guests as well. Not all guests need to speak, and keep the conversation between 10-15 exchanges. The output need to be a structured JSON format.

Current time is {currentTime}, and party location is {locationName}. Based on this, you will generate a conversation where the tone, emotions, and progression evolve naturally.

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick an appropriate background music code based on the emotional tone and the context of the birthday party.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.
- Be dynamic with the conversation. Make it interesting and unique experiences.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrai ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrai ID here""
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
{string.Join("\n", $"{guestName}, {npcTarget}".Split(',')
    .Select(npc => {
        var name = npc.Trim();
        var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
        return $"{name}: {portrait}";
    }))}


Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting.

";

            var user = @$"{npcTarget} is holding a party at {locationName} to celebrate her birthday. Player {Game1.player.Name}, {guestName} is attending. They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.Name))} to the party, along with many gifts to {npcTarget}.
{(string.IsNullOrEmpty(birthdayGiftName) ? "" : $"Player {Game1.player.Name} gift for {npcTarget} this year is {birthdayGiftName}.")}
You will generate a structured list of dialogue between 10-15 exchanges for them to say during the party.";
            birthdayGiftName = "";
            SMonitor.Log(user, LogLevel.Error);


            var key = k1 + k2 + k3;
            var model = birthdayEventModel;
            string responseMessage = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                        {
                            new { role = "system", content = system},
                            new { role = "user", content = user },
                        },
                    reasoning_effort = "none",
                    verbosity = "low"
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    SMonitor.Log(jsonResponse.ToString(), LogLevel.Error);

                    dynamic json = JsonConvert.DeserializeObject(jsonResponse);
                    responseMessage = json.choices[0].message.content;


                    try
                    {
                        JToken parsed = JToken.Parse(responseMessage);
                        eventString = parsed;
                    }
                    catch (JsonReaderException)
                    {
                        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
                    }

                }
                else
                {
                    // Get the status code
                    var statusCode = (int)httpResponse.StatusCode; // Convert to int for switch
                    string errorMessage = "Check for mod update";
                    switch (statusCode)
                    {
                        case 403:
                            errorMessage = "Country, region, or territory not supported.";
                            break;
                        case 429:
                            errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
                            break;
                        case 500:
                            errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
                            break;
                        case 503:
                            errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
                            break;
                    }

                    SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
                }
            }
        }

        public static async Task GenerateDinnerEvent(string npcTarget, string locationName, string playerDish, string npcDish)
        {
            NPC npc = Game1.getCharacterFromName(npcTarget);
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            int heartLevel = 0;
            if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
            string relation = heartLevel <= 6 ? "friend" : "best friend";
            bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
            bool isMarried = friendship != null && friendship.IsMarried();
            if (isDating) relation = "dating";
            else if (isMarried) relation = "married";

            string summary = "";
            if (npcConversationSummary.ContainsKey(npc.Name))
                summary = $"\nSummary of previous conversation: {npcConversationSummary[npc.Name]}";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";



            var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a dinner event between Player {Game1.player.Name} and NPC {npcTarget} in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for {npcTarget} to exchange with Player during the meal, using a structured JSON format

Current time is {currentTime}, party location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}. Based on this and given context, you will generate a conversation where the tone, emotions, and progression evolve naturally.

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from {npcTarget}
- If it is a monologue (D) or if it is {npcTarget}'s reponse to player's choices, you will pick 1 portrait that best fit with the what {npcTarget} saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick an appropriate background music code based on the emotional tone and the context of the birthday party.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrai ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrai ID here""
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

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses.  For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting.

";

            var user = @$"{npcTarget} and Player {Game1.player.Name} is having dinner together at {locationName}. Player {Game1.player.Name} having {playerDish}, and {npcTarget} having {npcDish}.
You will generate a structured list of dialogue for {npcTarget} to say during the meal. Keep the conversation under 10-12 exchanges.
This is some other context you can use: {summary} {data}";



            var key = k1 + k2 + k3;
            var model = dinnerEventModel;
            string responseMessage = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                        {
                            new { role = "system", content = system},
                            new { role = "user", content = user },
                        },
                    reasoning_effort = "none",
                    verbosity = "low"
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    SMonitor.Log(jsonResponse.ToString(), LogLevel.Error);

                    dynamic json = JsonConvert.DeserializeObject(jsonResponse);
                    responseMessage = json.choices[0].message.content;
                    SMonitor.Log(responseMessage, LogLevel.Error);


                    try
                    {
                        JToken parsed = JToken.Parse(responseMessage);
                        eventString = parsed;
                    }
                    catch (JsonReaderException)
                    {
                        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
                    }

                }
                else
                {
                    // Get the status code
                    SMonitor.Log(httpResponse.ToString(), LogLevel.Error);
                    var statusCode = (int)httpResponse.StatusCode; // Convert to int for switch
                    string errorMessage = "Check for mod update";
                    switch (statusCode)
                    {
                        case 403:
                            errorMessage = "Country, region, or territory not supported.";
                            break;
                        case 429:
                            errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
                            break;
                        case 500:
                            errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
                            break;
                        case 503:
                            errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
                            break;
                    }

                    SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
                }
            }
        }

        public static async Task GeneratePicnicEvent(string npcTarget, string locationName)
        {
            NPC npc = Game1.getCharacterFromName(npcTarget);
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            int heartLevel = 0;
            if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
            string relation = heartLevel <= 6 ? "friend" : "best friend";
            bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
            bool isMarried = friendship != null && friendship.IsMarried();
            if (isDating) relation = "dating";
            else if (isMarried) relation = "married";

            string summary = "";
            if (npcConversationSummary.ContainsKey(npc.Name))
                summary = $"\n{(npcConversationSummary.ContainsKey(npc.Name) ? "Summary of previous conversation: " + npcConversationSummary[npc.Name] : "")}";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";


            var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a picnic event between Player {Game1.player.Name} and NPC {npcTarget} in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for {npcTarget} to exchange with Player during the picnic, using a structured JSON format.

Current time is {currentTime}, picnic location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}. Based on this and given context, you will generate a conversation where the tone, emotions, and progression evolve naturally.

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from {npcTarget}
- If it is a monologue (D) or if it is {npcTarget}'s reponse to player's choices, you will pick 1 portrait that best fit with the what {npcTarget} saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick an appropriate background music code based on the emotional tone and the context of the picnic.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrai ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrai ID here""
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

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting.

";

            var user = @$"{npcTarget} and Player {Game1.player.Name} is going for a picnic together at {locationName}.
You will generate a structured list of dialogue for {npcTarget} to say during the time, limit to under 10 exchanges.
This is some other context you can use: {summary} {data}";


            var key = k1 + k2 + k3;
            var model = picnicEventModel;
            string responseMessage = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                        {
                            new { role = "system", content = system},
                            new { role = "user", content = user },
                        },
                    reasoning_effort = "none",
                    verbosity = "low"
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    SMonitor.Log(jsonResponse.ToString(), LogLevel.Error);

                    dynamic json = JsonConvert.DeserializeObject(jsonResponse);
                    responseMessage = json.choices[0].message.content;


                    try
                    {
                        JToken parsed = JToken.Parse(responseMessage);
                        eventString = parsed;
                    }
                    catch (JsonReaderException)
                    {
                        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
                    }

                }
                else
                {
                    // Get the status code
                    var statusCode = (int)httpResponse.StatusCode; // Convert to int for switch
                    string errorMessage = "Check for mod update";
                    switch (statusCode)
                    {
                        case 403:
                            errorMessage = "Country, region, or territory not supported.";
                            break;
                        case 429:
                            errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
                            break;
                        case 500:
                            errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
                            break;
                        case 503:
                            errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
                            break;
                    }

                    SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
                }
            }
        }

        public static async Task GenerateCampfireEvent(string npcs, string locationName)
        {
            NPC npc = Game1.getCharacterFromName(npcs);
            string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

            string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";


            var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a night campfire event between a group of close friends: Player {Game1.player.Name}, NPC {npcs} in context of game Stardew Valley. 
Your task is to generate engaging and naturalistic dialogue for the NPC to exchange with Player during a campfire night, using a structured JSON format.

Current time is {currentTime}, campfire location is {locationName}. Based on this and given context, you will generate a conversation where the tone, emotions, and progression evolve naturally.

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick an appropriate background music code based on the emotional tone and the context of the picnic.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""Name of the NPC"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrai ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""Name of the NPC"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrai ID here""
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
{string.Join("\n", npcs.Split(',')
    .Select(npc => {
        var name = npc.Trim();
        var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
        return $"{name}: {portrait}";
    }))}

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting. Only return the structured JSON.

";


            var user = @$"NPC {npcs} and Player {Game1.player.Name} is having a campfire night together at {locationName}.
You will generate a structured list of dialogue for them to exchange during the time. Keep the conversation under 10-12 exchanges total.
This is some other context you can use: {data}";


            var key = k1 + k2 + k3;
            var model = campfireEventModel;
            string responseMessage = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                        {
                            new { role = "system", content = system},
                            new { role = "user", content = user },
                        },
                    reasoning_effort = "none",
                    verbosity = "low"
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    SMonitor.Log(jsonResponse.ToString(), LogLevel.Error);

                    dynamic json = JsonConvert.DeserializeObject(jsonResponse);
                    responseMessage = json.choices[0].message.content;


                    try
                    {
                        JToken parsed = JToken.Parse(responseMessage);
                        eventString = parsed;
                    }
                    catch (JsonReaderException)
                    {
                        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
                    }

                }
                else
                {
                    // Get the status code
                    SMonitor.Log(httpResponse.ToString(), LogLevel.Error);
                    var statusCode = (int)httpResponse.StatusCode; // Convert to int for switch
                    string errorMessage = "Check for mod update";
                    switch (statusCode)
                    {
                        case 403:
                            errorMessage = "Country, region, or territory not supported.";
                            break;
                        case 429:
                            errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
                            break;
                        case 500:
                            errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
                            break;
                        case 503:
                            errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
                            break;
                    }

                    SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
                }
            }
        }
    }
}
